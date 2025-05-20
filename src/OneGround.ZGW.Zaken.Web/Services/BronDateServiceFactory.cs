using System;
using System.Collections.Generic;
using AutoMapper;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Web.Services.BronDate;

namespace OneGround.ZGW.Zaken.Web.Services;

public class BronDateServiceFactory : IBronDateServiceFactory
{
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IMapper _mapper;
    private readonly ZrcDbContext _context;

    public BronDateServiceFactory(
        IBesluitenServiceAgent besluitenServiceAgent,
        IZakenServiceAgent zakenServiceAgent,
        ICatalogiServiceAgent catalogiServiceAgent,
        IMapper mapper,
        ZrcDbContext context
    )
    {
        _besluitenServiceAgent = besluitenServiceAgent;
        _zakenServiceAgent = zakenServiceAgent;
        _catalogiServiceAgent = catalogiServiceAgent;
        _mapper = mapper;
        _context = context;
    }

    public IBronDateService Create(ResultaatTypeDto resultaatType, List<ArchiveValidationError> errors)
    {
        if (resultaatType.ArchiefActieTermijn == null)
            return new NullBronDate();

        var afleidingswijze = ToEnum<Afleidingswijze>(resultaatType.BronDatumArchiefProcedure?.Afleidingswijze);
        var datumKenmerk = resultaatType.BronDatumArchiefProcedure?.DatumKenmerk;

        switch (afleidingswijze)
        {
            case Afleidingswijze.afgehandeld:
                return new AfgehandeldBronDate();
            case Afleidingswijze.hoofdzaak:
                return new HoofdzaakBronDate(_context);
            case Afleidingswijze.ander_datumkenmerk:
                // The brondatum, and therefore the archiefactiedatum, needs to be determined manually.
                return new NullBronDate();
            case Afleidingswijze.eigenschap:
                return new EigenschapBronDate(datumKenmerk, _context, _catalogiServiceAgent);
            case Afleidingswijze.gerelateerde_zaak:
                return new GerelateerdeZaakBronDate(_zakenServiceAgent, _context);
            case Afleidingswijze.ingangsdatum_besluit:
                return new IngangsdatumBesluitBronDate(_besluitenServiceAgent, _context);
            case Afleidingswijze.termijn:
                return new TermijnBronDate(resultaatType.BronDatumArchiefProcedure?.ProcesTermijn);
            case Afleidingswijze.vervaldatum_besluit:
                return new VervaldatumBesluitBronDate(_besluitenServiceAgent, _context);
            case Afleidingswijze.zaakobject:
                var objectType = ToEnum<ObjectType>(resultaatType.BronDatumArchiefProcedure?.ObjectType);
                return new ZaakObjectBronDate(_zakenServiceAgent, _mapper, objectType, datumKenmerk, _context);
        }

        errors.Add(new ArchiveValidationError(string.Empty, ErrorCode.ArchiefActieDatumError, $"Onbekende \"Afleidingswijze\": {afleidingswijze}."));
        return new NullBronDate();
    }

    private static T? ToEnum<T>(string value)
        where T : struct
    {
        if (value == null)
            return null;

        if (Enum.TryParse<T>(value, out var enumValue))
        {
            return enumValue;
        }

        return null;
    }
}
