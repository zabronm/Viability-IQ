using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;


namespace ViabilityIQ.Application.Dtos
{

    [TableName("dbo.vw_client_list")] // Point it straight to your view!
    public class ClientDto
    {
        [Key] public long ClientId { get; set; }      
        public string? ClientName { get; set; }       
        public string? IDNumber { get; set; }
        public long GenderId { get; set; }
        public string? Gender { get; set; }
        public string? Race { get; set; }
        public long RaceId { get; set; }       
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }       
        public string? Address_Street{ get; set; }
        public string? Address_Surburb { get; set; }
        public string? Address_CityTown { get; set; }
        public long ClientTypeId { get; set; }
        public string? ClientType { get; set; }
        public long ProvinceId { get; set; }
        public string? ProvinceName { get; set; }
        public string? Address_Postal { get; set; }
        public string? Address_PostalLocation { get; set; }
        public string? Address_PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
    }
}
