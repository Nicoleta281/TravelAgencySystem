using System.Threading;
using System.Threading.Tasks;

namespace TravelAgency.Core.Services.Sms
{
    public interface ISmsSender
    {
        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> when the sender cannot send (e.g. missing configuration).
        /// </summary>
        void EnsureConfigured();

        Task SendAsync(string toPhoneNumber, string textBody, CancellationToken cancellationToken = default);
    }
}

