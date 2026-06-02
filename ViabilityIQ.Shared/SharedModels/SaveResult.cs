using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class SaveResult
    {
        public bool Success { get; set; }
        public bool CreateSave { get; set; }
        public bool ClosePanel { get; set; }

        public bool RefreshGrid { get; set; }

        public bool ClearForm { get; set; }

        public string Message { get; set; } = "";
        public int SelectedDocumentCount { get; set; }
    }
}

