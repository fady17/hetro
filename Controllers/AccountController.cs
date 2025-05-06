// File: Hetro/Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks; 

namespace Hetro.Controllers
{
    // Optional: Can apply [Authorize] here to protect all actions by default,
    // then use [AllowAnonymous] on specific actions like Login.
    // Or apply [Authorize] only to specific actions like Profile and Logout.
    // Let's apply per-action for clarity.

    public class AccountController : Controller
    {
        // --- Login Action ---
        // This action doesn't display a view. Its sole purpose is to trigger
        // the OIDC challenge when the user clicks "Login".
        [AllowAnonymous] // Allow access even if not logged in
        public IActionResult Login(string? returnUrl = "/")
        {
            // Check if user is already authenticated, maybe redirect them
             if (User.Identity?.IsAuthenticated ?? false)
             {
                 return LocalRedirect(returnUrl ?? "/");
             }

            // Create AuthenticationProperties to potentially pass the returnUrl
            // The OIDC handler should pick this up and use it after successful login.
            var props = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/" // Ensure a fallback redirect
            };

            // Issue a challenge to the OpenIdConnect scheme.
            // This middleware will handle constructing the redirect to the IdP.
            return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // --- Profile Action ---
        // This page requires the user to be logged in.
        [Authorize] // Ensures only authenticated users can access
        public IActionResult Profile()
        {
            // The view will display claims from the User object (HttpContext.User)
            return View();
        }


        // --- Logout Action ---
        [Authorize] // Must be logged in to log out
        [HttpPost] // Should be triggerd by a POST (usually from a form)
        [ValidateAntiForgeryToken] // Protect against CSRF
        public async Task<IActionResult> Logout()
        {
            // Create properties for signout - including redirect after IdP logout
             var props = new AuthenticationProperties
             {
                 // URL to redirect to locally AFTER the OIDC logout completes
                 // This should match one of the PostLogoutRedirectUris in the IdP client config
                 RedirectUri = Url.Action("Index", "Home") // Redirect to home page after logout
             };

            // Sign out of the local cookie scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign out of the OIDC scheme. This triggers the redirect to the IdP's
            // end_session_endpoint, which then redirects back to the PostLogoutRedirectUri
            // specified in the client config (if matched) or the RedirectUri in props here.
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, props);

            // Note: The final redirect back from the IdP happens AFTER this action completes.
            // You typically don't return a specific RedirectResult here, SignOutAsync handles it.
            // However, sometimes you might return a view indicating logout is in progress.
            // For simplicity, let SignOutAsync handle the redirect. This return is often not hit.
            return SignOut(props, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Optional: Access Denied page (if authorization fails for other reasons)
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}