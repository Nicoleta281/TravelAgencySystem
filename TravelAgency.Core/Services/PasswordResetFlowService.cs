using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services.Email;
using TravelAgency.Core.Services.Sms;

namespace TravelAgency.Core.Services
{
    public class PasswordResetFlowService : IPasswordResetFlow
    {
        private const int OtpDigits = 6;
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan LinkTtl = TimeSpan.FromHours(1);
        private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMinutes(1);
        private const int MaxAttempts = 5;

        private readonly Func<TravelAgencyDbContext> _createDb;
        private readonly IEmailSender? _emailSender;
        private readonly ISmsSender? _smsSender;
        private readonly string _publicBaseUrl;

        public PasswordResetFlowService(
            Func<TravelAgencyDbContext> createDb,
            IEmailSender? emailSender = null,
            ISmsSender? smsSender = null,
            string? publicBaseUrl = null)
        {
            _createDb = createDb ?? throw new ArgumentNullException(nameof(createDb));
            _emailSender = emailSender;
            _smsSender = smsSender;
            _publicBaseUrl = string.IsNullOrWhiteSpace(publicBaseUrl) ? "http://localhost:5280" : publicBaseUrl.Trim().TrimEnd('/');
        }

        /// <summary>
        /// Returns <c>true</c> only after an email was sent. Returns <c>false</c> when no matching account/email
        /// or when rate-limited (caller should not advance UI to the OTP step).
        /// </summary>
        public async Task<bool> RequestResetAsync(string emailOrUsername, CancellationToken cancellationToken = default)
        {
            var key = (emailOrUsername ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
                return false;

            using var db = _createDb();

            var normalized = key.ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(
                x =>
                    x.Username.ToLower() == normalized ||
                    (x.Email != null && x.Email.ToLower() == normalized) ||
                    (x.PhoneNumber != null && x.PhoneNumber == key),
                cancellationToken);

            // Do not leak account existence.
            if (user == null)
                return false;

            var now = DateTime.UtcNow;

            var lastToken = await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastToken != null && (now - lastToken.CreatedAtUtc) < MinRequestInterval)
                return false;

            var code = GenerateOtp();
            var codeHash = HashOtp(code);

            var wantsEmail = LooksLikeEmail(key) || (!LooksLikePhoneNumber(key) && !string.IsNullOrWhiteSpace(user.Email));
            if (wantsEmail)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                    return false;
                if (_emailSender == null)
                    throw new InvalidOperationException("Email sender is not configured on the server.");

                _emailSender.EnsureConfigured();

                // Link-based reset (no copy/paste): send user a clickable link.
                var linkToken = GenerateLinkToken();
                var tokenHash = HashToken(linkToken);

                var lastLink = await db.PasswordResetLinkTokens
                    .Where(t => t.UserId == user.Id)
                    .OrderByDescending(t => t.CreatedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastLink != null && (now - lastLink.CreatedAtUtc) < MinRequestInterval)
                    return false;

                db.PasswordResetLinkTokens.Add(new PasswordResetLinkTokenEntity
                {
                    UserId = user.Id,
                    TokenHash = tokenHash,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(LinkTtl),
                    ConsumedAtUtc = null
                });
                await db.SaveChangesAsync(cancellationToken);

                var link = $"{_publicBaseUrl}/reset-password?token={Uri.EscapeDataString(linkToken)}";

                await _emailSender.SendAsync(
                    user.Email!,
                    "Reset your Travel Agency password",
                    $"Click the link to reset your password:\n\n{link}\n\nThis link expires in {(int)LinkTtl.TotalMinutes} minutes.",
                    cancellationToken);

                return true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                    return false;
                if (_smsSender == null)
                    throw new InvalidOperationException("SMS sender is not configured on the server.");

                _smsSender.EnsureConfigured();
                await _smsSender.SendAsync(
                    user.PhoneNumber!,
                    $"Cod resetare parolă TravelAgency: {code}. Expiră în {(int)OtpTtl.TotalMinutes} minute.",
                    cancellationToken);
            }

            var token = new PasswordResetTokenEntity
            {
                UserId = user.Id,
                CodeHash = codeHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(OtpTtl),
                Attempts = 0,
                ConsumedAtUtc = null
            };

            db.PasswordResetTokens.Add(token);
            await db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task ConfirmResetAsync(
            string emailOrUsername,
            string otpCode,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            var key = (emailOrUsername ?? "").Trim();
            var otp = (otpCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Email/phone/username is required.");
            if (string.IsNullOrWhiteSpace(otp))
                throw new InvalidOperationException("Verification code is required.");

            using var db = _createDb();

            var normalized = key.ToLowerInvariant();
            var user = await db.Users
                .FirstOrDefaultAsync(x =>
                    x.Username.ToLower() == normalized ||
                    (x.Email != null && x.Email.ToLower() == normalized) ||
                    (x.PhoneNumber != null && x.PhoneNumber == key),
                    cancellationToken);

            if (user == null)
                throw new InvalidOperationException("Invalid code or expired request.");

            var now = DateTime.UtcNow;
            var token = await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.ConsumedAtUtc == null && t.ExpiresAtUtc > now)
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (token == null)
                throw new InvalidOperationException("Invalid code or expired request.");

            if (token.Attempts >= MaxAttempts)
                throw new InvalidOperationException("Too many attempts. Please request a new code.");

            token.Attempts += 1;

            var expected = token.CodeHash;
            var actual = HashOtp(otp);
            var ok = ConstantTimeEquals(expected, actual);

            if (!ok)
            {
                await db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Invalid code or expired request.");
            }

            user.PasswordHash = PasswordHasher.Hash(newPassword);
            token.ConsumedAtUtc = now;

            await db.SaveChangesAsync(cancellationToken);
        }

        private static string GenerateOtp()
        {
            // Uniform 6-digit code.
            var value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, OtpDigits));
            return value.ToString(new string('0', OtpDigits));
        }

        private static string HashOtp(string otp)
        {
            // Scope note: we keep the same hash family already used in app (SHA256),
            // but never store plaintext OTP.
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(otp);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static bool ConstantTimeEquals(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a ?? "");
            var bb = Encoding.UTF8.GetBytes(b ?? "");
            if (ba.Length != bb.Length) return false;
            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }

        private static bool LooksLikePhoneNumber(string value)
        {
            var v = (value ?? "").Trim();
            if (v.Length < 7) return false;

            var digits = 0;
            for (var i = 0; i < v.Length; i++)
            {
                var c = v[i];
                if (char.IsDigit(c)) digits++;
                else if (c is '+' or ' ' or '-' or '(' or ')') continue;
                else return false;
            }
            return digits >= 7;
        }

        private static bool LooksLikeEmail(string value)
            => (value ?? "").Contains('@');

        private static string GenerateLinkToken()
        {
            // 32 bytes -> ~43 chars base64url. Plenty of entropy.
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token ?? "");
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            var b64 = Convert.ToBase64String(bytes);
            return b64.Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');
        }
    }
}

