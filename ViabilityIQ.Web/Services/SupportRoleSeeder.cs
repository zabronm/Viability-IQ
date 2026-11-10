using Microsoft.AspNetCore.Identity;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Services;

public static class SupportRoleSeeder
{
    public const string RoleName = "Support Consultant";

    public static async Task EnsureRoleExistsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        if (await roleManager.RoleExistsAsync(RoleName))
            return;

        var result = await roleManager.CreateAsync(new ApplicationRole
        {
            Name = RoleName,
            Description = "Can claim and respond to user support chats."
        });
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Unable to create the {RoleName} role: {errors}");
        }
    }
}
