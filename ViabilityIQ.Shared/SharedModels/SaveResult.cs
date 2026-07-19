namespace ViabilityIQ.Shared.SharedModels
{
    
    /// Standard response object exchanged between ZabCanvas child forms
    /// and their parent pages.  
    public class SaveResult
    {
        #region Core Status

       
        /// Indicates whether the operation completed successfully.       
        public bool Success { get; set; }

       
        /// Indicates that the user cancelled the operation.       
        public bool Cancelled { get; set; }

        #endregion


        #region Canvas Behaviour
        
        /// Keep the current "Save & New" behaviour.
        /// Preserved for backward compatibility.        
        public bool CreateSave { get; set; }

       
        /// Requests the OffCanvas to close.        
        public bool ClosePanel { get; set; }

       
        /// Clear the form ready for another record.        
        public bool ClearForm { get; set; }

        #endregion

        #region Parent Page Actions

       
        /// Refresh the parent grid.       
        public bool RefreshGrid { get; set; }

       
        /// Refresh summary cards/totals.        
        public bool RefreshSummary { get; set; }

        
        /// Refresh KPI calculations.       
        public bool RefreshKPIs { get; set; }

      
        /// Refresh dashboard widgets.      
        public bool RefreshDashboard { get; set; }

        #endregion

        #region User Feedback

        public string Message { get; set; } = string.Empty;

        public int SelectedDocumentCount { get; set; }

        #endregion


        #region Payload
       
        /// Optional payload returned to the parent.       
        public object? Data { get; set; }

        #endregion

        #region Factory Methods

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

        public static SaveResult SavedAndClose(object data, string message = "")
        {
            var result = SavedAndClose(message);
            result.Data = data;
            return result;
        }

       
        /// Save, keep the canvas open and clear the form.
        /// Ideal for rapid data entry.     
        public static SaveResult SavedAndNew(string message = "")
        {
            return new SaveResult
            {
                Success = true,
                CreateSave = true,
                ClearForm = true,
                RefreshGrid = true,
                ClosePanel = false,
                Message = message
            };
        }

        public static SaveResult SavedAndNew(object data, string message = "")
        {
            var result = SavedAndNew(message);
            result.Data = data;
            return result;
        }

        
        /// Save changes but continue editing the current record.     
        public static SaveResult SavedAndContinue(string message = "")
        {
            return new SaveResult
            {
                Success = true,
                RefreshGrid = true,
                ClosePanel = false,
                ClearForm = false,
                Message = message
            };
        }

        public static SaveResult SavedAndContinue(object data, string message = "")
        {
            var result = SavedAndContinue(message);
            result.Data = data;
            return result;
        }

        
        /// Return an error without closing the canvas.       
        public static SaveResult Failed(string message)
        {
            return new SaveResult
            {
                Success = false,
                ClosePanel = false,
                Message = message
            };
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