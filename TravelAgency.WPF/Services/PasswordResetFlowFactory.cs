using System;
using System.Configuration;
using TravelAgency.Core.Services;

namespace TravelAgency.WPF.Services
{
    internal static class PasswordResetFlowFactory
    {
        /// <summary>
        /// If <c>PasswordReset.ApiBaseUrl</c> is set in App.config, uses the HTTP API (production-style).
        /// Otherwise throws — password reset requires the API (SMS/Twilio runs on server).
        /// </summary>
        public static IPasswordResetFlow Create()
        {
            var apiBase = ConfigurationManager.AppSettings["PasswordReset.ApiBaseUrl"];
            if (!string.IsNullOrWhiteSpace(apiBase))
                return new ApiPasswordResetClient(apiBase.Trim());

            throw new InvalidOperationException(
                "Password reset requires the API. Set PasswordReset.ApiBaseUrl in App.config and start TravelAgency.Api.");
        }
    }
}
