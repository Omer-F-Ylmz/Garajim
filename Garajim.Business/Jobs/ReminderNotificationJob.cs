using Garajim.Business.Abstract;
using Garajim.Core.Multitenancy;
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
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public ReminderNotificationJob(ICompanyDal companyDal, IReminderDal reminderDal, TenantContext tenantContext, IEmailSender emailSender, IConfiguration configuration)
        {
            _companyDal = companyDal;
            _reminderDal = reminderDal;
            _tenantContext = tenantContext;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task RunAsync()
        {
            var companies = await _companyDal.GetListAsync();

            foreach (var company in companies)
            {
                _tenantContext.SetCompany(company.Id);
                await SirketIcinCalistirAsync();
            }

            _tenantContext.Clear();
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
