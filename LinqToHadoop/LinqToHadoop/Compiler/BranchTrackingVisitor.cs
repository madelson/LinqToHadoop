using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToHadoop.Compiler
{
    class BranchTrackingVisitor : ExpressionVisitor
    {
        public const string PathSeparator = "/";

        private readonly Stack<int> _path = new Stack<int>();
        private readonly List<int> _claimedBranchIdsByDepth = new List<int>();

        public BranchTrackingVisitor()
        {
            this.Depth = -1;
        }

        public bool IsFinished { get; private set; }

        protected int Depth { get; private set; }
        protected string Path 
        { 
            get { return string.Join(PathSeparator, this._claimedBranchIdsByDepth.Take(this.Depth).Select(i => (char)('0' + i))); } 
        }

        public override Expression Visit(Expression node)
        {
            //Throw.InvalidIf(this.IsFinished, "The visitor cannot be re-used!");

            // MA not sure why this gets called with null, but it seems to sometimes
            if (node == null)
            {
                Console.WriteLine("Saw null");
                return null;
            }

            this.Depth++;

            // ensure that we have a counter for that depth
            if (this._claimedBranchIdsByDepth.Count <= this.Depth)
            {
                this._claimedBranchIdsByDepth.Add(-1);
            }
            this._claimedBranchIdsByDepth[this.Depth]++;
            
            // reset all counters at greater depth than current
            for (var i = this.Depth + 1; i < this._claimedBranchIdsByDepth.Count; ++i)
            {
                this._claimedBranchIdsByDepth[i] = -1;
            }

            Console.WriteLine(this.Path + ": " + node);
            var result = this.VisitImpl(node);

            this.Depth--;
            if (this.Depth == 0)
            {
                this.IsFinished = true;
            }

            return result;
        }

        protected virtual Expression VisitImpl(Expression node)
        {
            return base.Visit(node);
        }
    }
}
