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
    [Table("tblProvince")]
    public class Province: IEntity, ISortableEntity, IAuditableEntity
    {
        [Key] public long ProvinceId { get; set; }
        public string? ShortName { get; set; }
        public string? ProvinceName { get; set; }
        public string? Address_Street {get; set;} 
	    public string? Address_Location {get; set;}
	    public string? Address_Suburb {get; set;}
	    public string? Address_City {get; set;} 
	    public string? Manager_UserId {get; set;}
	    public string? Telephone {get; set;}
	    public string? Mobile {get; set;} 
	    public string? Email {get; set;}
	    public string? Address_Postal {get; set;} 
	    public string? Address_PostalLocation {get; set;} 
	    public string? Address_PostalCity {get; set;} 
	    public string? Address_PostalCode {get; set;} 
        public bool Active { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => ProvinceId;
        string ISortableEntity.DisplayName => ProvinceName;
    }
}
