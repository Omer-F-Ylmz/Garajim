namespace Garajim.Entity.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class TokenDto
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    }
}
