using System;
using System.Data;
using System.IO;
using HtmlAgilityPack;
using ZXing;

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
            using (var reader = new StreamReader(filePath, true)) {html = reader.ReadToEnd();}

            var doc = new HtmlDocument();   
            doc.LoadHtml(html);

            var htmlTable = doc.DocumentNode.SelectSingleNode("//table");
            if(htmlTable == null)
                throw new Exception("No table found in the HTML file.");)

            var rows = htmlTable.SelectNodes(".//tr");
            if(rows == null || rows==0) 
                throw new Exception("No rows found in the table.");

            var result = new DataTable();

            //Process headers
            var headers = rows[0].SelectNodes("th|td");
            foreach(var header in headers)
            {
                string columnName = header.InnerText.Trim();
                if(string.IsNullOrEmpty(columnName))
                    columnName = $"Column{dataTable.Columns.Count + 1}";

                result.Columns.Add(columnName);
            }

            //Process data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes("th|td");
                if (cells == null) continue;
                var newRow = result.NewRow();
                for (int j = 0; j < cells.Count; j++)
                {
                    newRow[j] = cells[j].InnerText.Trim();
                }
                result.Rows.Add(newRow);
            }

            return result;
        }
    }
