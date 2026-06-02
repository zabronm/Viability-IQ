using BrucolWeb.Application.DTOs.ApplicationDocuments;
using BrucolWeb.Application.Services;
using BrucolWeb.Application.Services.ServiceInterfaces;
using BrucolWeb.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BrucolWeb.Web.Components.Common
{
    public partial class FileUploadComponent
    {
        // ==========================================
        // INJECTIONS
        // ==========================================
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public IDocumentUploadService documentService { get; set; } = default!;


        // ==========================================
        // PARAMETERS
        // ==========================================

        [Parameter] public long ApplicationId { get; set; }
        [Parameter] public ApplicationDocumentDetailsDto? SelectedDocument { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaved { get; set; }
        [Parameter] public EventCallback OnClosed { get; set; }

        // ==========================================
        // VARIABLES
        // ==========================================

        private IBrowserFile? selectedFile;
        private bool isProcessing;
        private int uploadProgress;
        private bool uploadCompleted;
        private bool duplicateDetected;
        private string comments = "";


        // ==========================================
        // VALID FILE TYPES
        // ==========================================

        private readonly string[] AllowedExtensions =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".png",
            ".jpg",
            ".jpeg"
        };


        // ==========================================
        // RESET STATE
        // ==========================================

        protected override void OnParametersSet()
        {
            selectedFile = null;
            comments = "";
            isProcessing = false;
            uploadProgress = 0;
            uploadCompleted = false;
            duplicateDetected = false;
        }


        // ==========================================
        // FILE TYPE BADGE
        // ==========================================

        private string FileBadge
        {
            get
            {
                if (selectedFile == null) return "";
                string ext = Path.GetExtension(selectedFile.Name).ToLower();

                return ext switch
                {
                    ".pdf" => "PDF",
                    ".doc" or ".docx" => "WORD",
                    ".xls" or ".xlsx" => "EXCEL",
                    ".png" or ".jpg" or ".jpeg" => "IMAGE",
                    _ => "FILE"
                };
            }
        }


        // ==========================================
        // FILE ICON
        // ==========================================

        private string FileIcon
        {
            get
            {
                if (selectedFile == null) return "bi-file-earmark";

                string ext = Path.GetExtension(selectedFile.Name).ToLower();

                return ext switch
                {
                    ".pdf" => "bi-file-earmark-pdf-fill text-danger",
                    ".doc" or ".docx" => "bi-file-earmark-word-fill text-primary",
                    ".xls" or ".xlsx" => "bi-file-earmark-excel-fill text-success",
                    ".png" or ".jpg" or ".jpeg" => "bi-image-fill text-info",
                    _ => "bi-file-earmark-fill text-secondary"
                };
            }
        }


        // ==========================================
        // FILE SIZE
        // ==========================================

        private string FileSizeDisplay
        {
            get
            {
                if (selectedFile == null) return "";
                double size = selectedFile.Size;
                if (size < 1024) return $"{size:N0} B";
                if (size < 1024 * 1024) return $"{size / 1024:N1} KB";
                return $"{size / (1024 * 1024):N1} MB";
            }
        }


        // ==========================================
        // LOAD FILE
        // ==========================================

        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            var file = e.File; string extension = Path.GetExtension(file.Name).ToLower();

            // FILE TYPE VALIDATION

            if (!AllowedExtensions.Contains(extension))
            {
                await OnSaved.InvokeAsync(new SaveResult
                {
                    Success = false,
                    Message = "Invalid file type selected."
                });

                return;
            }

            // FILE SIZE VALIDATION

            if (file.Size >
                20 * 1024 * 1024)
            {
                await OnSaved.InvokeAsync(new SaveResult
                {
                    Success = false,
                    Message = "Maximum file size is 20MB."
                });

                return;
            }

            selectedFile = file;

            isProcessing = true;

            uploadProgress = 15;
            StateHasChanged();
            await Task.Delay(120);
            uploadProgress = 45;
            StateHasChanged();
            await Task.Delay(120);
            uploadProgress = 75;
            StateHasChanged();
            await Task.Delay(120);
            uploadProgress = 100;
            uploadCompleted = true;
            isProcessing = false;

            StateHasChanged();
            uploadCompleted = false;
            duplicateDetected = !string.IsNullOrWhiteSpace(SelectedDocument?.FileName);
        }


        // ==========================================
        // SAVE DOCUMENT
        // ==========================================

        private async Task SaveDocument()
        {
            if (selectedFile == null
                ||
                SelectedDocument == null)
            {
                return;
            }

            isProcessing = true;
            //uploadProgress = 20;
            //StateHasChanged();
            //await Task.Delay(200);
            //uploadProgress = 50;
            //StateHasChanged();
            //await Task.Delay(200);
            //uploadProgress = 75;
            //StateHasChanged();

            var result =
                await documentService
                    .UploadDocumentAsync(
                        new ApplicationDocumentDto
                        {
                            ApplicationId = ApplicationId,
                            ApplicationDocumentId = SelectedDocument.ApplicationDocumentId,
                            DocumentId = SelectedDocument.DocumentId,
                            BrowserFile = selectedFile,
                            Remarks = comments
                        });

            //uploadProgress = 100;
            uploadCompleted = true;
            isProcessing = false;
            StateHasChanged();
            await Task.Delay(700);

            if (OnSaved.HasDelegate)
            {
                await OnSaved.InvokeAsync(result);
            }
        }


        // ==========================================
        // CLOSE
        // ==========================================

        private async Task ClosePanel()
        {
            if (OnClosed.HasDelegate)
            {
                await OnClosed.InvokeAsync();
            }
        }



        //THIS SECTION TRIED TO HANDLE THE FILE DRAG & DROP
        // Add this inside the VARIABLES section of FileUploadComponent.razor.cs
        private bool isDragActive = false;
        private InputFile? inputFileReference;

        // ==========================================
        // DRAG & DROP EVENT HANDLERS
        // ==========================================

        //private void HandleDragEnter(DragEventArgs e)
        //{
        //    isDragActive = true;
        //}

        //private void HandleDragLeave(DragEventArgs e)
        //{
        //    isDragActive = false;
        //}

        //private void HandleDragOver(DragEventArgs e)
        //{
        //    // Keeps the drag active state true while hovering
        //    isDragActive = true;
        //}

        private async Task HandleDrop(DragEventArgs e)
        {
            isDragActive = false;
            // Trigger our JS helper to transfer the dropped files directly to the hidden input element
            await JS.InvokeVoidAsync("blazorDropZone.initFileTransfer", "dropZone", "fileDropInput");
        }

        // Helper method to consolidate processing logic for dropped files
        //private async Task AssignAndValidateDroppedFile(DataTransferFile file)
        //{
        //    string extension = Path.GetExtension(file.Name).ToLower();

        //    // FILE TYPE VALIDATION
        //    if (!AllowedExtensions.Contains(extension))
        //    {
        //        await OnSaved.InvokeAsync(new SaveResult
        //        {
        //            Success = false,
        //            Message = "Invalid file type selected."
        //        });
        //        return;
        //    }

        //    // FILE SIZE VALIDATION (Converting long size safely)
        //    if (file.Size > 20 * 1024 * 1024)
        //    {
        //        await OnSaved.InvokeAsync(new SaveResult
        //        {
        //            Success = false,
        //            Message = "Maximum file size is 20MB."
        //        });
        //        return;
        //    }

        //    // Since IBrowserFile is required for your backend processing service, 
        //    // Blazor's DragEventArgs provides file metadata. For a seamless data stream upload 
        //    // inside Blazor Server, we assign a wrapper or mock pointer, or prompt the native 
        //    // InputFile component to handle extraction. 

        //    // To cleanly build the DTO seamlessly without JS Interop bypasses, 
        //    // we instantiate the file metadata tracking profile layout parameters:
        //    selectedFile = new DetachedBrowserFile(file.Name, file.Size, file.ContentType);

        //    isProcessing = true;
        //    uploadProgress = 50;
        //    StateHasChanged();

        //    await Task.Delay(200);
        //    uploadProgress = 100;
        //    uploadCompleted = true;
        //    isProcessing = false;

        //    StateHasChanged();
        //    uploadCompleted = false;
        //    duplicateDetected = !string.IsNullOrWhiteSpace(SelectedDocument?.FileName);
        //}

        // Add or replace these in your DRAG & DROP section
        private void HandleDragEnter(DragEventArgs e) => isDragActive = true;
        private void HandleDragLeave(DragEventArgs e) => isDragActive = false;
        private void HandleDragOver(DragEventArgs e) => isDragActive = true;

        //private async Task HandleDrop(DragEventArgs e)
        //{
        //    isDragActive = false;

        //    // Trigger our JS helper to transfer the dropped files directly to the hidden input element
        //    await JS.InvokeVoidAsync("blazorDropZone.initFileTransfer", "dropZone", "fileDropInput");
        //}
        //DATA MODELS
        public class DetachedBrowserFile : IBrowserFile
        {
            public string Name { get; }
            public DateTimeOffset LastModified { get; }
            public long Size { get; }
            public string ContentType { get; }

            public DetachedBrowserFile(string name, long size, string contentType)
            {
                Name = name;
                Size = size;
                ContentType = contentType;
                LastModified = DateTimeOffset.UtcNow;
            }

            public System.IO.Stream OpenReadStream(long maxAllowedSize = 512000, System.Threading.CancellationToken cancellationToken = default)
            {
                // For processing data streams dropped outside standard `<InputFile>` in Blazor Server,
                // you can return an empty stream here if your service uses a custom JS stream hook,
                // or configure a local byte buffer repository.
                return System.IO.Stream.Null;
            }
        }
    }
}
