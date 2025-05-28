using System;
using System.Linq.Expressions;

namespace AspNetCoreMvcTemplate.Data.Repository
{
    public class Specification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; }

        public Specification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
        }

        public Specification<T> And(Expression<Func<T, bool>> newCriteria)
        {
            var parameter = Expression.Parameter(typeof(T));

            var left = ReplaceParameter(Criteria.Body, Criteria.Parameters[0], parameter);
            var right = ReplaceParameter(newCriteria.Body, newCriteria.Parameters[0], parameter);

            var combined = Expression.AndAlso(left, right);

            return new Specification<T>(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }

        public Specification<T> Or(Expression<Func<T, bool>> newCriteria)
        {
            var parameter = Expression.Parameter(typeof(T));

            var left = ReplaceParameter(Criteria.Body, Criteria.Parameters[0], parameter);
            var right = ReplaceParameter(newCriteria.Body, newCriteria.Parameters[0], parameter);

            var combined = Expression.OrElse(left, right);

            return new Specification<T>(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }

        private Expression ReplaceParameter(Expression body, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacer(oldParam, newParam).Visit(body);
        }

        // Implicit conversion for convenience
        public static implicit operator Expression<Func<T, bool>>(Specification<T> spec) => spec.Criteria;
    }

    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
