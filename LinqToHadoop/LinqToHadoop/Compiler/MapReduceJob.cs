using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToHadoop.Compiler
{
    public class MapReduceJob
    {
        [Flags]
        private enum Phase
        {
            Map = 1,
            Combine = 2,
            Reduce = 4,
        }

        public LambdaExpression MapExpression { get; private set; }
        public LambdaExpression CombineExpression { get; private set; }
        public LambdaExpression ReduceExpression { get; private set; }

        private IEnumerable<Phase> Phases
        {
            get
            {
                if (this.MapExpression != null) yield return Phase.Map;
                if (this.CombineExpression != null) yield return Phase.Combine;
                if (this.ReduceExpression != null) yield return Phase.Reduce;
            }
        }

        public MapReduceJob(LambdaExpression mapExpression = null,
                            LambdaExpression combineExpression = null,
                            LambdaExpression reduceExpression = null)
        {
            Throw.If((mapExpression ?? combineExpression ?? reduceExpression) == null, "Must specify at least one phase");
            this.MapExpression = mapExpression;
            this.CombineExpression = combineExpression;
            this.ReduceExpression = reduceExpression;
        }

        public bool TryMergeWith(MapReduceJob next, out MapReduceJob combined)
        {
            // we can merge any two jobs with the first having phases i .. j and the second having phases j .. k
            if (this.Phases.Max() <= next.Phases.Min())
            {
                combined = this.Merge(
                    next,
                    mapExpression: Merge(this.MapExpression, next.MapExpression),
                    combineExpression: Merge(this.CombineExpression, next.CombineExpression),
                    reduceExpression: Merge(this.ReduceExpression, next.ReduceExpression)
                );
                return true;
            }

            // we can also push map-only jobs back onto the output of reduce jobs
            if (this.Phases.Contains(Phase.Reduce) && next.Phases.SequenceEqual(new[] { Phase.Map }))
            {
                combined = this.Merge(
                    next,
                    mapExpression: this.MapExpression,
                    combineExpression: this.CombineExpression,
                    reduceExpression: Merge(this.ReduceExpression, next.MapExpression)
                );
                return true;
            }

            combined = null;
            return false;
        }

        private MapReduceJob Merge(MapReduceJob next,
            LambdaExpression mapExpression,
            LambdaExpression combineExpression,
            LambdaExpression reduceExpression)
        {
            var mergedJob = new MapReduceJob(
                mapExpression: mapExpression,
                combineExpression: combineExpression,
                reduceExpression: reduceExpression
            );
            return mergedJob;
        }

        private static LambdaExpression Merge(LambdaExpression prev, LambdaExpression next)
        {
            // we can skip the merge if one is null
            if (prev == null || next == null)
            {
                return prev ?? next;
            }

            // otherwise, we want to pipe the output of prev to the input of next
            var invoked = Expression.Invoke(next, prev);
            var mergedLambda = Expression.Lambda(invoked, prev.Parameters);
            return mergedLambda;
        }
    }
}
