using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Table("vw_business_list")]
    public class BusinessDto
    {
        [Dapper.Contrib.Extensions.Key] public long BusinessId { get; set; }     
        public string? BusinessName { get; set; }
        public bool Registered { get; set; }
        public bool VATRegistered { get; set; }
        public bool BEE_Exempt { get; set; }
        public long BusinessSectorId { get; set; }     
        public string ? BusinessSectorName { get; set; }
        public DateTime? RegisteredDate { get; set; } = null;
        public long ClientId { get; set; }
        public string? Client { get; set; }
        public string? CKNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? Street_Address { get; set; }
        public string? Surburb { get; set; }        
        public string? CityTown { get; set; }
        public long ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
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

    }
}
