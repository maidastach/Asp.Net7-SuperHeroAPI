using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SuperHeroAuth.Models.DTOs;
using SuperHeroAuth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SuperHeroAuth.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SuperHeroAuth.Services
{
    public class IdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AppDbContext _context;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly string _jwtExpiryTimeFrame;
        public readonly TokenValidationParameters _tokenValidationParameters;

        public IdentityService(
            IConfiguration configuration,
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            TokenValidationParameters tokenValidationParameters
            )
        {
            _userManager = userManager;
            _context = context;
            _jwtSecret = configuration.GetValue<string>("JWTSecret");
            _jwtIssuer = configuration.GetValue<string>("JWTIssuer");
            _jwtAudience = configuration.GetValue<string>("JWTAudience");
            _jwtExpiryTimeFrame = configuration.GetValue<string>("JWTExpiryTimeFrame");
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task<AuthResult> GenerateJwtToken(IdentityUser user)
        {
            // initialising the token handler
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            // fetching the secret key
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var user_role = await _context.UserRoles.FirstOrDefaultAsync(x => x.UserId== user.Id);

            var role = "User";

            if (user_role != null)
            {
                role = user_role.Role;
            }

            // creating identity
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim("UserId", user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()), // UtcNow.ToUniversalTime().ToString()),

            });

            // creating credentials
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            // creating token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = identity,
                Expires = DateTime.Now.Add(TimeSpan.Parse(_jwtExpiryTimeFrame)),
                SigningCredentials = credentials,
                Issuer = _jwtIssuer,
                Audience = _jwtAudience
            };

            // generating Access Token
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            // generating Refresh Token
            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                Token = RandomStringGenerator(22),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMinutes(1),
                IsRevoked = false,
                IsUsed = false,
                UserId = user.Id
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            // return Tokens
            return new AuthResult()
            {
                Result = true,
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
            };
        }

        public async Task<AuthResult> VerifyAndGenerateToken(TokenRequestDTO tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                _tokenValidationParameters.ValidateLifetime = false;
                var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters, out var validatedToken);
                _tokenValidationParameters.ValidateLifetime = true;

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (result == false)
                        throw new SecurityTokenException("Invalid Token!");
                }

                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.Now)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Expired Token"
                        }
                    };
                }

                var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                if (storedToken.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (storedToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Invalid Token"
                        }
                    };
                }

                if (storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Expired Token"
                        }
                    };
                }

                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);

                return await GenerateJwtToken(dbUser);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new AuthResult()
                {
                    Result = false,
                    Errors = new List<string>()
                        {
                            "Server Error"
                        }
                };
            }
        }

        private string RandomStringGenerator(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();

            return dateTimeVal;
        }
    }
}
