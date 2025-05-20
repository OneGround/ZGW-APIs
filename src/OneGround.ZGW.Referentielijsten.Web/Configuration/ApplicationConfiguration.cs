using System.ComponentModel.DataAnnotations;

namespace OneGround.ZGW.Referentielijsten.Web.Configuration;

public class ApplicationConfiguration
{
    public static string ApplicationConfig => "Application";

    [Required]
    public int CommunicatieKanalenPageSize { get; set; }

    [Required]
    public int ResultatenPageSize { get; set; }
}
