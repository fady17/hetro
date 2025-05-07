# Hetro Clothing - E-Commerce Client (Rally IdP Demo)

## Overview

This "Hetro Clothing" application is a sample ASP.NET Core MVC web application designed to demonstrate integration with the **Rally Identity Provider (IdP)**. It simulates basic e-commerce functionality, allowing users to browse products, view details, manage a shopping cart, and "place orders" (without actual payment processing).

The primary purpose of this client is to serve as a **Relying Party (RP)**, showcasing how a brand application can use the central Rally IdP for user authentication and Single Sign-On (SSO) via OpenID Connect (OIDC).

**Features:**

*   User authentication via Rally IdP (Passwordless email code flow).
*   Display of products (list and detail pages).
*   User-specific shopping cart functionality (persisted in a local SQLite database).
*   Basic order placement simulation.
*   User profile page displaying claims received from the IdP.
*   Synchronization of basic user information (Subject ID, Name, Email) from the IdP into a local client database (`LocalUser` table).

## Technology Stack

*   **Framework:** .NET 9 (Preview) / ASP.NET Core MVC
*   **Authentication:**
    *   `Microsoft.AspNetCore.Authentication.Cookies` (for local session)
    *   `Microsoft.AspNetCore.Authentication.OpenIdConnect` (for OIDC integration with Rally IdP)
*   **Local Database:** SQLite (using Entity Framework Core)
*   **Services:**
    *   `IProductService` / `DbProductService`: Manages product data (seeded into SQLite).
    *   `ICartService` / `DbCartService`: Manages shopping cart data (persisted in SQLite).
    *   `IUserSyncService` / `UserSyncService`: Syncs basic user info from IdP claims to the local SQLite DB upon login.

## Setup & Configuration (Development)

This guide assumes you have the **Rally IdP project** set up and running locally.

### Prerequisites

1.  **.NET 9 SDK** (Ensure correct preview version is installed).
2.  The **Rally IdP application must be running** (typically on `https://localhost:7223`).
3.  Git (optional, if cloning).
4.  .NET User Secrets Tool.

### Configuration Steps:

1.  **Clone/Obtain Project:** Get the `HetroClothingClient` project files.
2.  **Navigate to Project Directory:**
    ```bash
    cd path/to/HetroClothingClient
    ```
3.  **Initialize User Secrets (if not already done):**
    ```bash
    dotnet user-secrets init
    ```
4.  **Set Client Secret:**
    Your `HetroClothingClient` application needs a `ClientSecret` to authenticate itself to the Rally IdP. This secret must match the one configured for the `"hetro-clothing-store"` client within the Rally IdP's `Configuration/Config.cs` (or its database).
    *   The default secret used in the IdP's `Config.cs` for this client is `"secret_hetro"`.
    *   Set this in the Hetro client's User Secrets:
        ```bash
        dotnet user-secrets set "OIDC:ClientSecret" "secret_hetro"
        ```
5.  **Verify IdP Authority URL:**
    *   Open `HetroClothingClient/Program.cs`.
    *   Find the `.AddOpenIdConnect(...)` configuration block.
    *   Ensure `options.Authority` points to the correct HTTPS URL and port where your Rally IdP is running (e.g., `"https://localhost:7223"`).
        ```csharp
        options.Authority = "https://localhost:7223"; // Verify this matches your Rally IdP
        ```
6.  **Verify Client Redirect URI in IdP:**
    *   The `HetroClothingClient` will tell the IdP to redirect back to `https://localhost:PORT/signin-oidc` where `PORT` is the HTTPS port the client is running on.
    *   Check `HetroClothingClient/Properties/launchSettings.json` for the `https` profile's `applicationUrl` to find this port (e.g., `https://localhost:7272`).
    *   Ensure this exact Redirect URI (e.g., `https://localhost:7272/signin-oidc`) is listed in the `RedirectUris` for the `"hetro-clothing-store"` client in the **Rally IdP's `Configuration/Config.cs` file**.
    *   Similarly, ensure the `PostLogoutRedirectUris` in the IdP's config matches where the client should land after logout (e.g., `https://localhost:7272/signout-callback-oidc` or `https://localhost:7272/`).
    *   *If the IdP's client configuration for Redirect URIs doesn't match the client's actual running port, you will get `Invalid redirect_uri` errors from the IdP.*

7.  **Database Setup (SQLite):**
    *   This application uses a local SQLite database (`hetro_client_dev.db` by default for development).
    *   The first time you run the application after building, Entity Framework Core migrations should automatically create this database file in the project's output directory (e.g., `bin/Debug/net9.0/`).
    *   If you need to re-create the database:
        *   Delete the existing `*.db` file(s).
        *   Run migrations:
            ```bash
            dotnet ef database update -c HetroDbContext
            ```
    *   Product data is seeded into this database on first run if the `Products` table is empty.

8.  **Run the Application:**
    *   Ensure the **Rally IdP is running first**.
    *   In a new terminal, navigate to the `HetroClothingClient` directory.
    *   Run:
        ```bash
        dotnet run --launch-profile https
        ```
        *(Or simply `dotnet run` if the `https` profile is the default in `launchSettings.json`)*.
    *   Note the HTTPS URL the Hetro client is listening on (e.g., `https://localhost:7272`).

### Using the Application

1.  Open your browser and navigate to the Hetro client's HTTPS URL (e.g., `https://localhost:7272`).
2.  Browse products.
3.  Click "Login" in the navbar.
    *   You will be redirected to the Rally IdP (`https://localhost:7223`).
    *   Enter your email address for the passwordless flow.
    *   Check your email for a login code.
    *   Enter the code on the IdP's "Verify Login Code" page.
    *   If it's your first time logging into this client with this user, you may see a Consent screen from the Rally IdP. Grant consent.
    *   You will be redirected back to the Hetro Clothing application, now logged in.
4.  Your email should appear in the navbar.
5.  Access your Profile page to see claims.
6.  Add items to your cart.
7.  Proceed to Checkout and "Place Order" (simulated).
8.  View "My Orders".
9.  Log out.

## Notes for AI Agents / Further Development

*   **Authentication Flow:** The client uses OIDC Authorization Code Flow with PKCE. It relies on `form_post` response mode from the IdP. The IdP's Content Security Policy (CSP) for `script-src` includes `'unsafe-inline'` to support this.
*   **User Sync:** On successful token validation (`OnTicketReceived` event), the `UserSyncService` is called to create or update a `LocalUser` record in the client's SQLite database, storing `SubjectId`, `Name`, and `Email` claims.
*   **Cart & Orders:** Cart and Order data are stored locally in the client's SQLite database and are associated with the `LocalUser` via their `SubjectId`.
*   **Secrets:** Client Secret for OIDC is managed via .NET User Secrets.
*   **Payment Processing:** Not implemented. The "Place Order" functionality is a simulation.
*   **Production Deployment:** This setup is for development. For production, client secrets, IdP URLs, and other configurations would need to be managed via environment variables or a secure configuration provider. HTTPS metadata requirement would be re-enabled.
⸻

HetroClient – ASP.NET Core MVC with Rally IdP (OIDC) Integration

This project is a realistic ASP.NET Core MVC client application (Hetro) built to integrate with Rally Identity Provider (IdP) using OpenID Connect (OIDC) for authentication. This guide outlines each step in the development process.

⸻

📦 Project Setup & OIDC Authentication

1. Create the Project

dotnet new mvc -o Hetro -n Hetro --framework net9.0
cd Hetro

2. Install Required Packages

dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools

3. Configure User Secrets

dotnet user-secrets init
dotnet user-secrets set "OIDC:ClientSecret" "your_client_secret_here"

4. Authentication Setup in Program.cs

Add namespaces:

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using Hetro.Services;

Configure Authentication:

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(...)
.AddOpenIdConnect(...); // Full implementation in code

Add Authorization and Middleware:

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();



⸻

🖼️ Basic UI & Public Pages
	•	Models: Product.cs
	•	Services: IProductService, DbProductService (formerly MockProductService)
	•	Views:
	•	Home/Index.cshtml – Product grid
	•	Products/Detail.cshtml – Product detail
	•	Shared/_LoginPartial.cshtml – Login UI
	•	Controllers:
	•	HomeController, ProductsController

⸻

🔐 Authentication Features
	•	AccountController:
	•	Login, Logout, Profile, AccessDenied
	•	Views:
	•	Account/Profile.cshtml – Display user claims
	•	Updated _LoginPartial.cshtml for authentication links

⸻

🗃️ Data Layer & EF Core

Models
	•	LocalUser, ShoppingCart, CartItem
	•	Order, OrderItem

DbContext Setup
	•	HetroDbContext with all DbSet properties and relationships
	•	Configure in Program.cs:

builder.Services.AddDbContext<HetroDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=hetro_client_dev.db"));



Migrations

dotnet ef migrations add InitialClientDbSchema -c HetroDbContext -o Data/Migrations
dotnet ef database update -c HetroDbContext



⸻

🛒 Cart Implementation
	•	Service Layer:
	•	ICartService, DbCartService
	•	Controllers/Views:
	•	CartController: Index, AddToCart, RemoveItem, ClearCart
	•	Cart/Index.cshtml – Cart UI

⸻

📦 Orders (Without Payment)
	•	OrderController:
	•	Checkout, PlaceOrder, Confirmation, MyOrders
	•	Views:
	•	Order/Checkout.cshtml, Order/Confirmation.cshtml, Order/MyOrders.cshtml

⸻

👤 User Sync
	•	SyncUserAsync updated to take a ClaimsPrincipal
	•	Removed IHttpContextAccessor from sync logic
	•	Called from OIDC OnTicketReceived event

⸻

🛠️ Development Notes
	•	Requires .NET 9 SDK
	•	Rally IdP must be running with matching ClientId and ClientSecret
	•	Add .db to .gitignore:

*.db*



⸻

📌 TODO / Next Steps
	•	Integrate payment gateway
	•	Add product admin backend
	•	Secure APIs
	•	Optimize database queries
	•	Improve error handling and telemetry

⸻

📄 License

MIT

⸻

