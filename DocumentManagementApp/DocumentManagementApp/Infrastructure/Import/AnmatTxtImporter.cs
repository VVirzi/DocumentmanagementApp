using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DocumentManagementApp.Infrastructure.Import
{
    /// <summary>
    /// Imports and parses fixed-with ANMAT .txt files into DataTable.
    /// </summary>
    public static class AnmatTxtImporter
    {
        private readonly string _companyFilter;

        ///<summary>
        ///Inittializes the importer whith a company name to filter records by.
        ///</summary>
        ///<param name="companyFilter"> The company name to match in the ANMAT file.</param>
        public AnmatTxtImporter(string companyFilter)
        {
            if (string.IsNullOrWhiteSpace(companyFilter))
                throw new ArgumentException("Company filter cannot be empty.", nameof(companyFilter));

            _companyFilter = companyFilter;
        }

        ///<summary>
        /// Parses the ANMAT .txt file and returns matching records as a DataTable.
        /// </summary>
        /// <param name="filePath"> Full path to the .txt ANMAT file.</param>
        public DataTable Import(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            var lines = ReadLines(filePath);
            var result = CreateResultTable();

            foreach ( var line in lines )
            {
                if (line.Length < 1374) continue;

                string companyName = line.Substring(894,15).Trim();

                if(companyName.Equals(_companyFilter, StringComparison.OrdinalIgnoreCase))
                {
                    DataRow row = result.NewRow();
                    row["TransactionId"] = ExtractField(line, 0, 10);
                    row["Gtin"] = ExtractField(line, 526, 15);
                    row["SerialNumber"] = ExtractField(line, 810, 21);
                    row["Batch"] = ExtractField(line, 831, 20);
                    row["ExpiryDate"] = ExtractField(line, 851, 10);
                    row["DeliveryNote"] = ExtractField(line, 1334, 13);

                    result.Rows.Add(row);
                }
            }
            return result;
        }


        /// <summary>
        /// Opens a file dialog to let the user select the ANMAT .txt file.
        /// </summary>
        public string PickFilePath()
        {
            using (var openFileDialog = new OpenFileDialog { Filter = "Text files|*.txt" })
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : nul
            }
        }

        private List<string> ReadLines(string filePath)
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }
            return lines;
        }

        private DataTable CreateResultTable()
        {
            var table = new DataTable("Anmat");
            table.Columns.Add("TransactionId", typeof(string));
            table.Columns.Add("Gtin", typeof(string));
            table.Columns.Add("SerialNumber", typeof(string));
            table.Columns.Add("Batch", typeof(string));
            table.Columns.Add("ExpiryDate", typeof(string));
            table.Columns.Add("DeliveryNote", typeof(string));
            return table;
        }

        private string ExtractField(string line, int start, int length)
        {
            if (start >= line.Length) return string.Empty;
            if (start + length > line.Length) length = line.Length - start;
            return line.Substring(start, length).Trim();
        }
    }
}