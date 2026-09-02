using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Davetler;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Core.Utilities.Security;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Garajim.Business.Concrete
{
    public class AuthManager : IAuthService
    {
        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IKodGonderimSayaci _gonderimSayaci;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthManager> _logger;

        public AuthManager(
            IUserDal userDal,
            ICompanyDal companyDal,
            IConfiguration configuration,
            IEmailSender emailSender,
            IKodGonderimSayaci gonderimSayaci,
            IUnitOfWork unitOfWork,
            ILogger<AuthManager> logger = null)
        {
            _userDal = userDal;
            _companyDal = companyDal;
            _configuration = configuration;
            _emailSender = emailSender;
            _gonderimSayaci = gonderimSayaci;
            _unitOfWork = unitOfWork;
            _logger = logger ?? NullLogger<AuthManager>.Instance;
        }

        public async Task<IDataResult<KayitSonucuDto>> RegisterAsync(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return new ErrorDataResult<KayitSonucuDto>(Messages.InvalidValue);
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _userDal.ExistsForRegistrationAsync(email))
                return new ErrorDataResult<KayitSonucuDto>(Messages.EmailAlreadyExists);
            Company davetEden = null;
            var davetKodu = DavetKoduUretici.Normalize(dto.DavetKodu);
            if (davetKodu != null)
            {
                davetEden = await _companyDal.GetByDavetKoduAsync(davetKodu);
                if (davetEden == null)
                    return new ErrorDataResult<KayitSonucuDto>(Messages.DavetKoduGecersiz);
            }
            HashingHelper.CreatePasswordHash(dto.Password, out var passwordHash, out var passwordSalt);
            var fullName = Kirp(dto.FullName, 100);
            var companyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? fullName : dto.CompanyName.Trim();
            var company = new Company
            {
                Name = Kirp(companyName, 150),
                PlanType = PlanType.Bireysel,
                DavetEdenCompanyId = davetEden?.Id,
                CreatedAt = DateTime.UtcNow
            };
            await using var islem = await _unitOfWork.BeginTransactionAsync();

            await _companyDal.AddAsync(company);
            var user = new AppUser
            {
                CompanyId = company.Id,
                Role = CompanyRole.Owner,
                IsActive = true,
                Email = email,
                FullName = fullName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };
            await _userDal.AddAsync(user);

            await _unitOfWork.CommitAsync();

            await KodUretVeGonderAsync(user);

            return new SuccessDataResult<KayitSonucuDto>(
                new KayitSonucuDto { DogrulamaGerekli = true, Email = user.Email },
                Messages.DogrulamaKoduGonderildi);
        }

        public async Task<IDataResult<TokenDto>> DogrulaAsync(DogrulaDto dto)
        {
            var email = (dto?.Email ?? string.Empty).Trim().ToLowerInvariant();
            var kod = (dto?.Kod ?? string.Empty).Trim();

            if (email.Length == 0 || kod.Length != DogrulamaKodu.Uzunluk)
                return new ErrorDataResult<TokenDto>(Messages.DogrulamaKoduGecersiz);

            var user = await _userDal.GetForAuthenticationAsync(email);
            if (user == null || user.EmailDogrulandi || string.IsNullOrWhiteSpace(user.DogrulamaKodHash))
                return new ErrorDataResult<TokenDto>(Messages.DogrulamaKoduGecersiz);

            if (user.DogrulamaKodSonTarih == null || user.DogrulamaKodSonTarih < DateTime.UtcNow)
            {
                await KodTemizleAsync(user);
                return new ErrorDataResult<TokenDto>(Messages.DogrulamaKoduGecersiz);
            }

            if (!DogrulamaKodu.Esit(kod, user.DogrulamaKodHash))
            {
                user.DogrulamaDenemeSayisi++;
                if (user.DogrulamaDenemeSayisi >= DogrulamaKodu.MaxDeneme)
                {
                    user.DogrulamaKodHash = null;
                    user.DogrulamaKodSonTarih = null;
                }

                await _userDal.UpdateAsync(user);
                return new ErrorDataResult<TokenDto>(Messages.DogrulamaKoduGecersiz);
            }

            user.EmailDogrulandi = true;
            user.DogrulamaKodHash = null;
            user.DogrulamaKodSonTarih = null;
            user.DogrulamaDenemeSayisi = 0;
            await _userDal.UpdateAsync(user);

            var sirket = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            return new SuccessDataResult<TokenDto>(CreateTokenDto(user, sirket?.Name), Messages.EmailDogrulandiMesaji);
        }

        public async Task<IResult> KodGonderAsync(KodGonderDto dto)
        {
            var email = (dto?.Email ?? string.Empty).Trim().ToLowerInvariant();
            var yanit = new SuccessResult(Messages.KodGonderimYaniti);

            if (email.Length == 0)
                return yanit;

            var user = await _userDal.GetForAuthenticationAsync(email);
            if (user == null || user.EmailDogrulandi)
                return yanit;

            if (user.SonKodGonderim != null &&
                user.SonKodGonderim.Value.AddSeconds(DogrulamaKodu.GonderimAraligiSaniye) > DateTime.UtcNow)
                return yanit;

            if (!_gonderimSayaci.IzinVer(email))
                return yanit;

            await KodUretVeGonderAsync(user);
            return yanit;
        }

        private async Task KodUretVeGonderAsync(AppUser user)
        {
            var kod = DogrulamaKodu.Uret();

            user.DogrulamaKodHash = DogrulamaKodu.Hashle(kod);
            user.DogrulamaKodSonTarih = DateTime.UtcNow.AddMinutes(DogrulamaKodu.GecerlilikDakika);
            user.DogrulamaDenemeSayisi = 0;
            user.SonKodGonderim = DateTime.UtcNow;
            await _userDal.UpdateAsync(user);

            _gonderimSayaci.Say(user.Email);

            try
            {
                await _emailSender.SendAsync(user.Email, DogrulamaKodu.EpostaKonusu, DogrulamaKodu.EpostaGovdesi(kod));
            }
            catch (Exception hata)
            {
                _logger.LogError(hata, "Doğrulama kodu e-postası gönderilemedi: {Alici}", user.Email);
            }
        }

        private async Task KodTemizleAsync(AppUser user)
        {
            user.DogrulamaKodHash = null;
            user.DogrulamaKodSonTarih = null;
            await _userDal.UpdateAsync(user);
        }

        public async Task<IDataResult<TokenDto>> LoginAsync(LoginDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _userDal.GetForAuthenticationAsync(email);
            if (user == null || !HashingHelper.VerifyPasswordHash(dto.Password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
                return new ErrorDataResult<TokenDto>(Messages.InvalidCredentials);
            if (!user.IsActive)
                return new ErrorDataResult<TokenDto>(Messages.UserInactive);
            if (!user.EmailDogrulandi)
                return new ErrorDataResult<TokenDto>(Messages.EmailDogrulanmadi);
            var company = await _companyDal.GetAsync(c => c.Id == user.CompanyId);
            return new SuccessDataResult<TokenDto>(CreateTokenDto(user, company?.Name), Messages.LoginSuccess);
        }

        private static string Kirp(string metin, int uzunluk)
        {
            var kirpik = (metin ?? string.Empty).Trim();
            return kirpik.Length > uzunluk ? kirpik.Substring(0, uzunluk) : kirpik;
        }

        private TokenDto CreateTokenDto(AppUser user, string companyName)
        {
            var token = JwtTokenHelper.CreateToken(
                user.Id,
                user.CompanyId,
                user.Role.ToString(),
                user.Email,
                user.FullName,
                _configuration["Jwt:Key"],
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                int.Parse(_configuration["Jwt:ExpireDays"]));
            return new TokenDto
            {
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                CompanyName = companyName
            };
        }
    }
}
