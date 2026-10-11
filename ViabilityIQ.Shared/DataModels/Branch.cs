using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dapper.Contrib.Extensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{

	[Dapper.Contrib.Extensions.Table("tblBranch")]
	public class Branch : IEntity, IAuditableEntity, ISortableEntity, ITenantEntity
	{
		[Dapper.Contrib.Extensions.Key] public long BranchId { get; set; }
        public long? TenantId { get; set; }
        [StringLength(12)]
        public string? BranchCode { get; set; }
        [Required(ErrorMessage = "Site name is required.")]
		public string? BranchName { get; set; }
	    public string? Address_Street { get; set; }
        public string? Address_Location { get; set; }
        public string? Address_Suburb { get; set; }
        public string? CityTown { get; set; }
        public string? Address_Postal { get; set; }
        public string? Address_PostalLocation { get; set; }
        public string? Address_PostalCity { get; set; }
        public string? Address_PostalCode { get; set; }
        [Range(1, long.MaxValue, ErrorMessage = "Region is required.")]
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
