using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Extensions;

public static class ZrcDbContextExtension
{
    public static async Task<(Geometry geometrie, string error)> TryConvertZaakGeometrieAsync(
        this ZrcDbContext context,
        Geometry geometrie,
        CancellationToken cancellationToken
    )
    {
        if (geometrie == null)
        {
            return (geometrie: null, error: null);
        }

        try
        {
            var converted = await context.ST_TransformAsync(geometrie, 28992, cancellationToken);
            if (!converted.IsValid)
            {
                return (
                    geometrie: null,
                    error: $"Could not transform the specified geometry with the current SRID {geometrie.SRID}. Invalid coordinates."
                );
            }
            return (geometrie: converted, error: null);
        }
        catch (Exception ex)
        {
            return (
                geometrie: null,
                error: $"Could not transform the specified geometry with the current SRID {geometrie.SRID}. Exception: {ex.Message}"
            );
        }
    }
}
