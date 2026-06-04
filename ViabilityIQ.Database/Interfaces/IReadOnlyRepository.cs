using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IReadOnlyRepository<TDto, TId> where TDto : class                               //see below for usage notes
    {
       
        /// 1) Returns all records mapped directly to the DTO payload.       
        Task<IEnumerable<TDto>> GetAllAsync();
       
        /// 2) Returns all records matching a specific foreign key or relational ID field.        
        Task<IEnumerable<TDto>> GetListByIdAsync(string idFieldName, TId idValue);
       
        /// 3) Returns a single record (FirstOrDefault) matching an identifier.      
        Task<TDto?> GetFirstOrDefaultAsync(string idFieldName, TId idValue);
    }
}


//========USAGE EXAMPLE OF THE INTERFACE REPOSITORY ABOVE 1) ALL RECORDS NO KEY, 2 MULTIPLE RECORDS ONE KEY, 3) ONE RECORD, ONE KEY
//====================================================================
//@inject IReadOnlyRepository<ProductServiceReportDto, long> ReportRepo

//<h3> Product Catalog Data Print Ledger</h3>

//@code {
//    private List<ProductServiceReportDto> _allRecords = new();
//private List<ProductServiceReportDto> _categoryRecords = new();
//private ProductServiceReportDto? _singleRecord;

//protected override async Task OnInitializedAsync()
//{
//    // 1. Returns all records from the view
//    var data1 = await ReportRepo.GetAllAsync();
//    _allRecords = data1.ToList();

//    // 2. Pass the foreign key column name + ID value to get filtered list
//    var data2 = await ReportRepo.GetListByIdAsync("ProductCategoryId", 4);
//    _categoryRecords = data2.ToList();

//    // 3. Returns FirstOrDefault matching specific ID parameters
//    _singleRecord = await ReportRepo.GetFirstOrDefaultAsync("ProductServiceId", 1025);
//}
//}