namespace AirlineHansen.Utils;

/// <summary>
/// Game constants and configuration
/// </summary>
public static class Constants
{
    // Financial
    public const decimal STARTING_CAPITAL = 100_000m;
    public const double LOAN_INTEREST_RATE = 0.08; // 8% annual

    // Aircraft
    public const string AIRCRAFT_SMALL = "Small";
    public const string AIRCRAFT_MEDIUM = "Medium";
    public const string AIRCRAFT_LARGE = "Large";

    // Crew
    public const string CREW_TYPE_PILOT = "Pilot";
    public const string CREW_TYPE_CABIN = "CabinCrew";

    // Flight
    public const string FLIGHT_STATUS_SCHEDULED = "Scheduled";
    public const string FLIGHT_STATUS_IN_PROGRESS = "InProgress";
    public const string FLIGHT_STATUS_COMPLETED = "Completed";
    public const string FLIGHT_STATUS_CANCELLED = "Cancelled";

    // Game
    public const int INITIAL_GAME_DAY = 1;
    public const double SECONDS_PER_GAME_DAY = 5.0;
    public const int CRUISE_SPEED_KMH = 500;

    // UI Map
    public const int MAP_WIDTH = 1200;
    public const int MAP_HEIGHT = 800;
    public const double MAP_PADDING = 50;

    // Map coordinates for EU bounding box (approximate)
    public const double MAP_MIN_LAT = 35.0;  // Southern limit (Mediterranean)
    public const double MAP_MAX_LAT = 71.0;  // Northern limit (Iceland/Northern Europe)
    public const double MAP_MIN_LON = -10.0; // Western limit (Atlantic)
    public const double MAP_MAX_LON = 40.0;  // Eastern limit (Eastern Europe)
}
