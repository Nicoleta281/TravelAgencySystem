using System;
using System.Threading;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TravelAgency.Core.Services.Sms
{
    public sealed class TwilioSmsSender : ISmsSender
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public TwilioSmsSender(string? accountSid, string? authToken, string? fromPhoneNumber)
        {
            _accountSid = (accountSid ?? "").Trim();
            _authToken = (authToken ?? "").Trim();
            _fromPhoneNumber = (fromPhoneNumber ?? "").Trim();
        }

        public void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_accountSid))
                throw new InvalidOperationException("Twilio Account SID is missing. Set Twilio:AccountSid.");
            if (string.IsNullOrWhiteSpace(_authToken))
                throw new InvalidOperationException("Twilio Auth Token is missing. Set Twilio:AuthToken.");
            if (string.IsNullOrWhiteSpace(_fromPhoneNumber))
                throw new InvalidOperationException("Twilio From phone number is missing. Set Twilio:FromPhoneNumber (E.164, e.g. +15551234567).");
        }

        public async Task SendAsync(string toPhoneNumber, string textBody, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var to = (toPhoneNumber ?? "").Trim();
            if (string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Destination phone number is required.");

            TwilioClient.Init(_accountSid, _authToken);

            // Twilio SDK doesn't accept CancellationToken in CreateAsync currently.
            _ = cancellationToken;

            await MessageResource.CreateAsync(
                to: new PhoneNumber(to),
                from: new PhoneNumber(_fromPhoneNumber),
                body: textBody ?? "").ConfigureAwait(false);
        }
    }
}

