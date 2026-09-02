using System.Text;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Planlar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class PlanManager : IPlanService
    {
        private const int MaxMesajUzunlugu = 1000;

        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IVehicleDal _vehicleDal;
        private readonly IEmailSender _emailSender;
        private readonly PlanKurallari _planKurallari;
        private readonly IConfiguration _configuration;

        public PlanManager(
            IUserDal userDal,
            ICompanyDal companyDal,
            IVehicleDal vehicleDal,
            IEmailSender emailSender,
            PlanKurallari planKurallari,
            IConfiguration configuration)
        {
            _userDal = userDal;
            _companyDal = companyDal;
            _vehicleDal = vehicleDal;
            _emailSender = emailSender;
            _planKurallari = planKurallari;
            _configuration = configuration;
        }

        public async Task<IResult> YukseltmeTalebiAsync(int userId, PlanYukseltmeTalebiDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorResult(Messages.UserNotFound);

            if (user.Role != CompanyRole.Owner)
                return new ErrorResult(Messages.AuthorizationDenied);

            if (!Enum.IsDefined(dto.IstenenPlan))
                return new ErrorResult(Messages.InvalidValue);

            var company = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (company == null)
                return new ErrorResult(Messages.UserNotFound);

            if (company.PlanType == dto.IstenenPlan)
                return new ErrorResult(Messages.PlanZatenAktif);

            var destek = (_configuration["App:DestekEposta"] ?? string.Empty).Trim();
            if (destek.Length == 0)
                return new ErrorResult(Messages.DestekEpostasiTanimsiz);

            var davetSayisi = await _companyDal.DavetSayisiAsync(company.Id);
            var aracSayisi = await _vehicleDal.CountAsync(v => v.CompanyId == company.Id);
            var limit = _planKurallari.AracLimiti(company.PlanType, company.AracLimiti, davetSayisi);

            var govde = new StringBuilder();
            govde.AppendLine("Plan yükseltme talebi");
            govde.AppendLine();
            govde.AppendLine("Şirket: " + company.Name);
            govde.AppendLine("Mevcut plan: " + company.PlanType);
            govde.AppendLine("İstenen plan: " + dto.IstenenPlan);
            govde.AppendLine("Araç: " + aracSayisi + " / " + limit);
            govde.AppendLine("Davet sayısı: " + davetSayisi);
            govde.AppendLine("Talep eden: " + user.FullName + " (" + user.Email + ")");

            var mesaj = Kirp(dto.Mesaj);
            if (mesaj != null)
            {
                govde.AppendLine();
                govde.AppendLine("Not: " + mesaj);
            }

            await _emailSender.SendAsync(destek, "Plan yükseltme talebi — " + company.Name, govde.ToString());

            return new SuccessResult(Messages.PlanTalebiAlindi);
        }

        private static string Kirp(string mesaj)
        {
            if (string.IsNullOrWhiteSpace(mesaj))
            {
                return null;
            }

            var kirpik = mesaj.Trim();
            return kirpik.Length > MaxMesajUzunlugu ? kirpik.Substring(0, MaxMesajUzunlugu) : kirpik;
        }
    }
}
