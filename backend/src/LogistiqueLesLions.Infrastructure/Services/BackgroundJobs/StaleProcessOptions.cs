namespace LogistiqueLesLions.Infrastructure.Services.BackgroundJobs;

public class StaleProcessOptions
{
    public const string SectionName = "StaleProcess";

    public bool   Enabled            { get; set; } = true;
    public int    DaysThreshold      { get; set; } = 7;
    public int    CheckIntervalHours { get; set; } = 24;
    public string AdminEmail         { get; set; } = string.Empty;
}
