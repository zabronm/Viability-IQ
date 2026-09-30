using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Components.Pages;

public partial class Settings
{
    [Inject] private ITenantAuthorizationService TenantAuthorizationService { get; set; } = default!;
    [Inject] private ILogger<Settings> Logger { get; set; } = default!;

    private bool CanManageTenantSecurity { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var context = await TenantAuthorizationService.GetAccessContextAsync();
            CanManageTenantSecurity =
                context.RoleCodes.Contains(TenantRoleCodes.Owner)
                || context.RoleCodes.Contains(TenantRoleCodes.Administrator);
        }
        catch (UnauthorizedAccessException)
        {
            CanManageTenantSecurity = false;
        }
        catch (Exception exception)
        {
            CanManageTenantSecurity = false;
            Logger.LogError(exception, "Unable to resolve tenant-security settings access.");
        }
    }
}
