namespace Roxit.ZGW.Common.DataModel;

public enum ArchiefNominatie
{
    /// <summary>
    /// Het zaakdossier moet bewaard blijven en op de Archiefactiedatum overgedragen worden naar een archiefbewaarplaats.
    /// </summary>
    blijvend_bewaren,

    /// <summary>
    /// Het zaakdossier moet op of na de Archiefactiedatum vernietigd worden.
    /// </summary>
    vernietigen,
}
