using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Roxit.ZGW.Common.Web.Middleware;

public class GuidBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(Guid))
            return new GuidBinder();

        return null;
    }
}
