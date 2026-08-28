using System.Globalization;
using System.IO.Compression;
using System.Text;
using BLL.Admin.Interfaces;

namespace BLL.Admin.Services
{
    /// <summary>
    /// Формує .xlsx з одним аркушем одразу в масив байтів.
    /// <para>
    /// Книга Excel — це zip-архів із XML-частин, тому збирання її вручну звільняє рішення від
    /// залежності на Excel-бібліотеку (ClosedXML/EPPlus), яку довелося б відновлювати всій команді.
    /// Рядки пишуться вбудовано, а не через спільну таблицю рядків: це коштує кількох зайвих байтів,
    /// зате знімає цілий клас помилок з індексами.
    /// </para>
    /// </summary>
    public class ExcelExportService : IExcelExportService
    {
        private const string SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Індекси стилів у cellXfs нижче.
        private const int StyleDefault = 0;
        private const int StyleHeader = 1;
        private const int StyleDate = 2;
        private const int StyleMoney = 3;

        public byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
        {
            ArgumentNullException.ThrowIfNull(headers);
            ArgumentNullException.ThrowIfNull(rows);

            var materialised = rows as IList<IReadOnlyList<object?>> ?? rows.ToList();
            var safeSheetName = SanitiseSheetName(sheetName);

            using var buffer = new MemoryStream();

            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(archive, "[Content_Types].xml", ContentTypes());
                WriteEntry(archive, "_rels/.rels", RootRelationships());
                WriteEntry(archive, "xl/workbook.xml", Workbook(safeSheetName));
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
                WriteEntry(archive, "xl/styles.xml", Styles());
                WriteEntry(archive, "xl/worksheets/sheet1.xml", Sheet(headers, materialised));
            }

            return buffer.ToArray();
        }

        // -----------------------------------------------------------------
        // Частини книги
        // -----------------------------------------------------------------

        private static string ContentTypes() =>
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
              <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
            </Types>
            """;

        private static string RootRelationships() =>
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="{RelationshipsNs}/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """;

        private static string Workbook(string sheetName) =>
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="{SpreadsheetNs}" xmlns:r="{RelationshipsNs}">
              <sheets>
                <sheet name="{Escape(sheetName)}" sheetId="1" r:id="rId1"/>
              </sheets>
            </workbook>
            """;

        private static string WorkbookRelationships() =>
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="{RelationshipsNs}/worksheet" Target="worksheets/sheet1.xml"/>
              <Relationship Id="rId2" Type="{RelationshipsNs}/styles" Target="styles.xml"/>
            </Relationships>
            """;

        private static string Styles() =>
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <styleSheet xmlns="{SpreadsheetNs}">
              <numFmts count="1">
                <numFmt numFmtId="164" formatCode="yyyy\-mm\-dd\ hh:mm"/>
              </numFmts>
              <fonts count="2">
                <font><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
                <font><b/><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
              </fonts>
              <fills count="3">
                <fill><patternFill patternType="none"/></fill>
                <fill><patternFill patternType="gray125"/></fill>
                <fill><patternFill patternType="solid"><fgColor rgb="FFEFF3F8"/><bgColor indexed="64"/></patternFill></fill>
              </fills>
              <borders count="1">
                <border><left/><right/><top/><bottom/><diagonal/></border>
              </borders>
              <cellStyleXfs count="1">
                <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
              </cellStyleXfs>
              <cellXfs count="4">
                <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
                <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
                <xf numFmtId="4" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
              </cellXfs>
            </styleSheet>
            """;

        private static string Sheet(IReadOnlyList<string> headers, IList<IReadOnlyList<object?>> rows)
        {
            var columnCount = Math.Max(headers.Count, rows.Count == 0 ? 0 : rows.Max(r => r.Count));
            columnCount = Math.Max(columnCount, 1);

            var rowCount = rows.Count + 1;
            var lastColumn = ColumnName(columnCount);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append($"<worksheet xmlns=\"{SpreadsheetNs}\">");
            sb.Append($"<dimension ref=\"A1:{lastColumn}{rowCount}\"/>");

            // Закріплюємо рядок заголовків, щоб довгі вивантаження лишалися читабельними.
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\">");
            sb.Append("<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>");
            sb.Append("</sheetView></sheetViews>");

            sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");

            sb.Append("<cols>");
            for (var i = 1; i <= columnCount; i++)
            {
                var width = EstimateWidth(i - 1, headers, rows);
                sb.Append($"<col min=\"{i}\" max=\"{i}\" width=\"{width.ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
            }
            sb.Append("</cols>");

            sb.Append("<sheetData>");

            sb.Append("<row r=\"1\">");
            for (var c = 0; c < headers.Count; c++)
            {
                AppendCell(sb, ColumnName(c + 1) + "1", headers[c], StyleHeader);
            }
            sb.Append("</row>");

            for (var r = 0; r < rows.Count; r++)
            {
                var rowNumber = r + 2;
                sb.Append($"<row r=\"{rowNumber}\">");

                var row = rows[r];
                for (var c = 0; c < row.Count; c++)
                {
                    AppendCell(sb, ColumnName(c + 1) + rowNumber, row[c], StyleDefault);
                }

                sb.Append("</row>");
            }

            sb.Append("</sheetData>");

            if (headers.Count > 0)
            {
                sb.Append($"<autoFilter ref=\"A1:{ColumnName(headers.Count)}{rowCount}\"/>");
            }

            sb.Append("</worksheet>");
            return sb.ToString();
        }

        // -----------------------------------------------------------------
        // Комірки
        // -----------------------------------------------------------------

        private static void AppendCell(StringBuilder sb, string reference, object? value, int defaultStyle)
        {
            if (value == null)
            {
                return; // An omitted cell is a legitimate empty cell and keeps the file smaller.
            }

            switch (value)
            {
                case DateTime dateTime:
                    sb.Append($"<c r=\"{reference}\" s=\"{StyleDate}\"><v>{dateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                    return;

                case DateTimeOffset dateTimeOffset:
                    sb.Append($"<c r=\"{reference}\" s=\"{StyleDate}\"><v>{dateTimeOffset.UtcDateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                    return;

                case decimal decimalValue:
                    sb.Append($"<c r=\"{reference}\" s=\"{StyleMoney}\"><v>{decimalValue.ToString(CultureInfo.InvariantCulture)}</v></c>");
                    return;

                case double doubleValue:
                    sb.Append($"<c r=\"{reference}\" s=\"{StyleMoney}\"><v>{doubleValue.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                    return;

                case float floatValue:
                    sb.Append($"<c r=\"{reference}\" s=\"{StyleMoney}\"><v>{floatValue.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                    return;

                case bool boolValue:
                    sb.Append($"<c r=\"{reference}\" t=\"b\"><v>{(boolValue ? 1 : 0)}</v></c>");
                    return;

                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    sb.Append($"<c r=\"{reference}\"><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>");
                    return;

                default:
                    var text = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    sb.Append($"<c r=\"{reference}\" t=\"inlineStr\" s=\"{defaultStyle}\"><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>");
                    return;
            }
        }

        /// <summary>Назва стовпця Excel за номером від 1: 1 → A, 27 → AA.</summary>
        private static string ColumnName(int index)
        {
            var name = string.Empty;

            while (index > 0)
            {
                var remainder = (index - 1) % 26;
                name = (char)('A' + remainder) + name;
                index = (index - 1) / 26;
            }

            return name.Length == 0 ? "A" : name;
        }

        /// <summary>
        /// Приблизна ширина стовпця за заголовком і першими рядками — достатньо, щоб дати не
        /// перетворилися на ######, і не доводилося переглядати всі 10 000 рядків вивантаження.
        /// </summary>
        private static double EstimateWidth(int columnIndex, IReadOnlyList<string> headers, IList<IReadOnlyList<object?>> rows)
        {
            var longest = columnIndex < headers.Count ? headers[columnIndex].Length : 8;

            foreach (var row in rows.Take(50))
            {
                if (columnIndex >= row.Count)
                {
                    continue;
                }

                var value = row[columnIndex];
                var length = value switch
                {
                    null => 0,
                    DateTime => 17,
                    DateTimeOffset => 17,
                    decimal or double or float => 12,
                    _ => (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Length
                };

                longest = Math.Max(longest, length);
            }

            return Math.Clamp(longest + 2, 8, 60);
        }

        private static string SanitiseSheetName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Sheet1";
            }

            var cleaned = new string(name.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
            if (cleaned.Length == 0)
            {
                return "Sheet1";
            }

            return cleaned.Length <= 31 ? cleaned : cleaned[..31];
        }

        private static string Escape(string value)
        {
            var sb = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default:
                        // Керуючі символи заборонені в XML 1.0 і зіпсували б книгу Excel.
                        if (ch is '\t' or '\n' or '\r' || ch >= 0x20)
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
        }
    }
}
