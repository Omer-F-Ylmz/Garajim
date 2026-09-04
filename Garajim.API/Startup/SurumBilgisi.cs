using System.Reflection;

namespace Garajim.API.Startup
{
    public static class SurumBilgisi
    {
        public const string BaslikAdi = "X-App-Version";

        public const string YerTutucu = "__SURUM__";

        public static string Surum { get; } = Coz();

        private static string Coz()
        {
            var derleme = Assembly.GetEntryAssembly() ?? typeof(SurumBilgisi).Assembly;

            var bilgi = derleme.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(bilgi))
            {
                return Sadelestir(bilgi);
            }

            return Sadelestir(derleme.GetName().Version?.ToString() ?? "0.0.0");
        }

        public static string Sadelestir(string ham)
        {
            var temiz = new string(ham.Where(k => char.IsLetterOrDigit(k) || k == '.' || k == '-' || k == '+').ToArray());
            var arti = temiz.IndexOf('+');

            if (arti >= 0)
            {
                var surum = temiz.Substring(0, arti);
                var ek = temiz.Substring(arti + 1);
                temiz = surum + "+" + (ek.Length > 7 ? ek.Substring(0, 7) : ek);
            }

            return temiz.Length > 40 ? temiz.Substring(0, 40) : temiz;
        }
    }
}
