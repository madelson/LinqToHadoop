using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToHadoop.Compiler
{
    /// <summary>
    /// A visitor which provides access to a path value along the way
    /// </summary>
    public class BranchTrackingVisitor : ExpressionVisitor
    {
        public const string PathSeparator = "/";

        private readonly List<int> _path = new List<int> { -1 };

        public BranchTrackingVisitor()
        {
            this.Depth = -1;
        }

        public bool IsFinished { get; private set; }

        protected int Depth { get; private set; }
        protected string Path 
        { 
            get { return string.Join(PathSeparator, this._path.Take(this.Depth + 1).Select(i => (char)('0' + i))); } 
        }

        public override Expression Visit(Expression node)
        {
            Throw.InvalidIf(this.IsFinished, "The visitor cannot be re-used!");

            // MA not sure why this gets called with null, but it seems to sometimes
            if (node == null)
            {
                return null;
            }

            // increment depth
            this.Depth++;

            // increment the counter at the current depth
            this._path[this.Depth]++;

            // prepare a new counter for the children of this node
            this._path.Add(-1);

            var result = this.VisitImpl(node);

            // pop the child counter
            this._path.RemoveAt(this.Depth + 1);

            // we don't decrement the counter at the current depth because we want
            // siblings to use it

            // decrement depth
            this.Depth--;
            
            // when we get back to a depth of -1, we're back at the start
            if (this.Depth < 0)
            {
                this.IsFinished = true;
            }

            return result;
        }

        /// <summary>
        /// This should be overriden by subclasses if they want special behavior on Visit
        /// </summary>
        protected virtual Expression VisitImpl(Expression node)
        {
            return base.Visit(node);
        }

        public static bool IsXChildOfY(string x, string y)
        {
            var isXChildOfY = x.StartsWith(y) && x != y;
            return isXChildOfY;
        }
    }
}
