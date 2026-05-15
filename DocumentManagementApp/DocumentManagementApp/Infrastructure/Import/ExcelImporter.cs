using System;
using System.Data;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DocumentManagementApp.Infrastructure.Import
{
    /// <summary>
    /// Imports native Excel files (.xls and .xlsx) into Datatable using NPOI.
    /// </summary>
    public static class ExcelImporter
    {
        ///<summary>
        /// Reads an Excel file and returns the first sheet as a DataTable.
        /// </summary>
        /// <param name="filePath"> Full path into the Excel file.</param>
        /// <returns> DataTable populates with the sheet's data.</returns>
        public static DataTable Import(string filePath)
        {
            if(!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            var result = new DataTable();

            using(FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = null;

                if(Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                    workbook = new HSSFWorkbook(file);
                else if(Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    workbook = new XSSFWorkbook(file);
                else
                    throw new NotSupportedException($"Unsupported file format: {Path.GetExtension(filePath)}.");

                ISheet sheet = workbook.GetSheetAt(0);
                if(sheet == null)
                    throw new Exception("Could not read the first sheet.");

                // Process headers
                IRow headerRow = sheet.GetRow(0);
                for(int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var columnName = headerRow.GetCell(i)?.ToString() ?? $"Column{i+1}";
                    result.Columns.Add(columnName);
                }


                // Process rows
                for(int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if(row == null) continue;
                    DataRow dataRow = result.NewRow();
                    for(int j = 0; j < row.LastCellNum; j++)
                    {
                        dataRow[j] = row.GetCell(j)?.ToString() ?? string.Empty;
                    }
                    result.Rows.Add(dataRow);
                }
            }
            return result;
        }
    }
}
