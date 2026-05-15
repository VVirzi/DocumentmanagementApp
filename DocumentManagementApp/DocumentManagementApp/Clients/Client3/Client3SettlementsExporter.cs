using System;
using System.Collections.Generic;
using System.Data;
using DocumentManagementApp.Core.Base;

namespace DocumentManagementApp.Clients.Client3
{
    /// <summary>
    /// Generate the settlements report table to Client 3.
    /// </summary>
    public class Client3SettlementsExporter : ClientExporterBase
    {
        /// <inheritdoc/>
        public override DataTable GenerateFormattedTable(DataTable rawData)
        {
            DataTable result = CreateResultTable();
            HashSet<string> processedInvoices = new HashSet<string>();

            foreach (DataRow row in rawData)
            {
                string invoiceId = row[1]?.ToString();

                if (string.IsNullOrEmpty(invoiceId)) continue;
                if (!processedInvoices.Add(invoiceId)) continue;

                result.Rows.Add(BuildRow(result, row));
            }

            return result;
        }

        /// <inheritdoc/>
        public override DataTable CreateResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Invoice", typeof(string));
            table.Columns.Add("DeliveryNote", typeof(string));
            table.Columns.Add("PurchaseOrder", typeof(string));
            table.Columns.Add("TotalAmount", typeof(string));
            return table;
        }

        private DataRow BuildRow(DataTable table, DataRow source)
        {
            DataRow newRow = table.NewRow();

            newRow["Date"] = ParseDate(source[2]?.ToString(), "dd/MM/yyyy");

            string documentType = TrimDocumentType(source[0]?.ToString());
            newRow["Invoice"] = $"{documentType}-{source[1]}";

            newRow["DeliveryNote"] = source[3]?.ToString();
            newRow["PurchaseOrder"] = source[19]?.ToString()?.Trim() ?? string.Empty;
            newRow["TotalAmount"] = source[16]?.ToString()?.Replace(".", string.Empty);

            return newRow;
        })

        private string ParseDate(string raw, string format)
        {
            return DateTime.TryParse(raw, out DateTime date)
                ? date.ToString(format)
                : string.Empty;
        }

        private string TrimDocumentType(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return raw.Trim().Substring(raw.Trim().Length - 1);
        }
    }
}
