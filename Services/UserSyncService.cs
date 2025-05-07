using Hetro.Data;
using Hetro.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Hetro.Services
{
    public interface IUserSyncService
    {
        Task SyncUserAsync();
    }

    public class UserSyncService : IUserSyncService
    {
        private readonly HetroDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserSyncService> _logger;

        public UserSyncService(
            HetroDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserSyncService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Original method - pulls user from HttpContext
        public async Task SyncUserAsync()
        {
            // Get user from current HttpContext
            var principal = _httpContextAccessor.HttpContext?.User;

            if (!(principal?.Identity?.IsAuthenticated ?? false))
            {
                _logger.LogWarning("UserSyncService: Attempted to sync an unauthenticated user.");
                return;
            }

            // Pass to the direct method
            await SyncUserWithPrincipalAsync(principal);
        }

        // New method taking principal directly - used by authentication events
        public async Task SyncUserWithPrincipalAsync(ClaimsPrincipal principal)
        {
           if (principal?.Identity?.IsAuthenticated != true)
{
                _logger.LogWarning("SyncUserWithPrincipalAsync: Attempted to sync with null or unauthenticated principal.");
                return;
            }

            // Log EVERYTHING in the principal for debugging
            _logger.LogDebug("UserSyncService: ClaimsPrincipal received for sync:");
            foreach (var claim in principal.Claims.OrderBy(c => c.Type))
            {
                _logger.LogDebug(" > Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            // Try multiple common claim types
            var subjectId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue("sub");

            var email = principal.FindFirstValue(ClaimTypes.Email)
                     ?? principal.FindFirstValue("email");

            var name = principal.FindFirstValue(ClaimTypes.Name)
                    ?? principal.FindFirstValue("name")
                    ?? principal.FindFirstValue("preferred_username");

            if (string.IsNullOrWhiteSpace(name))
            {
                var givenName = principal.FindFirstValue(ClaimTypes.GivenName) 
                             ?? principal.FindFirstValue("given_name");
                var familyName = principal.FindFirstValue(ClaimTypes.Surname) 
                              ?? principal.FindFirstValue("family_name");
                if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
                {
                    name = $"{givenName} {familyName}".Trim();
                }
            }

            var emailVerifiedString = principal.FindFirstValue("email_verified");
            var emailVerified = bool.TryParse(emailVerifiedString, out var verified) && verified;

            _logger.LogInformation("UserSyncService: Attempting sync. SubjectId: '{FoundSubjectId}', Email: '{FoundEmail}', Name: '{FoundName}', EmailVerified: {FoundEmailVerified}",
                                subjectId, email, name, emailVerified);

            if (string.IsNullOrEmpty(subjectId))
            {
                _logger.LogError("CRITICAL ERROR: Cannot sync user: Subject ID (sub or NameIdentifier) claim is missing from principal.");
                return;
            }

            // Look for existing user
            var localUser = await _context.LocalUsers.FindAsync(subjectId);

            // Create new user if not found, or update existing
            if (localUser == null)
            {
                localUser = new LocalUser
                {
                    SubjectId = subjectId,
                    Email = email,
                    Name = name,
                    EmailVerified = emailVerified,
                    LastLoginUtc = DateTime.UtcNow
                };
                
                _context.LocalUsers.Add(localUser);
                _logger.LogInformation("CREATING new LocalUser record for Subject ID {SubjectId} with Email: '{UserEmail}', Name: '{UserName}'", 
                    subjectId, email, name);
            }
            else
            {
                bool updated = false;
                // Only update fields if they have values
                if (!string.IsNullOrEmpty(email) && localUser.Email != email) 
                { 
                    _logger.LogDebug("Updating email from '{OldEmail}' to '{NewEmail}'", localUser.Email, email);
                    localUser.Email = email; 
                    updated = true; 
                }
                
                if (!string.IsNullOrEmpty(name) && localUser.Name != name) 
                { 
                    _logger.LogDebug("Updating name from '{OldName}' to '{NewName}'", localUser.Name, name);
                    localUser.Name = name; 
                    updated = true; 
                }
                
                if (localUser.EmailVerified != emailVerified) 
                { 
                    _logger.LogDebug("Updating emailVerified from {OldVerified} to {NewVerified}", localUser.EmailVerified, emailVerified);
                    localUser.EmailVerified = emailVerified; 
                    updated = true; 
                }
                
                localUser.LastLoginUtc = DateTime.UtcNow;

                if (updated)
                {
                     _logger.LogInformation("UPDATING existing LocalUser record for Subject ID {SubjectId}. New Email: '{UserEmail}', New Name: '{UserName}'", 
                        subjectId, email, name);
                }
                else
                {
                     _logger.LogInformation("LocalUser record for Subject ID {SubjectId} already up-to-date. LastLoginUtc updated.", 
                        subjectId);
                }
            }

            try
            {
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync successful in UserSyncService for {SubjectId}. Changes saved: {Changes}", 
                    subjectId, result);
                
                // Double-check the user was saved
                var userExists = await _context.LocalUsers.AnyAsync(u => u.SubjectId == subjectId);
                _logger.LogDebug("After save, LocalUser with SubjectId {SubjectId} exists: {Exists}", subjectId, userExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR saving changes to LocalUser for SubjectId {SubjectId}", subjectId);
                throw; // Re-throw to ensure error is propagated
            }
        }
    }
}
// using Hetro.Data;
// using Hetro.Models;
// using Microsoft.EntityFrameworkCore;
// using System.Security.Claims;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http; // Required for IHttpContextAccessor

// namespace Hetro.Services
// {
//     public interface IUserSyncService { Task SyncUserAsync(ClaimsPrincipal principal); }

//     public class UserSyncService : IUserSyncService
//     {
//         private readonly HetroDbContext _context;
//         private readonly ILogger<UserSyncService> _logger;

//         public UserSyncService(HetroDbContext context, ILogger<UserSyncService> logger)
//         {
//             _context = context;
//             _logger = logger;
//         }

//         public async Task SyncUserAsync(ClaimsPrincipal principal)
//         {
//             if (!(principal?.Identity?.IsAuthenticated ?? false)) {
//                 _logger.LogWarning("UserSyncService: Attempted to sync an unauthenticated user.");
//                 return;
//     }


//             // Get essential claims (adjust claim types if needed based on IdP output)
//             var subjectId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
//             var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
//             var name = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");

//             if (string.IsNullOrWhiteSpace(name))
//     {
//                 var givenName = principal.FindFirstValue(ClaimTypes.GivenName) ?? principal.FindFirstValue("given_name");
//                 var familyName = principal.FindFirstValue(ClaimTypes.Surname) ?? principal.FindFirstValue("family_name");
//                 if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
//                 {
//                     name = $"{givenName} {familyName}".Trim();
//                 }
//     }
//             var emailVerifiedString = principal.FindFirstValue("email_verified");
//             var emailVerified = bool.TryParse(emailVerifiedString, out var verified) && verified;

//             _logger.LogDebug("UserSyncService: Raw claims for SubjectId {RawSub}:", subjectId);
//             foreach (var claim in principal.Claims) {
//                 _logger.LogDebug(" > Claim: {Type} = {Value}", claim.Type, claim.Value);
//             }

//             _logger.LogInformation("UserSyncService: Syncing user. SubjectId: '{FoundSubjectId}', Email: '{FoundEmail}', Name: '{FoundName}', EmailVerified: {FoundEmailVerified}",
//                                 subjectId, email, name, emailVerified);



//             if (string.IsNullOrEmpty(subjectId))
//             {
//                 _logger.LogWarning("Cannot sync user: Subject ID (sub or NameIdentifier) claim is missing.");
//                 return;
//             }

//             var localUser = await _context.LocalUsers.FindAsync(subjectId);

//             if (localUser == null)
//             {
//                 // User doesn't exist locally, create them
//                 localUser = new LocalUser
//                 {
//                     SubjectId = subjectId,
//                     Email = email,
//                     Name = name,
//                     EmailVerified = emailVerified,
//                     LastLoginUtc = DateTime.UtcNow
//                 };
//                 _context.LocalUsers.Add(localUser);
//                 _logger.LogInformation("Creating local user record for Subject ID {SubjectId}", subjectId);
//             }
//             else
//             {
//                bool updated = false;
//         // Only update if the new value is not null AND different
//         if (email != null && localUser.Email != email) { localUser.Email = email; updated = true; }
//         if (name != null && localUser.Name != name) { localUser.Name = name; updated = true; }
//         if (localUser.EmailVerified != emailVerified) { localUser.EmailVerified = emailVerified; updated = true; }
//         localUser.LastLoginUtc = DateTime.UtcNow;

//         if (updated)
//         {
//              _logger.LogInformation("Updating local user record for Subject ID {SubjectId}. New Email: {Email}, New Name: {Name}", subjectId, email, name);
//         } else {
//              _logger.LogInformation("Local user record for Subject ID {SubjectId} already up-to-date or claims for update were missing. LastLoginUtc updated.", subjectId);
//         }
//     }

//     try
//     {
//         await _context.SaveChangesAsync();
//     }
//     catch(Exception ex)
//     {
//         _logger.LogError(ex, "Error saving changes to LocalUser for SubjectId {SubjectId}", subjectId);
//     }
// }}
// }