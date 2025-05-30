namespace Roxit.ZGW.Zaken.DataModel;

public enum IndicatieMachtiging
{
    /// <summary>
    /// De betrokkene in de rol bij de zaak is door een andere betrokkene bij dezelfde zaak gemachtigd om namens hem of haar te handelen
    /// </summary>
    gemachtigde,

    /// <summary>
    /// De betrokkene in de rol bij de zaak heeft een andere betrokkene bij dezelfde zaak gemachtigd om namens hem of haar te handelen
    /// </summary>
    machtiginggever,
}
