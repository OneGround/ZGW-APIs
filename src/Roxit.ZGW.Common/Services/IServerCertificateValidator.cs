using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Roxit.ZGW.Common.Services;

public interface IServerCertificateValidator
{
    bool ValidateCertificate(HttpRequestMessage request, X509Certificate2 certificate, X509Chain certificateChain, SslPolicyErrors policy);
}
