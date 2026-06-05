using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Table("tblBusiness")]
    public class Business: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public long BusinessSectorId { get; set; }
        public bool Registered { get; set; }
        public DateTime? RegisteredDate { get; set; } = null;
        public long ClientId { get; set; }
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
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }


        long IEntity.Id => BusinessId;
        string ISortableEntity.DisplayName =>  BusinessName;

    }
}
