using System.ComponentModel.DataAnnotations;

namespace UniRide.PL.DTO.Auth
{
    public class LoginRequestDto
    {
        [Required]
        public string EmailOrPhone { get; set; } = null!;

        [Required, MinLength(8)]
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; } = false;

    }
}
