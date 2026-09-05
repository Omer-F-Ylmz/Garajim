using System.Globalization;

namespace Garajim.RehberUretici
{
    public static class Basliklar
    {
        public const int BaslikSiniri = 60;
        public const int AciklamaSiniri = 155;

        private static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");

        public static string Uret(RehberKaydi kayit)
        {
            var ham = kayit.Bolum switch
            {
                Bolumler.Belirti => BelirtiBasligi(kayit.Metin),
                Bolumler.Obd => ObdBasligi(kayit),
                Bolumler.Bakim => BakimBasligi(kayit),
                _ => IlkCumleBasligi(kayit.Metin)
            };

            return Kirp(ham, BaslikSiniri);
        }

        public static string Aciklama(RehberKaydi kayit)
        {
            var govde = kayit.Metin ?? string.Empty;

            if (kayit.Bolum == Bolumler.Belirti)
            {
                var bolumler = MetinAyristirici.Bolumler(govde);
                var belirti = bolumler.FirstOrDefault(b => b.Baslik == "Belirti");
                var enSik = bolumler.FirstOrDefault(b => b.Baslik == "En sık");

                if (belirti != null)
                {
                    govde = MetinAyristirici.IlkCumle(belirti.Metin);

                    if (enSik != null)
                    {
                        govde += ". En sık: " + MetinAyristirici.IlkCumle(enSik.Metin);
                    }

                    return Kirp(BuyukHarfeBasla(govde) + ".", AciklamaSiniri);
                }
            }

            return Kirp(MetinAyristirici.IlkCumle(govde) + ".", AciklamaSiniri);
        }

        private static string IlkCumleBasligi(string metin)
        {
            var cumle = MetinAyristirici.IlkCumle(metin);
            var derinlik = 0;

            for (var i = 0; i < cumle.Length; i++)
            {
                var harf = cumle[i];

                if (harf == '(' || harf == '[')
                {
                    derinlik++;
                }
                else if (harf == ')' || harf == ']')
                {
                    derinlik = Math.Max(0, derinlik - 1);
                }
                else if (harf == ':' && derinlik == 0 && i >= 20)
                {
                    return cumle.Substring(0, i).Trim();
                }
            }

            return cumle;
        }

        private static string BelirtiBasligi(string metin)
        {
            var bolumler = MetinAyristirici.Bolumler(metin);
            var belirti = bolumler.FirstOrDefault(b => b.Baslik == "Belirti");
            var govde = MetinAyristirici.IlkCumle(belirti != null ? belirti.Metin : metin);

            if (govde.Length == 0)
            {
                return string.Empty;
            }

            return BuyukHarfeBasla(govde) + " neden olur?";
        }

        private static string ObdBasligi(RehberKaydi kayit)
        {
            var kod = (kayit.Metin ?? string.Empty).Split(new[] { ' ', '\t', '—', '-' },
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(kod) && kayit.Id != null && kayit.Id.StartsWith("obd-", StringComparison.OrdinalIgnoreCase))
            {
                kod = kayit.Id.Substring(4);
            }

            return kod + " arıza kodu nedir? Anlamı, nedenleri, aciliyet";
        }

        private static string BakimBasligi(RehberKaydi kayit)
        {
            if (kayit.Id == Sabitler.BakimKuralId)
            {
                return "Periyodik bakım aralıkları: genel kural";
            }

            var metin = kayit.Metin ?? string.Empty;
            var derinlik = 0;
            var kesim = -1;

            for (var i = 0; i < metin.Length; i++)
            {
                var harf = metin[i];

                if (harf == '(' || harf == '[')
                {
                    derinlik++;
                }
                else if (harf == ')' || harf == ']')
                {
                    derinlik = Math.Max(0, derinlik - 1);
                }
                else if (harf == ':' && derinlik == 0)
                {
                    kesim = i;
                    break;
                }
            }

            var ad = kesim > 0 ? metin.Substring(0, kesim) : MetinAyristirici.IlkCumle(metin);

            ad = ParantezleriAt(ad).Trim(' ', ',', ';', '-');

            return ad + " bakım aralıkları";
        }

        private static string ParantezleriAt(string metin)
        {
            var yazi = new System.Text.StringBuilder(metin.Length);
            var derinlik = 0;

            foreach (var harf in metin)
            {
                if (harf == '(' || harf == '[')
                {
                    derinlik++;
                }
                else if (harf == ')' || harf == ']')
                {
                    derinlik = Math.Max(0, derinlik - 1);
                }
                else if (derinlik == 0)
                {
                    yazi.Append(harf);
                }
            }

            return System.Text.RegularExpressions.Regex.Replace(yazi.ToString(), "\\s{2,}", " ").Trim();
        }

        public static string BuyukHarfeBasla(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return metin;
            }

            return char.ToUpper(metin[0], Turkce) + metin.Substring(1);
        }

        public static string Kirp(string metin, int sinir)
        {
            var govde = (metin ?? string.Empty).Trim();

            if (Uzunluk(govde) <= sinir)
            {
                return govde;
            }

            var kesim = Math.Min(govde.Length, sinir) - 1;

            while (kesim > 0)
            {
                var aday = govde.Substring(0, kesim).TrimEnd(' ', ',', ';', '-', '.') + "…";

                if (Uzunluk(aday) <= sinir)
                {
                    return aday;
                }

                kesim--;
            }

            return "…";
        }

        public static int Uzunluk(string metin)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return 0;
            }

            var uzunluk = 0;

            foreach (var harf in metin)
            {
                uzunluk += harf switch
                {
                    '&' => 5,
                    '<' => 4,
                    '>' => 4,
                    '"' => 6,
                    _ => 1
                };
            }

            return uzunluk;
        }
    }
}
