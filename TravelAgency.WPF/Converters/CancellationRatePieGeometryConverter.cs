using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TravelAgency.WPF.Converters
{
    /// <summary>
    /// Builds a wedge <see cref="Geometry"/> from center for a donut chart (risk % = cancellation rate).
    /// </summary>
    public sealed class CancellationRatePieGeometryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double rate = value switch
            {
                double d => d,
                float f => f,
                int i => i,
                decimal m => (double)m,
                _ => 0d
            };

            rate = Math.Clamp(rate, 0, 100);
            double sweepDeg = Math.Clamp(rate * 3.6, 0.8, 359.2);

            const double cx = 50, cy = 50, r = 47;
            double start = -Math.PI / 2;
            double end = start + sweepDeg * (Math.PI / 180.0);
            double x0 = cx + r * Math.Cos(start);
            double y0 = cy + r * Math.Sin(start);
            double x1 = cx + r * Math.Cos(end);
            double y1 = cy + r * Math.Sin(end);
            int largeArc = sweepDeg > 180 ? 1 : 0;

            var data = FormattableString.Invariant(
                $"M {cx},{cy} L {x0},{y0} A {r},{r} 0 {largeArc} 1 {x1},{y1} Z");

            try
            {
                return Geometry.Parse(data);
            }
            catch (FormatException)
            {
                return Geometry.Empty;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
