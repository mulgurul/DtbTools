using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;

namespace DtbMerger2
{
    class Program
    {
        private const string Usage = "DtbMerger2 <macro> <ourdir>";

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"Invalid number of arguments\n{Usage}");
                return -1;
            }

            try
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine($"Could not find macro file {args[0]}\n{Usage}");
                }

                XDocument macro;
                try
                {
                    macro = XDocument.Load(args[0], LoadOptions.SetBaseUri|LoadOptions.SetLineInfo);
                }
                catch (XmlException xe)
                {
                    Console.WriteLine(
                        $"Could not load macro {args[0]}: {xe.Message}\nat line {xe.LineNumber}, pos {xe.LinePosition})");
                    return -1;
                }
                Console.WriteLine($"Loaded macro {args[0]}");
                DtbBuilder builder;
                try
                {
                    builder = new DtbBuilder(MergeEntry.LoadMergeEntriesFromMacro(macro));
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Could not load merge entries from macro {args[0]}: {e.Message}\n{Usage}");
                    return -1;
                }
                builder.BuildDtb();
                Console.WriteLine("Built Dtb");
                if (!Directory.Exists(args[1]))
                {
                    try
                    {
                        Directory.CreateDirectory(args[1]);
                        Console.WriteLine($"Created output directory {args[1]}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not create directory {args[1]}: {e.Message}\n{Usage}");
                        return -1;
                    }
                }

                builder.SaveDtb(
                    args[1],
                    (i, s) =>
                    {
                        Console.Write($"{i} % {s}".PadRight(100).Substring(0,100)+"\r");
                        return false;
                    });
                Console.Write("".PadRight(101)+"\r");
                Console.WriteLine($"Saved built Dtb to {args[1]}");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected {e.GetType()} occured: {e.Message}\n{Usage}");
                return -2;
            }
        }
    }
}
