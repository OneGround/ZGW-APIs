using System;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Referentielijsten.Web.Models;

public class ResultaatTypeOmschrijving : IBaseEntity, IUrlEntity
{
    public Guid Id { get; set; }
    public string Url => $"https://dummy/api/v1/resultaattypeomschrijvingen/{Id}";
    public string Omschrijving { get; set; }
    public string Definitie { get; set; }
    public string Opmerking { get; set; }
}
