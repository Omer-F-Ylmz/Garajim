using System.Security.Cryptography;
using Garajim.Business.Abstract;
using Garajim.Business.Concrete.Evraklar;
using Garajim.Business.Constants;
using Garajim.Core.Utilities.Results;
using Garajim.Core.Utilities.Security;
using Garajim.Dal.Abstract;
using Garajim.Entity.Concrete;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;

namespace Garajim.Business.Concrete
{
    public class TeamManager : ITeamService
    {
        private const string SifreAlfabesi = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        private const int GeciciSifreUzunlugu = 12;

        private readonly IUserDal _userDal;
        private readonly IEvrakDal _evrakDal;

        public TeamManager(IUserDal userDal, IEvrakDal evrakDal)
        {
            _userDal = userDal;
            _evrakDal = evrakDal;
        }

        public async Task<IDataResult<List<TeamMemberDto>>> GetListAsync(int currentUserId)
        {
            var current = await _userDal.GetAsync(u => u.Id == currentUserId);
            if (current == null)
                return new ErrorDataResult<List<TeamMemberDto>>(Messages.UserNotFound);

            var users = await _userDal.GetListAsync();
            var list = users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Select(MapToDto)
                .ToList();

            return new SuccessDataResult<List<TeamMemberDto>>(list);
        }

        public async Task<IDataResult<List<SurucuBelgeDto>>> GetBelgelerAsync(int currentUserId)
        {
            var current = await _userDal.GetAsync(u => u.Id == currentUserId);
            if (current == null)
                return new ErrorDataResult<List<SurucuBelgeDto>>(Messages.UserNotFound);

            var users = await _userDal.GetListAsync(u => u.IsActive);
            var idler = users.Select(u => u.Id).ToList();

            var bugun = Saat.BugunTr();
            var evraklar = idler.Count == 0
                ? new List<EvrakKaydi>()
                : await _evrakDal.GetListAsync(e => e.Aktif && e.UserId != null && idler.Contains(e.UserId.Value));

            var liste = users.Select(user =>
            {
                var satirlar = evraklar
                    .Where(e => e.UserId == user.Id)
                    .OrderBy(e => e.BitisTarihi)
                    .Select(e => new SurucuBelgeSatiriDto
                    {
                        EvrakId = e.Id,
                        EvrakTuru = e.EvrakTuru.ToString(),
                        EvrakAdi = EvrakAdlari.Ad(e.EvrakTuru),
                        BitisTarihi = e.BitisTarihi,
                        Durum = EvrakKurallari.Durum(e.BitisTarihi, bugun),
                        KalanGun = (int)(e.BitisTarihi.Date - bugun).TotalDays
                    })
                    .ToList();

                return new SurucuBelgeDto
                {
                    UserId = user.Id,
                    AdSoyad = user.FullName,
                    Eposta = user.Email,
                    Rol = user.Role.ToString(),
                    EnKotuDurum = EnKotu(satirlar),
                    Belgeler = satirlar
                };
            })
            .OrderBy(u => DurumSirasi(u.EnKotuDurum))
            .ThenBy(u => u.AdSoyad)
            .ToList();

            return new SuccessDataResult<List<SurucuBelgeDto>>(liste);
        }

        private static string EnKotu(List<SurucuBelgeSatiriDto> satirlar)
        {
            if (satirlar.Any(s => s.Durum == "Gecti"))
            {
                return "Gecti";
            }

            return satirlar.Any(s => s.Durum == "Yaklasiyor") ? "Yaklasiyor" : "Iyi";
        }

        private static int DurumSirasi(string durum)
        {
            if (durum == "Gecti")
            {
                return 0;
            }

            return durum == "Yaklasiyor" ? 1 : 2;
        }

        public async Task<IDataResult<TeamMemberCreatedDto>> AddAsync(int currentUserId, TeamMemberCreateDto dto)
        {
            var current = await _userDal.GetAsync(u => u.Id == currentUserId);
            if (current == null)
                return new ErrorDataResult<TeamMemberCreatedDto>(Messages.UserNotFound);

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName) || !Enum.IsDefined(dto.Role))
                return new ErrorDataResult<TeamMemberCreatedDto>(Messages.InvalidValue);

            var email = dto.Email.Trim().ToLowerInvariant();
            if (await _userDal.ExistsForRegistrationAsync(email))
                return new ErrorDataResult<TeamMemberCreatedDto>(Messages.EmailAlreadyExists);

            var temporaryPassword = GeciciSifreUret();
            HashingHelper.CreatePasswordHash(temporaryPassword, out var passwordHash, out var passwordSalt);

            var user = new AppUser
            {
                CompanyId = current.CompanyId,
                Role = dto.Role,
                IsActive = true,
                EmailDogrulandi = true,
                GeciciSifre = true,
                Email = email,
                FullName = dto.FullName.Trim(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow
            };
            await _userDal.AddAsync(user);

            var created = new TeamMemberCreatedDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                TemporaryPassword = temporaryPassword
            };

            return new SuccessDataResult<TeamMemberCreatedDto>(created, Messages.UserAdded);
        }

        public async Task<IResult> ChangeRoleAsync(int currentUserId, int targetUserId, TeamMemberRoleDto dto)
        {
            if (currentUserId == targetUserId)
                return new ErrorResult(Messages.CannotManageSelf);

            if (!Enum.IsDefined(dto.Role))
                return new ErrorResult(Messages.InvalidValue);

            var target = await _userDal.GetAsync(u => u.Id == targetUserId);
            if (target == null)
                return new ErrorResult(Messages.UserNotFound);

            if (target.Role == CompanyRole.Owner && dto.Role != CompanyRole.Owner && await SonSahipMiAsync(target))
                return new ErrorResult(Messages.LastOwnerRequired);

            target.Role = dto.Role;
            await _userDal.UpdateAsync(target);
            return new SuccessResult(Messages.UserRoleChanged);
        }

        public async Task<IResult> DeactivateAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId)
                return new ErrorResult(Messages.CannotManageSelf);

            var target = await _userDal.GetAsync(u => u.Id == targetUserId);
            if (target == null)
                return new ErrorResult(Messages.UserNotFound);

            if (target.Role == CompanyRole.Owner && await SonSahipMiAsync(target))
                return new ErrorResult(Messages.LastOwnerRequired);

            target.IsActive = false;
            await _userDal.UpdateAsync(target);
            return new SuccessResult(Messages.UserDeactivated);
        }

        private async Task<bool> SonSahipMiAsync(AppUser target)
        {
            var owners = await _userDal.CountAsync(u => u.Role == CompanyRole.Owner && u.IsActive);
            return owners <= 1;
        }

        private static string GeciciSifreUret()
        {
            var karakterler = new char[GeciciSifreUzunlugu];
            for (var i = 0; i < karakterler.Length; i++)
            {
                karakterler[i] = SifreAlfabesi[RandomNumberGenerator.GetInt32(SifreAlfabesi.Length)];
            }

            return new string(karakterler);
        }

        private static TeamMemberDto MapToDto(AppUser user)
        {
            return new TeamMemberDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
