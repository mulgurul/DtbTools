using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DtbSynthesizerLibrary;
using DtbSynthesizerLibrary.Xhtml;
using Mono.Options;

namespace DtbSynthesizer
{
    class Program
    {
        private static string input = null;
        private static string format = null;
        private static string output = null;
        private static string identifier = null;
        private static string creator =
            FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).CompanyName;
        private static string publisher = creator;
        private static bool mp3 = false;
        private static int bitrate = 48;

        private static readonly OptionSet Options = new OptionSet()
            .Add("Synthesize DTB")
            .Add("i|input=", "Input file", s => input = s)
            .Add("f|format=", "Output format", s => format = s)
            .Add("o|output=", "Output directory", s => output = s)
            .Add("identifier=", "dc:identifier of the synthesized DTB", s => identifier = s)
            .Add("c|creator=", "Default creator of the synthesized DTB", s => creator = s)
            .Add("p|publisher=", "Default creator of the synthesized DTB", s => publisher = s)
            .Add("m|mp3", "Switch on mp3 encoding", s => mp3 = s!=null)
            .Add<int>("b|bitrate", "Mp3 bitrate", i => bitrate = i);

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
                    throw new OptionException($"Unhandled arguments {unhandledArgs.Aggregate((s,v) => $"{s} {v}")}", "");
                }
                if (input == null)
                {
                    throw new OptionException("Missing input", "input");
                }
                if (format == null)
                {
                    throw new OptionException("Missing format", "format");
                }
                output = output ?? Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), input));
                if (output == null)
                {
                    throw new OptionException("Could not determine output directory", "output");
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{e.Message}\n{OptionDescriptions}");
                return -1;
            }
            Console.WriteLine($"Input: {input}\nFormat: {format}\nOutput: {output}");
            if (!File.Exists(input))
            {
                Console.WriteLine($"Could not find input file {input}");
                return -2;
            }
            if (!String.IsNullOrEmpty(output) && !Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }
            switch (format.ToLowerInvariant())
            {
                case "daisy202":
                    return SynthesizeDaisy202Dtb();
                default:
                    Console.WriteLine($"Unknown format {format}\n{OptionDescriptions}");
                    return -2;
            }
        }

        static int SynthesizeDaisy202Dtb(XDocument xhtmlDocument)
        {
            var startTime = DateTime.Now;
            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }
            output = Path.GetFullPath(output);
            var synthesizer = new XhtmlSynthesizer()
            {
                XhtmlDocument = xhtmlDocument,
                EncodeMp3 = mp3,
                Mp3BitRate = bitrate
            };
            synthesizer.Progress += (sender, args) =>
            {
                Console.Write($"{args.ProgressPercentage:D3}% {args.ProgressMessage}".PadRight(80).Substring(0, 80) + "\r");
            };
            synthesizer.Synthesize();
            Console.Write($"{new String(' ', 80)}\r");
            synthesizer.GenerateDaisy202SmilFiles();
            synthesizer.GenerateNccDocument();
            if (!String.IsNullOrWhiteSpace(identifier))
            {
                foreach (var doc in synthesizer
                    .SmilFiles
                    .Values
                    .Concat(new[] { synthesizer.NccDocument, synthesizer.XhtmlDocument }))
                {
                    Utils.SetMeta(doc, "dc:identifier", identifier);
                }
            }
            if (String.IsNullOrWhiteSpace(Utils.GetMetaContent(synthesizer.NccDocument, "dc:creator")))
            {
                Utils.SetMeta(synthesizer.NccDocument, "dc:creator", creator);
            }
            if (String.IsNullOrWhiteSpace(Utils.GetMetaContent(synthesizer.NccDocument, "dc:publisher")))
            {
                Utils.SetMeta(synthesizer.NccDocument, "dc:publisher", publisher);
            }

            synthesizer.NccDocument.Save(Path.Combine(output, "ncc.html"));
            foreach (var smilFile in synthesizer.SmilFiles)
            {
                smilFile.Value.Save(Path.Combine(output, smilFile.Key));
            }
            Console.WriteLine($"Succesfulle generated Daisy 2.02 DTB in {output}\nDuration: {DateTime.Now.Subtract(startTime)}");
            return 0;
        }

        static int SynthesizeDaisy202Dtb()
        {
            if (new[] { "ncc.htm", "ncc.html" }.Any(n =>
                n.Equals(Path.GetFileName(input), StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.WriteLine("Input file cannot be named ncc.htm or ncc.html");
                return -2;
            }
            output = output ?? Path.GetDirectoryName(Path.GetFullPath(input));
            if (output == null)
            {
                Console.WriteLine("output unexpectedly missing");
                return -2;
            }
            input = Path.GetFullPath(input);
            string xhtml;
            var inputDoc = XDocument.Load(input, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            if (inputDoc.Root?.Name.LocalName == "dtbook")
            {
                xhtml = Path.Combine(output, "content.html");
                var transformedDoc = Utils.TransformDtbookToXhtml(inputDoc);
                var inputDocUri = new Uri(inputDoc.BaseUri);
                var xhtmlUri = new Uri(xhtml);
                foreach (var src in transformedDoc
                    .Descendants(XhtmlSynthesizer.XhtmlNs + "img")
                    .Select(img => img.Attribute("src")?.Value)
                    .Where(s => !String.IsNullOrEmpty(s))
                    .Distinct())
                {
                    File.Copy(new Uri(inputDocUri, src).LocalPath, new Uri(xhtmlUri, src).LocalPath);
                }
                transformedDoc.Save(xhtml);
            }
            else if (output.Equals(Path.GetDirectoryName(input)))
            {
                xhtml = input;
            }
            else
            {
                xhtml = Path.Combine(output, Path.GetFileName(input));
                File.Copy(input, xhtml, true);
            }
            inputDoc = XDocument.Load(xhtml, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            return SynthesizeDaisy202Dtb(inputDoc);
        }
    }
}
