using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.DataProtection.DataModel;
using OneGround.ZGW.DataAccess.Encryption;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.WebApi.Extensions;

public static class ZakenDataProtectionExtensions
{
    public static IServiceCollection AddZakenDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HmacHasherConfiguration>(configuration.GetSection("HmacHasher"));
        services.AddSingleton<IHmacHasher, HmacSha256Hasher>();
        services.AddSingleton<IDatabaseProtector<ZrcDbContext>>(sp => new DatabaseProtector<ZrcDbContext>(
            sp.GetRequiredService<IDataProtectionProvider>(),
            "ZakenDatabaseProtection"
        ));

        var builder = services
            .AddDataProtection()
            .PersistKeysToDbContext<DataProtectionKeyDbContext>()
            .SetApplicationName("OneGround.ZGW.Zaken")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

        var certBase64 = configuration["DataProtection:Certificate"];
        if (!string.IsNullOrWhiteSpace(certBase64))
        {
            var certPassword = configuration["DataProtection:CertificatePassword"];
            var flags = X509KeyStorageFlags.EphemeralKeySet;
            var certBytes = Convert.FromBase64String(certBase64);
            var cert = string.IsNullOrWhiteSpace(certPassword)
                ? new X509Certificate2(certBytes, (string?)null, flags)
                : new X509Certificate2(certBytes, certPassword, flags);

            if (!cert.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    "DataProtection:Certificate does not contain a private key. "
                        + "A certificate with a private key is required to protect DataProtection keys."
                );
            }

            builder.ProtectKeysWithCertificate(cert);
        }
        else
        {
            Console.Error.WriteLine(
                "[SECURITY WARNING] DataProtection:Certificate is not configured. "
                    + "DataProtection keys will be persisted to the database in unencrypted form. "
                    + "Configure DataProtection:Certificate for production deployments."
            );
        }

        return services;
    }
}
