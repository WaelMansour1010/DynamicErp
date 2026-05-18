using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ExcelImportDebug
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var workbookPath = args.Length > 0
                    ? args[0]
                    : @"C:\Users\Wael\Downloads\مصروفات عموميةAccounts_balance.xls";

                var binDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\bin"));
                AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
                {
                    var assemblyName = new AssemblyName(eventArgs.Name).Name + ".dll";
                    var candidate = Path.Combine(binDir, assemblyName);
                    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
                };

                foreach (var dll in new[]
                {
                    "System.Runtime.CompilerServices.Unsafe.dll",
                    "System.Buffers.dll",
                    "System.Memory.dll",
                    "Microsoft.Bcl.AsyncInterfaces.dll",
                    "System.Text.Encoding.CodePages.dll",
                    "ExcelDataReader.dll",
                    "ExcelDataReader.DataSet.dll",
                    "MyERP.dll"
                })
                {
                    var path = Path.Combine(binDir, dll);
                    if (File.Exists(path))
                    {
                        Assembly.LoadFrom(path);
                    }
                }

                var myErpAssembly = Assembly.LoadFrom(Path.Combine(binDir, "MyERP.dll"));
                var readerType = myErpAssembly.GetType("MyERP.Areas.MainErp.Services.MasterDataImport.ExcelImportReader", true);
                var reader = Activator.CreateInstance(readerType);

                var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { Path.GetFileName(workbookPath), workbookPath }
                };
                var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var method = readerType.GetMethod("ReadOpeningBalances");
                var rows = (System.Collections.IEnumerable)method.Invoke(reader, new object[] { files, hashes });

                var diagnosticsProp = readerType.GetProperty("LastWorksheetDiagnostics");
                var diagnostics = diagnosticsProp.GetValue(reader) as System.Collections.IEnumerable;

                Console.WriteLine("WORKBOOK=" + workbookPath);
                Console.WriteLine("=== SHEETS ===");
                if (diagnostics != null)
                {
                    foreach (var sheet in diagnostics)
                    {
                        var sheetType = sheet.GetType();
                        Console.WriteLine(
                            string.Join(" | ", new[]
                            {
                                "Sheet=" + GetValue(sheetType, sheet, "SheetName"),
                                "UsedRange=" + GetValue(sheetType, sheet, "UsedRange"),
                                "HeaderRow=" + GetValue(sheetType, sheet, "HeaderRowNumber"),
                                "DataRows=" + GetValue(sheetType, sheet, "DataRowsCount"),
                                "SerialCol=" + GetValue(sheetType, sheet, "DetectedAccountSerialColumn"),
                                "NameCol=" + GetValue(sheetType, sheet, "DetectedAccountNameColumn"),
                                "DebitCol=" + GetValue(sheetType, sheet, "DetectedDebitColumn"),
                                "CreditCol=" + GetValue(sheetType, sheet, "DetectedCreditColumn"),
                                "BalanceCol=" + GetValue(sheetType, sheet, "DetectedBalanceColumn"),
                                "Skip=" + GetValue(sheetType, sheet, "SkipReason")
                            }));

                        var columnDiagnostics = GetValue(sheetType, sheet, "ColumnDiagnostics") as System.Collections.IEnumerable;
                        if (columnDiagnostics == null)
                        {
                            continue;
                        }

                        foreach (var column in columnDiagnostics.Cast<object>().Take(10))
                        {
                            var ct = column.GetType();
                            Console.WriteLine(
                                string.Join(" | ", new[]
                                {
                                    "  Col=" + GetValue(ct, column, "ColumnIndex"),
                                    "Header=" + GetValue(ct, column, "HeaderText"),
                                    "Samples=" + GetValue(ct, column, "SampleValues"),
                                    "DigitRatio=" + GetValue(ct, column, "DigitOnlyRatio"),
                                    "DecimalRatio=" + GetValue(ct, column, "DecimalOrCommaRatio"),
                                    "ZeroRatio=" + GetValue(ct, column, "ZeroRatio"),
                                    "Distinct=" + GetValue(ct, column, "DistinctCount"),
                                    "Score=" + GetValue(ct, column, "FinalScore"),
                                    "Decision=" + GetValue(ct, column, "Decision"),
                                    "Role=" + GetValue(ct, column, "AcceptedRole"),
                                    "Reason=" + GetValue(ct, column, "Reason")
                                }));
                        }
                    }
                }

                Console.WriteLine("=== ROWS ===");
                var count = 0;
                foreach (var row in rows.Cast<object>().Take(10))
                {
                    var rt = row.GetType();
                    Console.WriteLine(
                        string.Join(" | ", new[]
                        {
                            "Row=" + GetValue(rt, row, "RowNumber"),
                            "Serial=" + GetValue(rt, row, "AccountSerial"),
                            "Name=" + GetValue(rt, row, "AccountName"),
                            "Debit=" + GetValue(rt, row, "Debit"),
                            "Credit=" + GetValue(rt, row, "Credit"),
                            "Opening=" + GetValue(rt, row, "OpeningBalanceText"),
                            "Date=" + GetValue(rt, row, "EntryDateText"),
                            "Label=" + GetValue(rt, row, "BalanceType")
                        }));
                    count++;
                }

                Console.WriteLine("ROW_COUNT=" + CountEnumerable(rows));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static object GetValue(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property == null ? string.Empty : property.GetValue(instance, null) ?? string.Empty;
        }

        private static int CountEnumerable(System.Collections.IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return count;
        }
    }
}
