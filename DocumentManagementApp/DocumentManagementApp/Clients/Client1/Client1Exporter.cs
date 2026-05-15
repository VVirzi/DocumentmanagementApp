using System;
using System.Collections.Generic;
using System.Data;
using DocumentManagementApp.Core.Base;

namespace DocumentManagementApp.Clients.Client1
{
    /// <summary>
    /// Generates the formatted export table for Client1.
    /// </summary>
    public class Client1Exporter : ClientExporterBase
    {
        /// <inheritdoc/>
        public override DataTable GenerateFormattedTable(DataTable rawData)
        {
            DataTable result = CreateResultTable();
            DataTable invoiceData = ImportFileToDataTable(SecondaryPath);

            HashSet<string> processedInvoices = new HashSet<string>();

            foreach (DataRow row in rawData.Rows)
            {
                string invoiceId = row[1]?.ToString();

                if (string.IsNullOrWhiteSpace(invoiceId)) continue;
                if (!processedInvoices.Add(invoiceId)) continue;

                DataRow newRow = BuildBaseRow(result.NewRow(), row);
                CompleteWithInvoiceData(newRow, invoiceData);
                result.Rows.Add(newRow);
            }

            return result;
        }

        /// <inheritdoc/>
        public override DataTable CreateResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("DeliveryNote", typeof(string));
            table.Columns.Add("Invoice", typeof(string));
            table.Columns.Add("Amount", typeof(string));
            table.Columns.Add("InvoiceDate", typeof(string));
            table.Columns.Add("RequestType", typeof(string));
            table.Columns.Add("RequestNumber", typeof(string));
            table.Columns.Add("SupplierCode", typeof(string));
            table.Columns.Add("PurchaseOrder", typeof(string));
            table.Columns.Add("Cae", typeof(string));
            table.Columns.Add("CaeExpiryDate", typeof(string));
            return table;
        }
        /// <inheritdoc/>
        public override void ExportToFile(DataTable formattedTable, string outputPath)
        {
            PadPurchaseOrders(formattedTable);

            var pdfExporter = new Infrastructure.Export.QrPdfExporter();
            pdfExporter.Export(formattedTable, outputPath);
        }

        private DataRow BuildBaseRow(DataRow newRow, DataRow source)
        {
            string deliveryNote = source[13]?.ToString().Replace("-", string.Empty) ?? string.Empty;
            newRow["DeliveryNote"] = deliveryNote.Length > 12
                ? deliveryNote.Substring(deliveryNote.Length - 12)
                : deliveryNote;

            string invoice = source[1]?.ToString().Replace("-", string.Empty) ?? string.Empty;
            newRow["Invoice"] = invoice.PadLeft(12, '0');

            newRow["Amount"] = FormatAmount(source[6]);
            newRow["InvoiceDate"] = ParseDate(source[3]?.ToString(), "ddMMyyyy");
            newRow["RequestType"] = "01";
            newRow["RequestNumber"] = source[9]?.ToString().Replace("/", string.Empty);
            newRow["SupplierCode"] = "9002";
            newRow["PurchaseOrder"] = source[17]?.ToString().Replace("/", string.Empty);

            return newRow;
        }

        private void CompleteWithInvoiceData(DataRow row, DataTable invoiceData)
        {
            if (invoiceData == null) return;

            string deliveryNote = row["DeliveryNote"].ToString();

            foreach (DataRow invoiceRow in invoiceData.Rows)
            {
                if (invoiceRow.IsNull(3)) continue;

                string suffix = invoiceRow[3]?.ToString();
                if (string.IsNullOrWhiteSpace(suffix)) continue;
                if (!deliveryNote.EndsWith(suffix)) continue;

                row["Cae"] = invoiceRow[25]?.ToString();
                row["CaeExpiryDate"] = ParseDate(invoiceRow[26]?.ToString(), "ddMMyyyy");
                return;
            }
        }

        private void PadPurchaseOrders(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                string purchaseOrder = row["PurchaseOrder"].ToString();
                if (purchaseOrder.Length < 10)
                    row["PurchaseOrder"] = purchaseOrder.PadLeft(10, '0');
            }
        }

        private string ParseDate(string raw, string format)
        {
            return DateTime.TryParse(raw, out DateTime date)
                ? date.ToString(format)
                : string.Empty;
        }

        private string FormatAmount(object value)
        {
            string amount = value?.ToString()
                ?.Replace(".", string.Empty)
                ?.Replace(",", string.Empty) ?? string.Empty;

            return amount.Length > 9
                ? amount.Substring(amount.Length - 9)
                : amount.PadLeft(9, '0');
        }
    }
}
