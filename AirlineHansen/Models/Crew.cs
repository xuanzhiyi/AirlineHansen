namespace AirlineHansen.Models;

/// <summary>
/// Represents a crew member (pilot or cabin crew)
/// </summary>
public class Crew
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Crew type: "Pilot" or "CabinCrew"
    /// </summary>
    public string CrewType { get; set; } = string.Empty;

    /// <summary>
    /// Monthly salary in euros
    /// </summary>
    public decimal MonthlySalary { get; set; }

    /// <summary>
    /// Aircraft ID assigned to (null if not assigned)
    /// </summary>
    public int? AssignedAircraftId { get; set; }

    /// <summary>
    /// Is crew member active/employed
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date hired
    /// </summary>
    public DateTime HiredDate { get; set; }

    public Crew()
    {
    }

    public Crew(int id, string name, string crewType, decimal monthlySalary)
    {
        Id = id;
        Name = name;
        CrewType = crewType;
        MonthlySalary = monthlySalary;
        HiredDate = DateTime.Now;
    }

    public override string ToString() => $"{Name} ({CrewType})";
}
