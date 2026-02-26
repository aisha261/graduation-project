namespace UniRide.PL.DTO.Auth
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public bool? IsVerified { get; set; }
        public string Token { get; set; } 
        public DateTime ExpiresAtUtc { get; set; }

        public string Message { get; set; } = "Success";
    }
}
