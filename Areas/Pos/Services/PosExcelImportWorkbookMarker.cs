using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MyERP.Areas.Pos.Services
{
    public class PosExcelImportWorkbookMarker
    {
        private const uint HeaderRowNumber = 3;
        private const int StatusColumnIndex = 14; // N
        private const int InvoiceColumnIndex = 15; // O
        private const int MessageColumnIndex = 16; // P
        private const int MarkedAtColumnIndex = 17; // Q

        public string MarkWorkbook(PosExcelImportPreviewResult preview, PosExcelImportCommitResult commitResult, string outputDirectory)
        {
            if (preview == null || commitResult == null || string.IsNullOrWhiteSpace(preview.StoredWorkbookPath))
            {
                return null;
            }

            if (!File.Exists(preview.StoredWorkbookPath) || !string.Equals(Path.GetExtension(preview.StoredWorkbookPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Directory.CreateDirectory(outputDirectory);
            var safeName = Path.GetFileNameWithoutExtension(preview.SourceFileName ?? "ExcelImport");
            var outputName = safeName + "_POS_Marked_" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".xlsx";
            var outputPath = Path.Combine(outputDirectory, outputName);
            File.Copy(preview.StoredWorkbookPath, outputPath, true);

            var rowResults = commitResult.Rows
                .GroupBy(x => BuildKey(x.SheetName, x.RowNumber))
                .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

            using (var document = SpreadsheetDocument.Open(outputPath, true))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null || workbookPart.Workbook == null)
                {
                    return outputName;
                }

                foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                {
                    var sheetName = sheet.Name == null ? string.Empty : sheet.Name.Value;
                    var worksheetPart = workbookPart.GetPartById(sheet.Id) as WorksheetPart;
                    if (worksheetPart == null)
                    {
                        continue;
                    }

                    WriteHeader(worksheetPart);

                    foreach (var row in preview.Rows.Where(x => string.Equals(x.SheetName, sheetName, StringComparison.OrdinalIgnoreCase)))
                    {
                        PosExcelImportCommitRowResult rowResult;
                        if (!rowResults.TryGetValue(BuildKey(row.SheetName, row.RowNumber), out rowResult))
                        {
                            rowResult = BuildPreviewOnlyResult(row);
                        }

                        WriteRowMarker(worksheetPart, (uint)row.RowNumber, rowResult);
                    }

                    worksheetPart.Worksheet.Save();
                }

                workbookPart.Workbook.Save();
            }

            return outputName;
        }

        public string ClearImportedMarkers(PosExcelImportPreviewResult preview, PosExcelImportRollbackResult rollbackResult, string outputDirectory)
        {
            if (preview == null || rollbackResult == null || string.IsNullOrWhiteSpace(preview.StoredWorkbookPath))
            {
                return null;
            }

            if (!File.Exists(preview.StoredWorkbookPath) || !string.Equals(Path.GetExtension(preview.StoredWorkbookPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Directory.CreateDirectory(outputDirectory);
            var safeName = Path.GetFileNameWithoutExtension(preview.SourceFileName ?? "ExcelImport");
            var outputName = safeName + "_POS_RolledBack_" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".xlsx";
            var outputPath = Path.Combine(outputDirectory, outputName);
            File.Copy(preview.StoredWorkbookPath, outputPath, true);

            var rolledBackRows = rollbackResult.Rows
                .Where(x => string.Equals(x.Status, "RolledBack", StringComparison.OrdinalIgnoreCase))
                .Select(x => BuildKey(x.SheetName, x.RowNumber))
                .ToDictionary(x => x, x => true, StringComparer.OrdinalIgnoreCase);

            using (var document = SpreadsheetDocument.Open(outputPath, true))
            {
                var workbookPart = document.WorkbookPart;
                if (workbookPart == null || workbookPart.Workbook == null)
                {
                    return outputName;
                }

                foreach (var sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                {
                    var sheetName = sheet.Name == null ? string.Empty : sheet.Name.Value;
                    var worksheetPart = workbookPart.GetPartById(sheet.Id) as WorksheetPart;
                    if (worksheetPart == null)
                    {
                        continue;
                    }

                    foreach (var row in preview.Rows.Where(x => rolledBackRows.ContainsKey(BuildKey(sheetName, x.RowNumber)) && string.Equals(x.SheetName, sheetName, StringComparison.OrdinalIgnoreCase)))
                    {
                        ClearRowMarker(worksheetPart, (uint)row.RowNumber);
                    }

                    worksheetPart.Worksheet.Save();
                }

                workbookPart.Workbook.Save();
            }

            return outputName;
        }

        private static PosExcelImportCommitRowResult BuildPreviewOnlyResult(PosExcelImportRowPreview row)
        {
            var status = string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase) ? "Rejected" : "Skipped";
            return new PosExcelImportCommitRowResult
            {
                SheetName = row.SheetName,
                RowNumber = row.RowNumber,
                IPN = row.IPN,
                ServiceType = row.InternalServiceName,
                Status = status,
                Message = row.Reasons == null || row.Reasons.Count == 0 ? "لم يتم ترحيل الصف." : string.Join(" - ", row.Reasons)
            };
        }

        private static void WriteHeader(WorksheetPart worksheetPart)
        {
            WriteText(worksheetPart, HeaderRowNumber, StatusColumnIndex, "POS Import Status");
            WriteText(worksheetPart, HeaderRowNumber, InvoiceColumnIndex, "POS Invoice No");
            WriteText(worksheetPart, HeaderRowNumber, MessageColumnIndex, "POS Import Message");
            WriteText(worksheetPart, HeaderRowNumber, MarkedAtColumnIndex, "POS Marked At");
        }

        private static void WriteRowMarker(WorksheetPart worksheetPart, uint rowNumber, PosExcelImportCommitRowResult rowResult)
        {
            WriteText(worksheetPart, rowNumber, StatusColumnIndex, TranslateStatus(rowResult.Status));
            WriteText(worksheetPart, rowNumber, InvoiceColumnIndex, rowResult.NoteSerial1 ?? string.Empty);
            WriteText(worksheetPart, rowNumber, MessageColumnIndex, rowResult.Message ?? string.Empty);
            WriteText(worksheetPart, rowNumber, MarkedAtColumnIndex, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        private static void ClearRowMarker(WorksheetPart worksheetPart, uint rowNumber)
        {
            WriteText(worksheetPart, rowNumber, StatusColumnIndex, string.Empty);
            WriteText(worksheetPart, rowNumber, InvoiceColumnIndex, string.Empty);
            WriteText(worksheetPart, rowNumber, MessageColumnIndex, string.Empty);
            WriteText(worksheetPart, rowNumber, MarkedAtColumnIndex, string.Empty);
        }

        private static string TranslateStatus(string status)
        {
            if (string.Equals(status, "Imported", StringComparison.OrdinalIgnoreCase))
            {
                return "Imported - تم الترحيل";
            }

            if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                return "Failed - فشل";
            }

            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return "Rejected - مرفوض";
            }

            return "Skipped - متروك";
        }

        private static void WriteText(WorksheetPart worksheetPart, uint rowNumber, int columnIndex, string value)
        {
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null)
            {
                sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
            }

            var row = GetOrCreateRow(sheetData, rowNumber);
            var cell = GetOrCreateCell(row, columnIndex, rowNumber);
            cell.DataType = CellValues.InlineString;
            cell.InlineString = new InlineString(new Text(value ?? string.Empty));
            cell.CellValue = null;
        }

        private static Row GetOrCreateRow(SheetData sheetData, uint rowNumber)
        {
            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex != null && r.RowIndex.Value == rowNumber);
            if (row != null)
            {
                return row;
            }

            row = new Row { RowIndex = rowNumber };
            var refRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex != null && r.RowIndex.Value > rowNumber);
            if (refRow == null)
            {
                sheetData.Append(row);
            }
            else
            {
                sheetData.InsertBefore(row, refRow);
            }

            return row;
        }

        private static Cell GetOrCreateCell(Row row, int columnIndex, uint rowNumber)
        {
            var cellReference = ColumnName(columnIndex) + rowNumber.ToString(CultureInfo.InvariantCulture);
            var cell = row.Elements<Cell>().FirstOrDefault(c => string.Equals(c.CellReference == null ? string.Empty : c.CellReference.Value, cellReference, StringComparison.OrdinalIgnoreCase));
            if (cell != null)
            {
                return cell;
            }

            cell = new Cell { CellReference = cellReference };
            Cell refCell = null;
            foreach (var existing in row.Elements<Cell>())
            {
                var existingColumn = GetColumnIndex(existing.CellReference == null ? string.Empty : existing.CellReference.Value);
                if (existingColumn > columnIndex)
                {
                    refCell = existing;
                    break;
                }
            }

            if (refCell == null)
            {
                row.Append(cell);
            }
            else
            {
                row.InsertBefore(cell, refCell);
            }

            return cell;
        }

        private static string ColumnName(int columnIndex)
        {
            var dividend = columnIndex;
            var columnName = string.Empty;
            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var letters = new string((cellReference ?? string.Empty).Where(char.IsLetter).ToArray()).ToUpperInvariant();
            var sum = 0;
            foreach (var letter in letters)
            {
                sum *= 26;
                sum += letter - 'A' + 1;
            }

            return sum;
        }

        private static string BuildKey(string sheetName, int rowNumber)
        {
            return (sheetName ?? string.Empty) + "|" + rowNumber.ToString(CultureInfo.InvariantCulture);
        }
    }
}
