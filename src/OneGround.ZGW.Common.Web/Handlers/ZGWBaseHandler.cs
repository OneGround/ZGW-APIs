using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.Web.Handlers;

public abstract class ZGWBaseHandler
{
    protected readonly IConfiguration Configuration;
    protected readonly IAuthorizationContextAccessor AuthorizationContextAccessor;
    protected readonly string _rsin;

    protected ZGWBaseHandler(IConfiguration configuration, IAuthorizationContextAccessor authorizationContextAccessor)
    {
        Configuration = configuration;
        AuthorizationContextAccessor = authorizationContextAccessor;
        _rsin = authorizationContextAccessor.AuthorizationContext.Authorization.Rsin;
    }

    protected Expression<Func<T, bool>> GetRsinFilterPredicate<T>()
        where T : OwnedEntity
    {
        return z => z.Owner == _rsin;
    }

    protected Expression<Func<T, bool>> GetRsinFilterPredicate<T>(Expression<Func<T, bool>> predicate)
    {
        return predicate;
    }
}
