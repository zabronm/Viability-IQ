using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Application.Dtos
{
    public class ClientDto
    {
        public long ClientId { get; set; }      
        public string? ClientName { get; set; }       
        public string? IDNumber { get; set; }
        public long Gender { get; set; }
        public long Race { get; set; }
        public bool SA_ID { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        [DisplayName("Street Address")]
        public string? Street_Address { get; set; }
        public string? Suburb { get; set; }
        public string? CityTown { get; set; }
        public long ProvinceId { get; set; }
        public string? Province { get; set; }
        public string? Postal_Address { get; set; }
        public string? Postal_City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
    }
}
