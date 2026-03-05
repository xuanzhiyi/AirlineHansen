using AirlineHansen.Data;
using AirlineHansen.Models;
using AirlineHansen.Utils;

namespace AirlineHansen.Controls;

/// <summary>
/// Custom control for displaying EU map with cities and flights
/// </summary>
public class MapControl : Control
{
    private List<City>? _cities;
    private List<Flight>? _flights;
    private List<Route>? _routes;
    private Bitmap? _mapBitmap;
    private bool _needsRedraw = true;

    // Visual settings
    private readonly Font _cityFont = new("Arial", 8);
    private readonly Font _infoFont = new("Arial", 7);
    private readonly Pen _gridPen = new(Color.LightGray, 1);
    private readonly Pen _routePen = new(Color.LightBlue, 1);
    private readonly Pen _flightPen = new(Color.Red, 2);
    private readonly Brush _cityBrush = new SolidBrush(Color.Blue);
    private readonly Brush _textBrush = new SolidBrush(Color.Black);
    private readonly Brush _flightBrush = new SolidBrush(Color.Red);

    // Map bounds
    private const double MinLat = Constants.MAP_MIN_LAT;
    private const double MaxLat = Constants.MAP_MAX_LAT;
    private const double MinLon = Constants.MAP_MIN_LON;
    private const double MaxLon = Constants.MAP_MAX_LON;
    private const double Padding = Constants.MAP_PADDING;

    public MapControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
    }

    /// <summary>
    /// Set cities to display
    /// </summary>
    public void SetCities(List<City> cities)
    {
        _cities = cities;
        _needsRedraw = true;
        Invalidate();
    }

    /// <summary>
    /// Set flights to display
    /// </summary>
    public void SetFlights(List<Flight> flights)
    {
        _flights = flights;
        _needsRedraw = true;
        Invalidate();
    }

    /// <summary>
    /// Set routes to display
    /// </summary>
    public void SetRoutes(List<Route> routes)
    {
        _routes = routes;
        _needsRedraw = true;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_needsRedraw || _mapBitmap == null)
        {
            _mapBitmap = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(_mapBitmap))
            {
                g.Clear(BackColor);
                DrawMap(g);
            }
            _needsRedraw = false;
        }

        if (_mapBitmap != null)
        {
            e.Graphics.DrawImageUnscaled(_mapBitmap, 0, 0);
        }
    }

    private void DrawMap(Graphics g)
    {
        // Draw grid lines
        DrawGrid(g);

        // Draw routes as lines
        if (_routes != null)
        {
            DrawRoutes(g);
        }

        // Draw cities as circles with labels
        if (_cities != null)
        {
            DrawCities(g);
        }

        // Draw active flights as moving objects
        if (_flights != null)
        {
            DrawFlights(g);
        }
    }

    private void DrawGrid(Graphics g)
    {
        // Draw latitude lines
        for (double lat = MinLat; lat <= MaxLat; lat += 10)
        {
            var p1 = MathUtils.LatLonToScreenCoords(lat, MinLon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            var p2 = MathUtils.LatLonToScreenCoords(lat, MaxLon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            g.DrawLine(_gridPen, p1.x, p1.y, p2.x, p2.y);
        }

        // Draw longitude lines
        for (double lon = MinLon; lon <= MaxLon; lon += 10)
        {
            var p1 = MathUtils.LatLonToScreenCoords(MinLat, lon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            var p2 = MathUtils.LatLonToScreenCoords(MaxLat, lon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            g.DrawLine(_gridPen, p1.x, p1.y, p2.x, p2.y);
        }
    }

    private void DrawRoutes(Graphics g)
    {
        if (_routes == null)
            return;

        foreach (var route in _routes)
        {
            var originCity = CityDatabase.GetCityById(route.OriginCityId);
            var destCity = CityDatabase.GetCityById(route.DestinationCityId);

            if (originCity != null && destCity != null)
            {
                var p1 = MathUtils.LatLonToScreenCoords(originCity.Latitude, originCity.Longitude,
                    Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
                var p2 = MathUtils.LatLonToScreenCoords(destCity.Latitude, destCity.Longitude,
                    Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

                g.DrawLine(_routePen, p1.x, p1.y, p2.x, p2.y);
            }
        }
    }

    private void DrawCities(Graphics g)
    {
        if (_cities == null)
            return;

        const int cityRadius = 5;

        foreach (var city in _cities)
        {
            var pos = MathUtils.LatLonToScreenCoords(city.Latitude, city.Longitude,
                Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

            // Draw city circle
            g.FillEllipse(_cityBrush, pos.x - cityRadius, pos.y - cityRadius, cityRadius * 2, cityRadius * 2);
            g.DrawEllipse(Pens.DarkBlue, pos.x - cityRadius, pos.y - cityRadius, cityRadius * 2, cityRadius * 2);

            // Draw city label
            g.DrawString(city.Name, _cityFont, _textBrush, pos.x + cityRadius + 3, pos.y - 8);
        }
    }

    private void DrawFlights(Graphics g)
    {
        if (_flights == null)
            return;

        const int planeSize = 8;

        foreach (var flight in _flights.Where(f => f.Status == "InProgress"))
        {
            var route = CityDatabase.GetAllCities().FirstOrDefault(c =>
                _routes?.Any(r => r.Id == flight.RouteId) ?? false);

            if (route == null)
            {
                // Find route from flight
                var routeObj = _routes?.FirstOrDefault(r => r.Id == flight.RouteId);
                if (routeObj == null)
                    continue;

                var originCity = CityDatabase.GetCityById(routeObj.OriginCityId);
                var destCity = CityDatabase.GetCityById(routeObj.DestinationCityId);

                if (originCity == null || destCity == null)
                    continue;

                // Calculate current position based on flight progress
                var p1 = MathUtils.LatLonToScreenCoords(originCity.Latitude, originCity.Longitude,
                    Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
                var p2 = MathUtils.LatLonToScreenCoords(destCity.Latitude, destCity.Longitude,
                    Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

                // Simple linear interpolation for now (in a real implementation, calculate based on actual time)
                double progress = 0.5; // Midway for demo
                int x = (int)(p1.x + (p2.x - p1.x) * progress);
                int y = (int)(p1.y + (p2.y - p1.y) * progress);

                // Draw plane
                g.FillEllipse(_flightBrush, x - planeSize / 2, y - planeSize / 2, planeSize, planeSize);
                g.DrawEllipse(Pens.DarkRed, x - planeSize / 2, y - planeSize / 2, planeSize, planeSize);
            }
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _needsRedraw = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapBitmap?.Dispose();
            _cityFont?.Dispose();
            _infoFont?.Dispose();
            _gridPen?.Dispose();
            _routePen?.Dispose();
            _flightPen?.Dispose();
            _cityBrush?.Dispose();
            _textBrush?.Dispose();
            _flightBrush?.Dispose();
        }
        base.Dispose(disposing);
    }
}
