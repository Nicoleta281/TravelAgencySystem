using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TravelAgency.Core.Models.Users.Access;

namespace TravelAgency.Core.Services
{
    /// <summary>
    /// Calls a remote HTTP API that runs <see cref="PasswordResetFlowService"/> (production-style: secrets stay on the server).
    /// </summary>
    public sealed class ApiPasswordResetClient : IPasswordResetFlow, IDisposable
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ApiPasswordResetClient(string apiBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                throw new ArgumentException("API base URL is required.", nameof(apiBaseUrl));

            var baseUri = apiBaseUrl.Trim().TrimEnd('/');
            _http = new HttpClient { BaseAddress = new Uri(baseUri + "/") };
        }

        public async Task<bool> RequestResetAsync(string emailOrUsername, CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsJsonAsync(
                "api/password-reset/request",
                new PasswordResetRequestApiModel { EmailOrUsername = emailOrUsername ?? "" },
                Json,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var err = await response.Content.ReadFromJsonAsync<PasswordResetErrorApiResponse>(Json, cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException(err?.Error ?? "Password reset request failed.");
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<PasswordResetRequestApiResponse>(Json, cancellationToken)
                .ConfigureAwait(false);
            return body?.CodeSent ?? false;
        }

        public async Task ConfirmResetAsync(
            string emailOrUsername,
            string otpCode,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            var response = await _http.PostAsJsonAsync(
                "api/password-reset/confirm",
                new PasswordResetConfirmApiModel
                {
                    EmailOrUsername = emailOrUsername ?? "",
                    OtpCode = otpCode ?? "",
                    NewPassword = newPassword ?? ""
                },
                Json,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var err = await response.Content.ReadFromJsonAsync<PasswordResetErrorApiResponse>(Json, cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException(err?.Error ?? "Password reset failed.");
            }

            response.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
