using System;
using System.Data;
using System.IO;
using HtmlAgilityPack;

namespace DocumentManagementApp.Infrastructure.Import
{
    /// <summary>
    /// Imports HTML-based Excel files (.xls saved as HTML) into DataTable.
    /// </summary>
    public static class HtmlExcelImporter
    {
        ///<summary>
        /// Reads an HTML file and returns its content as a DataTable.
        /// </summary>
        /// <param name="filePath">Full path to the .xls HTML file.</param>
        /// <returns> DataTable populated with the file's data</returns>
        public static DataTable Import(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            string html;
            using (var reader = new StreamReader(filePath, true))
            {
                html = reader.ReadToEnd();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var htmlTable = doc.DocumentNode.SelectSingleNode("//table");
            if (htmlTable == null)
                throw new Exception("No table found in the HTML file.");

            var rows = htmlTable.SelectNodes(".//tr");
            if (rows == null || rows.Count < 1)
                throw new Exception("No rows found in the table.");

            var result = new DataTable();

            // Process headers
            var headerCells = rows[0].SelectNodes("th|td");
            if (headerCells == null)
                throw new Exception("No header cells found.");

            foreach (var header in headerCells)
            {
                string columnName = header.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = $"Column{result.Columns.Count + 1}";
                result.Columns.Add(columnName);
            }

            // Process data rows
            for (int i = 1; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes("th|td");
                if (cells == null || cells.Count == 0) continue;

                var newRow = result.NewRow();
                for (int j = 0; j < result.Columns.Count; j++)
                {
                    newRow[j] = j < cells.Count
                        ? cells[j].InnerText.Trim()
                        : string.Empty;
                }
                result.Rows.Add(newRow);
            }

            return result;
        }
    }
}
