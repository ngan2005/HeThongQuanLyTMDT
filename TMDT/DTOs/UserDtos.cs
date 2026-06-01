namespace TMDT.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserCode { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string Avatar { get; set; }
        public int? ShopId { get; set; }
        public string? ShopName { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
    }
}
