using Garajim.Business.Abstract;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Core.Utilities.Security;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.Extensions.Configuration;

namespace Garajim.Business.Concrete
{
    public class AuthManager : IAuthService
    {
        private readonly IUserDal _userDal;
        private readonly ICompanyDal _companyDal;
        private readonly IConfiguration _configuration;

        public AuthManager(IUserDal userDal, ICompanyDal companyDal, IConfiguration configuration)
        {
            _userDal = userDal;
            _companyDal = companyDal;
            _configuration = configuration;
        }

        public async Task<IDataResult<TokenDto>> RegisterAsync(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return new ErrorDataResult<TokenDto>(Messages.InvalidValue);
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _userDal.ExistsForRegistrationAsync(email))
                return new ErrorDataResult<TokenDto>(Messages.EmailAlreadyExists);
            HashingHelper.CreatePasswordHash(dto.Password, out var passwordHash, out var passwordSalt);
            var fullName = dto.FullName.Trim();
            var company = new Company
            {
                Name = fullName.Length > 150 ? fullName.Substring(0, 150) : fullName,
                PlanType = PlanType.Standart,
                CreatedAt = DateTime.UtcNow
            };
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
            return new SuccessDataResult<TokenDto>(CreateTokenDto(user), Messages.RegisterSuccess);
        }

        public async Task<IDataResult<TokenDto>> LoginAsync(LoginDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _userDal.GetForAuthenticationAsync(email);
            if (user == null || !HashingHelper.VerifyPasswordHash(dto.Password ?? string.Empty, user.PasswordHash, user.PasswordSalt))
                return new ErrorDataResult<TokenDto>(Messages.InvalidCredentials);
            if (!user.IsActive)
                return new ErrorDataResult<TokenDto>(Messages.UserInactive);
            return new SuccessDataResult<TokenDto>(CreateTokenDto(user), Messages.LoginSuccess);
        }

        private TokenDto CreateTokenDto(AppUser user)
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
            return new TokenDto { Token = token, Email = user.Email, FullName = user.FullName };
        }
    }
}
