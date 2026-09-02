using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NotesApp.Api.Models;
using NotesApp.Api.Repositories;

namespace NotesApp.Api.Services
{
    /// <summary>
    /// Handles registration, login, password hashing (BCrypt) and JWT issuance.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        // Work factor for BCrypt. 12 is a reasonable, current-day minimum for
        // interactive login paths; tune upward as hardware improves.
        private const int BCryptWorkFactor = 12;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
                throw new AuthValidationException("A valid email address is required.");

            if (string.IsNullOrWhiteSpace(request.DisplayName))
                throw new AuthValidationException("Display name is required.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                throw new AuthValidationException("Password must be at least 8 characters long.");

            if (await _userRepository.EmailExistsAsync(normalizedEmail))
                throw new AuthValidationException("An account with this email already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCryptWorkFactor);

            var user = await _userRepository.CreateAsync(normalizedEmail, request.DisplayName.Trim(), passwordHash);

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            // Constant-shaped failure: don't reveal whether the email exists.
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new AuthValidationException("Invalid email or password.");

            return BuildAuthResponse(user);
        }

        private AuthResponse BuildAuthResponse(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var secret = jwtSection["Secret"]
                ?? throw new InvalidOperationException("Missing 'Jwt:Secret' configuration value.");
            var issuer = jwtSection["Issuer"] ?? "NotesApp";
            var audience = jwtSection["Audience"] ?? "NotesAppClient";
            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var mins) ? mins : 120;

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("displayName", user.DisplayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse
            {
                Token = tokenString,
                ExpiresAtUtc = expiresAtUtc,
                User = UserDto.FromEntity(user)
            };
        }
    }

    /// <summary>
    /// Thrown for expected, user-facing auth validation failures (bad credentials,
    /// duplicate email, weak password). Caught explicitly in the controller and
    /// mapped to HTTP 400/401, never surfaced as a 500 by the global handler.
    /// </summary>
    public class AuthValidationException : Exception
    {
        public AuthValidationException(string message) : base(message) { }
    }
}
