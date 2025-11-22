using ClosedXML.Excel;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using System.Reflection;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// Excel export service using ClosedXML
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class
    {
        return await Task.Run(() =>
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
            var dataList = data.ToList();
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
            return stream.ToArray();
        }, cancellationToken);
    }

    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        Dictionary<string, Func<T, object>> columnMappings,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class
    {
        return await Task.Run(() =>
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
            var dataList = data.ToList();
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
            return stream.ToArray();
        }, cancellationToken);
    }

    public async Task<byte[]> ExportMultipleSheetsAsync(
        Dictionary<string, object> sheets,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();

            foreach (var sheet in sheets)
            {
                var sheetName = sheet.Key;
                var data = sheet.Value;

                var worksheet = workbook.Worksheets.Add(sheetName);

                // Use reflection to get the type
                var dataType = data.GetType();
                if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
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
            return stream.ToArray();
        }, cancellationToken);
    }
}
