using System;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Services;

public interface IValidatorService
{
    bool IsValid<T>(T instance, out ValidationResult result);
    bool IsValid<T>(object instance, out ValidationResult result);
}

public class ValidatorService : IValidatorService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidatorService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool IsValid<T>(T instance, out ValidationResult result)
    {
        var validator =
            _serviceProvider.GetService<IValidator<T>>() ?? throw new InvalidOperationException($"IValidator<{typeof(T)}> is not registered.)");
        result = validator.Validate(instance);
        return result.IsValid;
    }

    public bool IsValid<T>(object instance, out ValidationResult result)
    {
        var validator =
            _serviceProvider.GetService<IValidator<T>>() ?? throw new InvalidOperationException($"IValidator<{typeof(T)}> is not registered.)");
        if (instance is not JObject objectRequest)
        {
            throw new InvalidOperationException($"{instance} is not JObject");
        }

        result = validator.Validate(objectRequest.ToObject<T>());

        return result.IsValid;
    }
}
