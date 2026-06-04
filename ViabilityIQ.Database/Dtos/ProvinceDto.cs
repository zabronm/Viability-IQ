using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Dtos
{

    [Table("dbo.vw_provinces_list")]
    public class ProvinceDto
    {
        public long ProvinceId { get; set; }
        public string? ShortName { get; set; }
        public string? ProvinceName { get; set; }
        public string? Address_Street { get; set; }
        public string? Address_Location { get; set; }
        public string? Address_Suburb { get; set; }
        public string? Address_City { get; set; }
        public string? Manager_UserId { get; set; }
        public string? Manager { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address_Postal { get; set; }
        public string? Address_PostalLocation { get; set; }
        public string? Address_PostalCity { get; set; }
        public string? Address_PostalCode { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }        
    }
}
