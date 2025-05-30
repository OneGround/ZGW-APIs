using System.Collections.Generic;

namespace Roxit.ZGW.Catalogi.Web.Models.v1;

public class GetAllCatalogussenFilter
{
    public string Rsin { get; set; }
    public IList<string> Rsin__in { get; set; }
    public string Domein { get; set; }
    public IList<string> Domein__in { get; set; }
}
