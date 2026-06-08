using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblDirectorWages")]
    public  class DirectorWages:  IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long DirectorWagesId { get; set; }
        public long AssessmentId { get; set; }
        public decimal MonthlyDirectorWagesAmountTotal { get; set; }
        public decimal MonthlyDirectorWagesAmount { get; set; }
        public int NumberOfDirectors { get; set; }
        public bool Active { get; set; }
        public String Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }

        long IEntity.Id => DirectorWagesId;
        string ISortableEntity.DisplayName => throw new NotImplementedException();


    }
}
