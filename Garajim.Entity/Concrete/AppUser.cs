using Garajim.Core.Entities;
using Garajim.Entity.Enums;

namespace Garajim.Entity.Concrete
{
    public class AppUser : IEntity
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public CompanyRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool EmailDogrulandi { get; set; }
        public string DogrulamaKodHash { get; set; }
        public DateTime? DogrulamaKodSonTarih { get; set; }
        public int DogrulamaDenemeSayisi { get; set; }
        public DateTime? SonKodGonderim { get; set; }
        public string SifirlamaKodHash { get; set; }
        public DateTime? SifirlamaKodSonTarih { get; set; }
        public int SifirlamaDenemeSayisi { get; set; }
        public DateTime? SonSifirlamaGonderim { get; set; }
        public DateTime? SifreDegisimTarihi { get; set; }
        public bool GeciciSifre { get; set; }
        public string SilmeKodHash { get; set; }
        public DateTime? SilmeKodSonTarih { get; set; }
        public int SilmeDenemeSayisi { get; set; }
        public DateTime? SonSilmeGonderim { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
