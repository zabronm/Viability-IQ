

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

    [Table("tblClient")]
    public class Client: IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? IDNumber { get; set; }      
        public long Gender { get; set; }
        public long Race { get; set; }
        public bool SA_ID { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? FaxNumber { get; set; }
        public string? Street_Address { get; set; }
        public string? Suburb { get; set; }
        public string? CityTown { get; set; }
        public long ProvinceId { get; set; }
        public string? Postal_Address { get; set; }
        public string? Postal_City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = default;
        public DateTime ModifiedDate { get; set; }       
        public long ModifiedBy { get; set; }


        long IEntity.Id => ClientId;
        string ISortableEntity.DisplayName =>  ClientName;

    }
}
