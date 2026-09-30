using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.Reporting;

namespace ViabilityIQ.Application.Reporting;

public sealed class OpenXmlReportWorkbookWriter : IReportWorkbookWriter
{
    public byte[] Write(ReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Add(archive, "[Content_Types].xml", ContentTypes);
            Add(archive, "_rels/.rels", PackageRelationships);
            Add(archive, "docProps/core.xml", CoreProperties(document));
            Add(archive, "docProps/app.xml", AppProperties);
            Add(archive, "xl/workbook.xml", Workbook);
            Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships);
            Add(archive, "xl/styles.xml", Styles);
            Add(archive, "xl/worksheets/sheet1.xml", BuildSheet(document));
        }
        return output.ToArray();
    }

    private static string BuildSheet(ReportDocument document)
    {
        var rows = new List<IReadOnlyList<Cell>>();
        rows.Add([Text(document.Definition.Name, 1)]);
        rows.Add([Text("Entity", 2), Text(document.EntityName ?? "Not recorded")]);
        rows.Add([Text("Assessment", 2), Text(document.AssessmentReference)]);
        rows.Add([Text("Period", 2), Text(document.ReportingPeriod)]);
        rows.Add([Text("Generated (UTC)", 2), Text(document.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"))]);
        rows.Add([Text("Status", 2), Text(document.ReadinessStatus)]);
        if (!string.IsNullOrWhiteSpace(document.ReadinessWarning))
            rows.Add([Text("Warning", 2), Text(document.ReadinessWarning)]);
        rows.Add([]);

        foreach (var section in document.Sections)
        {
            rows.Add([Text(section.Title, 1)]);
            if (!string.IsNullOrWhiteSpace(section.Narrative))
                rows.Add([Text(section.Narrative)]);
            foreach (var metric in section.Metrics)
                rows.Add([Text(metric.Label, 2),
                    metric.NumericValue.HasValue ? Number(metric.NumericValue.Value, 3) : Text(metric.DisplayValue)]);
            if (section.Table is not null)
            {
                rows.Add(section.Table.Headers.Select(x => Text(x, 2)).ToArray());
                foreach (var row in section.Table.Rows)
                {
                    if (row.IsHeading)
                    {
                        rows.Add([Text(row.Label, 2)]);
                        continue;
                    }
                    var style = row.IsPercentage ? 5 : row.IsTotal ? 4 : 3;
                    rows.Add([Text(row.Label, row.IsTotal ? 2 : 0),
                        .. row.Values.Select(value => value.HasValue ? Number(value.Value, style) : Text("—"))]);
                }
            }
            foreach (var finding in section.Findings)
            {
                rows.Add([Text($"{finding.Priority}. {finding.Title}", 2), Text(finding.Severity)]);
                rows.Add([Text("Evidence"), Text(finding.Evidence)]);
                rows.Add([Text("Recommended action"), Text(finding.Recommendation)]);
            }
            rows.Add([]);
        }

        if (document.Footnotes.Count > 0)
        {
            rows.Add([Text("Notes", 1)]);
            rows.AddRange(document.Footnotes.Select(note => (IReadOnlyList<Cell>)[Text(note)]));
        }

        var maxColumns = Math.Max(2, rows.Max(row => row.Count));
        var xml = new StringBuilder();
        xml.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        xml.Append($"<dimension ref=\"A1:{ColumnName(maxColumns)}{rows.Count}\"/>");
        xml.Append("""<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");
        xml.Append("<cols><col min=\"1\" max=\"1\" width=\"34\" customWidth=\"1\"/>");
        if (maxColumns >= 2)
            xml.Append($"<col min=\"2\" max=\"{maxColumns}\" width=\"16\" customWidth=\"1\"/>");
        xml.Append("</cols><sheetData>");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            xml.Append($"<row r=\"{rowIndex + 1}\">");
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                var cell = rows[rowIndex][columnIndex];
                var reference = $"{ColumnName(columnIndex + 1)}{rowIndex + 1}";
                if (cell.IsNumber)
                    xml.Append($"<c r=\"{reference}\" s=\"{cell.Style}\"><v>{cell.Value}</v></c>");
                else
                    xml.Append($"<c r=\"{reference}\" s=\"{cell.Style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(cell.Value)}</t></is></c>");
            }
            xml.Append("</row>");
        }
        xml.Append("</sheetData><autoFilter ref=\"A1:");
        xml.Append(ColumnName(maxColumns));
        xml.Append("1\"/><pageMargins left=\"0.25\" right=\"0.25\" top=\"0.5\" bottom=\"0.5\" header=\"0.2\" footer=\"0.2\"/>");
        xml.Append($"<pageSetup orientation=\"{(document.Definition.Landscape ? "landscape" : "portrait")}\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
        xml.Append("</worksheet>");
        return xml.ToString();
    }

    private static Cell Text(string value, int style = 0) => new(value ?? string.Empty, style, false);
    private static Cell Number(decimal value, int style) =>
        new(value.ToString(CultureInfo.InvariantCulture), style, true);
    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
    private static string ColumnName(int number)
    {
        var name = string.Empty;
        while (number > 0)
        {
            number--;
            name = (char)('A' + number % 26) + name;
            number /= 26;
        }
        return name;
    }
    private static void Add(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string CoreProperties(ReportDocument document) =>
        $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>{Escape(document.Definition.Name)}</dc:title><dc:creator>ViabilityIQ</dc:creator><dcterms:created xsi:type="dcterms:W3CDTF">{document.GeneratedAtUtc:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created></cp:coreProperties>""";

    private sealed record Cell(string Value, int Style, bool IsNumber);

    private const string ContentTypes =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/><Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/></Types>""";
    private const string PackageRelationships =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>""";
    private const string AppProperties =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>ViabilityIQ</Application></Properties>""";
    private const string Workbook =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets></workbook>""";
    private const string WorkbookRelationships =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";
    private const string Styles =
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><numFmts count="2"><numFmt numFmtId="164" formatCode="R #,##0;[Red](R #,##0);-"/><numFmt numFmtId="165" formatCode="0.0%"/></numFmts><fonts count="3"><font><sz val="10"/><name val="Aptos"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="14"/><name val="Aptos Display"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="10"/><name val="Aptos"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF12324A"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="2"><border/><border><bottom style="thin"><color rgb="FF9FB5C5"/></bottom></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="6"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0"/><xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0"/><xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/><xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1"/><xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>""";
}
