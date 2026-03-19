using FitnessClub.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitnessClub.Core.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expireDays;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSettings:SecretKey"];
            _issuer = configuration["JwtSettings:Issuer"];
            _audience = configuration["JwtSettings:Audience"];
            _expireDays = configuration.GetValue<int>("JwtSettings:ExpireDays", 7);

            // Проверка что настройки загружены
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new ArgumentNullException("JwtSettings:SecretKey не настроен");
            }
        }

        public string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            };

            var key = Encoding.UTF8.GetBytes(_secretKey);
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256Signature
            );

            // Создать токен
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expireDays), 
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) 
            { 
               return null; 
            }

            try
            {
                // Получаем _secretKey путем преобразования в объект, строки конфигурации - "tvoi-sekretniy-key-min-32-simvola"
                var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
                var securityKey = new SymmetricSecurityKey(keyBytes);

                // Валидация
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = securityKey,

                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };

                // Обработчик токенов
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(
                    token,
                    validationParameters,
                    out var validatedToken
                );

                if (validatedToken is not JwtSecurityToken jwtToken)
                    return null;

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                Console.WriteLine("Токен просрочен");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                Console.WriteLine("Неверная подпись токена");
                return null;
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                Console.WriteLine("Неверный издатель токена");
                return null;
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                Console.WriteLine("Неверная аудитория токена");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                Console.WriteLine($"Ошибка безопасности токена: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка валидации токена: {ex.Message}");
                return null;
            }
        }
    }
}
