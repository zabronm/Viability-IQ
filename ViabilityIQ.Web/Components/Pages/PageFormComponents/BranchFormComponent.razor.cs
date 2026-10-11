using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages.PageFormComponents;

public partial class BranchFormComponent
{
    [Inject] private IGenericDataRepository<Branch> BranchRepository { get; set; } = default!;
    [Inject] private OffCanvasStateService OffcanvasService { get; set; } = default!;

    [Parameter] public long BranchId { get; set; }

    private Branch branchModel = new();
    private bool isProcessingData;
    private bool isRowActive = true;

    protected override async Task OnParametersSetAsync()
    {
        if (BranchId == 0)
        {
            branchModel = new Branch { Active = true };
            isRowActive = true;
            return;
        }

        isProcessingData = true;
        try
        {
            branchModel = await BranchRepository.GetByIdAsync(BranchId)
                ?? throw new InvalidOperationException("The requested Site is unavailable.");
            isRowActive = branchModel.Active;
        }
        finally
        {
            isProcessingData = false;
        }
    }

    private async Task HandleFormSubmissionAsync()
    {
        isProcessingData = true;
        try
        {
            branchModel.Active = isRowActive;
            var saved = await BranchRepository.SaveAsync(branchModel);
            await OffcanvasService.PublishResultAsync(new SaveResult
            {
                Success = saved,
                RefreshGrid = saved,
                ClosePanel = saved,
                Message = saved
                    ? $"Site {branchModel.BranchName} saved with code {branchModel.BranchCode}."
                    : "The Site could not be saved."
            });
        }
        catch (Exception exception)
        {
            await OffcanvasService.PublishResultAsync(new SaveResult
            {
                Success = false,
                ClosePanel = false,
                Message = exception.Message
            });
        }
        finally
        {
            isProcessingData = false;
        }
    }
}
