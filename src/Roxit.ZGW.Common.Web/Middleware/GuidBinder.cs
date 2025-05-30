using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Roxit.ZGW.Common.Web.Middleware;

public class GuidBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;
        if (bindingContext.ModelState.ContainsKey(modelName))
            return Task.CompletedTask;

        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        var attemptedValue = valueProviderResult.FirstValue;

        if (!Guid.TryParse(attemptedValue, out var id))
        {
            id = Guid.Empty;
        }

        bindingContext.ModelState.SetModelValue(modelName, id, attemptedValue);
        bindingContext.Result = ModelBindingResult.Success(id);

        return Task.CompletedTask;
    }
}
