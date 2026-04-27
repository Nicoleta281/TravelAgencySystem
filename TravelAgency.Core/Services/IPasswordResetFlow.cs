using System.Threading;
using System.Threading.Tasks;

namespace TravelAgency.Core.Services
{
    /// <summary>
    /// Password reset by email OTP — local service or remote API implementation.
    /// </summary>
    public interface IPasswordResetFlow
    {
        Task<bool> RequestResetAsync(string emailOrUsername, CancellationToken cancellationToken = default);

        Task ConfirmResetAsync(
            string emailOrUsername,
            string otpCode,
            string newPassword,
            CancellationToken cancellationToken = default);
    }
}
