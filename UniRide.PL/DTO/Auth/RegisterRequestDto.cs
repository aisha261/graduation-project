using System.ComponentModel.DataAnnotations;

namespace UniRide.PL.DTO.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        public string Role { get; set; } = null!;
        [Required, StringLength(100)]
        public string FullName { get; set; } = null!;
        [Required]
        public string UniversityId { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; } = null!;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = null!;
    }
}