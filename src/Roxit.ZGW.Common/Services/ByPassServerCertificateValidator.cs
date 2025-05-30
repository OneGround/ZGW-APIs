using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Roxit.ZGW.Common.Services;

public class ByPassServerCertificateValidator : IServerCertificateValidator
{
    public ByPassServerCertificateValidator(ILogger<ByPassServerCertificateValidator> logger, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Application:DontCheckServerValidation"))
        {
            logger.LogDebug(
                "{ByPassServerCertificateValidator} - Warning: Server certificates of calling Web Api's will not be validated (security risk)!",
                nameof(ByPassServerCertificateValidator)
            );
        }
    }

    public bool ValidateCertificate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain certificateChain, SslPolicyErrors policy)
    {
        return true;
    }
}
