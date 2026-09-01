using System.Globalization;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete.Evraklar
{
    public class KisLastigiPenceresiBilgisi
    {
        public int BaslangicGun { get; set; }
        public int BaslangicAy { get; set; }
        public int BitisGun { get; set; }
        public int BitisAy { get; set; }
    }

    public class EvrakKurallari
    {
        public const int YaklasiyorGun = 30;
        private const string VarsayilanPencere = "01-12..01-04";
        private static readonly int[] VarsayilanUyariGunleri = { 30, 7 };
        private const int IlkMuayeneYili = 3;

        private readonly IConfiguration _configuration;

        public EvrakKurallari(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DateTime SonrakiTarih(EvrakTuru tur, KullanimTuru kullanim, DateTime? oncekiBitis, DateTime? ilkTescil)
        {
            if (tur == EvrakTuru.KisLastigi)
            {
                return KisLastigiBitisi(oncekiBitis ?? ilkTescil ?? DateTime.UtcNow.Date);
            }

            if (tur == EvrakTuru.Muayene && oncekiBitis == null && ilkTescil != null)
            {
                return ilkTescil.Value.Date.AddYears(IlkMuayeneYili);
            }

            var taban = (oncekiBitis ?? ilkTescil ?? DateTime.UtcNow.Date).Date;
            return taban.AddYears(YilAraligi(tur, kullanim));
        }

        private static int YilAraligi(EvrakTuru tur, KullanimTuru kullanim)
        {
            return tur switch
            {
                EvrakTuru.Muayene => kullanim == KullanimTuru.Ticari ? 1 : 2,
                EvrakTuru.EgzozEmisyon => kullanim == KullanimTuru.Ticari ? 1 : 2,
                _ => 1
            };
        }

        public KisLastigiPenceresiBilgisi KisLastigiPenceresi()
        {
            var deger = _configuration["Evrak:KisLastigi"];
            var pencere = Coz(deger);
            return pencere ?? Coz(VarsayilanPencere);
        }

        private static KisLastigiPenceresiBilgisi Coz(string deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return null;
            }

            var parcalar = deger.Split("..", StringSplitOptions.TrimEntries);
            if (parcalar.Length != 2)
            {
                return null;
            }

            if (!GunAy(parcalar[0], out var bg, out var ba) || !GunAy(parcalar[1], out var sg, out var sa))
            {
                return null;
            }

            return new KisLastigiPenceresiBilgisi
            {
                BaslangicGun = bg,
                BaslangicAy = ba,
                BitisGun = sg,
                BitisAy = sa
            };
        }

        private static bool GunAy(string metin, out int gun, out int ay)
        {
            gun = 0;
            ay = 0;
            var parcalar = metin.Split('-');
            return parcalar.Length == 2
                   && int.TryParse(parcalar[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out gun)
                   && int.TryParse(parcalar[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ay)
                   && gun >= 1 && gun <= 31 && ay >= 1 && ay <= 12;
        }

        public bool KisLastigiDonemindeMi(DateTime tarih)
        {
            var pencere = KisLastigiPenceresi();
            var baslangic = pencere.BaslangicAy * 100 + pencere.BaslangicGun;
            var bitis = pencere.BitisAy * 100 + pencere.BitisGun;
            var deger = tarih.Month * 100 + tarih.Day;

            return baslangic <= bitis
                ? deger >= baslangic && deger <= bitis
                : deger >= baslangic || deger <= bitis;
        }

        public DateTime KisLastigiBitisi(DateTime referans)
        {
            var pencere = KisLastigiPenceresi();
            var yil = referans.Month * 100 + referans.Day > pencere.BitisAy * 100 + pencere.BitisGun
                ? referans.Year + 1
                : referans.Year;

            return new DateTime(yil, pencere.BitisAy, Math.Min(pencere.BitisGun, DateTime.DaysInMonth(yil, pencere.BitisAy)));
        }

        public int[] UyariGunleri()
        {
            var deger = _configuration["Evrak:UyariGunleri"];
            if (string.IsNullOrWhiteSpace(deger))
            {
                return VarsayilanUyariGunleri;
            }

            var gunler = deger
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(g => int.TryParse(g, out var sayi) ? sayi : -1)
                .Where(g => g >= 0)
                .Distinct()
                .OrderByDescending(g => g)
                .ToArray();

            return gunler.Length == 0 ? VarsayilanUyariGunleri : gunler;
        }

        public static string Durum(DateTime bitisTarihi, DateTime bugun)
        {
            var kalan = (bitisTarihi.Date - bugun.Date).TotalDays;

            if (kalan < 0)
            {
                return "Gecti";
            }

            return kalan <= YaklasiyorGun ? "Yaklasiyor" : "Iyi";
        }
    }
}
