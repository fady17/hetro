using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect; 
using Microsoft.IdentityModel.Logging;
using Hetro.Services;
using Hetro.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Mock Product Service (use Scoped or Singleton for simple mock)
builder.Services.AddSingleton<IProductService, MockProductService>();

builder.Services.AddDbContext<HetroDbContext>(options =>
     options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")
         ?? "Data Source=hetro_client.db")); // Default filename if not configured

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Local login via cookie
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme; // Challenge via OIDC
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // Configure cookie options if needed (e.g., sliding expiration)
    options.Cookie.Name = "HetroClient.Auth"; // Give it a distinct name
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Example lifetime
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // --- OIDC Handler Configuration ---
    options.Authority = "https://localhost:7223"; // <<< Your Rally IdP HTTPS URL/Port

    options.ClientId = "hetro-clothing-store"; // Client ID from IdP Config

    // Get Client Secret from User Secrets
    options.ClientSecret = builder.Configuration["OIDC:ClientSecret"]; // Reads from user-secrets via config
    if (string.IsNullOrEmpty(options.ClientSecret)) {
         // Fail fast during startup if secret is missing in Development
         if(builder.Environment.IsDevelopment()) {
             throw new InvalidOperationException("OIDC:ClientSecret is missing. Configure via User Secrets.");
         }
         // In Production, log an error or handle appropriately - don't throw potentially sensitive info
         // Log.Error("OIDC Client Secret is not configured!"); // Assuming Serilog or similar
    }

    // Standard Code Flow settings
    options.ResponseType = OpenIdConnectResponseType.Code; // Use Authorization Code flow
    options.ResponseMode = OpenIdConnectResponseMode.FormPost; // Default, but explicit
    options.UsePkce = true; // Handler uses PKCE automatically with Code flow

    // Scopes to request
    options.Scope.Clear();
    options.Scope.Add("openid"); // Standard OIDC scopes
    options.Scope.Add("profile");
    options.Scope.Add("email");
    // options.Scope.Add("offline_access"); // Add if you need Refresh Tokens later

    // Store tokens received from IdP in the authentication cookie/properties
    options.SaveTokens = true;

    // Development only: Disable HTTPS requirement for metadata endpoint
    // IMPORTANT: Set to true or remove for Staging/Production
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    // Optional: Configure token validation parameters if needed (defaults often okay)
    // options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    // {
    //     NameClaimType = "name", // Map 'name' claim to User.Identity.Name
    //     RoleClaimType = "role" // Map 'role' claim to User.IsInRole
    // };

    // Optional: Handle OIDC events for logging or custom logic
    // options.Events = new OpenIdConnectEvents
    // {
    //     OnTokenValidated = context => { /* Log success */ return Task.CompletedTask; },
    //     OnAuthenticationFailed = context => { /* Log failure */ context.HandleResponse(); return Task.CompletedTask; },
    //     OnRemoteFailure = context => { /* Log failure from IdP */ context.HandleResponse(); return Task.CompletedTask; }
    // };
});

// --- Add Authorization Service ---
builder.Services.AddAuthorization();

// ============================
var app = builder.Build();
// ============================
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
