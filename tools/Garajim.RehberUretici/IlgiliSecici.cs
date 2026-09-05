namespace Garajim.RehberUretici
{
    public static class IlgiliSecici
    {
        public const int EnAz = 3;
        public const int EnCok = 6;

        private static readonly HashSet<string> Durak = new HashSet<string>(StringComparer.Ordinal)
        {
            "ile", "icin", "olan", "olur", "gibi", "cok", "var", "yok", "bir", "bu", "ne", "nasil"
        };

        public static HashSet<string> Belirtec(RehberKaydi kayit)
        {
            var kume = new HashSet<string>(StringComparer.Ordinal);

            foreach (var anahtar in kayit.Anahtarlar)
            {
                foreach (var parca in Slug.Uret(anahtar).Split('-', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (parca.Length >= 3 && !Durak.Contains(parca))
                    {
                        kume.Add(parca);
                    }
                }
            }

            return kume;
        }

        public static void Bagla(List<RehberKaydi> kayitlar)
        {
            var belirtecler = kayitlar.ToDictionary(k => k.Id, Belirtec, StringComparer.Ordinal);
            var sirali = kayitlar.OrderBy(k => k.Id, StringComparer.Ordinal).ToList();

            foreach (var kayit in kayitlar)
            {
                var benim = belirtecler[kayit.Id];

                var adaylar = sirali
                    .Where(a => a.Id != kayit.Id)
                    .Select(a => new
                    {
                        Kayit = a,
                        Puan = benim.Count == 0 ? 0 : belirtecler[a.Id].Count(t => benim.Contains(t)),
                        Capraz = a.Bolum != kayit.Bolum
                    })
                    .Where(a => a.Puan > 0)
                    .OrderByDescending(a => a.Puan)
                    .ThenByDescending(a => a.Capraz)
                    .ThenBy(a => a.Kayit.Id, StringComparer.Ordinal)
                    .Take(EnCok)
                    .Select(a => a.Kayit)
                    .ToList();

                if (adaylar.Count < EnAz)
                {
                    var secili = new HashSet<string>(adaylar.Select(a => a.Id), StringComparer.Ordinal);

                    foreach (var komsu in sirali.Where(a => a.Bolum == kayit.Bolum && a.Id != kayit.Id))
                    {
                        if (adaylar.Count >= EnAz)
                        {
                            break;
                        }

                        if (secili.Add(komsu.Id))
                        {
                            adaylar.Add(komsu);
                        }
                    }
                }

                kayit.Ilgili = adaylar;
            }
        }
    }
}
