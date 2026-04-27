using System.Threading;
using System.Threading.Tasks;

namespace TravelAgency.Core.Services.Email
{
    public interface IEmailSender
    {
        void EnsureConfigured();

        Task SendAsync(string toEmail, string subject, string textBody, CancellationToken cancellationToken = default);
    }
}

