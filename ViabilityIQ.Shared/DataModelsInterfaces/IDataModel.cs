using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Shared.DataModelsInterfaces
{
    public interface IEntity
    {
        long Id { get; }
    }


    public interface ISortableEntity
    { 
        string DisplayName { get; }
    }


    public interface IAuditableEntity
    { 
        DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get;set; }
        public long CreatedBy { get; set; }
        bool Active { get; set; }
        string? Remarks { get; set; }
    }
}
