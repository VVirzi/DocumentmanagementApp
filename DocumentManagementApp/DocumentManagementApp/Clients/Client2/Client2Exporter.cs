using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DocumentManagementApp.Core.Base;
using DocumentManagementApp.Infrastructure.Import;

namespace DocumentManagementApp.Clients.Client2
{
    /// <summary>
    /// Generates the formatted export table for Client2.
    /// Integrates data from three sources: main file, invoice information, and ANMAT registry.
    /// </summary>
    public class Client2Exporter : ClientExporterBase
    {
        private readonly AnmatTxtImporter _anmatImporter;

        public Client2Exporter()
        {
            _anmatImporter = new AnmatTxtImporter("ITAL FARMA S.A.");
        }

        /// <inheritdoc/>
        public override DataTable GenerateFormattedTable(DataTable rawData)
        {
            DataTable result = CreateResultTable();
            DataTable invoiceData = ImportFileToDataTable(SecondaryPath);
            DataTable anmatData = _anmatImporter.Import(TertiaryPath);

            HashSet<string> processedInvoices = new HashSet<string>();

            foreach (DataRow row in rawData.Rows)
            {
                string invoiceId = row[1]?.ToString();

                if (string.IsNullOrWhiteSpace(invoiceId)) continue;
                if (!processedInvoices.Add(invoiceId)) continue;

                DataRow baseRow = BuildBaseRow(result, row);
                DataTable invoiceDetails = BuildInvoiceDetails(invoiceData, baseRow, invoiceId);
                EnrichWithAnmatData(invoiceDetails, anmatData);
                MergeIntoResult(result, invoiceDetails);
            }

            return result;
        }

        /// <inheritdoc/>
        public override DataTable CreateResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("RecordType", typeof(string));
            table.Columns.Add("InvoiceNumber", typeof(string));
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("F5Number", typeof(string));
            table.Columns.Add("MemberNumber", typeof(string));
            table.Columns.Add("DeliveryNote", typeof(string));
            table.Columns.Add("ItemNumber", typeof(string));
            table.Columns.Add("Gtin", typeof(string));
            table.Columns.Add("Medicine", typeof(string));
            table.Columns.Add("Troquel", typeof(string));
            table.Columns.Add("DeliveryDate", typeof(string));
            table.Columns.Add("Quantity", typeof(string));
            table.Columns.Add("Amount", typeof(string));
            table.Columns.Add("SerialNumber", typeof(string));
            table.Columns.Add("Batch", typeof(string));
            table.Columns.Add("ExpiryDate", typeof(string));
            table.Columns.Add("Gln", typeof(string));
            table.Columns.Add("ListNumber", typeof(string));
            table.Columns.Add("TransactionId", typeof(string));
            return table;
        }

        /// <inheritdoc/>
        public override void ExportToFile(DataTable formattedTable, string outputPath)
        {
            DialogResult choice = MessageBox.Show(
                "How would you like to export?\n\nYes = One file per invoice\nNo = Groups of up to 200 lines (without splitting invoices)",
                "Export type",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel) return;

            var invoiceGroups = formattedTable.AsEnumerable()
                .GroupBy(row => row["InvoiceNumber"])
                .ToList();

            if (choice == DialogResult.Yes)
                ExportOneFilePerInvoice(invoiceGroups, outputPath);
            else
                ExportInGroups(invoiceGroups, outputPath);
        }

        private DataRow BuildBaseRow(DataTable result, DataRow source)
        {
            DataRow newRow = result.NewRow();

            newRow["RecordType"] = "1";
            newRow["InvoiceNumber"] = source[1]?.ToString();

            // BUG FIX: date.AddDays(1) originally returned a new DateTime without assigning it.
            // DateTime is a struct — it's immutable. The result must be captured in a variable.
            if (DateTime.TryParse(source[2]?.ToString(), out DateTime invoiceDate))
            {
                newRow["Date"] = invoiceDate.ToString("yyyyMMdd");

                DateTime deliveryDate = invoiceDate.AddDays(1);
                while (deliveryDate.DayOfWeek == DayOfWeek.Saturday ||
                       deliveryDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    deliveryDate = deliveryDate.AddDays(1);
                }
                newRow["DeliveryDate"] = deliveryDate.ToString("yyyyMMdd");
            }
            else
            {
                newRow["Date"] = string.Empty;
                newRow["DeliveryDate"] = string.Empty;
            }

            newRow["F5Number"] = source[5]?.ToString();
            newRow["MemberNumber"] = source[14]?.ToString();
            newRow["DeliveryNote"] = source[3]?.ToString();
            newRow["Quantity"] = "1";
            newRow["Gln"] = "7798374190009";
            newRow["ListNumber"] = string.Empty;

            return newRow;
        }

        private DataTable BuildInvoiceDetails(DataTable invoiceData, DataRow baseRow, string invoiceId)
        {
            DataTable details = CreateResultTable();

            foreach (DataRow row in invoiceData.Rows)
            {
                if (row[1]?.ToString() != invoiceId) continue;
                if (row[10]?.ToString().Length <= 1) continue;

                float quantity = float.TryParse(row[12]?.ToString(), out float q) ? q : 0f;

                for (int i = 0; i < quantity; i++)
                {
                    DataRow detailRow = details.NewRow();
                    detailRow.ItemArray = (object[])baseRow.ItemArray.Clone();

                    detailRow["ItemNumber"] = (details.Rows.Count + 1).ToString();
                    detailRow["Gtin"] = "0" + row[10];
                    detailRow["Medicine"] = row[8]?.ToString();
                    detailRow["Troquel"] = row[11]?.ToString();
                    detailRow["Amount"] = FormatPrice(row[13]?.ToString());
                    detailRow["Batch"] = row[21]?.ToString();
                    detailRow["ExpiryDate"] = ParseDate(row[22]?.ToString(), "yyyyMMdd");

                    details.Rows.Add(detailRow);
                }
            }

            return details;
        }

        private void EnrichWithAnmatData(DataTable invoiceDetails, DataTable anmatData)
        {
            if (invoiceDetails.Rows.Count == 0) return;

            string deliveryNote = invoiceDetails.Rows[0]["DeliveryNote"].ToString();

            var anmatByGtin = anmatData.AsEnumerable()
                .Where(r => r.Field<string>("DeliveryNote") == deliveryNote)
                .GroupBy(r => r.Field<string>("Gtin"))
                .ToDictionary(g => g.Key, g => new Queue<DataRow>(g));

            foreach (DataRow row in invoiceDetails.Rows)
            {
                if (!string.IsNullOrWhiteSpace(row["SerialNumber"].ToString())) continue;

                string gtin = row["Gtin"].ToString();
                if (!anmatByGtin.TryGetValue(gtin, out Queue<DataRow> queue) || queue.Count == 0) continue;

                DataRow anmatRow = queue.Dequeue();

                row["SerialNumber"] = anmatRow["SerialNumber"];
                row["Batch"] = anmatRow["Batch"];
                row["ExpiryDate"] = ParseDate(anmatRow["ExpiryDate"]?.ToString(), "yyyyMMdd");
                row["TransactionId"] = anmatRow["TransactionId"];
            }
        }

        private void ExportOneFilePerInvoice(IEnumerable<IGrouping<object, DataRow>> groups, string basePath)
        {
            foreach (var group in groups)
            {
                string invoicePath = BuildFilePath(basePath, group.Key.ToString());
                WriteRowsToFile(group, invoicePath);
            }
        }

        private void ExportInGroups(IEnumerable<IGrouping<object, DataRow>> groups, string basePath)
        {
            int fileIndex = 1;
            int currentLineCount = 0;
            var currentBatch = new List<DataRow>();

            foreach (var group in groups)
            {
                int groupSize = group.Count();

                if (groupSize > 200)
                {
                    FlushBatch(currentBatch, basePath, ref fileIndex);
                    string largePath = BuildFilePath(basePath, $"LargeInvoice_{group.Key}");
                    WriteRowsToFile(group, largePath);
                    continue;
                }

                if (currentLineCount + groupSize > 200 && currentBatch.Count > 0)
                    FlushBatch(currentBatch, basePath, ref fileIndex);

                currentBatch.AddRange(group);
                currentLineCount += groupSize;
            }

            if (currentBatch.Count > 0)
                FlushBatch(currentBatch, basePath, ref fileIndex);
        }

        private void FlushBatch(List<DataRow> batch, string basePath, ref int fileIndex)
        {
            string batchPath = BuildFilePath(basePath, $"Group{fileIndex}");
            WriteRowsToFile(batch, batchPath);
            fileIndex++;
            batch.Clear();
        }

        private void WriteRowsToFile(IEnumerable<DataRow> rows, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (DataRow row in rows)
                {
                    AlignRowFields(row);
                    writer.WriteLine(string.Join(string.Empty, row.ItemArray.Select(c => c.ToString())));
                }
            }
        }

        private void AlignRowFields(DataRow row)
        {
            row["F5Number"] = row["F5Number"].ToString().PadRight(15);
            row["MemberNumber"] = row["MemberNumber"].ToString().PadRight(15);
            row["DeliveryNote"] = row["DeliveryNote"].ToString().Replace("R", string.Empty).PadLeft(15, '0');
            row["ItemNumber"] = row["ItemNumber"].ToString().PadLeft(15, '0');
            row["Gtin"] = row["Gtin"].ToString().PadLeft(14, '0');
            row["Medicine"] = row["Medicine"].ToString().PadRight(60);
            row["Troquel"] = row["Troquel"].ToString().PadLeft(8, '0');
            row["Quantity"] = row["Quantity"].ToString().PadLeft(3, '0');
            row["Amount"] = row["Amount"].ToString().PadLeft(12, '0');
            row["SerialNumber"] = row["SerialNumber"].ToString().PadRight(20);
            row["Batch"] = row["Batch"].ToString().PadRight(20);
            row["ListNumber"] = row["ListNumber"].ToString().PadRight(10);
            row["TransactionId"] = row["TransactionId"].ToString().PadRight(18);
        }

        private void MergeIntoResult(DataTable target, DataTable source)
        {
            foreach (DataRow row in source.Rows)
                target.Rows.Add(row.ItemArray);
        }

        private string BuildFilePath(string basePath, string suffix)
        {
            return Path.Combine(
                Path.GetDirectoryName(basePath),
                $"{Path.GetFileNameWithoutExtension(basePath)}_{suffix}.txt");
        }

        private string ParseDate(string raw, string format)
        {
            return DateTime.TryParse(raw, out DateTime date)
                ? date.ToString(format)
                : string.Empty;
        }

        private string FormatPrice(string raw)
        {
            string price = raw?.Replace(".", string.Empty).Replace(",", string.Empty) ?? string.Empty;
            return price.Length > 9
                ? price.Substring(price.Length - 9)
                : price.PadLeft(9, '0');
        }
    }
}