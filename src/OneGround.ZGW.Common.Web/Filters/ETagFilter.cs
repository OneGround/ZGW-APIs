using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ETagFilter : ActionFilterAttribute
{
    private readonly string[] _supportedVerbs = [HttpMethod.Get.Method, HttpMethod.Head.Method];

    public override async Task OnActionExecutionAsync(ActionExecutingContext executingContext, ActionExecutionDelegate next)
    {
        var request = executingContext.HttpContext.Request;

        var executedContext = await next();

        var response = executedContext.HttpContext.Response;

        if (_supportedVerbs.Contains(request.Method) && response.StatusCode == (int)HttpStatusCode.OK)
        {
            ValidateETagForResponseCaching(executedContext);
        }
    }

    private static void ValidateETagForResponseCaching(ActionExecutedContext executedContext)
    {
        if (executedContext.Result == null)
        {
            return;
        }

        var request = executedContext.HttpContext.Request;
        var response = executedContext.HttpContext.Response;

        if (executedContext.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            var etag = GetResponseEtag(objectResult.Value);

            var ifNoneMatchValues = executedContext.HttpContext.Request.Headers.IfNoneMatch;

            if (ifNoneMatchValues.Count > 0)
            {
                foreach (var headerValue in ifNoneMatchValues)
                {
                    if (EntityTagHeaderValue.TryParse(headerValue, out var requestEtag))
                    {
                        if (requestEtag.Compare(etag, false))
                        {
                            executedContext.Result = new StatusCodeResult((int)HttpStatusCode.NotModified);
                            response.ContentLength = 0;
                        }
                    }
                }
            }
            executedContext.HttpContext.Response.Headers.ETag = etag.Tag.ToString();
        }
    }

    private static EntityTagHeaderValue GetResponseEtag(object value)
    {
        var zgwJsonSerializerSettings = new ZGWJsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        var serialized = JsonConvert.SerializeObject(value, zgwJsonSerializerSettings);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));

        var base64 = Convert.ToBase64String(bytes);

        var etag = new EntityTagHeaderValue($"\"{base64}\"");

        return etag;
    }
}
