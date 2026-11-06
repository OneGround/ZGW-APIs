using System;
using MapsterMapper;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Gesprekken.Contracts.v1.Responses;
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Events;

public interface IGesprekEventLog
{
    /// <summary>
    /// Adds an event about <paramref name="resource"/> to the event log. It is stored by the next SaveChanges, in
    /// the same transaction as the change itself.
    /// </summary>
    void Append(Gesprek gesprek, string type, object resource);
}

public class GesprekEventLog : IGesprekEventLog
{
    private readonly GrcDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEntityUriService _uriService;

    public GesprekEventLog(GrcDbContext context, IMapper mapper, IEntityUriService uriService)
    {
        _context = context;
        _mapper = mapper;
        _uriService = uriService;
    }

    public void Append(Gesprek gesprek, string type, object resource)
    {
        // Keys and foreign keys of new entities are assigned here at the latest, so the data can refer to them
        _context.ChangeTracker.DetectChanges();

        var record = new GesprekEventRecord
        {
            Owner = gesprek.Owner,
            EventId = Guid.NewGuid(),
            Type = type,
            GesprekId = gesprek.Id,
            Subject = _uriService.GetUri(gesprek),
            AanleidingZaak = gesprek.AanleidingZaak,
            Time = DateTime.UtcNow,
            Data = JsonConvert.SerializeObject(ToData(resource)),
        };
        _context.Events.Add(record);
    }

    private object ToData(object resource) =>
        resource switch
        {
            Gesprek g => _mapper.Map<GesprekResponseDto>(g),
            Gespreksbijdrage b => _mapper.Map<GespreksbijdrageResponseDto>(b),
            Gespreksdeelname d => _mapper.Map<GespreksdeelnameResponseDto>(d),
            GelezenGespreksbijdrage l => _mapper.Map<GelezenGespreksbijdrageResponseDto>(l),
            RedactieResponseDto r => r,
            _ => throw new ArgumentException($"No event data for {resource?.GetType().Name}", nameof(resource)),
        };
}
