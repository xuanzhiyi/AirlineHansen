namespace AirlineHansen.Engine;

/// <summary>
/// Manages accelerated game time
/// Converts real time to game time
/// </summary>
public class TimeManager
{
    /// <summary>
    /// Real seconds per game day
    /// Default: 5 seconds = 1 game day (allows for 288 days per hour)
    /// </summary>
    public double SecondsPerGameDay { get; set; } = 5.0;

    /// <summary>
    /// Is the game paused
    /// </summary>
    public bool IsPaused { get; set; } = false;

    /// <summary>
    /// Game time speed multiplier (1.0 = normal, 2.0 = 2x speed)
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Time remaining until next game tick (in seconds)
    /// </summary>
    private double _timeUntilTick;

    /// <summary>
    /// Calculate how many game days have passed
    /// </summary>
    public int CalculateDaysElapsed(double realTimeElapsedSeconds)
    {
        if (IsPaused)
            return 0;

        double adjustedTime = realTimeElapsedSeconds * SpeedMultiplier;
        return (int)(adjustedTime / SecondsPerGameDay);
    }

    /// <summary>
    /// Update time and return number of ticks that should occur
    /// </summary>
    public int UpdateTime(double deltaTimeSeconds)
    {
        if (IsPaused)
            return 0;

        _timeUntilTick -= deltaTimeSeconds * SpeedMultiplier;

        int ticks = 0;
        while (_timeUntilTick <= 0)
        {
            ticks++;
            _timeUntilTick += SecondsPerGameDay / 24.0;
		}
        if(ticks> 0 )
			Console.WriteLine("break");

        return ticks;
    }

    /// <summary>
    /// Set game speed (0.5 = half speed, 2.0 = double speed)
    /// </summary>
    public void SetSpeed(float multiplier)
    {
        SpeedMultiplier = Math.Max(0.1f, Math.Min(5.0f, multiplier)); // Clamp between 0.1x and 5.0x
    }

    /// <summary>
    /// Toggle pause
    /// </summary>
    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    /// <summary>
    /// Reset time tracker (call at start of game)
    /// </summary>
    public void Reset()
    {
        _timeUntilTick = SecondsPerGameDay / 24.0;  // 24 ticks per game day
    }
}
