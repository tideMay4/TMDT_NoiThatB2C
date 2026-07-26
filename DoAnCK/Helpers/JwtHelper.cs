using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace DoAnCK.Helpers
{
    public class JwtHelper
    {
        // Lấy key từ web.config
        private static readonly string SecretKey = ConfigurationManager.AppSettings["JwtSecretKey"];
        private static readonly string Issuer = ConfigurationManager.AppSettings["JwtIssuer"];

        // 1. Tạo AccessToken (Thời hạn ngắn: ví dụ 15-30 phút)
        public static string GenerateAccessToken(int maTK, string email, string vaiTro)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, maTK.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, vaiTro)
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), // AccessToken sống 30 phút
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // 2. Tạo RefreshToken (Thời hạn dài: ví dụ 7 ngày)
        public static string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString() + "-" + DateTime.UtcNow.Ticks;
        }

        // 3. Giải mã và Đọc Claims từ AccessToken
        public static ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = Issuer,
                    ValidAudience = Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                    ValidateLifetime = true, // Bật kiểm tra thời hạn
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}