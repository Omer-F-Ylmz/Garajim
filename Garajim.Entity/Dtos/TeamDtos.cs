using Garajim.Entity.Enums;

namespace Garajim.Entity.Dtos
{
    public class TeamMemberCreateDto
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public CompanyRole Role { get; set; }
    }

    public class TeamMemberRoleDto
    {
        public CompanyRole Role { get; set; }
    }

    public class TeamMemberDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public CompanyRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SurucuBelgeSatiriDto
    {
        public int EvrakId { get; set; }
        public string EvrakTuru { get; set; }
        public string EvrakAdi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public string Durum { get; set; }
        public int KalanGun { get; set; }
    }

    public class SurucuBelgeDto
    {
        public int UserId { get; set; }
        public string AdSoyad { get; set; }
        public string Eposta { get; set; }
        public string Rol { get; set; }
        public string EnKotuDurum { get; set; }
        public List<SurucuBelgeSatiriDto> Belgeler { get; set; } = new List<SurucuBelgeSatiriDto>();
    }

    public class TeamMemberCreatedDto

    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public CompanyRole Role { get; set; }
        public string TemporaryPassword { get; set; }
    }
}
