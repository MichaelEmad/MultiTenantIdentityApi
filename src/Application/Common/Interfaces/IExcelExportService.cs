namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// Interface for Excel export operations
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Export data to Excel file
    /// </summary>
    Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Export data to Excel with custom columns
    /// </summary>
    Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        Dictionary<string, Func<T, object>> columnMappings,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Export multiple sheets to Excel
    /// </summary>
    Task<byte[]> ExportMultipleSheetsAsync(
        Dictionary<string, object> sheets,
        CancellationToken cancellationToken = default);
}
