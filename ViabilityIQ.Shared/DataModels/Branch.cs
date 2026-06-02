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

	[Table("tblBranch")]
	public class Branch : IEntity, IAuditableEntity, ISortableEntity
	{
		[Key] public long BranchId { get; set; }
		public string? BranchName { get; set; }
	    public string? Address_Street { get; set; }
        public string? Address_Location { get; set; }
        public string? Address_Suburb { get; set; }
        public string? CityTown { get; set; }
        public string? Address_Postal { get; set; }
        public string? Address_PostalLocation { get; set; }
        public string? Address_PostalCity { get; set; }
        public string? Address_PostalCode { get; set; }
        public long ProvinceId { get; set; }
        public long Manager_UserId { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public bool Active { get; set; }
        public string? Remarks { get; set; }       
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public long ModifiedBy { get; set; }

        long IEntity.Id => BranchId;
        string ISortableEntity.DisplayName => BranchName;

    }
}
