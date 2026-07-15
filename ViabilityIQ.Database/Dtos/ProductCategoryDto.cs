using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    [Table("vw_product_category_list")]
    public class ProductCategoryDto
    {
        [Key] public long ProductCategoryId { get; set; }
        public long IncomeTypeId { get; set; }
        public string? ProductCategoryName { get; set; }
        public string? IncomeTypeName { get; set; }
        public string? UoM { get; set; }
        public decimal? MarkupPercentage { get; set; }
        public bool? Active { get; set; }
        public string? Remarks { get; set; }
    }
}
