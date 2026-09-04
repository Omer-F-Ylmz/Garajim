using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Business.Katalog;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Concrete
{
    public class HesapManager : IHesapService
    {
        public const string AnonimAd = "Silinmiş kullanıcı";

        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IEmailSender _emailSender;
        private readonly IKodGonderimSayaci _gonderimSayaci;
        private readonly IVehicleAssignmentDal _assignmentDal;
        private readonly ILogger<HesapManager> _logger;

        public HesapManager(
            IUserDal userDal,
            ICompanyDal companyDal,
            IEmailSender emailSender,
            IKodGonderimSayaci gonderimSayaci,
            IVehicleAssignmentDal assignmentDal,
            ILogger<HesapManager> logger = null)
        {
            _userDal = userDal;
            _companyDal = companyDal;
            _emailSender = emailSender;
            _gonderimSayaci = gonderimSayaci;
            _assignmentDal = assignmentDal;
            _logger = logger ?? NullLogger<HesapManager>.Instance;
        }

        public async Task<IDataResult<ProfilDto>> ProfilAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<ProfilDto>(Messages.UserNotFound);

            return new SuccessDataResult<ProfilDto>(new ProfilDto
            {
                FullName = user.FullName,
                Email = user.Email,
                BildirimEvrak = user.BildirimEvrak,
                BildirimHatirlatma = user.BildirimHatirlatma,
                GeciciSifre = user.GeciciSifre
            });
        }

        public async Task<IResult> ProfilGuncelleAsync(int userId, ProfilGuncelleDto dto)
        {
            var ad = (dto?.FullName ?? string.Empty).Trim();
            if (ad.Length == 0)
                return new ErrorResult(Messages.InvalidValue);

            if (!UygunsuzIfadeFiltresi.Varsayilan.Temiz(ad))
                return new ErrorResult(Messages.UygunsuzIfade);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            user.FullName = MetinSinirlari.Kirp(ad, 100);
            user.BildirimEvrak = dto.BildirimEvrak;
            user.BildirimHatirlatma = dto.BildirimHatirlatma;
            await _userDal.UpdateAsync(user);

            return new SuccessResult(Messages.ProfilGuncellendi);
        }

        public async Task<IResult> EpostaKoduGonderAsync(int userId, EpostaDegistirKodDto dto)
        {
            var yeni = (dto?.YeniEposta ?? string.Empty).Trim().ToLowerInvariant();

            if (!EpostaGecerliMi(yeni))
                return new ErrorResult(Messages.EpostaGecersiz);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (string.Equals(user.Email, yeni, StringComparison.OrdinalIgnoreCase))
                return new ErrorResult(Messages.EpostaZatenKullanimda);

            if (await _userDal.ExistsForRegistrationAsync(yeni))
                return new ErrorResult(Messages.EpostaZatenKullanimda);

            if (user.SonEpostaGonderim != null &&
                user.SonEpostaGonderim.Value.AddSeconds(DogrulamaKodu.GonderimAraligiSaniye) > DateTime.UtcNow)
                return new SuccessResult(Messages.EpostaKoduGonderildi);

            var kod = DogrulamaKodu.Uret();

            user.YeniEposta = yeni;
            user.EpostaKodHash = DogrulamaKodu.Hashle(kod);
            user.EpostaKodSonTarih = DateTime.UtcNow.AddMinutes(DogrulamaKodu.GecerlilikDakika);
            user.EpostaDenemeSayisi = 0;
            user.SonEpostaGonderim = DateTime.UtcNow;
            await _userDal.UpdateAsync(user);

            try
            {
                await _emailSender.SendAsync(yeni, "Garajım e-posta değişikliği kodunuz",
                    "Garajım e-posta değişikliği kodunuz: " + kod + Environment.NewLine + Environment.NewLine +
                    "Kod " + DogrulamaKodu.GecerlilikDakika + " dakika geçerlidir." + Environment.NewLine +
                    "Bu isteği siz yapmadıysanız kodu kimseyle paylaşmayın; adresiniz değişmez.");

                await _emailSender.SendAsync(user.Email, "Garajım e-posta değişikliği isteği",
                    "Hesabınız için yeni bir e-posta adresi istendi: " + yeni + Environment.NewLine + Environment.NewLine +
                    "Değişiklik ancak yeni adrese gelen kod girildiğinde tamamlanır." + Environment.NewLine +
                    "Bu isteği siz yapmadıysanız şifrenizi değiştirin.");
            }
            catch (Exception hata)
            {
                _logger.LogWarning(hata, "E-posta değişikliği kodu gönderilemedi.");
            }

            return new SuccessResult(Messages.EpostaKoduGonderildi);
        }

        public async Task<IResult> EpostaDegistirAsync(int userId, EpostaDegistirDto dto)
        {
            var kod = (dto?.Kod ?? string.Empty).Trim();

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (string.IsNullOrWhiteSpace(user.YeniEposta) || user.EpostaKodHash == null)
                return new ErrorResult(Messages.EpostaKoduIstenmedi);

            if (user.EpostaKodSonTarih == null || user.EpostaKodSonTarih < DateTime.UtcNow ||
                user.EpostaDenemeSayisi >= DogrulamaKodu.MaxDeneme)
                return new ErrorResult(Messages.EpostaKoduGecersiz);

            if (!DogrulamaKodu.Esit(kod, user.EpostaKodHash))
            {
                user.EpostaDenemeSayisi++;
                await _userDal.UpdateAsync(user);
                return new ErrorResult(Messages.EpostaKoduGecersiz);
            }

            if (await _userDal.ExistsForRegistrationAsync(user.YeniEposta))
            {
                EpostaAlanlariniTemizle(user);
                await _userDal.UpdateAsync(user);
                return new ErrorResult(Messages.EpostaZatenKullanimda);
            }

            user.Email = user.YeniEposta;
            EpostaAlanlariniTemizle(user);
            await _userDal.UpdateAsync(user);

            return new SuccessResult(Messages.EpostaDegistirildi);
        }

        private static void EpostaAlanlariniTemizle(AppUser user)
        {
            user.YeniEposta = null;
            user.EpostaKodHash = null;
            user.EpostaKodSonTarih = null;
            user.EpostaDenemeSayisi = 0;
        }

        private static bool EpostaGecerliMi(string eposta)
        {
            if (eposta.Length < 5 || eposta.Length > 200)
                return false;

            var at = eposta.IndexOf('@');
            if (at <= 0 || at != eposta.LastIndexOf('@') || at == eposta.Length - 1)
                return false;

            var alan = eposta[(at + 1)..];

            return alan.Contains('.') && !alan.StartsWith('.') && !alan.EndsWith('.') && !eposta.Contains(' ');
        }

        public async Task<IResult> SilmeKoduGonderAsync(int userId)
        {
            var yanit = new SuccessResult(Messages.HesapSilmeKoduYaniti);

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null || user.Role != CompanyRole.Owner)
                return yanit;

            if (user.SonSilmeGonderim != null &&
                user.SonSilmeGonderim.Value.AddSeconds(DogrulamaKodu.GonderimAraligiSaniye) > DateTime.UtcNow)
                return yanit;

            var sayacAnahtari = HesapSilme.SayacAnahtari(user.Email);
            if (!_gonderimSayaci.IzinVer(sayacAnahtari))
                return yanit;

            var kod = DogrulamaKodu.Uret();

            user.SilmeKodHash = DogrulamaKodu.Hashle(kod);
            user.SilmeKodSonTarih = DateTime.UtcNow.AddMinutes(DogrulamaKodu.GecerlilikDakika);
            user.SilmeDenemeSayisi = 0;
            user.SonSilmeGonderim = DateTime.UtcNow;
            await _userDal.UpdateAsync(user);

            _gonderimSayaci.Say(sayacAnahtari);

            try
            {
                await _emailSender.SendAsync(user.Email, HesapSilme.EpostaKonusu, HesapSilme.EpostaGovdesi(kod));
            }
            catch (Exception hata)
            {
                _logger.LogError(hata, "Hesap silme kodu e-postası gönderilemedi: {Alici}", user.Email);
            }

            return yanit;
        }

        public async Task<IResult> SilmeyiPlanlaAsync(int userId, HesapSilDto dto)
        {
            var kod = (dto?.Kod ?? string.Empty).Trim();

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null || user.Role != CompanyRole.Owner)
                return new ErrorResult(Messages.AuthorizationDenied);

            if (kod.Length != DogrulamaKodu.Uzunluk || string.IsNullOrWhiteSpace(user.SilmeKodHash))
                return new ErrorResult(Messages.HesapSilmeKoduGecersiz);

            if (user.SilmeKodSonTarih == null || user.SilmeKodSonTarih < DateTime.UtcNow)
            {
                await KodTemizleAsync(user);
                return new ErrorResult(Messages.HesapSilmeKoduGecersiz);
            }

            if (!DogrulamaKodu.Esit(kod, user.SilmeKodHash))
            {
                user.SilmeDenemeSayisi++;
                if (user.SilmeDenemeSayisi >= DogrulamaKodu.MaxDeneme)
                {
                    user.SilmeKodHash = null;
                    user.SilmeKodSonTarih = null;
                }

                await _userDal.UpdateAsync(user);
                return new ErrorResult(Messages.HesapSilmeKoduGecersiz);
            }

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (sirket == null)
                return new ErrorResult(Messages.InvalidValue);

            sirket.SilinmePlanlanan = DateTime.UtcNow.AddDays(HesapSilme.BeklemeGunu);
            await _companyDal.UpdateAsync(sirket);

            await KodTemizleAsync(user);

            return new SuccessResult(Messages.HesapSilmePlanlandi);
        }

        public async Task<IResult> SilmeyiIptalEtAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null || user.Role != CompanyRole.Owner)
                return new ErrorResult(Messages.AuthorizationDenied);

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (sirket == null)
                return new ErrorResult(Messages.InvalidValue);

            sirket.SilinmePlanlanan = null;
            await _companyDal.UpdateAsync(sirket);

            return new SuccessResult(Messages.HesapSilmeIptalEdildi);
        }

        public async Task<IDataResult<HesapDurumDto>> DurumAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<HesapDurumDto>(Messages.UserNotFound);

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (sirket == null)
                return new ErrorDataResult<HesapDurumDto>(Messages.InvalidValue);

            var planlanan = sirket.SilinmePlanlanan;
            var kalan = planlanan == null ? 0 : Math.Max(0, (int)Math.Ceiling((planlanan.Value - DateTime.UtcNow).TotalDays));

            return new SuccessDataResult<HesapDurumDto>(new HesapDurumDto
            {
                SilmePlanlandi = planlanan != null,
                SilinmeTarihi = planlanan,
                KalanGun = kalan
            });
        }

        public async Task<IResult> UyeHesabiniSilAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (user.Role == CompanyRole.Owner)
                return new ErrorResult(Messages.SahipHesabiniBoyleSilemez);

            var zimmetler = await _assignmentDal.GetListAsync(a => a.UserId == userId && a.EndDate == null);
            foreach (var zimmet in zimmetler)
            {
                zimmet.EndDate = DateTime.UtcNow;
                await _assignmentDal.UpdateAsync(zimmet);
            }

            var eskiAd = user.FullName;
            var eskiEposta = user.Email;

            user.IsActive = false;
            user.FullName = AnonimAd;
            user.Email = "silinmis-" + Guid.NewGuid().ToString("N") + "@garajim.local";
            user.PasswordHash = Array.Empty<byte>();
            user.PasswordSalt = Array.Empty<byte>();
            user.DogrulamaKodHash = null;
            user.SifirlamaKodHash = null;
            user.SilmeKodHash = null;
            user.SifreDegisimTarihi = DateTime.UtcNow;
            await _userDal.UpdateAsync(user);

            await SahipleriBilgilendirAsync(user.CompanyId, eskiAd, eskiEposta);

            return new SuccessResult(Messages.UyeHesabiSilindi);
        }

        private async Task SahipleriBilgilendirAsync(int companyId, string ad, string eposta)
        {
            var sahipler = await _userDal.GetListAsync(u => u.Role == CompanyRole.Owner && u.IsActive);

            foreach (var sahip in sahipler)
            {
                try
                {
                    await _emailSender.SendAsync(
                        sahip.Email,
                        "Garajım | Ekip üyesi hesabını sildi",
                        ad + " (" + eposta + ") hesabını sildi. Zimmetli aracı varsa zimmet kapatıldı, "
                        + "araç ve kayıtlar şirkette kaldı.");
                }
                catch (Exception hata)
                {
                    _logger.LogError(hata, "Üye silme bildirimi gönderilemedi: {Alici}", sahip.Email);
                }
            }
        }

        private async Task KodTemizleAsync(AppUser user)
        {
            user.SilmeKodHash = null;
            user.SilmeKodSonTarih = null;
            user.SilmeDenemeSayisi = 0;
            await _userDal.UpdateAsync(user);
        }
    }
}
