using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Zaken.Web.Services.BronDate;

public class ZaakObjectBronDate : IBronDateService
{
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IMapper _mapper;
    private readonly ObjectType? _objectType;
    private readonly string _datumKenmerk;
    private readonly ZrcDbContext _context;

    public ZaakObjectBronDate(IZakenServiceAgent zakenServiceAgent, IMapper mapper, ObjectType? objectType, string datumKenmerk, ZrcDbContext context)
    {
        _zakenServiceAgent = zakenServiceAgent;
        _mapper = mapper;
        _objectType = objectType;
        _datumKenmerk = datumKenmerk;
        _context = context;
    }

    public Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken)
    {
        // TODO: Must be worked out how to deal with this situation. Log and fallback on archiefstatus => ArchiefStatus.gearchiveerd_procestermijn_onbekend
        errors.Add(
            new ArchiveValidationError(
                string.Empty,
                ErrorCode.ArchiefActieDatumError,
                "De brondatum kan (nog) niet juist bepaald worden indien Afleidingswijze is zaakobject."
            )
        );
        return Task.FromResult<DateOnly?>(null);
        // ----

        // if (!_objectType.HasValue)
        // {
        //     errors.Add(
        //         new ArchiveValidationError(
        //             string.Empty,
        //             ErrorCode.ArchiefActieDatumError,
        //             $"Geen objecttype aanwezig om het zaakobject te achterhalen voor het bepalen van de brondatum."
        //         )
        //     );
        //     return null;
        // }
        //
        // if (string.IsNullOrEmpty(_datumKenmerk))
        // {
        //     errors.Add(
        //         new ArchiveValidationError(
        //             string.Empty,
        //             ErrorCode.ArchiefActieDatumError,
        //             $"Geen datumkenmerk aanwezig om het attribuut van het zaakobject te achterhalen voor het bepalen van de brondatum."
        //         )
        //     );
        //     return null;
        // }
        //
        // var zaakObjecten = await _context
        //     .ZaakObjecten.AsNoTracking()
        //     .Include(z => z.WozWaardeObject)
        //     .Include(z => z.Overige)
        //     .Where(z => z.ZaakId == zaak.Id)
        //     .Where(z => z.ObjectType == _objectType.Value)
        //     .ToListAsync(cancellationToken);
        //
        // var maxDate = DateOnly.MinValue;
        // foreach (var zaakObject in zaakObjecten)
        // {
        //     var value = await GetObject(zaakObject);
        //     if (value == null || !value.TryGetValue(_datumKenmerk, out var datumKenmerk))
        //     {
        //         errors.Add(
        //             new ArchiveValidationError(
        //                 string.Empty,
        //                 ErrorCode.ArchiefActieDatumError,
        //                 $"{_datumKenmerk} geen geldig attribuut voor ZaakObject van type {zaakObject.ObjectType}"
        //             )
        //         );
        //         return null;
        //     }
        //     var dateValue = datumKenmerk as string;
        //     if (!DateOnly.TryParse(dateValue, out var date))
        //     {
        //         errors.Add(
        //             new ArchiveValidationError(
        //                 string.Empty,
        //                 ErrorCode.ArchiefActieDatumError,
        //                 $"Geen geldige datumwaarde in attribuut \"{_datumKenmerk}\": {dateValue}'"
        //             )
        //         );
        //         return null;
        //     }
        //     if (date > maxDate)
        //     {
        //         maxDate = date;
        //     }
        // }
        //
        // if (maxDate == DateOnly.MinValue)
        // {
        //     errors.Add(
        //         new ArchiveValidationError(
        //             string.Empty,
        //             ErrorCode.ArchiefActieDatumError,
        //             $"Geen attribuut gevonden die overeenkomt met het datumkenmerk \"{_datumKenmerk}\" voor het bepalen van de brondatum."
        //         )
        //     );
        //     return null;
        // }
        //
        // return maxDate;
    }

    private async Task<IDictionary<string, object>> GetObject(ZaakObject zaakObject)
    {
        if (zaakObject.Object != null)
        {
            // external object
            var response = await _zakenServiceAgent.GetZaakObjectByUrlAsync(zaakObject.Object);
            return response.Response;
        }

        // local zaak object
        var obj = JObject.FromObject(_mapper.Map<ZaakObjectResponseDto>(zaakObject));
        if (obj.TryGetValue("objectIdentificatie", out var objectIdentificatie))
        {
            return objectIdentificatie.ToObject<IDictionary<string, object>>();
        }

        return null;
    }
}
