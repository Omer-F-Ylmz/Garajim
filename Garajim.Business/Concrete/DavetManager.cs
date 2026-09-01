using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Davetler;
using Garajim.Business.Concrete.Planlar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class DavetManager : IDavetService
    {
        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly PlanKurallari _planKurallari;
        private readonly IConfiguration _configuration;

        public DavetManager(IUserDal userDal, ICompanyDal companyDal, PlanKurallari planKurallari, IConfiguration configuration)
        {
            _userDal = userDal;
            _companyDal = companyDal;
            _planKurallari = planKurallari;
            _configuration = configuration;
        }

        public async Task<IDataResult<DavetDurumDto>> GetDurumAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user == null)
                return new ErrorDataResult<DavetDurumDto>(Messages.UserNotFound);

            if (user.Role == CompanyRole.Driver)
                return new ErrorDataResult<DavetDurumDto>(Messages.AuthorizationDenied);

            var company = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            if (company == null)
                return new ErrorDataResult<DavetDurumDto>(Messages.UserNotFound);

            if (string.IsNullOrWhiteSpace(company.DavetKodu))
            {
                company.DavetKodu = await BenzersizKodAsync();
                await _companyDal.UpdateAsync(company);
            }

            var davetliler = await _companyDal.GetDavetlilerAsync(company.Id);
            var davetEden = company.DavetEdenCompanyId == null
                ? null
                : await _companyDal.GetAsync(c => c.Id == company.DavetEdenCompanyId.Value);

            var durum = new DavetDurumDto
            {
                Kod = company.DavetKodu,
                PaylasimBaglantisi = Taban() + "/?davet=" + company.DavetKodu,
                DavetSayisi = davetliler.Count,
                OdulGun = company.OdulGun,
                DavetEden = davetEden?.Name,
                Davetliler = davetliler.Select(d => new DavetSatiriDto { SirketAdi = d.Name, KatilmaTarihi = d.CreatedAt }).ToList()
            };

            return new SuccessDataResult<DavetDurumDto>(durum);
        }

        private async Task<string> BenzersizKodAsync()
        {
            for (var deneme = 0; deneme < 10; deneme++)
            {
                var aday = DavetKoduUretici.Uret();
                if (!await _companyDal.DavetKoduVarMiAsync(aday))
                {
                    return aday;
                }
            }

            throw new InvalidOperationException("Benzersiz davet kodu üretilemedi.");
        }

        private string Taban()
        {
            var taban = _configuration["App:BaseUrl"];
            return string.IsNullOrWhiteSpace(taban) ? string.Empty : taban.TrimEnd('/');
        }
    }
}
