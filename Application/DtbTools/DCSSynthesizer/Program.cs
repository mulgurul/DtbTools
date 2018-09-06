using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Options;

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
        private static string uncRoot = @"\\smb-files\Temp\DCSSynthesizer";
        private static string dcsServiceUri = "http://http-dcsarchive.beta.dbb.dk";


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
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected {e.GetType()} occured: {e.Message}\nStack Trace:\n{e.StackTrace}");
                return -1000;
            }
        }
    }
}
