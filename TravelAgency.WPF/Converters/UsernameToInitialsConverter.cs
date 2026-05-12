using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.WPF.Converters
{
    /// <summary>1–2 litere pentru avatar (din username sau obiect User).</summary>
    public class UsernameToInitialsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var name = value switch
            {
                User u => u.Username,
                string s => s,
                _ => null
            };

            return string.IsNullOrWhiteSpace(name) ? "?" : BuildInitials(name.Trim());
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Binding.DoNothing;

        private static string BuildInitials(string t)
        {
            if (t.Length == 1)
                return t.ToUpperInvariant();

            var parts = t.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]));

            var letters = new string(t.Where(char.IsLetterOrDigit).Take(2).ToArray());
            return letters.Length >= 2
                ? letters.ToUpperInvariant()
                : t[..Math.Min(2, t.Length)].ToUpperInvariant();
        }
    }
}
