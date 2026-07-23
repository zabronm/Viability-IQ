using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{
    [Table("vw_AssessmentLoanRepayment_list")]
    public class AssessmentLoanRepaymentDto 
    {
        [Dapper.Contrib.Extensions.Key] public long AssessmentLoanRepaymentId { get; set; }
        [Required] public long AssessmentId { get; set; }
        public long AssessmentLoanId { get; set;  } 
        public long LoanTypeId { get; set; }
        public string? LoanTypeName { get; set; }
        public long BankId { get; set; }
        public string? BankName { get; set; }
        public DateTime LoanDate { get; set; }
        public decimal LoanAmount { get; set; }
        public int StartMonth { get; set; }
        public int  MetricTypeId { get; set; }// "1=>ExpectedRepayment", "2 =>Interest", "3=>Extra"
        [Write(false)]
        public string MetricType => MetricTypeId switch
        {
            1 => "Expected Repayment",
            2 => "Calculated Interest",
            3 => "Extra Repayment",
            _ => "Unknown"
        };
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

        [Write(false)]
        public decimal[] MonthlyValues
        {
            get => new decimal[] { Month_1, Month_2, Month_3, Month_4, Month_5, Month_6, Month_7, Month_8, Month_9, Month_10, Month_11, Month_12 };
            set
            {
                if (value.Length == 12)
                {
                    Month_1 = value[0]; Month_2 = value[1]; Month_3 = value[2]; Month_4 = value[3];
                    Month_5 = value[4]; Month_6 = value[5]; Month_7 = value[6]; Month_8 = value[7];
                    Month_9 = value[8]; Month_10 = value[9]; Month_11 = value[10]; Month_12 = value[11];
                }
            }
        }

    }
}
