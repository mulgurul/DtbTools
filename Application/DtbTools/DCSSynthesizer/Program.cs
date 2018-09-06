using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DCSArchiveClientApi;
using DCSArchiveLibrary.Model;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using Mono.Options;
using Directory = System.IO.Directory;

namespace DCSSynthesizer
{
    class Program
    {
        private static string sourceCode = null;
        private static string destCode = null;
        private static string sourceTitleNumber = null;
        private static string destTitleNumber = null;
        private static int year = DateTime.Now.Year;
        private static int? number;
        private static bool useWeekNumber = false;
        private static int bitrate = 48;
        private static string uncRoot = @"\\smb-files.ngt.dbb.dk\Temp\DCSSynthesizer";
        private static string dcsServiceUri = "http://http-dcsarchive.ngt.dbb.dk";
        private static string defaultCreator = "Nota";
        private static DCSArchiveClientApi.ClientApi clientApi = null;

        private static string destPath => Path.Combine(uncRoot, destTitleNumber);


        private static string GetTitleNumber(string c, int y, int n)
        {
            if (String.IsNullOrEmpty(c))
            {
                return null;
            }
            if (Regex.IsMatch(c, @"^UNI\d$", RegexOptions.IgnoreCase))
            {
                return (Int32.Parse(c.Substring(4)) + n).ToString();
            }
            if (Regex.IsMatch(c, @"^UN\d\d$", RegexOptions.IgnoreCase))
            {
                return (Int32.Parse(c.Substring(3)) + n).ToString();
            }
            return $"{c}{y:D4}{n:D4}";
        }

        private static int GetIsoWeekNumber(DateTime date)
        {
            if (new[] {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday}.Contains(date.DayOfWeek))
            {
                date = date.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, 
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }

        private static readonly OptionSet Options = new OptionSet()
            .Add("Synthesize DTB from DCS")
            .Add("sourcecode=", "Source code", s => sourceCode = s)
            .Add("destcode=", "Destination code", s => destCode = s)
            .Add("sourcetitleno=", "Source title number", s => sourceTitleNumber = s)
            .Add("desttitleno=", "Destination title number", s => destTitleNumber = s)
            .Add("uncroot=", "UNC path to temporarily store generated DTB", s => uncRoot = s)
            .Add("dcsuri=", "DCS Archive Service Base Uri", s => dcsServiceUri = s)
            .Add<int>("year=", "Year", i => year = i)
            .Add<int>("number=", "Number", i => number = i)
            .Add<int>("bitrate=", "BitRate (default is 48)", i => bitrate = i)
            .Add("useweek", "Use week number for default number (alternatively mmdd is used)", s => useWeekNumber = s != null);
        private static string OptionDescriptions
        {
            get
            {
                var wr = new StringWriter();
                Options.WriteOptionDescriptions(wr);
                return wr.GetStringBuilder().ToString();
            }
        }
        static int Main(string[] args)
        {
            try
            {
                var unhandledArgs = Options.Parse(args);
                if (unhandledArgs.Any())
                {
                    throw new OptionException($"Unhandled arguments {unhandledArgs.Aggregate((s, v) => $"{s} {v}")}", "");
                }
                if (!number.HasValue)
                {
                    number = useWeekNumber ? 100 * DateTime.Now.Month + DateTime.Now.Day : GetIsoWeekNumber(DateTime.Now);
                }
                if (String.IsNullOrWhiteSpace(sourceTitleNumber))
                {
                    sourceTitleNumber = GetTitleNumber(sourceCode, year, number.Value);
                }
                if (String.IsNullOrWhiteSpace(destTitleNumber))
                {
                    destTitleNumber = GetTitleNumber(destCode, year, number.Value);
                }
                if (String.IsNullOrEmpty(sourceTitleNumber))
                {
                    throw new OptionException("No DCS source was given", "");
                }
                if (String.IsNullOrEmpty(destTitleNumber))
                {
                    throw new OptionException("No DCS destination was given", "");
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{e.Message}\n{OptionDescriptions}");
                return -1;
            }
            try
            {
                Console.WriteLine($"Synthesizing {sourceTitleNumber} to {destTitleNumber}");
                clientApi = new ClientApi {BaseAddress = dcsServiceUri};
                try
                {
                    if (!clientApi.Ping().Wait(10000))//Will throw an exception, if service is not accessible
                    {
                        Console.WriteLine($"Timeout while pinging DCS Archive api {dcsServiceUri}");
                        return -1;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not connect to DCS Archive api {dcsServiceUri} due to an {e.GetType()}: {e.Message}");
                    return -1;
                }
                var task = Execute();
                task.Wait();
                if (task.Result != 0)
                {
                    return task.Result;
                }
                Console.WriteLine($"Succesfully synthesized {sourceTitleNumber} to {destTitleNumber} in DCS Archive");
                return 0;
            }
            catch (AggregateException e)
            {
                Console.WriteLine($"An unexpected {e.GetType()} occured with the following inner exceptions:");
                foreach (var ie in e.InnerExceptions)
                {
                    Console.WriteLine($"{ie.GetType()}: {ie.Message}");
                }
                return -1000;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected {e.GetType()} occured: {e.Message}\nStack Trace:\n{e.StackTrace}");
                return -1000;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(destPath))
                    {
                        Directory.Delete(destPath, true);
                        Console.WriteLine($"Cleaned up destination folder {destPath}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not cleanup destination folder {destPath} due to an unexpected {e.GetType()}: {e.Message}");
                }
            }
        }

        private static async Task<int> DownloadSource()
        {
            var title = await clientApi.GetTitle(sourceTitleNumber);
            if (title == null)
            {
                Console.WriteLine($"Could not find source title {sourceTitleNumber} in DCS Archive");
                return -1;
            }
            if (title.MaterialType.Code != "ETXT")
            {
                Console.WriteLine($"Source title has Material Type Code {title.MaterialType.Code}, expected ETXT");
                return -1;
            }
            if (!title.Items.ContainsKey("DTB"))
            {
                Console.WriteLine($"Source title {sourceTitleNumber} has no DTB (Dtbook xml) item");
                return -1;
            }
            var dir = title.DirectoriesAsList.First();
            if (!Directory.Exists(dir.WebFullPath))
            {
                Console.WriteLine($"Could not access source title at {dir.WebFullPath}");
                return -1;
            }
            CopyDirectory(dir.WebFullPath, destPath);
            Console.WriteLine("Downloaded source dtbook from DCS Archive");
            return 0;
        }

        private static async Task<int> UploadDestination()
        {
            var ncc = XDocument.Load(Path.Combine(destPath, "ncc.html"));
            var titleUpdate = new DCSArchiveLibrary.Model.TitleUpdate
            {
                SourcePath = destPath,
                OriginCode = "DDS",
                MaterialTypeCode = "DTB",
                MaterialFormatCode = "D202",
                TitleNo = destTitleNumber,
                Title = Utils.GetMetaContent(ncc, "dc:title"),
                Creator = Utils.GetMetaContent(ncc, "dc:creator") ?? defaultCreator,
                MetadataFromDBBDokSys = false
            };

            var existingTitle = await clientApi.GetTitle(destTitleNumber);
            if (existingTitle != null)
            {
                titleUpdate.TitleID = existingTitle.ID;
                await clientApi.UpdateTitleFormat(titleUpdate);
            }
            else
            {
                await clientApi.CreateTitle(titleUpdate);
            }
            Console.WriteLine("Uploaded synthesized DTB to DCSArchive");
            return 0;
        }

        private static int Synthesize()
        {
            var sourceFileName = Path.Combine(destPath, $"{sourceTitleNumber}.xml");
            if (!File.Exists(sourceFileName))
            {
                Console.WriteLine($"Could not find source file {sourceFileName}");
                return -1;
            }
            XDocument dtbook;
            try
            {
                dtbook = XDocument.Load(sourceFileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load source file {sourceFileName}: {e.Message}");
                return -1;
            }
            var xhtmlFileName = Path.Combine(destPath, $"{destTitleNumber}.html");
            Utils.SetMeta(dtbook, "dc:identifier", $"dk-nota-{destTitleNumber}");
            Utils.TransformDtbookToXhtml(dtbook).Save(xhtmlFileName);
            if (File.Exists(sourceFileName))
            {
                File.Delete(sourceFileName);
            }
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = XDocument.Load(xhtmlFileName, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo)
            };
            synthesizer.Progress += (sender, args) =>
            {
                Console.Write($"{args.ProgressPercentage:D3}% {args.ProgressMessage}".PadRight(80).Substring(0, 80) + "\r");
            };
            synthesizer.Synthesize();
            Console.Write($"{new String(' ', 80)}\r");
            synthesizer.GenerateDaisy202SmilFiles();
            synthesizer.GenerateNccDocument();
            synthesizer.NccDocument.Save(Path.Combine(destPath, "ncc.html"));
            foreach (var smilFile in synthesizer.SmilFiles)
            {
                smilFile.Value.Save(Path.Combine(destPath, smilFile.Key));
            }
            Console.WriteLine("Synthesized DTB");
            return 0;
        }

        private static async Task<int> Execute()
        {
            try
            {
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }
                Directory.CreateDirectory(destPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not create destination directory {destPath} due to an unexpected {e.GetType()}: {e.Message}");
                return -2;
            }

            var res = await DownloadSource();
            if (res != 0)
            {
                return res;
            }

            res = Synthesize();
            if (res != 0)
            {
                return res;
            }

            return await UploadDestination();
        }

        private static void CopyDirectory(string source, string dest)
        {
            CopyDirectory(new DirectoryInfo(source), dest);
        }

        private static void CopyDirectory(DirectoryInfo source, string dest)
        {
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }
            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(dest, file.Name));
            }
            foreach (var dir in source.GetDirectories())
            {
                CopyDirectory(dir, Path.Combine(dest, dir.Name));
            }
        }
    }
}
