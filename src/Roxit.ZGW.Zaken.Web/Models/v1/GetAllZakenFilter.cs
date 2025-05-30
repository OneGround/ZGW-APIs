using System;
using System.Collections.Generic;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Models.v1;

public class GetAllZakenFilter
{
    public string Identificatie { get; set; }
    public string Bronorganisatie { get; set; }
    public string Zaaktype { get; set; }
    public ArchiefNominatie? Archiefnominatie { get; set; }
    public IList<ArchiefNominatie> Archiefnominatie__in { get; set; }
    public DateOnly? Archiefactiedatum { get; set; }
    public DateOnly? Archiefactiedatum__lt { get; set; }
    public DateOnly? Archiefactiedatum__gt { get; set; }
    public ArchiefStatus? Archiefstatus { get; set; }
    public IList<ArchiefStatus> Archiefstatus__in { get; set; }
    public DateOnly? Startdatum { get; set; }
    public DateOnly? Startdatum__gt { get; set; }
    public DateOnly? Startdatum__gte { get; set; }
    public DateOnly? Startdatum__lt { get; set; }
    public DateOnly? Startdatum__lte { get; set; }
}
