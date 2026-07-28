//using Asp.Versioning;
//using AutoMapper;
//using Microsoft.AspNetCore.Http;
//using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
//using OneGround.ZGW.Autorisaties.DataModel;

//namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

//public class ApplyApiVersionRestrictionsAction : IMappingAction<Applicatie, ApplicatieResponseDto>
//{
//    private static readonly ApiVersion MinimumVersionForAlleenIsGereedVoorPublicatie = new(1, 1);

//    private readonly IHttpContextAccessor _httpContextAccessor;

//    public ApplyApiVersionRestrictionsAction(IHttpContextAccessor httpContextAccessor)
//    {
//        _httpContextAccessor = httpContextAccessor;
//    }

//    public void Process(Applicatie source, ApplicatieResponseDto destination, ResolutionContext context)
//    {
//        var requestedVersion = _httpContextAccessor.HttpContext?.GetRequestedApiVersion();

//        if (requestedVersion is null || requestedVersion < MinimumVersionForAlleenIsGereedVoorPublicatie)
//        {
//            destination.AlleenIsGereedVoorPublicatie = null;
//        }
//    }
//}
