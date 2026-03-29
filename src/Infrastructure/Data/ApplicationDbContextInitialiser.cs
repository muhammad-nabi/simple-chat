using SimpleChat.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SimpleChat.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            _logger.LogInformation("Applying database migrations...");
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole("Administrator");

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            var roleResult = await _roleManager.CreateAsync(administratorRole);
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to create seed role 'Administrator': {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            var userResult = await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!userResult.Succeeded)
            {
                _logger.LogWarning("Failed to create seed user: {Errors}",
                    string.Join(", ", userResult.Errors.Select(e => e.Description)));
                return;
            }

            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                var roleAssignResult = await _userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
                if (!roleAssignResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign 'Administrator' role to seed user: {Errors}",
                        string.Join(", ", roleAssignResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
