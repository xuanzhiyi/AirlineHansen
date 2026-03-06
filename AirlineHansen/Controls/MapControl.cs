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
    private DateTime? _currentGameTime;

    // Visual settings
    private readonly Font _cityFont = new("Segoe UI", 9, FontStyle.Bold);
    private readonly Font _smallFont = new("Segoe UI", 7);
    private readonly Pen _gridPen = new(Color.FromArgb(200, 220, 230), 1);
    private readonly Pen _routePen = new(Color.FromArgb(100, 150, 200), 2);
    private readonly Pen _activeRoutePen = new(Color.FromArgb(70, 130, 180), 3);
    private readonly Brush _textBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
    private readonly Brush _seaBrush = new SolidBrush(Color.FromArgb(200, 230, 245));

    // Map bounds
    private const double MinLat = Constants.MAP_MIN_LAT;
    private const double MaxLat = Constants.MAP_MAX_LAT;
    private const double MinLon = Constants.MAP_MIN_LON;
    private const double MaxLon = Constants.MAP_MAX_LON;
    private const double Padding = Constants.MAP_PADDING;

    public MapControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(230, 240, 245);
        SetStyle(ControlStyles.ResizeRedraw, true);
        _currentGameTime = DateTime.Now;
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

    /// <summary>
    /// Set current game time for flight animation
    /// </summary>
    public void SetGameTime(DateTime gameTime)
    {
        _currentGameTime = gameTime;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Always redraw if there are active flights (for animation)
        bool hasFlights = _flights?.Count(f => f.Status == "Scheduled" || f.Status == "InProgress") > 0;

        if (_needsRedraw || _mapBitmap == null || hasFlights)
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
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Draw background
        DrawBackground(g);

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

        // Draw title
        DrawTitle(g);
    }

    private void DrawBackground(Graphics g)
    {
        // Draw sea background
        g.FillRectangle(_seaBrush, 0, 0, Width, Height);

        // Draw border
        using (var borderPen = new Pen(Color.FromArgb(100, 100, 150), 2))
        {
            g.DrawRectangle(borderPen, (int)Padding, (int)Padding, Width - (int)(Padding * 2), Height - (int)(Padding * 2));
        }
    }

    private void DrawTitle(Graphics g)
    {
        string title = "European Airlines Network Map";
        using (var titleFont = new Font("Segoe UI", 14, FontStyle.Bold))
        {
            var titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, _textBrush, Width / 2 - titleSize.Width / 2, 5);
        }
    }

    private void DrawGrid(Graphics g)
    {
        // Draw subtle grid lines
        for (double lat = MinLat; lat <= MaxLat; lat += 10)
        {
            var p1 = MathUtils.LatLonToScreenCoords(lat, MinLon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            var p2 = MathUtils.LatLonToScreenCoords(lat, MaxLon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            g.DrawLine(_gridPen, p1.x, p1.y, p2.x, p2.y);

            // Draw latitude labels
            string latLabel = $"{lat:F0}°";
            var size = g.MeasureString(latLabel, _smallFont);
            g.DrawString(latLabel, _smallFont, _textBrush, (int)Padding - 25, p1.y - (int)size.Height / 2);
        }

        // Draw longitude lines
        for (double lon = MinLon; lon <= MaxLon; lon += 10)
        {
            var p1 = MathUtils.LatLonToScreenCoords(MinLat, lon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            var p2 = MathUtils.LatLonToScreenCoords(MaxLat, lon, Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            g.DrawLine(_gridPen, p1.x, p1.y, p2.x, p2.y);

            // Draw longitude labels
            string lonLabel = $"{lon:F0}°";
            var size = g.MeasureString(lonLabel, _smallFont);
            g.DrawString(lonLabel, _smallFont, _textBrush, p1.x - (int)size.Width / 2, (int)(Padding + Height - (Padding * 2)) + 5);
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

                // Count active flights on this route
                int activeFlights = _flights?.Count(f => f.RouteId == route.Id && f.Status == "InProgress") ?? 0;

                // Use brighter pen if flights are active
                var routePen = activeFlights > 0 ? _activeRoutePen : _routePen;
                g.DrawLine(routePen, p1.x, p1.y, p2.x, p2.y);

                // Draw route endpoints as small circles
                const int dotSize = 3;
                using (var dotBrush = new SolidBrush(Color.FromArgb(80, 120, 160)))
                {
                    g.FillEllipse(dotBrush, p1.x - dotSize, p1.y - dotSize, dotSize * 2, dotSize * 2);
                    g.FillEllipse(dotBrush, p2.x - dotSize, p2.y - dotSize, dotSize * 2, dotSize * 2);
                }
            }
        }
    }

    private void DrawCities(Graphics g)
    {
        if (_cities == null)
            return;

        // First pass: draw city circles (so they're behind labels)
        foreach (var city in _cities)
        {
            var pos = MathUtils.LatLonToScreenCoords(city.Latitude, city.Longitude,
                Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

            // City size based on population (min 4, max 12)
            double popFactor = Math.Min(city.Population / 1_000_000.0, 3) / 3.0;
            int cityRadius = Math.Max(4, Math.Min(12, (int)(4 + popFactor * 8)));

            // Draw shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, pos.x - cityRadius - 1, pos.y - cityRadius + 2, cityRadius * 2 + 2, cityRadius * 2);
            }

            // Draw city circle with gradient
            using (var cityBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new System.Drawing.Point(pos.x - cityRadius, pos.y - cityRadius),
                new System.Drawing.Point(pos.x + cityRadius, pos.y + cityRadius),
                Color.FromArgb(100, 150, 220),
                Color.FromArgb(50, 100, 180)))
            {
                g.FillEllipse(cityBrush, pos.x - cityRadius, pos.y - cityRadius, cityRadius * 2, cityRadius * 2);
            }

            using (var borderPen = new Pen(Color.FromArgb(30, 70, 130), 1.5f))
            {
                g.DrawEllipse(borderPen, pos.x - cityRadius, pos.y - cityRadius, cityRadius * 2, cityRadius * 2);
            }
        }

        // Second pass: draw labels
        foreach (var city in _cities)
        {
            var pos = MathUtils.LatLonToScreenCoords(city.Latitude, city.Longitude,
                Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

            double popFactor = Math.Min(city.Population / 1_000_000.0, 3) / 3.0;
            int cityRadius = Math.Max(4, Math.Min(12, (int)(4 + popFactor * 8)));

            // Draw city label with background
            var labelText = city.Name;
            var textSize = g.MeasureString(labelText, _cityFont);
            var labelRect = new RectangleF(pos.x + cityRadius + 5, pos.y - textSize.Height / 2, textSize.Width + 4, textSize.Height + 2);

            using (var labelBackBrush = new SolidBrush(Color.FromArgb(240, 245, 250)))
            {
                g.FillRectangle(labelBackBrush, labelRect);
            }

            using (var labelBorderPen = new Pen(Color.FromArgb(150, 180, 210), 0.5f))
            {
                g.DrawRectangle(labelBorderPen, labelRect);
            }

            g.DrawString(labelText, _cityFont, _textBrush, labelRect.X + 2, labelRect.Y + 1);
        }
    }

    private void DrawFlights(Graphics g)
    {
        if (_flights == null)
            return;

        // Clear any previous plane drawings by using a fresh graphics context
        // Draw both Scheduled (at origin) and InProgress flights (moving)
        foreach (var flight in _flights.Where(f =>
            (f.Status == "Scheduled" && f.ScheduledDeparture <= (_currentGameTime ?? DateTime.MinValue)) ||
            f.Status == "InProgress"))
        {
            var routeObj = _routes?.FirstOrDefault(r => r.Id == flight.RouteId);
            if (routeObj == null)
                continue;

            var originCity = CityDatabase.GetCityById(routeObj.OriginCityId);
            var destCity = CityDatabase.GetCityById(routeObj.DestinationCityId);

            if (originCity == null || destCity == null)
                continue;

            var p1 = MathUtils.LatLonToScreenCoords(originCity.Latitude, originCity.Longitude,
                Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);
            var p2 = MathUtils.LatLonToScreenCoords(destCity.Latitude, destCity.Longitude,
                Width, Height, MinLat, MaxLat, MinLon, MaxLon, Padding);

            // For return flights, animate in opposite direction (destination to origin)
            if (flight.IsReturnFlight)
            {
                var temp = p1;
                p1 = p2;
                p2 = temp;
            }

            // Calculate flight progress (0.0 to 1.0)
            double progress = 0.0;

            if (flight.Status == "Scheduled" && flight.ScheduledDeparture <= _currentGameTime)
            {
                // Scheduled flights appear at origin (progress = 0) only when departure time is reached
                progress = 0.0;
            }
            else if (flight.Status == "InProgress" && flight.ActualDeparture.HasValue && flight.EstimatedArrival.HasValue && _currentGameTime.HasValue)
            {
                // In-progress flights animate based on elapsed time
                var totalDuration = flight.EstimatedArrival.Value - flight.ActualDeparture.Value;
                if (totalDuration.TotalSeconds > 0)
                {
                    var elapsed = _currentGameTime.Value - flight.ActualDeparture.Value;
                    progress = Math.Min(1.0, Math.Max(0.0, elapsed.TotalSeconds / totalDuration.TotalSeconds));
                }
            }

            int x = (int)(p1.x + (p2.x - p1.x) * progress);
            int y = (int)(p1.y + (p2.y - p1.y) * progress);

            // Calculate rotation angle for plane icon
            double angle = MathUtils.GetAngle(p1.x, p1.y, p2.x, p2.y);

            // Draw plane with icon
            DrawPlaneIcon(g, x, y, angle);

		

            // Draw trail behind plane
            //if (progress > 0)
            //{
            //    int trailX = (int)(p1.x + (p2.x - p1.x) * (progress - 0.1));
            //    int trailY = (int)(p1.y + (p2.y - p1.y) * (progress - 0.1));

            //    using (var trailPen = new Pen(Color.FromArgb(150, 200, 100, 100), 1))
            //    {
            //        trailPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            //        g.DrawLine(trailPen, trailX, trailY, x, y);
            //    }
            //}

            // Draw passenger count tooltip
            //string tooltip = $"{flight.PassengersBooked}/{flight.Capacity} pax";
            //var tooltipSize = g.MeasureString(tooltip, _smallFont);
            //var tooltipRect = new RectangleF(x + 8, y - tooltipSize.Height - 2, tooltipSize.Width + 4, tooltipSize.Height + 2);

            //using (var tooltipBrush = new SolidBrush(Color.FromArgb(255, 200, 100, 50)))
            //{
            //    g.FillRectangle(tooltipBrush, tooltipRect);
            //}

            //g.DrawString(tooltip, _smallFont, Brushes.White, tooltipRect.X + 2, tooltipRect.Y + 1);
        }
    }

    private void DrawPlaneIcon(Graphics g, int x, int y, double angleInDegrees)
    {
        // Create transformation matrix to rotate the plane
        var state = g.Save();
        g.TranslateTransform(x, y);
        g.RotateTransform((float)angleInDegrees);

        // Scale factor for larger plane (1.5x bigger)
        const float scale = 1.5f;

        // Draw airplane shape (top-down view)
        // Main fuselage body
        using (var fuselageBrush = new SolidBrush(Color.FromArgb(255, 100, 50)))
        using (var fuselagePen = new Pen(Color.FromArgb(180, 50, 20), 1.5f))
        {
            var fuselagePoints = new Point[]
            {
                new Point((int)(15 * scale), 0),     // nose cone (front) - pointing right
                new Point((int)(9 * scale), (int)(-1.5f * scale)),      // upper fuselage
                new Point((int)(-12 * scale), (int)(-1.5f * scale)),    // upper rear
                new Point((int)(-15 * scale), 0),    // tail point
                new Point((int)(-12 * scale), (int)(1.5f * scale)),     // lower rear
                new Point((int)(9 * scale), (int)(1.5f * scale))        // lower fuselage
            };
            g.FillPolygon(fuselageBrush, fuselagePoints);
            g.DrawPolygon(fuselagePen, fuselagePoints);
        }

        // Main wings (single large triangle with 150° at fuselage, 15° at each tip)
        using (var wingBrush = new SolidBrush(Color.FromArgb(230, 110, 60)))
        using (var wingPen = new Pen(Color.FromArgb(170, 60, 25), 1.5f))
        {
            var wingPoints = new Point[]
            {
                new Point((int)(-3 * scale), (int)(-12 * scale)),         // left wing tip
                new Point(0, 0),                     // fuselage connection point
                new Point((int)(-3 * scale), (int)(12 * scale))           // right wing tip
            };
            g.FillPolygon(wingBrush, wingPoints);
            g.DrawPolygon(wingPen, wingPoints);
        }


        // Tail wing/fin (vertical triangle at rear, similar 150°/15°/15° angles)
        using (var tailBrush = new SolidBrush(Color.FromArgb(220, 100, 50)))
        using (var tailPen = new Pen(Color.FromArgb(160, 50, 20), 1.5f))
        {
            var tailPoints = new Point[]
            {
                new Point((int)(-14 * scale), (int)(-8 * scale)),        // top tail fin tip
                new Point((int)(-12 * scale), 0),                        // rear fuselage connection point
                new Point((int)(-14 * scale), (int)(8 * scale))          // bottom tail fin tip
            };
            g.FillPolygon(tailBrush, tailPoints);
            g.DrawPolygon(tailPen, tailPoints);
        }


        g.Restore(state);
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
            _smallFont?.Dispose();
            _gridPen?.Dispose();
            _routePen?.Dispose();
            _activeRoutePen?.Dispose();
            _textBrush?.Dispose();
            _seaBrush?.Dispose();
        }
        base.Dispose(disposing);
    }
}
