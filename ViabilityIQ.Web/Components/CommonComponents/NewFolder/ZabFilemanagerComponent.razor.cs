using BrucolWeb.Application.DTOs.ApplicationDocuments;
using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.Services;
using BrucolWeb.Application.Services.ServiceInterfaces;
using BrucolWeb.Domain.Interfaces;
using BrucolWeb.Domain.Models;
using BrucolWeb.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZabFilemanagerComponent
    {
        [Inject] IToastService? _Toast { get; set; }
        [Inject] public IDocumentUploadService documentService { get; set; } = default!;
        [Inject] public IApplicationSetupRepository? applicationSetup { get; set; }
        [Inject] public ZabSessionService? ZabSession { get; set; }
        [Inject] IJSRuntime JS { get; set; } = default!;
        [Parameter] public int PhaseId { get; set; }
        [Parameter] public string PhaseName { get; set; } = "";
        [Parameter] public bool ShowDelete { get; set; } = true;
        [Parameter] public RenderFragment? RequestDocumentForm { get; set; }            //For the Special Requested Document
        [Parameter] public RenderFragment? FileUploadForm { get; set; }                 //For the specified phase document
        [Parameter] public string RequestButtonText { get; set; } = "Enter/Edit details";
        [Parameter] public string? offcanvas_name { get; set; }

        private List<ApplicationDocumentDetailsDto> lstApplicationDocumentDto { get; set; } = new();       /* NEW: PASSED IN */

        private long ApplicationId { get; set; }
        private ZabOffCanvas? _offCanvas;
        private bool ShowRequestCanvas;
        private ApplicationDocumentDetailsDto? CurrentDocument;
        private RenderFragment? ActiveCanvasForm;


        private bool ShowUploadPanel;
        private bool CanProceed { get; set; } = false;          //use the next line as checking is done
        //private bool CanProceed => lstApplicationDocumentDto.Where(x => x.IsMandatory).All(x => x.IsSubmitted);

        protected override async Task OnInitializedAsync()
        {
            ApplicationId = ZabSession!.ApplicationId!.Value;
            //SetTitle();
            //await LoadData();
            await Task.CompletedTask;
        }
        protected override async Task OnParametersSetAsync()
        {
            ApplicationId = ZabSession!.ApplicationId!.Value;
            SetTitle();
            await LoadData();
        }

        async Task LoadData()
        {
            var results = await applicationSetup!.GetApplicationDocumentsByIDsAsync(ApplicationId, PhaseId);
            lstApplicationDocumentDto = results.ToList();
            await Task.CompletedTask;
        }

        void SetTitle()
        {
            //set Off-Canvas title
            switch (PhaseId)
            {
                case 1:
                    PhaseName = "Diligence details ..";
                    RequestButtonText = "Diligence details ..";
                    offcanvas_name = "DiligenceDetails";
                    break;

                case 2:
                    PhaseName = "Contracting details ..";
                    RequestButtonText = "Contracting details ..";
                    offcanvas_name = "ContractingDetails";
                    break;

                case 3:
                    PhaseName = "Marketing details ..";
                    RequestButtonText = "Marketing details ..";
                    offcanvas_name = "MarketingDetails";

                    break;
                case 4:
                    PhaseName = "Financing details ..";
                    RequestButtonText = "Financing details ..";
                    offcanvas_name = "FinancingDetails";
                    break;

                default:
                    PhaseName = "Marketing details ..";
                    RequestButtonText = "Diligence details ..";
                    offcanvas_name = "DiligenceDetails";
                    break;
            }
            //await base.OnParametersSetAsync();
        }


        //OPEN FILE UPLOAD WITH AN EXISTING MODEL, THAT REQUIRES FileName, FileUrl, LocalPath(wwwroot/..), SET Submitted=true
        private async Task OpenUpload(ApplicationDocumentDetailsDto doc)
        {
            CurrentDocument = doc;
            FileUploadForm = builder =>
                    {
                        builder.OpenComponent<FileUploadComponent>(0);
                        builder.AddAttribute(1, "ApplicationId", ApplicationId);
                        builder.AddAttribute(2, "SelectedDocument", CurrentDocument);
                        builder.AddAttribute(3, "OnSaved", EventCallback.Factory.Create<SaveResult>(this, DocumentSaved));
                        builder.AddAttribute(4, "OnClosed", EventCallback.Factory.Create(this, CloseUpload));

                        builder.CloseComponent();
                    };

            ActiveCanvasForm = FileUploadForm;
            await _offCanvas.Show();
        }


        //OPEN FILE UPLOAD WITH EMPTY MODEL, REQUIRES ApplicationId, DocumentId, FileName, FileUrl, LocalPath(wwwroot/..), Submitted=true,  
        private async Task OpenAdditionalDocuments()
        {
            //move default properties into the dto
            CurrentDocument = new ApplicationDocumentDetailsDto()
            {
                ApplicationDocumentId = 0,
                ApplicationId = ApplicationId,
                CreatedBy = 101,                      //replace with actual user
                Active = true,
                CreatedDate = DateTime.UtcNow,
                IsSpecialRequest = true,
                PhaseId = PhaseId,
                IsMandatory = true,
            };

            RequestDocumentForm = builder =>
            {
                builder.OpenComponent<FileUploadExtraComponent>(0);
                builder.AddAttribute(1, "ApplicationId", ApplicationId);
                builder.AddAttribute(2, "SelectedDocument", CurrentDocument);       //This is the dto being passed
                builder.AddAttribute(3, "OnSaved", EventCallback.Factory.Create<SaveResult>(this, DocumentSaved));
                builder.AddAttribute(4, "OnClosed", EventCallback.Factory.Create(this, CloseUpload));

                builder.CloseComponent();
            };

            ActiveCanvasForm = RequestDocumentForm;
            await _offCanvas.Show();

        }



        private async Task OpenRequestCanvas()
        {
            ActiveCanvasForm = RequestDocumentForm;

            if (_offCanvas != null)
            {
                await _offCanvas.Show();
            }
        }


        private async Task ViewDocument(ApplicationDocumentDetailsDto doc)
        {
            //await JS.InvokeVoidAsync("alert", $"Viewing {doc.DocumentName}");
            ActiveCanvasForm = BuildCanvas<DocumentPreviewComponent>(
            new()
            {
                ["FileUrl"] = doc.UrlPath
            });

            await _offCanvas.Show();
        }


        private async Task DownloadDocument(ApplicationDocumentDetailsDto doc)
        {
            //await JS.InvokeVoidAsync("alert", $"Downloading {doc.DocumentName}");
            Console.WriteLine($"Download clicked: {doc.DocumentName}");
            await JS.InvokeVoidAsync("console.log", $"Downloading {doc.DocumentName}");
        }


        private async Task DeleteDocument(ApplicationDocumentDetailsDto doc)
        {
            bool ok = await JS.InvokeAsync<bool>("confirm", $"Delete {doc.DocumentName}?");
            if (!ok) return;
            var result = await documentService.DeleteDocumentAsync(doc.ApplicationDocumentId);

            await LoadData();
            StateHasChanged();
        }


        private Task CloseUpload()
        {
            ShowRequestCanvas = false;
            StateHasChanged();
            return Task.CompletedTask;
        }


        private Task CloseRequestCanvas()
        {
            ShowRequestCanvas = false;
            StateHasChanged();
            return Task.CompletedTask;
        }



        //NEW method
        private async Task DocumentSaved(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(
                    result.Message);
            }
            else
            {
                _Toast!.ShowError(
                    result.Message);
            }

            if (_offCanvas != null)
            {
                await _offCanvas.Close();
            }

            CurrentDocument = null;
            await LoadData();
            StateHasChanged();
        }



        private RenderFragment BuildCanvas<TComponent>(Dictionary<string, object>? parameters = null)    where TComponent : IComponent
        {
            return builder =>
            {
                int index = 0;
                builder.OpenComponent<TComponent>(index++);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        builder.AddAttribute(index++, param.Key, param.Value);
                    }
                }
                builder.CloseComponent();
            };
        }
    }
}