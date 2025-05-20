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

    private readonly IEnumerable<string> _authorizedRsins;

    protected ZGWBaseHandler(IConfiguration configuration, IAuthorizationContextAccessor authorizationContextAccessor)
    {
        Configuration = configuration;
        AuthorizationContextAccessor = authorizationContextAccessor;
        _rsin = authorizationContextAccessor.AuthorizationContext.Authorization.Rsin;

        _authorizedRsins = Configuration.GetSection("Application:AuthorizedRsins").Get<IEnumerable<string>>() ?? [];
    }

    protected Expression<Func<T, bool>> GetRsinFilterPredicate<T>()
        where T : OwnedEntity
    {
        if (_authorizedRsins.Contains(_rsin))
        {
            return z => true;
        }

        return z => z.Owner == _rsin;
    }

    protected Expression<Func<T, bool>> GetRsinFilterPredicate<T>(Expression<Func<T, bool>> predicate)
    {
        if (_authorizedRsins.Contains(_rsin))
        {
            return z => true;
        }

        return predicate;
    }
}
