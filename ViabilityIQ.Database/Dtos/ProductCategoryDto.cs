using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class ProductCategoryDto
    {
        public long ProductCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? UoM { get; set; }
        public decimal? Markup { get; set; }
        public bool? Active { get; set; }
        public string? Remarks { get; set; }
    }
}
