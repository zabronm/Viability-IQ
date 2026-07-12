using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblIncomeType")]
    public class IncomeTypes
    {
        [Dapper.Contrib.Extensions.Key] public long IncomeTypeId { get; set; }
        public string? IncomeDescription { get; set; }
        public string? OtherName { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }

    }
}
