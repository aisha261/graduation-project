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
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            // 1) تحقق: الإيميل موجود؟
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new { message = "Email already exists" });

            // 2) تحقق: رقم الجامعة موجود؟ (إذا عندكم حقل UniversityId في User/AcademicProfile عدّلي حسبكم)
            // إذا ما عندكم الحقل بالـ DB لسه، احذفي هذا الجزء مؤقتاً.
            // bool uniIdExists = await _context.Users.AnyAsync(u => u.UniversityId == request.UniversityId);

            // 3) حددي الدور من التاب (Student/Driver)
            // افترضنا عندكم enum UserRole فيه Student و Driver
            UserRole role;
            if (!Enum.TryParse(request.Role, ignoreCase: true, out role))
                return BadRequest(new { message = "Invalid role" });

            // 4) أنشئي المستخدم (Entity)
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = HashPassword(request.Password),
                Role = role,
                Status = AccountStatus.Active // أو Pending إذا بدكم
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5) إذا الدور Driver: ممكن تعملوا DriverProfile مباشرة (حسب نظامكم)
            bool? isVerified = null;

            if (role == UserRole.Driver)
            {
                var driverProfile = new DriverProfile
                {
                    UserId = user.Id,
                    LicenseNumber = "PENDING",          // مؤقت لحين شاشة رفع البيانات
                    VehiclePlate = "PENDING",
                    VehicleModel = "PENDING",
                    DriverPhotoUrl = "PENDING",
                    AvailabilityStatus = DriverAvailabilityStatus.Inactive,
                    IsVerified = false
                };

                _context.DriverProfiles.Add(driverProfile);
                await _context.SaveChangesAsync();

                isVerified = driverProfile.IsVerified;
            }

            // 6) Response DTO
            var response = new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = isVerified,
                Message = "Registered successfully"
            };

            return Ok(response);
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == request.EmailOrPhone ||
                u.Phone == request.EmailOrPhone
            );

            if (user == null)
                return Unauthorized(new { message = "Invalid email/phone or password" });

            if (user.PasswordHash != HashPassword(request.Password))
                return Unauthorized(new { message = "Invalid email/phone or password" });

            // إذا كان Driver: رجّعي حالة التحقق
            bool? isVerified = null;
            if (user.Role == UserRole.Driver)
            {
                var driver = await _context.DriverProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                isVerified = driver?.IsVerified;
            }

            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = isVerified,
                Message = "Logged in successfully"
            });
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}