using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DCSArchiveClientApi;
using DCSArchiveLibrary.Model;
using Mono.Options;
using Directory = System.IO.Directory;

namespace DCSSiloSync
{
    class Program
    {
        private static string titleNumber;
        private static string code;
        private static int year = DateTime.Now.Year;
        private static string sourceSilo = "live";
        private static string destSilo = "ngt";
        private static string numbering = "week";

        private static readonly string SourceDcsServiceUri = $"http://http-dcsarchive.{sourceSilo}.dbb.dk";
        private static readonly string DestDcsServiceUri = $"http://http-dcsarchive.{destSilo}.dbb.dk";
        private static readonly string UncRoot = $"\\\\smb-files.{destSilo}.dbb.dk\\Temp\\DCSSiloSync";

        private static ClientApi sourceClientApi = null;
        private static ClientApi destClientApi = null;


        private static readonly OptionSet Options = new OptionSet()
            .Add("titlenumber=", "Title number of specific title to sync", s => titleNumber = s)
            .Add("code=", "Code of specific titles to sync - used when to sync periodicals in batch", s => code = s)
            .Add<int>("year=", "Year of specific titles to sync - used when to sync periodicals in batch. Defaults to current year", s => year = s)
            .Add("numbering", "Method of numbering: week, date or raw (1-9999) - used when to sync periodicals in batch. Defaults to week", s => numbering = s)
            .Add("sourcesilo=", "Title number of specific title to sync. Defaults to live", s => sourceSilo = s?.ToLowerInvariant())
            .Add("destsilo=", "Title number of specific title to sync. Defaults to ngt", s => destSilo = s?.ToLowerInvariant());

        /// ATTENTION: Used to copy specific titles from live/beta/ngt to beta/ngt silos for testing purposes. 
        /// Not intended for production use!!!
        static int Main(string[] args)
        {
            try
            {
                var unhandledArgs = Options.Parse(args);
                if (unhandledArgs.Any())
                {
                    throw new OptionException($"Unhandled arguments {unhandledArgs.Aggregate((s, v) => $"{s} {v}")}", "");
                }
                if (String.IsNullOrEmpty(sourceSilo) || !new[] { "live", "beta", "ngt" }.Contains(sourceSilo))
                {
                    Console.WriteLine("Invalid or missing source silo, must be one of live, beta or ngt");
                    return -1;
                }
                if (String.IsNullOrEmpty(destSilo) || !new[] {"beta", "ngt" }.Contains(destSilo))
                {
                    Console.WriteLine("Invalid or missing destination silo, must be one of beta or ngt");
                    return -1;
                }
                if (sourceSilo.Equals(destSilo))
                {
                    Console.WriteLine("Source and destination silos cannot be then same");
                    return -1;
                }
                if (!Directory.Exists(UncRoot))
                {
                    Directory.CreateDirectory(UncRoot);
                }
                sourceClientApi = new ClientApi() { BaseAddress = SourceDcsServiceUri };
                destClientApi = new ClientApi() { BaseAddress = DestDcsServiceUri };
                foreach (var clientApi in new[] {sourceClientApi, destClientApi})
                {
                    if (!clientApi.Ping().Wait(10000))//Will throw an exception, if service is not accessible
                    {
                        Console.WriteLine($"Timeout while pinging DCS Archive api {clientApi.BaseAddress}");
                        return -1;
                    }
                }
                var titlesToSync = new List<string>();
                if (!String.IsNullOrEmpty(titleNumber))
                {
                    titlesToSync.Add(titleNumber);
                }
                else if (!String.IsNullOrEmpty(code))
                {

                    var range = Enumerable.Range(1, 9999);
                    switch (numbering.ToLowerInvariant())
                    {
                        case "raw":
                            break;
                        case "week":
                            range = Enumerable.Range(1, 53);
                            break;
                        case "date":
                            range = Enumerable
                                .Range(0, 366)
                                .Select(i => new DateTime(year, 1, 1).AddDays(i))
                                .Where(d => d.Year == year)
                                .Select(d => 100 * d.Month + d.Day);
                            break;
                        default:
                            Console.WriteLine($"Invalid numbering method {numbering}");
                            return -1;
                    }

                    var checkTasks = range
                        .Select(n => $"{code}{year:D4}{n:D4}")
                        .Select(tno => new Task<string>(() => CheckTitleNumber(tno, sourceClientApi).Result))
                        .ToList();
                    foreach (var t in checkTasks)
                    {
                        t.RunSynchronously();
                    }
                    titlesToSync.AddRange(checkTasks.Select(t => t.Result).Where(tno => tno != null));
                    Console.WriteLine($"Found {titlesToSync.Count} titles in source to sync");
                }
                else
                {
                    Console.WriteLine("Neither titlenumber nor code was supplied. Nothing to do");
                    return - 1;
                }
                var checkDestTasks = titlesToSync
                    .Select(tno => new Task<string>(() => CheckTitleNumber(tno, destClientApi).Result == null ? tno : null))
                    .ToList();
                foreach (var t in checkDestTasks)
                {
                    t.RunSynchronously();
                }
                titlesToSync = new List<string>(checkDestTasks.Select(t => t.Result).Where(tno => tno != null));
                titlesToSync.Sort();
                switch (titlesToSync.Count)
                {
                    case 0:
                        Console.WriteLine("Found no titles to sync");
                        return 0;
                    case 1:
                        Console.WriteLine($"Syncing title {titlesToSync.First()}");
                        break;
                    default:
                        Console.WriteLine($"Syncing {titlesToSync.Count} titles ({titlesToSync.First()} to {titlesToSync.Last()})");
                        break;
                }

                var syncTasks = titlesToSync.Select(tno => new Task<int>(() => SyncTitle(tno).Result)).ToList();
                foreach (var t in syncTasks)
                {
                    t.RunSynchronously();
                }
                Console.WriteLine($"Succesfully synced {syncTasks.Count(t => t.Result == 0)} titles");
                var res = syncTasks.Select(t => t.Result).FirstOrDefault(i => i != 0);
                if (res != 0)
                {
                    Console.WriteLine($"{syncTasks.Select(t => t.Result).Count(i => i != 0)} sync operations failed");
                }
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected {e.GetType()} occured: {e.Message}");
                return -1000;
            }
        }

        private static async Task<string> CheckTitleNumber(string titleNo, ClientApi clientApi)
        {
            var res = (await clientApi.GetTitle(titleNo))?.TitleNo;
            Console.WriteLine((res==null)?$"{titleNo} not found in {clientApi.BaseAddress}":$"{res} found in {clientApi.BaseAddress}");
            return res;
        }

        private static async Task<int> SyncTitle(string titleNo)
        {
            var sourceTitle = await sourceClientApi.GetTitle(titleNo);
            if (sourceTitle == null)
            {
                Console.WriteLine($"Unexpectedly could not find source title {titleNo}");
                return -1000;
            }
            if (sourceTitle.DirectoriesAsList.Count != 1)
            {
                Console.WriteLine($"Title {titleNo} has {sourceTitle.DirectoriesAsList.Count} directories. Only single dir titles are aupported");
                return -1;
            }
            if (sourceTitle.ItemsAsList.Count != 1)
            {
                Console.WriteLine($"Title {titleNo} has {sourceTitle.ItemsAsList.Count} items. Only single item titles are aupported");
                return -1;
            }
            var tempDir = Path.Combine(UncRoot, titleNo);
            var di = Directory.CreateDirectory(tempDir);
            if (di.GetFiles().Any() || di.GetDirectories().Any())
            {
                Console.WriteLine($"Temp directory {tempDir} is not empty");
                return -1000;
            }
            Console.WriteLine($"Created temp directory {tempDir}");
            try
            {
                CopyDirectory(sourceTitle.DirectoriesAsList.First().WebFullPath, tempDir);
                Console.WriteLine($"Downloaded source from {sourceTitle.DirectoriesAsList.First().WebFullPath}");
                var titleUpdate = new DCSArchiveLibrary.Model.TitleUpdate
                {
                    SourcePath = tempDir,
                    OriginCode = sourceTitle.OriginCode,
                    MaterialTypeCode = sourceTitle.MaterialTypeCode,
                    MaterialFormatCode = sourceTitle.ItemsAsList.First().MaterialFormatCode,
                    TitleNo = sourceTitle.TitleNo,
                    Title = sourceTitle.Title,
                    Creator = sourceTitle.Creator,
                    SubTitle = sourceTitle.Subtitle,
                    MetadataFromDBBDokSys = false
                };
                var existingTitle = await destClientApi.GetTitle(sourceTitle.TitleNo);
                if (existingTitle != null)
                {
                    titleUpdate.TitleID = existingTitle.ID;
                    await destClientApi.UpdateTitleFormat(titleUpdate);
                    Console.WriteLine($"Updated title {titleNo} in destination");
                }
                else
                {
                    await destClientApi.CreateTitle(titleUpdate);
                    Console.WriteLine($"Created title {titleNo} in destination");
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                        Console.WriteLine($"Deleted temp directory {tempDir}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not delete temporary directory {tempDir} due to an unexpected {e.GetType()}: {e.Message}");
                }
            }
            return 0;
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

        /// <summary>
        /// returns the first saturday after the specified date.
        /// </summary>
        public static DateTime GetGuideStartDate(DateTime now, DayOfWeek dayOfWeek)
        {
            return Enumerable
                .Range(1, 7)
                .Select(i => now.AddDays(i))
                .Single(d => d.DayOfWeek == dayOfWeek);
        }

        /// <summary>
        /// returns the week number for the specified date as a string with trailing "0" if it is a single digit value.
        /// </summary>
        public static string GetFilenameWeekcode(DateTime fromDate)
        {
            int weeknumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(fromDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            if (weeknumber == 53) weeknumber = 1;
            string filenameWeekCode = "0" + weeknumber;
            filenameWeekCode = filenameWeekCode.Substring(filenameWeekCode.Length - 2);
            return filenameWeekCode;
        }

    }
}
