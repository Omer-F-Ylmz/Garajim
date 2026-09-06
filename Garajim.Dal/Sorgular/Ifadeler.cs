using System.Linq.Expressions;

namespace Garajim.Dal.Sorgular
{
    public static class Ifadeler
    {
        public static Expression<Func<T, bool>> Veya<T>(Expression<Func<T, bool>> sol, Expression<Func<T, bool>> sag)
        {
            if (sol == null)
            {
                return sag;
            }

            if (sag == null)
            {
                return sol;
            }

            var parametre = Expression.Parameter(typeof(T), "x");
            var solGovde = Degistir(sol.Body, sol.Parameters[0], parametre);
            var sagGovde = Degistir(sag.Body, sag.Parameters[0], parametre);

            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(solGovde, sagGovde), parametre);
        }

        private static Expression Degistir(Expression govde, ParameterExpression eski, ParameterExpression yeni)
        {
            return new Degistirici(eski, yeni).Visit(govde);
        }

        private sealed class Degistirici : ExpressionVisitor
        {
            private readonly ParameterExpression _eski;
            private readonly ParameterExpression _yeni;

            public Degistirici(ParameterExpression eski, ParameterExpression yeni)
            {
                _eski = eski;
                _yeni = yeni;
            }

            protected override Expression VisitParameter(ParameterExpression dugum)
            {
                return dugum == _eski ? _yeni : base.VisitParameter(dugum);
            }
        }
    }
}
