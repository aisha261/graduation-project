using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UniRide.PL.DTO.Auth
{
    public class DriverRegisterRequestDto
    {

        [Required, StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        public string? Phone { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; } = null!;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;



        [Required]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        public string VehiclePlateNumber { get; set; } = null!;

        [Required]
        public string VehicleType { get; set; } = null!;

        [Required, Range(1, 50)]
        public int NumberOfSeats { get; set; }



        public IFormFile? DriverLicenseImage { get; set; }
        public IFormFile? VehicleRegistrationImage { get; set; }
        public IFormFile? DriverPhoto { get; set; }



        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms.")]
        public bool AgreeToTerms { get; set; } = false;
    }
}