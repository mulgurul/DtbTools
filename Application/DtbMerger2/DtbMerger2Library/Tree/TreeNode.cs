using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtbMerger2Library.Tree
{
    /// <summary>
    /// Represents a node in a generic tree
    /// </summary>
    /// <typeparam name="T">The type of the nodes in the tree</typeparam>
    public class TreeNode<T> where T : TreeNode<T>
    {
        /// <summary>
        /// Gets the root of the tree to which the node belongs
        /// </summary>
        public T Root => Parent?.Root ?? (this as T);

        /// <summary>
        /// Gets the parent node of the node (may be null)
        /// </summary>
        public T Parent { get; protected set; }

        private readonly List<T> childNodes = new List<T>();

        /// <summary>
        /// Gets the child nodes of the node
        /// </summary>
        public ICollection<T> ChildNodes => childNodes.AsReadOnly();

        /// <summary>
        /// Adds a child to the node (as the last child)
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(T child)
        {
            InsertChild(childNodes.Count, child);
        }

        /// <summary>
        /// Adds children to the node (at the end)
        /// </summary>
        /// <param name="children"></param>
        public void AddChildren(IEnumerable<T> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        private readonly Mutex childMutex = new Mutex();

        /// <summary>
        /// Inserts a node as a child at a given index
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="child">The child node</param>
        public void InsertChild(int index, T child)
        {
            if (childMutex.WaitOne())
            {
                try
                {
                    if (child.Parent != null)
                    {
                        throw new InvalidOperationException("Cannot add a child that is already a child");
                    }

                    if (0 <= index && index <= childNodes.Count)
                    {
                        child.Parent = this as T;
                        childNodes.Insert(index, child);
                    }
                }
                finally
                {
                    childMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Removes a child from the node
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChild(T child)
        {
            if (!ChildNodes.Contains(child))
            {
                throw new InvalidOperationException("The child to remove is not one of the children");
            }

            child.Parent = null;
            childNodes.Remove(child);
        }

        /// <summary>
        /// Gets the ancestors of the node
        /// </summary>
        public IEnumerable<T> Ancestors => (Parent == null ? new T[0]: new[] {Parent}.Union(Parent.Ancestors));

        /// <summary>
        /// Gets the decendents of the node
        /// </summary>
        public IEnumerable<T> Descendents => ChildNodes.SelectMany(c => c.DescententsAndSelf);

        /// <summary>
        /// Gets the node and it's decendents
        /// </summary>
        public IEnumerable<T> DescententsAndSelf => new[] {this as T}.Union(Descendents);

        /// <summary>
        /// Gets the depth of the node in the tree (the root of the tree is at depth 1)
        /// </summary>
        public int Depth => (Parent?.Depth ?? 0) + 1;
    }
}
