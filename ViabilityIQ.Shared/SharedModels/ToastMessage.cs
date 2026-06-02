using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.SharedModels
{
    public class ToastMessage
    {

        public bool IsHovered { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string CssClass { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Duration { get; set; } = 6000;
    }
}

