using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages.Support;

public partial class SupportTicketFormComponent
{
    private const long MaximumFileBytes = 10 * 1024 * 1024;
    private const long MaximumCombinedFileBytes = 25 * 1024 * 1024;
    private const int MaximumFiles = 5;

    [Inject] private ISupportService SupportService { get; set; } = default!;
    [Inject] private ILogger<SupportTicketFormComponent> Logger { get; set; } = default!;

    private readonly SupportTicketRequest _request = new();
    private readonly List<SupportFileAttachment> _attachments = [];
    private SupportTicketResult? _result;
    private string? _error;
    private bool _isSubmitting;

    private async Task SelectFilesAsync(InputFileChangeEventArgs args)
    {
        _error = null;
        try
        {
            var selectedFiles = args.GetMultipleFiles(MaximumFiles);
            var combinedSize = _attachments.Sum(file => (long)file.Content.Length)
                + selectedFiles.Sum(file => file.Size);
            if (combinedSize > MaximumCombinedFileBytes)
                throw new InvalidOperationException("The combined attachment size cannot exceed 25 MB.");

            foreach (var file in selectedFiles)
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(file.Name)))
                    throw new InvalidOperationException($"'{file.Name}' is not a supported attachment type.");
                if (file.Size <= 0 || file.Size > MaximumFileBytes)
                    throw new InvalidOperationException($"'{file.Name}' must be a non-empty file no larger than 10 MB.");

                await using var stream = file.OpenReadStream(MaximumFileBytes);
                using var buffer = new MemoryStream((int)file.Size);
                await stream.CopyToAsync(buffer);
                _attachments.Add(new SupportFileAttachment(
                    Path.GetFileName(file.Name),
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    buffer.ToArray()));
            }
        }
        catch (InvalidOperationException exception)
        {
            _error = exception.Message;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to load support ticket attachment.");
            _error = "One or more attachments could not be read. Please choose the files again.";
        }
    }

    private void RemoveAttachment(SupportFileAttachment attachment) =>
        _attachments.Remove(attachment);

    private async Task SubmitAsync()
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        _error = null;
        try
        {
            _result = await SupportService.SubmitTicketAsync(_request, _attachments);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to submit a support ticket.");
            _error = exception is ArgumentException
                ? exception.Message
                : "We could not submit your ticket. Please try again.";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private static readonly HashSet<string> AllowedExtensions = new(
        [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".png", ".jpg", ".jpeg", ".zip"],
        StringComparer.OrdinalIgnoreCase);
}
