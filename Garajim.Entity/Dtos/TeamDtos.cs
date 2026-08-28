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

    public class TeamMemberCreatedDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public CompanyRole Role { get; set; }
        public string TemporaryPassword { get; set; }
    }
}
