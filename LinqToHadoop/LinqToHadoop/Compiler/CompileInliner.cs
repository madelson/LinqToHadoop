using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToHadoop.Compiler
{
    /// <summary>
    /// MA a utility for inlining calls to Expression.Compile
    /// </summary>
    public class CompileInliner : ExpressionVisitor
    {
        private static readonly CompileInliner _inliner = new CompileInliner();

        protected CompileInliner()
        {
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Compile"
                && node.Method.DeclaringType.Is(typeof(LambdaExpression)))
            {
                return node.Object;
            }

            return base.VisitMethodCall(node);
        }

        public static Expression ProcessExpression(Expression e)
        {
            var result = _inliner.Visit(e);
            return result;
        }
    }
}
