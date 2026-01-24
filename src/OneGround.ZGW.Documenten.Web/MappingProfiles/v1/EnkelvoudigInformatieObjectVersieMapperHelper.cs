using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

internal class EnkelvoudigInformatieObjectVersieMapperHelper
{
    public static OndertekeningDto CreateOptionalOndertekeningDto(EnkelvoudigInformatieObjectVersie src, bool createDefaultWhenEmpty)
    {
        if (src.Ondertekening_Datum == default && src.Ondertekening_Soort == default)
            return createDefaultWhenEmpty ? new OndertekeningDto() : null;

        return new OndertekeningDto
        {
            Datum = ProfileHelper.StringDateFromDate(src.Ondertekening_Datum),
            Soort = SoortToString(src.Ondertekening_Soort),
        };
    }

    public static IntegriteitDto CreateOptionalIntegriteitDto(EnkelvoudigInformatieObjectVersie src, bool createDefaultWhenEmpty)
    {
        if (src.Integriteit_Algoritme == default && src.Integriteit_Datum == default && src.Integriteit_Waarde == default)
            return createDefaultWhenEmpty ? new IntegriteitDto() : null;

        return new IntegriteitDto
        {
            Algoritme = $"{src.Integriteit_Algoritme}",
            Datum = ProfileHelper.StringDateFromDate(src.Integriteit_Datum),
            Waarde = src.Integriteit_Waarde,
        };
    }

    public static OndertekeningDto CreateOptionalOndertekeningDto(EnkelvoudigInformatieObject2 src, bool createDefaultWhenEmpty)
    {
        if (src.Ondertekening_Datum == default && src.Ondertekening_Soort == default)
            return createDefaultWhenEmpty ? new OndertekeningDto() : null;

        return new OndertekeningDto
        {
            Datum = ProfileHelper.StringDateFromDate(src.Ondertekening_Datum),
            Soort = SoortToString(src.Ondertekening_Soort),
        };
    }

    public static IntegriteitDto CreateOptionalIntegriteitDto(EnkelvoudigInformatieObject2 src, bool createDefaultWhenEmpty)
    {
        if (src.Integriteit_Algoritme == default && src.Integriteit_Datum == default && src.Integriteit_Waarde == default)
            return createDefaultWhenEmpty ? new IntegriteitDto() : null;

        return new IntegriteitDto
        {
            Algoritme = $"{src.Integriteit_Algoritme}",
            Datum = ProfileHelper.StringDateFromDate(src.Integriteit_Datum),
            Waarde = src.Integriteit_Waarde,
        };
    }

    private static string SoortToString(Soort? soort)
    {
        if (!soort.HasValue)
            return null;

        return $"{soort}";
    }
}
