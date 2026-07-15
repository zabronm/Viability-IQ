using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblIncomeType")]
    public class IncomeType: IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long IncomeTypeId { get; set; }

        [Required(ErrorMessage = "Revenue type name is required.")]
        public string? IncomeTypeName { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => IncomeTypeId;
        string ISortableEntity.DisplayName => IncomeTypeName;
    }
}
