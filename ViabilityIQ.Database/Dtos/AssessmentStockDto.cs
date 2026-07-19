using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Dtos
{
    public class AssessmentStockDto
    {
        public long AssessmentStockId { get; set; }
        public long AssessmentId { get; set; }
        public long AssessmentSalesCategoryId { get; set; }
        public string? SalesCategoryName { get; set; }
        public string? Description { get; set; }
        public bool blIncludeVAT { get; set; }
        public decimal SameMonthlyAmount { get; set; }
        public decimal Month_1 { get; set; }
        public decimal Month_2 { get; set; }
        public decimal Month_3 { get; set; }
        public decimal Month_4 { get; set; }
        public decimal Month_5 { get; set; }
        public decimal Month_6 { get; set; }
        public decimal Month_7 { get; set; }
        public decimal Month_8 { get; set; }
        public decimal Month_9 { get; set; }
        public decimal Month_10 { get; set; }
        public decimal Month_11 { get; set; }
        public decimal Month_12 { get; set; }
        public decimal TotalNoVAT { get; set; }
        public decimal TotalWithVAT { get; set; }
    }
}
