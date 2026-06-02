using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Infrastructure.Reporting
{
    public class ExcelEPPlusExportService : IExcelEPPlusExportService
    {
        //public ExcelEPPlusExportService()
        //{
        //    // Set the license context required by EPPlus 5+
        //    ExcelPackage.License = LicenseContext.NonCommercial;
        //    EPPlusLicense.
        //}

        public async Task<byte[]> GenerateDataReportExcelAsync<T>(List<T> dataset, string worksheetTitle)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(worksheetTitle);

                // 1. Get readable public properties from the generic model
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // 2. Generate and Style Headers
                for (int col = 0; col < properties.Length; col++)
                {
                    var cell = worksheet.Cells[1, col + 1];
                    cell.Value = properties[col].Name;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Size = 11;
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(13, 110, 253)); // Corporate Blue Accent
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }

                // 3. Populate Row Data Rows
                for (int row = 0; row < dataset.Count; row++)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var cell = worksheet.Cells[row + 2, col + 1];
                        var rawValue = properties[col].GetValue(dataset[row]);

                        // Handle formatting types gracefully
                        if (rawValue is DateTime dateVal)
                            cell.Value = dateVal.ToString("yyyy-MM-dd HH:mm");
                        else if (rawValue is bool boolVal)
                            cell.Value = boolVal ? "Active" : "Suspended";
                        else
                            cell.Value = rawValue?.ToString() ?? string.Empty;

                        // Subtle zebra striping for data density visibility
                        if (row % 2 != 0)
                        {
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 249, 250));
                        }
                    }
                }

                // 4. Auto-fit column spacing cleanly based on cell content size lengths
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            });
        }
    }
}
