using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace DocVault.UserManagement.API.Infrastructure;

public class ClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal == null || !(principal.Identity is ClaimsIdentity identity))
            return Task.FromResult(principal);

        var hasRoleClaim = identity.HasClaim(c => c.Type == ClaimTypes.Role || c.Type == "role");

        if (!hasRoleClaim)
            return Task.FromResult(principal);

        // Ensure both forms exist
        var roleClaims = identity.FindAll("role").ToList();
        if (roleClaims.Any() && !identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            foreach (var c in roleClaims)
                identity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
        }

        var nativeRoleClaims = identity.FindAll(ClaimTypes.Role).ToList();
        if (nativeRoleClaims.Any() && !identity.HasClaim(c => c.Type == "role"))
        {
            foreach (var c in nativeRoleClaims)
                identity.AddClaim(new Claim("role", c.Value));
        }

        return Task.FromResult(principal);
    }
}
