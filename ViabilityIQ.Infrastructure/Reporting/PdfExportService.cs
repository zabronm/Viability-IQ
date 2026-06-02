using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;


namespace ViabilityIQ.Infrastructure.Reporting
{
    public class PdfExportService: IPdfExportService
    {

        public PdfExportService()
        {
            // Set the QuestPDF Community License context globally
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateReportDataPdfAsync<T>(List<T> dataset, string documentTitle)
        {
            return await Task.Run(() =>
            {
                // Extract public instance properties from the model metadata layer
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape()); // Wide profile blueprint for complex tables
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                        // 1. HEADER SECTION DEFINITION
                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text(documentTitle).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                column.Item().Text($"System Data Extract generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // 2. MAIN TABLE CONTENT LAYOUT ENGINE
                        page.Content().PaddingTop(1, Unit.Centimetre).Table(table =>
                        {
                            // Initialize column mappings dynamically
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var prop in properties)
                                {
                                    columns.RelativeColumn();
                                }
                            });

                            // Generate & Style Table Header Row (Repeats gracefully across page boundaries)
                            table.Header(header =>
                            {
                                foreach (var prop in properties)
                                {
                                    // 1. Check if the property has a DisplayName attribute configured
                                    var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();

                                    // 2. Use the custom display text if present; otherwise fallback to raw property string name
                                    string headerText = displayNameAttribute != null ? displayNameAttribute.DisplayName : prop.Name;

                                    header.Cell()
                                                .Background(Colors.Blue.Darken2)
                                                .Padding(6)
                                                .Text(prop.Name)
                                                .Bold()
                                                .FontColor(Colors.White);
                                }
                            });

                            // Inject Rows Context Array
                            for (int r = 0; r < dataset.Count; r++)
                            {
                                // Alternate backgrounds for clean data line scanning visibility
                                var rowBgColor = (r % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                for (int c = 0; c < properties.Length; c++)
                                {
                                    var rawValue = properties[c].GetValue(dataset[r]);
                                    string outputDisplayString = rawValue switch
                                    {
                                        DateTime dateVal => dateVal.ToString("yyyy-MM-dd"),
                                        bool boolVal => boolVal ? "Active" : "Suspended",
                                        _ => rawValue?.ToString() ?? string.Empty
                                    };

                                    table.Cell().Background(rowBgColor)
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .Padding(5)
                                                .AlignMiddle()
                                                .Text(outputDisplayString);
                                }
                            }
                        });

                        // 3. FOOTER SECTION SYSTEM METRICS
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });

                return document.GeneratePdf();
            });
        }
    }
}
