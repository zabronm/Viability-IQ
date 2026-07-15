

using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Table("tblClient")]
    public class Client: IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long ClientId { get; set; }
        [Required(ErrorMessage ="Business Owner/client name is required.")]
        public string? FullName { get; set; }
        [Required(ErrorMessage = "ID/Passport number is required")]        
        public string? IDNumber { get; set; }
        [Required(ErrorMessage ="Client category is required")]
        public long ClientTypeId { get; set; }
        public long GenderId { get; set; }
        public long RaceId { get; set; }
        public bool SA_ID { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? FaxNumber { get; set; }
        public string? Address_Street { get; set; }
        public string? Address_Surburb { get; set; }
        public string? Address_CityTown { get; set; }
        public long ProvinceId { get; set; }
        public string? Address_Postal { get; set; }
        public string? Address_PostalLocation { get; set; }
        public string? Address_PostalCity { get; set; }
        public string? Address_PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow; 
        public long ModifiedBy { get; set; }


        long IEntity.Id => ClientId;
        string ISortableEntity.DisplayName =>  FullName;

    }
}
