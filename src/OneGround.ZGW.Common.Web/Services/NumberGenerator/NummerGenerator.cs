using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.DataAccess.NumberGenerator;

namespace OneGround.ZGW.Common.Web.Services.NumberGenerator;

public class NummerGenerator<TContext> : INummerGenerator
    where TContext : DbContext, IDbContextWithNummerGenerator
{
    private readonly Dictionary<string, string> _formats;
    private readonly TContext _context;
    private readonly ILogger<NummerGenerator<TContext>> _logger;
    private readonly ISqlCommandExecutor _sqlExecutor;
    private readonly List<KeyValuePair<string, string>> _templates = [];

    public NummerGenerator(IConfiguration configuration, TContext context, ILogger<NummerGenerator<TContext>> logger, ISqlCommandExecutor sqlExecutor)
    {
        _formats = configuration.GetSection("Application:NummerGeneratorFormats").Get<Dictionary<string, string>>() ?? [];
        _context = context;
        _logger = logger;
        _sqlExecutor = sqlExecutor;
    }

    public void SetTemplateKeyValue(string key, string value)
    {
        _templates.Add(new KeyValuePair<string, string>(key, value));
    }

    public async Task<string> GenerateAsync(string rsin, string entity, Func<string, bool> isUnique, CancellationToken cancellationToken = default)
    {
        var retry = 1;
        string nummer = null;
        do
        {
            if (nummer != null)
            {
                retry++;
                _logger.LogWarning("Identificatie {NummerGenerator} is already used. Generating a new one", nummer);
            }

            if (retry > 1000)
            {
                throw new InvalidOperationException("Too many attempts to generate a unique number.");
            }

            nummer = await GenerateAsync(rsin, entity, cancellationToken);
        } while (!isUnique(nummer));

        return nummer;
    }

    public async Task<string> GenerateAsync(string rsin, string entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"{nameof(NummerGenerator<TContext>)}: Generating number for organization {{Rsin}} {{Entity}}", rsin, entity);

        var tableName = typeof(OrganisatieNummer).GetCustomAttribute<TableAttribute>()?.Name;
        var lockTableCommand = $"LOCK TABLE {tableName};";

        using var trans = await _context.Database.BeginTransactionAsync(cancellationToken);
        // Note: We must retrieve/add several tables. To make the GenerateAsync function-call concurrent lock the main table organisatie_nummers (within this call)
        await _sqlExecutor.ExecuteSqlRawAsync(lockTableCommand, cancellationToken);

        var organisatieNummer = await GetOrCreateOrganisatieNummer(rsin, entity, cancellationToken);
        var (formattedNummer, volgendNummer) = Generate(_templates, organisatieNummer.Formaat, organisatieNummer.HuidigNummer);

        organisatieNummer.HuidigNummer = volgendNummer;
        organisatieNummer.HuidigNummerEntiteit = formattedNummer;
        organisatieNummer.ModificationTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await trans.CommitAsync(cancellationToken);

        _logger.LogDebug(
            $"{nameof(NummerGenerator<TContext>)}: {{Nummer}} for organization {{Rsin}} {{Entity}} successfully generated",
            formattedNummer,
            rsin,
            entity
        );

        return formattedNummer;
    }

    private async Task<OrganisatieNummer> GetOrCreateOrganisatieNummer(string rsin, string entity, CancellationToken cancellationToken = default)
    {
        if (!_formats.TryGetValue(entity, out var format))
        {
            throw new InvalidOperationException($"No format defined for entity: {entity}");
        }

        _logger.LogDebug("Using format: {format} for entity: {entity}", format, entity);
        // If the organisatienummer entry not exists add a new one
        var organisatieNummer = await _context.OrganisatieNummers.SingleOrDefaultAsync(
            o => o.Rsin == rsin && o.EntiteitNaam == entity,
            cancellationToken
        );
        if (organisatieNummer == null)
        {
            organisatieNummer = new OrganisatieNummer
            {
                Rsin = rsin,
                CreationTime = DateTime.UtcNow,
                HuidigNummer = 0,
                EntiteitNaam = entity,
                Formaat = format,
            };

            _context.OrganisatieNummers.Add(organisatieNummer);
        }

        return organisatieNummer;
    }

    private static (string formattedNumber, long next) Generate(IList<KeyValuePair<string, string>> templates, string format, long number)
    {
        const int maxNumberLength = 40;

        var now = DateTime.UtcNow;
        var next = number;
        // Next
        next++;

        string formattedNumber = Regex.Replace(
            format,
            "\\{(v\\^.*?)\\}",
            delegate(Match match)
            {
                var numdigits = int.Parse(match.Groups[1].Value.Split('^').Last());

                return $"{next}".PadLeft(numdigits, '0');
            }
        );

        formattedNumber = formattedNumber.Replace("{yyyy}", $"{now.Year}");

        foreach (var template in templates)
        {
            // Note: number should not reach maximum length of number of 40 so truncate it if so
            var tryReplace = formattedNumber.Replace(template.Key, template.Value);
            if (tryReplace.Length > maxNumberLength)
            {
                formattedNumber = formattedNumber.Replace(template.Key, template.Value[..^(tryReplace.Length - maxNumberLength)]);
                break; // Ignore rest of template(s) (which should not be the case in practice!)
            }

            formattedNumber = formattedNumber.Replace(template.Key, template.Value);
        }

        return (formattedNumber, next);
    }
}
