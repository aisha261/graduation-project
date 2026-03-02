namespace UniRide.PL.DTO.Auth
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        public string Role { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public bool? IsVerified { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }

        public string Message { get; set; } = "Success";
    }
}
