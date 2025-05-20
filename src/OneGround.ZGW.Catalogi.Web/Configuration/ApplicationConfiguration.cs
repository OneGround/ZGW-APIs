namespace OneGround.ZGW.Catalogi.Web.Configuration;

public class ApplicationConfiguration
{
    public int ZaakTypenPageSize { get; set; }
    public int StatusTypenPageSize { get; set; }
    public int RolTypenPageSize { get; set; }
    public int ResultaatTypenPageSize { get; set; }
    public int ZaakTypeInformatieObjectTypenPageSize { get; set; }
    public int CatalogussenPageSize { get; set; }
    public int InformatieObjectTypenPageSize { get; set; }
    public int EigenschappenPageSize { get; set; }
    public int BesluitTypenPageSize { get; set; }
    public int ZaakObjectTypenPageSize { get; set; }
    public bool DontSendNotificaties { get; set; }
    public bool IgnoreZaakTypeValidation { get; set; }
    public bool IgnoreStatusTypeValidation { get; set; }
    public bool IgnoreInformatieObjectTypeValidation { get; set; }
    public bool IgnoreBusinessRulesZtc010AndZtc011 { get; set; }
    public bool IgnoreBusinessRuleZtcConceptOnAddedRelations { get; set; }
    public bool IgnoreBusinessRuleStatustypeZaaktypeValidation { get; set; }
}
