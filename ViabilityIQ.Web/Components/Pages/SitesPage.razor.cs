using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages;

public partial class SitesPage
{
    [Inject] private IGenericDataRepository<Branch> BranchRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<Province> ProvinceRepository { get; set; } = default!;
    [Inject] private OffCanvasStateService OffcanvasService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;

    private List<Branch> sites = [];
    private Dictionary<long, string> regionNames = [];
    private bool loading;
    private List<ZabDataTableAdvanced<Branch>.ColumnDefinition<Branch>> tableColumns = [];

    protected override Task OnInitializedAsync()
    {
        tableColumns =
        [
            new() { Title = "Site Code", Value = site => site.BranchCode ?? "" },
            new() { Title = "Site Name", Value = site => site.BranchName ?? "" },
            new() { Title = "Region", Value = site => regionNames.GetValueOrDefault(site.ProvinceId, "") },
            new()
            {
                Title = "Status",
                Value = site => site.Active ? "Active" : "Inactive",
                UseBadge = true,
                BadgeClass = site => site.Active ? "badge-approved" : "badge-rejected"
            }
        ];

        return LoadSitesAsync();
    }

    private async Task LoadSitesAsync()
    {
        loading = true;
        try
        {
            var siteTask = BranchRepository.GetAllAsync();
            var regionTask = ProvinceRepository.GetAllAsync();
            await Task.WhenAll(siteTask, regionTask);
            sites = (await siteTask).ToList();
            regionNames = (await regionTask).ToDictionary(
                region => region.ProvinceId,
                region => region.ProvinceName ?? $"Region #{region.ProvinceId}");
        }
        finally
        {
            loading = false;
        }
    }

    private Task OpenFormAsync(long branchId) =>
        OffcanvasService.ShowAsync(new CanvasRequest
        {
            Title = branchId == 0 ? "Add Site" : "Modify Site",
            Width = 400,
            ComponentType = typeof(BranchFormComponent),
            Parameters = new Dictionary<string, object> { ["BranchId"] = branchId },
            ResultCallback = ProcessResultAsync
        });

    private async Task ProcessResultAsync(SaveResult result)
    {
        if (result.Success)
        {
            Toast.ShowSuccess(result.Message, SessionService.AppTitle);
            await LoadSitesAsync();
        }
        else if (!result.Cancelled)
        {
            Toast.ShowError(result.Message, SessionService.AppTitle);
        }
    }
}
