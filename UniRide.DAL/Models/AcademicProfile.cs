using System;
using System.Collections.Generic;
using System.Text;

namespace UniRide.DAL.Models
{
    public class AcademicProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string UniversityId { get; set; } = null!;

        public string WalletId { get; set; } = null!;
        public int RewardPointsTotal { get; set; } = 0;








    }
}
