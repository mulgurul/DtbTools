using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DtbMerger2Library.Tree
{
    public class TreeNode<T> where T : TreeNode<T>
    {
        public T Root => Parent?.Root ?? (this as T);

        public T Parent { get; protected set; }

        private readonly List<T> childNodes = new List<T>();
        public ICollection<T> ChildNodes => childNodes.AsReadOnly();

        public void AddChild(T child)
        {
            InsertChild(childNodes.Count, child);
        }

        public void AddChildren(IEnumerable<T> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        private readonly Mutex childMutex = new Mutex();

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

        public void RemoveChild(T child)
        {
            if (!ChildNodes.Contains(child))
            {
                throw new InvalidOperationException("The child to remove is not one of the children");
            }

            child.Parent = null;
            childNodes.Remove(child);
        }

        public IEnumerable<T> Ancestors => (Parent == null ? new T[0]: new[] {Parent}.Union(Parent.Ancestors));

        public IEnumerable<T> Descendents => ChildNodes.SelectMany(c => c.DescententsAndSelf);

        public IEnumerable<T> DescententsAndSelf => new[] {this as T}.Union(Descendents);

        public int Depth => (Parent?.Depth ?? 0) + 1;
    }
}
