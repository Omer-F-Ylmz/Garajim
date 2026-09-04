using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Planlar;
using Garajim.Core.Multitenancy;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class YonetimManager : IYonetimService
    {
        public const int SeriGun = 30;
        public const int GeriBildirimSayisi = 20;
        public const double MilyonTokenUsd = 0.30;

        private readonly ICompanyDal _companyDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IReceiptDraftDal _receiptDal;
        private readonly IKarnePaylasimiDal _karneDal;
        private readonly IGeriBildirimDal _geriBildirimDal;
        private readonly IAiTokenDal _tokenDal;
        private readonly PlanKurallari _planKurallari;
        private readonly TenantContext _tenantContext;
        private readonly IConfiguration _configuration;

        public YonetimManager(
            ICompanyDal companyDal,
            IUserDal userDal,
            IVehicleDal vehicleDal,
            IReceiptDraftDal receiptDal,
            IKarnePaylasimiDal karneDal,
            IGeriBildirimDal geriBildirimDal,
            IAiTokenDal tokenDal,
            PlanKurallari planKurallari,
            TenantContext tenantContext,
            IConfiguration configuration)
        {
            _companyDal = companyDal;
            _userDal = userDal;
            _vehicleDal = vehicleDal;
            _receiptDal = receiptDal;
            _karneDal = karneDal;
            _geriBildirimDal = geriBildirimDal;
            _tokenDal = tokenDal;
            _planKurallari = planKurallari;
            _tenantContext = tenantContext;
            _configuration = configuration;
        }

        public async Task<IDataResult<YonetimOzetDto>> OzetAsync(object bellek)
        {
            var sirketler = await _companyDal.GetListAsync();
            var ozet = new YonetimOzetDto
            {
                SirketSayisi = sirketler.Count,
                Bellek = bellek,
                UstaAcik = _configuration.GetValue("Usta:Enabled", true)
            };

            var kullanicilar = new List<AppUser>();
            var fisler = new List<ReceiptDraft>();
            var geriBildirimler = new List<GeriBildirim>();
            var aracToplam = 0;
            var karneliArac = 0;

            foreach (var sirket in sirketler)
            {
                using var kapsam = SystemScope.For(_tenantContext, sirket.Id);

                kullanicilar.AddRange(await _userDal.GetListAsync());
                fisler.AddRange(await _receiptDal.GetListAsync());
                geriBildirimler.AddRange(await _geriBildirimDal.SonlariAsync(GeriBildirimSayisi));

                var araclar = await _vehicleDal.GetListAsync();
                aracToplam += araclar.Count;

                var paylasimlar = await _karneDal.GetListAsync(k => k.Aktif);
                karneliArac += paylasimlar.Select(p => p.VehicleId).Distinct().Count();
            }

            ozet.KullaniciSayisi = kullanicilar.Count;
            ozet.AracSayisi = aracToplam;
            ozet.GunlukKayitlar = SeriUret(sirketler, kullanicilar);

            var onaylananlar = fisler.Where(f => f.Durum == ReceiptDraftStatus.Onaylandi).ToList();
            var elleOnaylananlar = onaylananlar.Where(f => !f.OtoOnaylandi).ToList();

            ozet.FisSayisi = fisler.Count;
            ozet.FisDogrulukOrani = elleOnaylananlar.Count == 0
                ? 0
                : Oran(elleOnaylananlar.Count(f => string.IsNullOrWhiteSpace(f.DuzeltilenAlanlar)), elleOnaylananlar.Count);
            ozet.OtoOnayOrani = Oran(onaylananlar.Count(f => f.OtoOnaylandi), onaylananlar.Count);

            ozet.KarnePaylasimOrani = Oran(karneliArac, aracToplam);
            ozet.DavetKayitOrani = Oran(sirketler.Count(s => s.DavetEdenCompanyId != null), sirketler.Count);

            var bugun = Saat.BugunTr();
            var sayac = await _tokenDal.AyiAlAsync(bugun.Year, bugun.Month);

            ozet.AiTokenKullanilan = sayac == null ? 0 : sayac.TokenGiris + sayac.TokenCikis;
            ozet.AiTokenTavani = _planKurallari.AylikTokenTavani();
            ozet.AiTahminiMaliyetUsd = Math.Round(ozet.AiTokenKullanilan / 1_000_000d * MilyonTokenUsd, 4);
            ozet.KotaHatasi = sayac?.KotaHatasi ?? 0;

            ozet.SonGeriBildirimler = geriBildirimler
                .OrderByDescending(g => g.Tarih)
                .Take(GeriBildirimSayisi)
                .Select(g => new GeriBildirimDto
                {
                    Id = g.Id,
                    Tur = g.Tur.ToString(),
                    Mesaj = g.Mesaj,
                    Sayfa = g.Sayfa,
                    Surum = g.Surum,
                    KullaniciAdi = kullanicilar.FirstOrDefault(u => u.Id == g.UserId)?.FullName,
                    Tarih = g.Tarih
                })
                .ToList();

            return new SuccessDataResult<YonetimOzetDto>(ozet);
        }

        private static List<YonetimGunDto> SeriUret(List<Company> sirketler, List<AppUser> kullanicilar)
        {
            var bugun = Saat.BugunTr();
            var seri = new List<YonetimGunDto>();

            for (var i = SeriGun - 1; i >= 0; i--)
            {
                var gun = bugun.AddDays(-i);

                seri.Add(new YonetimGunDto
                {
                    Gun = gun.ToString("yyyy-MM-dd"),
                    Sirket = sirketler.Count(s => Saat.Yerel(s.CreatedAt).Date == gun),
                    Kullanici = kullanicilar.Count(u => Saat.Yerel(u.CreatedAt).Date == gun)
                });
            }

            return seri;
        }

        private static double Oran(int pay, int payda)
        {
            return payda == 0 ? 0 : Math.Round(pay * 100d / payda, 1);
        }
    }
}
