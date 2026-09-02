using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Core.Multitenancy;
using Garajim.Entity.Concrete;
using Microsoft.Extensions.Configuration;
using Garajim.Dal.Abstract;
using Garajim.Entity.Enums;

namespace Garajim.Business.Jobs
{
    public class ReminderNotificationJob
    {
        private const int DueWithinDays = 7;
        private const int RenotifyAfterDays = 3;

        private readonly ICompanyDal _companyDal;
        private readonly IReminderDal _reminderDal;
        private readonly TenantContext _tenantContext;
        private readonly ILogger<ReminderNotificationJob> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IEvrakDal _evrakDal;
        private readonly IUserDal _userDal;
        private readonly IVehicleAssignmentDal _assignmentDal;
        private readonly EvrakKurallari _evrakKurallari;

        public ReminderNotificationJob(ICompanyDal companyDal, IReminderDal reminderDal, TenantContext tenantContext, IEmailSender emailSender, IConfiguration configuration,
            IEvrakDal evrakDal, IUserDal userDal, IVehicleAssignmentDal assignmentDal, EvrakKurallari evrakKurallari,
            ILogger<ReminderNotificationJob> logger = null)
        {
            _logger = logger ?? NullLogger<ReminderNotificationJob>.Instance;
            _companyDal = companyDal;
            _reminderDal = reminderDal;
            _tenantContext = tenantContext;
            _emailSender = emailSender;
            _configuration = configuration;
            _evrakDal = evrakDal;
            _userDal = userDal;
            _assignmentDal = assignmentDal;
            _evrakKurallari = evrakKurallari;
        }

        public async Task RunAsync()
        {
            var companies = await _companyDal.GetListAsync();

            try
            {
                foreach (var company in companies)
                {
                    try
                    {
                        _tenantContext.SetCompany(company.Id);
                        await SirketIcinCalistirAsync();
                        await EvraklariTaraAsync();
                    }
                    catch (Exception hata)
                    {
                        _logger.LogError(hata, "Hatırlatma taraması {SirketId} numaralı şirkette başarısız oldu, diğer şirketlerle devam ediliyor.", company.Id);
                    }
                }
            }
            finally
            {
                _tenantContext.Clear();
            }
        }

        private async Task EvraklariTaraAsync()
        {
            var now = DateTime.UtcNow;
            var uyariGunleri = _evrakKurallari.UyariGunleri();
            var enGenisPencere = uyariGunleri.Max();
            var dueLimit = now.Date.AddDays(enGenisPencere);
            var notifyBefore = now.AddDays(-RenotifyAfterDays);

            var dueList = await _evrakDal.GetDueListAsync(dueLimit, notifyBefore);

            foreach (var item in dueList)
            {
                var kalanGun = (int)(item.BitisTarihi.Date - now.Date).TotalDays;
                if (kalanGun > 0 && !uyariGunleri.Any(g => kalanGun <= g))
                {
                    continue;
                }

                var alicilar = await EvrakAlicilariAsync(item);
                if (alicilar.Count == 0)
                {
                    continue;
                }

                if (!await _evrakDal.TryClaimNotificationAsync(item.EvrakId, now, notifyBefore))
                {
                    continue;
                }

                var evrakAdi = EvrakAdlari.Ad(item.EvrakTuru);
                var konuBasi = item.Plate ?? "Sürücü belgesi";
                var subject = $"Garajım | {konuBasi} - {evrakAdi} hatırlatması";

                foreach (var alici in alicilar)
                {
                    var body = EvrakGovdesi(alici.FullName, item.Plate, evrakAdi, item.BitisTarihi, kalanGun);

                    try
                    {
                        await _emailSender.SendAsync(alici.Email, subject, body);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private string EvrakGovdesi(string adSoyad, string plaka, string evrakAdi, DateTime bitis, int kalanGun)
        {
            var konu = plaka == null ? evrakAdi : $"{plaka} plakalı aracınızın {evrakAdi}";

            string govde;
            if (kalanGun > 0)
                govde = $"Merhaba {adSoyad}, {konu} bitiş tarihine {kalanGun} gün kaldı ({bitis:dd.MM.yyyy}).";
            else if (kalanGun == 0)
                govde = $"Merhaba {adSoyad}, {konu} bitiş tarihi bugün ({bitis:dd.MM.yyyy}).";
            else
                govde = $"Merhaba {adSoyad}, {konu} bitiş tarihi {-kalanGun} gün önce geçti ({bitis:dd.MM.yyyy}).";

            var baseUrl = _configuration["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                govde += $" Kayıtlarınız: {baseUrl.TrimEnd('/')}";

            return govde;
        }

        private async Task<List<AppUser>> EvrakAlicilariAsync(EvrakDueDto item)
        {
            var kullanicilar = await _userDal.GetListAsync(u => u.IsActive);
            var alicilar = new List<AppUser>();

            if (item.UserId != null)
            {
                var sahibi = kullanicilar.FirstOrDefault(u => u.Id == item.UserId);
                if (sahibi != null)
                {
                    alicilar.Add(sahibi);
                }

                alicilar.AddRange(kullanicilar.Where(u => u.Role == CompanyRole.Owner));
            }
            else if (item.VehicleId != null)
            {
                alicilar.AddRange(kullanicilar.Where(u => u.Role != CompanyRole.Driver));

                var aktifZimmet = await _assignmentDal.GetActiveByVehicleAsync(item.VehicleId.Value);
                if (aktifZimmet != null)
                {
                    var surucu = kullanicilar.FirstOrDefault(u => u.Id == aktifZimmet.UserId);
                    if (surucu != null)
                    {
                        alicilar.Add(surucu);
                    }
                }
            }

            return alicilar.GroupBy(a => a.Id).Select(g => g.First()).ToList();
        }

        private async Task SirketIcinCalistirAsync()
        {
            var now = DateTime.UtcNow;
            var dueLimit = now.Date.AddDays(DueWithinDays);
            var notifyBefore = now.AddDays(-RenotifyAfterDays);
            var dueList = await _reminderDal.GetDueListAsync(dueLimit, notifyBefore);
            foreach (var item in dueList)
            {
                var claimed = await _reminderDal.TryClaimNotificationAsync(item.ReminderId, now, notifyBefore);
                if (!claimed)
                    continue;
                var daysLeft = (item.DueDate.Date - now.Date).Days;
                var typeName = GetTypeName(item.Type);
                var subject = $"Garajım | {item.Plate} - {typeName} hatırlatması";
                string body;
                if (daysLeft > 0)
                    body = $"Merhaba {item.FullName}, {item.Plate} plakalı aracınızın {typeName} tarihine {daysLeft} gün kaldı ({item.DueDate:dd.MM.yyyy}).";
                else if (daysLeft == 0)
                    body = $"Merhaba {item.FullName}, {item.Plate} plakalı aracınızın {typeName} tarihi bugün ({item.DueDate:dd.MM.yyyy}).";
                else
                    body = $"Merhaba {item.FullName}, {item.Plate} plakalı aracınızın {typeName} tarihi {-daysLeft} gün önce geçti ({item.DueDate:dd.MM.yyyy}).";
                var baseUrl = _configuration["App:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    body += $" Kayıtlarınız: {baseUrl.TrimEnd('/')}";
                try
                {
                    await _emailSender.SendAsync(item.Email, subject, body);
                }
                catch
                {
                }
            }
        }

        private static string GetTypeName(ReminderType type)
        {
            return type switch
            {
                ReminderType.Muayene => "araç muayenesi",
                ReminderType.TrafikSigortasi => "trafik sigortası",
                ReminderType.Kasko => "kasko yenileme",
                ReminderType.EgzozEmisyon => "egzoz emisyon ölçümü",
                ReminderType.Mtv => "MTV ödeme",
                ReminderType.PeriyodikBakim => "periyodik bakım",
                ReminderType.LastikDegisimi => "lastik değişimi",
                _ => "hatırlatma"
            };
        }
    }
}
