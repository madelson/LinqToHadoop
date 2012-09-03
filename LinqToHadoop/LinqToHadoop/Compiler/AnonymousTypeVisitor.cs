using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToHadoop.Compiler
{
    public abstract class AnonymousTypeVisitor : ExpressionVisitor
    {
        public static readonly MethodInfo EqualsMethod = Helpers.Method<object>(_ => Equals(null, null)),
            EqualsEqualsMethod, NotEqualsMethod;

        static AnonymousTypeVisitor()
        {
            Expression<Func<bool>> equalsEquals = () => 1 == 1;
            EqualsEqualsMethod = ((BinaryExpression)equalsEquals.Body).Method.GetGenericMethodDefinition();

            Expression<Func<bool>> notEquals = () => 1 != 1;
            NotEqualsMethod = ((BinaryExpression)notEquals.Body).Method.GetGenericMethodDefinition();
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Type.IsAnonymous())
            {
                var propertyAssignments = node.Constructor.GetParameters()
                    .Select(p => node.Type.GetProperty(p.Name))
                    .Zip(node.Arguments, (pi, arg) => new { pi, arg })
                    .ToDictionary(t => t.pi, t => t.arg);
                Expression result;
                if (this.TryVisitCreateAnonymousType(propertyAssignments, out result))
                {
                    return result;
                }
            }

            return base.VisitNew(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.Type.IsAnonymous())
            {
                Expression result;
                if (this.TryVisitMemberAccess(node.Expression, (PropertyInfo)node.Member, out result))
                {
                    return result;
                }
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Type == typeof(bool)
                && node.Method.IsGenericMethod
                && node.Left.Type.IsAnonymous()
                && node.Right.Type.IsAnonymous())
            {
                var genericDefinition = node.Method.GetGenericMethodDefinition();
                if (genericDefinition == EqualsEqualsMethod)
                {
                    return Expression.Call(null, EqualsMethod, node.Left, node.Right);
                }
                if (genericDefinition == NotEqualsMethod)
                {
                    return Expression.Not(Expression.Call(null, EqualsMethod, node.Left, node.Right));
                }
            }

            return base.VisitBinary(node);
        }

        protected abstract bool TryVisitCreateAnonymousType(IDictionary<PropertyInfo, Expression> propertyAssignments, out Expression replacement);
        protected abstract bool TryVisitMemberAccess(Expression anonymousObj, PropertyInfo property, out Expression replacement);
    }
}
