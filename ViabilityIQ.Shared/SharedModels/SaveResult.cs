namespace ViabilityIQ.Shared.SharedModels
{
    public class SaveResult
    {
        public bool Success { get; set; }
        public bool Cancelled { get; set; }
        public bool CreateSave { get; set; }
        public bool ClosePanel { get; set; }
        public bool RefreshGrid { get; set; }
        public bool ClearForm { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SelectedDocumentCount { get; set; }

        /// <summary>
        /// Holds the returned context or payload data (e.g., UnifiedIncomeViewModel) 
        /// passed back from form components to parents during state updates.
        /// </summary>
        public object? Data { get; set; }

        #region Static Factory Methods

        public static SaveResult Saved(string message = "")
        {
            return new SaveResult
            {
                Success = true,
                RefreshGrid = true,
                ClosePanel = false,
                Message = message
            };
        }

        // Overload to support carrying the form payload data without closing the panel
        public static SaveResult Saved(object data, string message = "")
        {
            var result = Saved(message);
            result.Data = data;
            return result;
        }

        public static SaveResult SavedAndClose(string message = "")
        {
            return new SaveResult
            {
                Success = true,
                RefreshGrid = true,
                ClosePanel = true,
                Message = message
            };
        }

        // Overload to support carrying the payload data and closing the panel
        public static SaveResult SavedAndClose(object data, string message = "")
        {
            var result = SavedAndClose(message);
            result.Data = data;
            return result;
        }

        public static SaveResult Cancel(string message = "")
        {
            return new SaveResult
            {
                Success = false,
                Cancelled = true,
                RefreshGrid = false,
                ClosePanel = true,
                Message = message
            };
        }

        #endregion
    }
}