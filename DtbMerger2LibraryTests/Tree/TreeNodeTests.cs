using System;
using System.Linq;
using DtbMerger2Library.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DtbMerger2LibraryTests.Tree
{
    [TestClass]
    public class TreeNodeTests
    {
        private class TestNode : TreeNode<TestNode>
        {
            public string Value { get; set; }

            public override String ToString()
            {
                return Value;
            }
        }

        [TestMethod]
        public void RootIsRootTest()
        {
            var root = new TestNode();
            Assert.AreSame(root, root.Root, "Root is not it's own root");
        }

        [TestMethod]
        public void AddTest()
        {
            var root = new TestNode {Value = "Root Node"};
            Assert.AreSame(root, root.Root, "Root is not it's own root");
            Assert.IsFalse(root.ChildNodes.Any(), "Newly created root has children");
            var child = new TestNode {Value = "Child Node"};
            Assert.AreSame(child, child.Root, "Standalone child is not it's own root");
            root.AddChild(child);
            Assert.AreSame(child, root.ChildNodes.Single(), "Child is not the single child after adding");
            Assert.AreSame(root.Root, child.Root, "Childs root is not the same as parents root after adding");
        }

        [TestMethod]
        public void AncestorsTest()
        {
            var root = new TestNode { Value = "Root Node" };
            Assert.IsNotNull(root.Ancestors, "Ancestors is null");
            Assert.IsFalse(root.Ancestors.Any(), "Root ancestors is not empty");
            var node1 = new TestNode() {Value = "Node 1"};
            root.AddChild(node1);
            Assert.AreEqual(1, node1.Ancestors.Count(), "Expected 1 ancestors");
            Assert.AreSame(root, node1.Ancestors.First(), "Expected root to be first ancestor of node 1");
            var node1S1 = new TestNode() { Value = "Node 1.1" };
            node1.AddChild(node1S1);
            Assert.AreEqual(2, node1S1.Ancestors.Count(), "Expected 2 ancestors");
            Assert.AreSame(node1, node1S1.Ancestors.First(), "Expected node 1 to be first ancestor of node 1.1");
            Assert.AreSame(root, node1S1.Ancestors.Last(), "Expected root to be last ancestor of node 1.1");
            var node1S1S1 = new TestNode() { Value = "Node 1.1.1" };
            node1S1.AddChild(node1S1S1);
            Assert.AreEqual(3, node1S1S1.Ancestors.Count(), "Expected 3 ancestors");
            Assert.AreSame(node1S1, node1S1S1.Ancestors.First(), "Expected node 1.1 to be first ancestor of node 1.1.1");
            Assert.AreSame(node1, node1S1S1.Ancestors.ElementAt(1), "Expected node to be ancestor at index 1 of node 1.1");
            Assert.AreSame(root, node1S1S1.Ancestors.Last(), "Expected root to be last ancestor of node 1.1");
        }


    }
}
