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
    [Table("tblLoanType")]
    public class LoanType: IEntity, ISortableEntity, IAuditableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long LoanTypeId { get; set; }
        public string? ShortName { get; set; }
        [Required] public string LoanTypeName { get; set; }


        public string? Remarks { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy    { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => LoanTypeId;
        string ISortableEntity.DisplayName => LoanTypeName;
    }
}
