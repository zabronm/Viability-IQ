using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblCompany")]
    public class Company: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CKNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? Street_Address { get; set; }
        public string? Suburb { get; set; }
        public string? CityTown { get; set; }
        public long ProvinceId { get; set; }
        public string? Country { get; set; }  //Use public, free API to get country name
        public string? Postal_Address { get; set; }
        public string? Postal_CityTown { get; set; }    
        public string? PostalCode { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }


        long IEntity.Id => CompanyId;
        string ISortableEntity.DisplayName => CompanyName;

  
    }
}
