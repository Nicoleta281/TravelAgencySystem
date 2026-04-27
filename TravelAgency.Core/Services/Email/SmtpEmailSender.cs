using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace TravelAgency.Core.Services.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _from;
        private readonly bool _enableSsl;

        public SmtpEmailSender(string host, string? from, string? user, string? pass, int port, bool enableSsl)
        {
            _host = (host ?? "").Trim();
            _user = (user ?? "").Trim();
            _pass = (pass ?? "").Trim().Replace(" ", "", StringComparison.Ordinal);
            _from = string.IsNullOrWhiteSpace(from) ? _user : from.Trim();
            _port = port > 0 ? port : 587;
            _enableSsl = enableSsl;
        }

        public void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_host))
                throw new InvalidOperationException("SMTP host is missing. Set Smtp:Host.");
            if (string.IsNullOrWhiteSpace(_from))
                throw new InvalidOperationException("SMTP From is missing. Set Smtp:From (or Smtp:User).");
            if (string.IsNullOrWhiteSpace(_user))
                throw new InvalidOperationException("SMTP User is missing. Set Smtp:User.");
            if (string.IsNullOrWhiteSpace(_pass))
                throw new InvalidOperationException("SMTP Pass is missing. Set Smtp:Pass (use Gmail App Password, not your normal password).");
        }

        public async Task SendAsync(string toEmail, string subject, string textBody, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_user, _pass)
            };

            using var message = new MailMessage(_from, toEmail, subject, textBody);
            await client.SendMailAsync(message, cancellationToken);
        }
    }
}

