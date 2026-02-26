using System;
using System.Collections.Generic;
using System.Text;
using UniRide.DAL.Models.Enums;


namespace UniRide.DAL.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        public string PasswordHash { get; set; } = null!;

        public UserRole Role { get; set; }
        public AccountStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AcademicProfile? AcademicProfile { get; set; }
        public DriverProfile? DriverProfile { get; set; }
    }
}
