using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class ProductServiceDto
    {
        public long ProductServiceId { get; set; }

        [DisplayName("Product/Service")]
        public string? ProductServiceName { get; set; }
        public string? OtherName { get; set; }
        [DisplayName("Markup(%)")]
        public decimal? MarkupPercentage { get; set; }  //will check if charged per product or product category
        public long ProductCategoryId { get; set; }
        [DisplayName("Category")]
        public string? ProductCategoryName { get; set; }
        [DisplayName("Type")]
        public bool ProductOrService { get; set; } = true;
        public string? Remarks { get; set; }
        public bool Active { get; set; }
    }
}
