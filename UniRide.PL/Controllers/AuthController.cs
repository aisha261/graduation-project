using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            // 1) تحقق: الإيميل موجود؟
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new { message = "Email already exists" });

            // 2) (اختياري) تحقق الهاتف لو بدك تمنعي تكرار رقم الهاتف
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.Phone != null && u.Phone == request.Phone);
                if (phoneExists)
                    return BadRequest(new { message = "Phone already exists" });
            }

            // 3) Role
            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
                return BadRequest(new { message = "Invalid role" });


            // 4) ✅ هنا نمنع تسجيل Driver من هذا الـ endpoint
            // لأن تسجيل السائق له endpoint خاص (multipart + uploads)
            if (role == UserRole.Driver)
                return BadRequest(new { message = "Use /api/driver-auth/register for driver registration." });

            // 4) إنشاء User
            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role,
                Status = AccountStatus.Active // أو Pending إذا بدكم
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if ((role == UserRole.Student || role == UserRole.Doctor) && !string.IsNullOrWhiteSpace(request.UniversityId))
            {
                _context.AcademicProfiles.Add(new AcademicProfile
                {
                    UserId = user.Id,
                    UniversityId = request.UniversityId.Trim(),
                    RewardPointsTotal = 0
                });
                await _context.SaveChangesAsync();
            }

            // 6) Response (بدون توكن عادة بالتسجيل)
            var response = new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = null,
                Token = string.Empty,
                ExpiresAtUtc = DateTime.UtcNow,
                Message = "Registered successfully"
            };

            return Ok(response);
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var input = request.EmailOrPhone?.Trim();

            if (string.IsNullOrWhiteSpace(input))
                return BadRequest(new { message = "EmailOrPhone is required" });

            // 1) ابحث بالإيميل أو الهاتف
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == input || (u.Phone != null && u.Phone == input));

            if (user == null)
                return Unauthorized(new { message = "Invalid email/phone or password" });

            // 2) تحقق الباسورد (SHA256)
            bool ok = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!ok)
                return Unauthorized(new { message = "Invalid email/phone or password" });
            // 3) إذا كان Driver: رجّع IsVerified
            bool? isVerified = null;
            if (user.Role == UserRole.Driver)
            {
                var driver = await _context.DriverProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                isVerified = driver?.IsVerified;
            }

            // 4) Token + Expiry (RememberMe)
            int minutes = int.Parse(_config["Jwt:DurationInMinutes"]!);
            if (request.RememberMe)
                minutes = Math.Max(minutes, 60 * 24 * 7); // أسبوع

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);
            var token = GenerateJwtToken(user, expiresAtUtc);


            // 5) منطق الحالة حسب الدور
            // Student/Doctor: لازم Active
            if (user.Role != UserRole.Driver && user.Status != AccountStatus.Active)
                return Unauthorized(new { message = "Your account is not active." });

            // Driver: لو Pending مسموح الدخول لكن رسالة انتظار
            if (user.Role == UserRole.Driver && user.Status == AccountStatus.Pending)
            {

                return Ok(new AuthResponseDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    Status = user.Status.ToString(),
                    IsVerified = isVerified,
                    Token = token,
                    ExpiresAtUtc = expiresAtUtc,
                    Message = "Your account is pending supervisor verification."
                });
            }

            // Driver: لو Blocked ممنوع
            if (user.Role == UserRole.Driver && user.Status == AccountStatus.Blocked)
                return Unauthorized(new { message = "Your account is blocked." });


            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = isVerified,
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                Message = "Logged in successfully"
            });
        }

        // =========================
        // GET: /api/auth/me
        // ✅ يرجع بيانات المستخدم الحالي من التوكن
        // =========================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            // نقرأ userId من الـ claim (NameIdentifier)
            var idStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "User not found." });

            bool? isVerified = null;
            if (user.Role == UserRole.Driver)
            {
                var driver = await _context.DriverProfiles.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == user.Id);
                isVerified = driver?.IsVerified;
            }

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Phone,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                IsVerified = isVerified
            });
        }



        // ===================== Helpers =====================

        private string GenerateJwtToken(User user, DateTime expiresAtUtc)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // (اختياري) لو بدك Status كـ claim
            // claims.Add(new Claim("status", user.Status.ToString()));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        
    }
}