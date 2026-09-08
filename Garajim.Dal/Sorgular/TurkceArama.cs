using System.Linq.Expressions;
using System.Reflection;

namespace Garajim.Dal.Sorgular
{
    public static class TurkceArama
    {
        private static readonly (string Kaynak, string Hedef)[] Harfler =
        {
            ("İ", "i"), ("I", "i"), ("ı", "i"),
            ("Ş", "s"), ("ş", "s"),
            ("Ğ", "g"), ("ğ", "g"),
            ("Ü", "u"), ("ü", "u"),
            ("Ö", "o"), ("ö", "o"),
            ("Ç", "c"), ("ç", "c"),
            ("Â", "a"), ("â", "a"),
            ("Î", "i"), ("î", "i"),
            ("Û", "u"), ("û", "u")
        };

        private static readonly MethodInfo ReplaceYontemi =
            typeof(string).GetMethod(nameof(string.Replace), new[] { typeof(string), typeof(string) });

        private static readonly MethodInfo ToLowerYontemi =
            typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);

        private static readonly MethodInfo ContainsYontemi =
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

        public static string Sadelestir(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return metin;
            }

            var sonuc = metin;

            foreach (var harf in Harfler)
            {
                sonuc = sonuc.Replace(harf.Kaynak, harf.Hedef);
            }

            return sonuc.ToLowerInvariant();
        }

        public static Expression<Func<T, bool>> Iceren<T>(string terim, params Expression<Func<T, string>>[] alanlar)
        {
            if (string.IsNullOrWhiteSpace(terim) || alanlar == null || alanlar.Length == 0)
            {
                return null;
            }

            var aranan = Expression.Constant(Sadelestir(terim.Trim()), typeof(string));
            var parametre = Expression.Parameter(typeof(T), "k");
            Expression birlesik = null;

            foreach (var alan in alanlar)
            {
                var govde = new ParametreDegistirici(alan.Parameters[0], parametre).Visit(alan.Body);
                var bosDegil = Expression.NotEqual(govde, Expression.Constant(null, typeof(string)));
                var sade = Sadelestirilmis(govde);
                var iceriyor = Expression.Call(sade, ContainsYontemi, aranan);
                var dal = Expression.AndAlso(bosDegil, iceriyor);

                birlesik = birlesik == null ? dal : Expression.OrElse(birlesik, dal);
            }

            return Expression.Lambda<Func<T, bool>>(birlesik, parametre);
        }

        public static Expression<Func<T, bool>> Ve<T>(Expression<Func<T, bool>> sol, Expression<Func<T, bool>> sag)
        {
            if (sol == null)
            {
                return sag;
            }

            if (sag == null)
            {
                return sol;
            }

            var parametre = Expression.Parameter(typeof(T), "k");
            var solGovde = new ParametreDegistirici(sol.Parameters[0], parametre).Visit(sol.Body);
            var sagGovde = new ParametreDegistirici(sag.Parameters[0], parametre).Visit(sag.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(solGovde, sagGovde), parametre);
        }

        private static Expression Sadelestirilmis(Expression govde)
        {
            var sonuc = govde;

            foreach (var harf in Harfler)
            {
                sonuc = Expression.Call(sonuc, ReplaceYontemi,
                    Expression.Constant(harf.Kaynak, typeof(string)),
                    Expression.Constant(harf.Hedef, typeof(string)));
            }

            return Expression.Call(sonuc, ToLowerYontemi);
        }

        private sealed class ParametreDegistirici : ExpressionVisitor
        {
            private readonly ParameterExpression _eski;
            private readonly ParameterExpression _yeni;

            public ParametreDegistirici(ParameterExpression eski, ParameterExpression yeni)
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
