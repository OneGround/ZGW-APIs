using System.Runtime.CompilerServices;
using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests;

internal static class ValidatorOptionsInitializer
{
    // Set once before any test runs, as the api does at startup, so parallel tests never see a mix of resolvers.
    [ModuleInitializer]
    internal static void SetPropertyNameResolver()
    {
        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Default;
    }
}
