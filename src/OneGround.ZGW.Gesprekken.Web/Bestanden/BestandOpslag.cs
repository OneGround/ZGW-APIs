using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace OneGround.ZGW.Gesprekken.Web.Bestanden;

/// <summary>
/// Objectopslag met tijdelijke (pre-signed) URL's, zoals in patterns/file-upload.
/// </summary>
public interface IBestandOpslag
{
    Task<string> GetUploadUrlAsync(string sleutel, string contentType, DateTime verlooptOp);

    Task<string> GetDownloadUrlAsync(string sleutel, string bestandsnaam, DateTime verlooptOp);

    /// <summary>The size of the uploaded object, or null when it is not there (yet).</summary>
    Task<long?> GetOmvangAsync(string sleutel, CancellationToken cancellationToken);

    Task VerwijderAsync(string sleutel, CancellationToken cancellationToken);
}

/// <summary>
/// S3-compatible storage: AWS S3 or, with a ServiceUrl, for example Ceph.
/// </summary>
public class S3BestandOpslag : IBestandOpslag
{
    private readonly Lazy<AmazonS3Client> _client;
    private readonly string _bucket;
    private readonly bool _http;

    public S3BestandOpslag(IConfiguration configuration)
    {
        var section = configuration.GetSection("BestandOpslag");
        _bucket = section["Bucket"];
        _http = section["ServiceUrl"]?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true;
        _client = new Lazy<AmazonS3Client>(() =>
        {
            var config = new AmazonS3Config();
            if (!string.IsNullOrEmpty(section["ServiceUrl"]))
            {
                config.ServiceURL = section["ServiceUrl"];
                config.ForcePathStyle = true;
            }
            else
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(section["Region"] ?? "eu-central-1");
            }

            return new AmazonS3Client(new BasicAWSCredentials(section["AccessKey"], section["SecretKey"]), config);
        });
    }

    public Task<string> GetUploadUrlAsync(string sleutel, string contentType, DateTime verlooptOp) =>
        _client.Value.GetPreSignedURLAsync(
            new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = sleutel,
                Verb = HttpVerb.PUT,
                Expires = verlooptOp,
                ContentType = contentType,
                Protocol = _http ? Protocol.HTTP : Protocol.HTTPS,
            }
        );

    public Task<string> GetDownloadUrlAsync(string sleutel, string bestandsnaam, DateTime verlooptOp)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = sleutel,
            Verb = HttpVerb.GET,
            Expires = verlooptOp,
            Protocol = _http ? Protocol.HTTP : Protocol.HTTPS,
        };
        request.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{bestandsnaam.Replace("\"", "")}\"";
        return _client.Value.GetPreSignedURLAsync(request);
    }

    public async Task<long?> GetOmvangAsync(string sleutel, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _client.Value.GetObjectMetadataAsync(_bucket, sleutel, cancellationToken);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task VerwijderAsync(string sleutel, CancellationToken cancellationToken) =>
        _client.Value.DeleteObjectAsync(_bucket, sleutel, cancellationToken);
}
