using System;
using Mapster;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1._1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAllEnkelvoudigInformatieObjectenQueryParameters, Models.v1.GetAllEnkelvoudigInformatieObjectenFilter>();

        // Create new initial EnkelvoudigInformatieObject: versie 1
        config
            .NewConfig<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Locked)
            .Ignore(dest => dest.Lock)
            .Ignore(dest => dest.ObjectInformatieObjecten)
            .Ignore(dest => dest.GebruiksRechten)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectVersies)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Map(
                dest => dest.InformatieObject,
                src => new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = src.InformatieObjectType.TrimEnd('/'),
                    IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                }
            )
            .Map(dest => dest.Ondertekening_Datum, src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum))
            .Map(dest => dest.Ondertekening_Soort, src => SoortFromString(src.Ondertekening.Soort))
            .Map(dest => dest.Integriteit_Algoritme, src => AlgoritmeFromString(src.Integriteit.Algoritme))
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit.Waarde)
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            .Map(dest => dest.Status, src => StatusFromString(src.Status))
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Map(dest => dest.Bestandsomvang, src => src.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion);

        // Create new version of EnkelvoudigInformatieObject: versie 2, versie 3, etc
        config
            .NewConfig<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Locked)
            .Ignore(dest => dest.ObjectInformatieObjecten)
            .Ignore(dest => dest.GebruiksRechten)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectVersies)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Map(
                dest => dest.InformatieObject,
                src => new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = src.InformatieObjectType,
                    Lock = src.Lock,
                    IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                }
            )
            .Map(dest => dest.Ondertekening_Datum, src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum))
            .Map(dest => dest.Ondertekening_Soort, src => SoortFromString(src.Ondertekening.Soort))
            .Map(dest => dest.Integriteit_Algoritme, src => AlgoritmeFromString(src.Integriteit.Algoritme))
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit.Waarde)
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            .Map(dest => dest.Status, src => StatusFromString(src.Status))
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Map(dest => dest.Bestandsomvang, src => src.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion);
    }

    private static VertrouwelijkheidAanduiding? VertrouwelijkheidAanduidingFromString(string vertrouwelijkheidaanduiding)
    {
        if (string.IsNullOrEmpty(vertrouwelijkheidaanduiding))
            return null;

        if (!Enum.TryParse<VertrouwelijkheidAanduiding>(vertrouwelijkheidaanduiding.Trim(), out var result))
            throw new InvalidOperationException($"VertrouwelijkheidAanduiding {vertrouwelijkheidaanduiding} not implemented.");

        return result;
    }

    private static Status? StatusFromString(string status)
    {
        if (string.IsNullOrEmpty(status))
            return null;

        if (!Enum.TryParse<Status>(status.Trim(), out var result))
            throw new InvalidOperationException($"Status {status} not implemented.");

        return result;
    }

    private static Algoritme AlgoritmeFromString(string algoritme)
    {
        if (algoritme == null)
            throw new ArgumentNullException(algoritme);

        if (!Enum.TryParse<Algoritme>(algoritme.Trim(), out var result))
            throw new InvalidOperationException($"Algoritme {algoritme} not implemented.");

        return result;
    }

    private static Soort? SoortFromString(string soort)
    {
        if (string.IsNullOrEmpty(soort))
            return null;

        if (!Enum.TryParse<Soort>(soort.Trim(), out var result))
            throw new InvalidOperationException($"Soort {soort} not implemented.");

        return result;
    }
}
