namespace Roxit.ZGW.Autorisaties.Web.Configuration;

public class ApplicationConfiguration
{
    public bool DontSendNotificaties { get; set; }
    public bool IgnoreZaakTypeValidation { get; set; }
    public int ApplicatiePageSize { get; set; }
    public bool SkipMigrationsAtStartup { get; set; }
    public bool ApplyFixturesAtStartup { get; set; }
    public string FixturesSource { get; set; }
    public string ClientSource { get; set; }
}
