namespace Garajim.Entity.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string CompanyName { get; set; }
        public string DavetKodu { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class DogrulaDto
    {
        public string Email { get; set; }
        public string Kod { get; set; }
    }

    public class KodGonderDto
    {
        public string Email { get; set; }
    }

    public class SifreSifirlamaKodDto
    {
        public string Email { get; set; }
    }

    public class SifreSifirlaDto
    {
        public string Email { get; set; }
        public string Kod { get; set; }
        public string YeniSifre { get; set; }
    }

    public class SifreDegistirDto
    {
        public string Mevcut { get; set; }
        public string Yeni { get; set; }
    }

    public class KayitSonucuDto
    {
        public bool DogrulamaGerekli { get; set; }
        public string Email { get; set; }
    }

    public class TokenDto
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string CompanyName { get; set; }
    }
}
