using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DtbMerger2Library.Daisy202;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Daisy202
{
    [TestClass]
    [DeploymentItem(@".\DTBs")]
    public class MacroEntryTests
    {
        private readonly Uri dtb1NccUri = new Uri(new Uri(new Uri(Directory.GetCurrentDirectory()), "Out/"), "DTB1/ncc.html");

        [TestMethod]
        public void GetMacroElementsFromNccTest()
        {
            var macroElements = MacroEntry.GetMacroElementsFromNcc(dtb1NccUri);
            var ncc = Utils.LoadXDocument(dtb1NccUri);
            Assert.AreEqual(ncc.Descendants(Utils.XhtmlNs+"h1").Count(), macroElements.Count(), "Number of macro elements must match number of h1 elements");
        }

    }
}
