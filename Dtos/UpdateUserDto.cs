namespace Trnkt.Dtos
{
    public class UpdateUserDto
    {
        public string Email { get; set; }
        public string NewEmail { get; set; }
        public string NewUserName { get; set; }
        public string NewPasswordHash { get; set; }
    }
}