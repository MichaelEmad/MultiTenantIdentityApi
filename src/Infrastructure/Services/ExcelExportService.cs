using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.Common.Models;
using System.Reflection;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// Excel export service using ClosedXML
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<byte[]>> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (data == null)
            {
                return Result<byte[]>.Failure("Data cannot be null");
            }

            var dataList = data.ToList();
            if (!dataList.Any())
            {
                return Result<byte[]>.Failure("No data to export");
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // Get properties
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    // Add headers
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = properties[i].Name;
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // Add data
                    for (int row = 0; row < dataList.Count; row++)
                    {
                        for (int col = 0; col < properties.Length; col++)
                        {
                            var value = properties[col].GetValue(dataList[row]);
                            worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? string.Empty;
                        }
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Convert to byte array
                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    var excelData = stream.ToArray();

                    _logger.LogInformation("Successfully exported {Count} rows to Excel", dataList.Count);
                    return Result<byte[]>.Success(excelData, "Excel export completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Excel export");
                    return Result<byte[]>.Failure($"Failed to export to Excel: {ex.Message}");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing Excel export");
            return Result<byte[]>.Failure($"Failed to prepare export: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        Dictionary<string, Func<T, object>> columnMappings,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (data == null)
            {
                return Result<byte[]>.Failure("Data cannot be null");
            }

            if (columnMappings == null || !columnMappings.Any())
            {
                return Result<byte[]>.Failure("Column mappings cannot be null or empty");
            }

            var dataList = data.ToList();
            if (!dataList.Any())
            {
                return Result<byte[]>.Failure("No data to export");
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // Add headers
                    int colIndex = 1;
                    foreach (var column in columnMappings)
                    {
                        worksheet.Cell(1, colIndex).Value = column.Key;
                        worksheet.Cell(1, colIndex).Style.Font.Bold = true;
                        worksheet.Cell(1, colIndex).Style.Fill.BackgroundColor = XLColor.LightGray;
                        colIndex++;
                    }

                    // Add data
                    for (int row = 0; row < dataList.Count; row++)
                    {
                        colIndex = 1;
                        foreach (var column in columnMappings)
                        {
                            var value = column.Value(dataList[row]);
                            worksheet.Cell(row + 2, colIndex).Value = value?.ToString() ?? string.Empty;
                            colIndex++;
                        }
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Convert to byte array
                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    var excelData = stream.ToArray();

                    _logger.LogInformation("Successfully exported {Count} rows with custom columns to Excel", dataList.Count);
                    return Result<byte[]>.Success(excelData, "Excel export completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Excel export with custom columns");
                    return Result<byte[]>.Failure($"Failed to export to Excel: {ex.Message}");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing Excel export with custom columns");
            return Result<byte[]>.Failure($"Failed to prepare export: {ex.Message}");
        }
    }

    public async Task<Result<byte[]>> ExportMultipleSheetsAsync(
        Dictionary<string, object> sheets,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (sheets == null || !sheets.Any())
            {
                return Result<byte[]>.Failure("Sheets cannot be null or empty");
            }

            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook();

                    foreach (var sheet in sheets)
                    {
                        var sheetName = sheet.Key;
                        var data = sheet.Value;

                        var worksheet = workbook.Worksheets.Add(sheetName);

                        // Use reflection to get the type
                        var dataType = data.GetType();
                        if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var itemType = dataType.GetGenericArguments()[0];
                            var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                            // Add headers
                            for (int i = 0; i < properties.Length; i++)
                            {
                                worksheet.Cell(1, i + 1).Value = properties[i].Name;
                                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                            }

                            // Add data
                            var dataList = ((System.Collections.IEnumerable)data).Cast<object>().ToList();
                            for (int row = 0; row < dataList.Count; row++)
                            {
                                for (int col = 0; col < properties.Length; col++)
                                {
                                    var value = properties[col].GetValue(dataList[row]);
                                    worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? string.Empty;
                                }
                            }

                            // Auto-fit columns
                            worksheet.Columns().AdjustToContents();
                        }
                    }

                    // Convert to byte array
                    using var stream = new MemoryStream();
                    workbook.SaveAs(stream);
                    var excelData = stream.ToArray();

                    _logger.LogInformation("Successfully exported {SheetCount} sheets to Excel", sheets.Count);
                    return Result<byte[]>.Success(excelData, "Multi-sheet Excel export completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during multi-sheet Excel export");
                    return Result<byte[]>.Failure($"Failed to export to Excel: {ex.Message}");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing multi-sheet Excel export");
            return Result<byte[]>.Failure($"Failed to prepare export: {ex.Message}");
        }
    }
}
