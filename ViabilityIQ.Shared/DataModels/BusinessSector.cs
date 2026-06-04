using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Shared.DataModels
{

    [TableName("tblBusinessSector")]
    public class BusinessSector : IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long BusinessSectorId {get; set;}
        public string? BusinessSectorName {get; set;}
        public string? Remarks {get; set;}
        public bool Active {get; set;}
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy   {get; set;}
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy {get; set;}

        long IEntity.Id => BusinessSectorId;
        string ISortableEntity.DisplayName => BusinessSectorName;

    }
}
