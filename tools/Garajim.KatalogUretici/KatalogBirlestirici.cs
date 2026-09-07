namespace Garajim.KatalogUretici
{
    public static class KatalogBirlestirici
    {
        public const string KaynakMetni =
            "Turkiye alt kumesi price-model.zip sozlugunden; global markalar ve modeller NHTSA vPIC (vpic.nhtsa.dot.gov)";

        public static GlobalKatalogBelgesi Uret(
            TrKatalog tr,
            IReadOnlyDictionary<string, List<string>> vpic,
            string surum,
            string uretimTarihi)
        {
            if (tr == null)
            {
                throw new ArgumentNullException(nameof(tr));
            }

            var markalar = new Dictionary<string, Dictionary<string, GlobalSeri>>(StringComparer.OrdinalIgnoreCase);
            var markaTr = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var marka in tr.Markalar)
            {
                var seriler = new Dictionary<string, GlobalSeri>(StringComparer.OrdinalIgnoreCase);

                foreach (var seri in marka.Value)
                {
                    seriler[seri] = new GlobalSeri { Ad = seri, Tr = true };
                }

                markalar[marka.Key] = seriler;
                markaTr[marka.Key] = true;
            }

            foreach (var girdi in vpic ?? new Dictionary<string, List<string>>())
            {
                var markaAdi = MarkaEslemesi.Kanonik(girdi.Key, tr);

                if (markaAdi == null)
                {
                    continue;
                }

                if (!markalar.TryGetValue(markaAdi, out var seriler))
                {
                    seriler = new Dictionary<string, GlobalSeri>(StringComparer.OrdinalIgnoreCase);
                    markalar[markaAdi] = seriler;
                    markaTr[markaAdi] = tr.MarkaVar(markaAdi);
                }

                foreach (var hamSeri in girdi.Value ?? new List<string>())
                {
                    var seriAdi = MetinTemizleyici.Duzelt(hamSeri);

                    if (seriAdi == null || seriler.ContainsKey(seriAdi))
                    {
                        continue;
                    }

                    seriler[seriAdi] = new GlobalSeri { Ad = seriAdi, Tr = tr.SeriVar(markaAdi, seriAdi) };
                }
            }

            var belge = new GlobalKatalogBelgesi
            {
                Surum = surum,
                Kaynak = KaynakMetni,
                UretimTarihi = uretimTarihi,
                Markalar = markalar
                    .OrderBy(m => m.Key, StringComparer.Ordinal)
                    .Select(m => new GlobalMarka
                    {
                        Ad = m.Key,
                        Tr = markaTr[m.Key],
                        Seriler = m.Value.Values.OrderBy(s => s.Ad, StringComparer.Ordinal).ToList(),
                    })
                    .Where(m => m.Seriler.Count > 0)
                    .ToList(),
            };

            return belge;
        }
    }
}
