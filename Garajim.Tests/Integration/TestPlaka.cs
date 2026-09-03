namespace Garajim.Tests.Integration
{
    public static class TestPlaka
    {
        private const string Harfler = "ABCDEFGHIJKLMNOPRSTUVYZ";

        private static long _sayac;

        public static string Uret()
        {
            var no = Interlocked.Increment(ref _sayac);

            var harf = new char[3];
            var kalan = no / 1000;
            for (var i = 2; i >= 0; i--)
            {
                harf[i] = Harfler[(int)(kalan % Harfler.Length)];
                kalan /= Harfler.Length;
            }

            return "34" + new string(harf) + ((no % 1000)).ToString("D3");
        }
    }
}
