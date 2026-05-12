using System.Linq;
using System.Windows;
using TravelAgency.WPF.Views;
using TravelAgency.WPF.Views.Agent;
using TravelAgency.WPF.Views.Common;

namespace TravelAgency.WPF.Services
{
    /// <summary>
    /// Evită <see cref="ArgumentException"/> „Cannot set Owner property to itself” când
    /// <see cref="Application.MainWindow"/> nu este fereastra shell așteptată.
    /// </summary>
    internal static class DialogOwnerHelper
    {
        public static void SetOwnerSafe(this Window dialog)
        {
            if (Application.Current == null)
                return;

            var owner = Application.Current.Windows.OfType<AgentWindow>().FirstOrDefault(w => w.IsVisible)
                ?? Application.Current.Windows.OfType<ClientWindow>().FirstOrDefault(w => w.IsVisible)
                ?? Application.Current.Windows.Cast<Window>()
                    .FirstOrDefault(w =>
                        w is not null &&
                        w is not UserConversationWindow &&
                        w.IsVisible &&
                        w.IsLoaded &&
                        !ReferenceEquals(w, dialog));

            var mw = Application.Current.MainWindow;
            if (owner == null && mw != null && mw.IsLoaded && !ReferenceEquals(mw, dialog))
                owner = mw;

            if (owner != null && !ReferenceEquals(owner, dialog))
                dialog.Owner = owner;
        }
    }
}
