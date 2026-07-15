using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Table("tblBusiness")]
    public class Business: IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long BusinessId { get; set; }
        [Required(ErrorMessage = "Business Name is required.")]
        public string? BusinessName { get; set; }

        [Required(ErrorMessage = "Business Sector must be known.")]
        public long BusinessSectorId { get; set; }
        public bool IsRegistered { get; set; }      
        public bool IsVATRegistered { get; set; }
        public bool IsBEE_Exempt { get; set; }
        public DateTime? RegisteredDate { get; set; } = null;
        [Required(ErrorMessage = "Business Owner(Client) is required.")]
        public long ClientId { get; set; }
        public string? CKNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? Street_Address { get; set; }
        public string? Surburb { get; set; }
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
