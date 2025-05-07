using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Hetro.Services;
using Hetro.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hetro.Models;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IProductService, DbProductService>();
builder.Services.AddScoped<ICartService, DbCartService>();

builder.Services.AddDbContext<HetroDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=hetro_client.db")); // Default filename if not configured

builder.Services.AddHttpContextAccessor(); // Needed to access User in services
builder.Services.AddScoped<IUserSyncService, UserSyncService>();

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
    if (string.IsNullOrEmpty(options.ClientSecret))
    {
        // Fail fast during startup if secret is missing in Development
        if (builder.Environment.IsDevelopment())
        {
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

    // --- ADD/MODIFY OIDC Events ---
    options.GetClaimsFromUserInfoEndpoint = true;

    // Modify only the OnTicketReceived event in Program.cs
// Modify only the OnTicketReceived event in Program.cs
options.Events = new OpenIdConnectEvents
{
    OnTokenValidated = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var userForLog = context.Principal;

        logger.LogInformation(
            "OIDC OnTokenValidated (Before UserInfo): Initial Principal has SubjectId: {SubjectId}, Name: {NameClaim}, Email: {EmailClaim}",
            userForLog?.FindFirstValue("sub") ?? userForLog?.FindFirstValue(ClaimTypes.NameIdentifier),
            userForLog?.FindFirstValue("name") ?? userForLog?.FindFirstValue(ClaimTypes.Name),
            userForLog?.FindFirstValue("email") ?? userForLog?.FindFirstValue(ClaimTypes.Email));
        return Task.CompletedTask;
    },
    
    OnTicketReceived = async context => // Use this event for sync - FIXED VERSION
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var userForLog = context.Principal; // This principal should be fully populated
        
        logger.LogInformation(
            "OIDC OnTicketReceived (After UserInfo): Principal has SubjectId: {SubjectId}, Name: {NameClaim}, Email: {EmailClaim}",
            userForLog?.FindFirstValue("sub") ?? userForLog?.FindFirstValue(ClaimTypes.NameIdentifier),
            userForLog?.FindFirstValue("name") ?? userForLog?.FindFirstValue(ClaimTypes.Name),
            userForLog?.FindFirstValue("email") ?? userForLog?.FindFirstValue(ClaimTypes.Email));

        if (context.Principal != null && (context.Principal.Identity?.IsAuthenticated ?? false))
        {
            // Get the UserSyncService from DI
            var syncService = context.HttpContext.RequestServices.GetRequiredService<IUserSyncService>();
            
            try
            {
                // IMPORTANT: Cast to concrete implementation to use the direct principal method
                if (syncService is UserSyncService concreteService)
                {
                    // Call a new method that will take the principal directly rather than 
                    // trying to get it from HttpContext
                    await concreteService.SyncUserWithPrincipalAsync(context.Principal);
                    logger.LogInformation("User sync with explicit principal completed from OnTicketReceived.");
                }
                else
                {
                    // Fallback to the original method - though this shouldn't happen
                    await syncService.SyncUserAsync();
                    logger.LogInformation("User sync via HttpContext initiated from OnTicketReceived.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during user sync called from OnTicketReceived.");
                // Decide if login should fail here: context.Fail("User sync failed.");
            }
        }
        else
        {
            logger.LogWarning("OnTicketReceived called but Principal was null or not authenticated.");
        }
    },
    
    OnAuthenticationFailed = context =>
    {
        // Rest of your event handlers remain the same
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(context.Exception, "OIDC Authentication Failed.");
        context.HandleResponse();
        context.Response.Redirect("/Home/Error?message=AuthenticationFailed");
        return Task.CompletedTask;
    },

    OnRemoteFailure = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(context.Failure, "OIDC Remote Failure. Error: {Error}, Description: {ErrorDescription}",
            context.Request?.Query["error"], context.Request?.Query["error_description"]);
        context.HandleResponse();
        context.Response.Redirect("/Home/Error?message=RemoteFailure");
        return Task.CompletedTask;
    }
};
    options.MapInboundClaims = true;
    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.GivenName, "given_name");
    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Surname, "sur_name");
    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapUniqueJsonKey("email_verified", "email_verified", ClaimValueTypes.Boolean);
});

// --- Add Authorization Service ---
builder.Services.AddAuthorization();

static async Task SeedDatabaseAsync(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>(); // Or specific logger
        var context = services.GetRequiredService<HetroDbContext>();

        // Ensure DB is created
        // await context.Database.EnsureCreatedAsync(); // Or rely on migrations

        if (!await context.Products.AnyAsync())
        {
            logger.LogInformation("Seeding product data...");
            context.Products.AddRange(
                new Product { Id = 1, Name = "Rally Signature Tee", Description = "Premium soft cotton tee with subtle Rally branding.", Price = 29.99m, ImageUrl = "/images/products/tee.png" },
                new Product { Id = 2, Name = "Urban Explorer Jeans", Description = "Durable and stylish denim for your everyday adventures.", Price = 79.95m, ImageUrl = "/images/products/hetro_jeans_blue.png" },
                new Product { Id = 3, Name = "Performance Hoodie", Description = "Lightweight tech fleece hoodie, perfect for active wear.", Price = 65.00m, ImageUrl = "/images/products/jeans.png" },
                new Product { Id = 4, Name = "Hetro Flowy Dress", Description = "Elegant and comfortable dress for any occasion.", Price = 89.50m, ImageUrl = "/images/products/hoodie.png" },
                new Product { Id = 5, Name = "Dental Pro Care Kit", Description = "Essential tools for a brighter, healthier smile (loyalty program tie-in!).", Price = 49.99m, ImageUrl = "/images/products/dress.png" }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Product data seeded.");
        }
        else
        {
            logger.LogInformation("Product data already exists, skipping seed.");
        }
    }
}

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

await SeedDatabaseAsync(app);

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