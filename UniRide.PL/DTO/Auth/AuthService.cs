using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UniRide.DAL.Data;

namespace UniRide.PL.DTO.Auth
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var input = dto.EmailOrPhone.Trim();

            // Email OR Phone
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == input || (u.Phone != null && u.Phone == input));

            if (user == null)
                return null;

            // Verify password (BCrypt)
            var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!ok)
                return null;

            var durationMinutes = int.Parse(_config["Jwt:DurationInMinutes"]!);
            if (dto.RememberMe)
                durationMinutes = Math.Max(durationMinutes, 60 * 24 * 7); // أسبوع

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(durationMinutes);
            var token = GenerateJwtToken(user, expiresAtUtc);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                Message = "Success"
            };
        }

        private string GenerateJwtToken(dynamic user, DateTime expiresAtUtc)
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