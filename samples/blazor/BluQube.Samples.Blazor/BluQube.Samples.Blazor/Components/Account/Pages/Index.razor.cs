using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;

namespace BluQube.Samples.Blazor.Components.Account.Pages;

public partial class Index(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
{
    protected override Task OnInitializedAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;

        var user = httpContext!.User;
        return user.Identity?.IsAuthenticated == false ? this.LoginAsync() : this.LogoutAsync();
    }

    private async Task LoginAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Create a simple claim for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "SimpleUser"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            navigationManager.NavigateTo("/");
        }
    }

    private async Task LogoutAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            navigationManager.NavigateTo("/");
        }
    }
}