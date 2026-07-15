using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModels
{

    [Dapper.Contrib.Extensions.Table("tblAssessmentIncomeStatement")]
    public class AssessmentIncomeStatement
    {
        [Key] public long IncomeStatementId { get; set; }
        public long AssessmentId { get; set; }

        //============= MONTHLY REVENUE ===================
        public decimal RevenueMonth_1 { get; set; }  
        public decimal RevenueMonth_2 { get; set; }
        public decimal RevenueMonth_3 { get; set; }
        public decimal RevenueMonth_4 { get; set; }
        public decimal RevenueMonth_5 { get; set; }
        public decimal RevenueMonth_6 { get; set; }
        public decimal RevenueMonth_7 { get; set; }
        public decimal RevenueMonth_8 { get; set; }
        public decimal RevenueMonth_9 { get; set; }
        public decimal RevenueMonth_10 { get; set; }
        public decimal RevenueMonth_11 { get; set; }
        public decimal RevenueMonth_12 { get; set; }


        //================ COST OF SALES FOR THE ASSESSMENT ====
        public decimal CostOfSalesMonth_1 { get; set; }
        public decimal CostOfSalesMonth_2 { get; set; }
        public decimal CostOfSalesMonth_3 { get; set; }
        public decimal CostOfSalesMonth_4 { get; set; }
        public decimal CostOfSalesMonth_5 { get; set; }
        public decimal CostOfSalesMonth_6 { get; set; }
        public decimal CostOfSalesMonth_7 { get; set; }
        public decimal CostOfSalesMonth_8 { get; set; }
        public decimal CostOfSalesMonth_9 { get; set; }
        public decimal CostOfSalesMonth_10 { get; set; }
        public decimal CostOfSalesMonth_11 { get; set; }
        public decimal CostOfSalesMonth_12 { get; set; }

        //================EXPENSES FOR THE ASSESSMENT ==========
        public decimal ExpensesMonth_1 { get; set; }
        public decimal ExpensesMonth_2 { get; set; }
        public decimal ExpensesMonth_3 { get; set; }
        public decimal ExpensesMonth_4 { get; set; }
        public decimal ExpensesMonth_5 { get; set; }
        public decimal ExpensesMonth_6 { get; set; }
        public decimal ExpensesMonth_7 { get; set; }
        public decimal ExpensesMonth_8 { get; set; }
        public decimal ExpensesMonth_9 { get; set; }
        public decimal ExpensesMonth_10 { get; set; }
        public decimal ExpensesMonth_11 { get; set; }
        public decimal ExpensesMonth_12 { get; set; }       



        public decimal GrossProfit { get; set; }

        public decimal NetProfit { get; set; }

        public decimal EBITDA { get; set; }
    }
}
