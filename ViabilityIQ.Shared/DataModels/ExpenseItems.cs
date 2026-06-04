using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Shared.DataModels
{
    [TableName("tblExpenseItems")]
    public class ExpenseItems: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long ExpenseItemId { get; set; }
        public string? ExpenseItemName { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public long  ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }

        long IEntity.Id => ExpenseItemId;
        string ISortableEntity.DisplayName => ExpenseItemName;
    }
}
