using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Habitu.Application.Abstractions;

namespace Habitu.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            }

            return Guid.TryParse(userIdClaim, out var parsedGuid) ? parsedGuid : null;
        }
    }
}