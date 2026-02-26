using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UniRide.DAL.Data;
using UniRide.DAL.Models;
using UniRide.DAL.Models.Enums;
using UniRide.PL.DTO.Auth;

namespace UniRide.PL.Controllers
{
    [Route("api/driver-auth")]
    [ApiController]
    public class DriverAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DriverAuthController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: /api/driver-auth/register
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromForm] DriverRegisterRequestDto request)
        {
            // ====== 1) Validations ======
            if (!request.AgreeToTerms)
                return BadRequest(new { message = "You must agree to the Terms and Privacy Policy." });

            // ConfirmPassword already validated by [Compare] in DTO, بس نخلي تحقق إضافي واضح
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Password and Confirm Password do not match." });

            // الملفات لازم تكون موجودة (لأنه شاشة السائق فيها uploads)
            if (request.DriverLicenseImage == null ||
                request.VehicleRegistrationImage == null ||
                request.DriverPhoto == null)
            {
                return BadRequest(new { message = "Driver license, vehicle registration, and driver photo are required." });
            }

            // منع تكرار Email
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new { message = "Email already exists." });

            // منع تكرار Phone (إذا دخلته)
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.Phone == request.Phone);
                if (phoneExists)
                    return BadRequest(new { message = "Phone already exists." });
            }

            // ====== 2) Upload Files ======
            // بنخزن داخل wwwroot/uploads/...
            string licenseUrl = await SaveUploadAsync(request.DriverLicenseImage, "uploads/driver_licenses");
            string registrationUrl = await SaveUploadAsync(request.VehicleRegistrationImage, "uploads/vehicle_registrations");
            string photoUrl = await SaveUploadAsync(request.DriverPhoto, "uploads/driver_photos");

            // ====== 3) Create User ======
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = HashPassword(request.Password),
                Role = UserRole.Driver,
                Status = AccountStatus.Active // إذا بدكم Pending للسائق عدّليها
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // ====== 4) Create Driver Profile ======
            var driverProfile = new DriverProfile
            {
                UserId = user.Id,
                LicenseNumber = request.DriverLicenseNumber,
                VehiclePlate = request.VehiclePlateNumber,
                VehicleModel = request.VehicleModel,
                DriverLicenseImageUrl = licenseUrl,
                VehicleRegistrationImageUrl = registrationUrl,
                DriverPhotoUrl = photoUrl,
                AvailabilityStatus = DriverAvailabilityStatus.Inactive,
                IsVerified = false,
                RejectionReason = null
            };

            _context.DriverProfiles.Add(driverProfile);
            await _context.SaveChangesAsync();

            // ====== 5) Response ======
            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = driverProfile.IsVerified,
                Message = "Driver registered successfully. Waiting for supervisor verification."
            });
        }

        private async Task<string> SaveUploadAsync(IFormFile file, string relativeFolder)
        {
            // مثال: relativeFolder = "uploads/driver_licenses"
            string root = _env.WebRootPath;

            string folderPath = Path.Combine(root, relativeFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string ext = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid():N}{ext}";
            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // يرجع مسار يوصل له المتصفح/Flutter عبر UseStaticFiles
            return "/" + relativeFolder.TrimEnd('/') + "/" + fileName;
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}