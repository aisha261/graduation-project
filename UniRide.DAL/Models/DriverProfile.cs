using System;
using System.Collections.Generic;
using System.Text;
using UniRide.DAL.Models.Enums;

namespace UniRide.DAL.Models
{
    public class DriverProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string LicenseNumber { get; set; } = null!;
        public string VehiclePlate { get; set; } = null!;
        public string VehicleType { get; set; } = null!;
        public int NumberOfSeats { get; set; }

        public string? DriverLicenseImageUrl { get; set; }
        public string? VehicleRegistrationImageUrl { get; set; }
        public string DriverPhotoUrl { get; set; } = null!;
        public DriverAvailabilityStatus AvailabilityStatus { get; set; }
            = DriverAvailabilityStatus.Available;
        public bool IsVerified { get; set; } = false;
        public string? RejectionReason { get; set; }









    }
}
