namespace AirlineHansen.Utils;

/// <summary>
/// Math utility functions for game calculations
/// </summary>
public static class MathUtils
{
    /// <summary>
    /// Convert latitude/longitude to screen coordinates on map
    /// </summary>
    public static (int x, int y) LatLonToScreenCoords(double latitude, double longitude,
        int mapWidth, int mapHeight,
        double minLat, double maxLat, double minLon, double maxLon,
        double padding)
    {
        // Map coordinates to screen space
        double latRange = maxLat - minLat;
        double lonRange = maxLon - minLon;

        // Normalize to 0-1
        double normLat = (latitude - minLat) / latRange;
        double normLon = (longitude - minLon) / lonRange;

        // Account for padding
        double availWidth = mapWidth - (padding * 2);
        double availHeight = mapHeight - (padding * 2);

        // Convert to screen coordinates (invert Y for screen coords where 0,0 is top-left)
        int x = (int)(padding + (normLon * availWidth));
        int y = (int)(padding + ((1.0 - normLat) * availHeight));

        return (x, y);
    }

    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    public static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Clamp value between min and max
    /// </summary>
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }

    /// <summary>
    /// Calculate angle from point A to point B (in degrees)
    /// </summary>
    public static double GetAngle(double x1, double y1, double x2, double y2)
    {
        double angle = Math.Atan2(y2 - y1, x2 - x1) * (180.0 / Math.PI);
        return angle < 0 ? angle + 360 : angle;
    }

    /// <summary>
    /// Format currency for display
    /// </summary>
    public static string FormatCurrency(decimal amount)
    {
        return amount.ToString("C", System.Globalization.CultureInfo.CreateSpecificCulture("de-DE"));
    }

    /// <summary>
    /// Format large numbers with thousand separators
    /// </summary>
    public static string FormatNumber(long number)
    {
        return number.ToString("N0");
    }
}
