using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;
using DocumentManagementApp.Core.Base;

namespace DocumentManagementApp.Clients.Client3
{
    /// <summary>
    /// Generates the billing report table for Client3.
    /// </summary>
    public class Client3BillingExporter : ClientExporterBase
    {
        /// <inheritdoc/>
        public override DataTable GenerateFormattedTable(DataTable rawData)
        {
            DataTable result = CreateResultTable();
            var invoiceProductMap = new Dictionary<string, DataRow>();

            foreach (DataRow row in rawData.Rows)
            {
                string invoiceId = row[1]?.ToString();
                if (string.IsNullOrEmpty(invoiceId)) continue;

                string productId = row[9]?.ToString();
                string key = $"{invoiceId}_{productId}";

                if(invoiceProductMap.TryGetValue(key, out DataRow existingRow))
                {
                    UpdateExistingRow(row, existingRow);
                }
                else
                {
                    DataRow newRow = BuildRow(result, row);
                    result.Rows.Add(newRow);
                    invoiceProductMap[key] = newRow;
                }
            }
            return result;
        }

        /// <inheritdoc/>
        public override DataTable CreateResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("Activity", typeof(string));
            table.Columns.Add("Supplier", typeof(string));
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Reference", typeof(string));
            table.Columns.Add("Class", typeof(string));
            table.Columns.Add("Division", typeof(string));
            table.Columns.Add("Text", typeof(string));
            table.Columns.Add("CommercialSite", typeof(string));
            table.Columns.Add("OrderNumber", typeof(string));
            table.Columns.Add("Position", typeof(string));
            table.Columns.Add("Quantity", typeof(string));
            table.Columns.Add("Amount", typeof(string));
            table.Columns.Add("TotalAmount", typeof(string));
            return table;
        }

        private DataRow BuildRow(DataTable table, DataRow source)
        {
            DataRow newRow = table.NewRow();

            newRow["Activity"] = "1";
            newRow["Supplier"] = "135835";
            newRow["Date"] = ParseDate(source[2]?.ToString(), "yyyyMMdd");
            newRow["Reference"] = BuildReference(source);
            newRow["Class"] = "RE";
            newRow["Division"] = "0001";
            newRow["Text"] = $"{source[14]} {source[13]} {source[3]}";
            newRow["CommercialSite"] = "0001";
            newRow["OrderNumber"] = source[19]?.ToString();
            newRow["Position"] = "101";

            decimal units = ParseDecimal(source[8]?.ToString());
            decimal unitPrice = ParseDecimal(NormalizePrice(source[12]?.ToString()));
            string totalPrice = NormalizePrice(source[16]?.ToString());

            newRow["Quantity"] = units.ToString("F3", CultureInfo.InvariantCulture);
            newRow["Amount"] = (unitPrice * units).ToString("F2", CultureInfo.InvariantCulture);
            newRow["TotalAmount"] = totalPrice;

            return newRow;
        }

        private void UpdateExistingRow(DataRow source, DataRow existing)
        {
            decimal newUnits = ParseDecimal(source[8]?.ToString());
            decimal oldUnits = ParseDecimal(existing["Quantity"].ToString());
            decimal totalUnits = oldUnits + newUnits;

            decimal unitPrice = ParseDecimal(NormalizePrice(source[12]?.ToString()));

            existing["Quantity"] = totalUnits.ToString("F3", CultureInfo.InvariantCulture);
            existing["Amount"] = (unitPrice * totalUnits).ToString("F2", CultureInfo.InvariantCulture);
        }

        private string ParseDate(string raw, string format)
        {
            return DateTime.TryParse(raw, out DateTime date) ? date.ToString(format) : string.Empty;
        }

        private decimal ParseDecimal(string raw)
        {
            return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result) ? result : 0m;
        }

        private string NormalizePrice(string price)
        {
            if(string.IsNullOrEmpty(price)) return "0";
            if(price.Length >= 3 && price[price.Length - 3] == ',') 
                return price.Replace(".", string.Empty).Replace(",", ".");
            return price.Replace(",", string.Empty);
        }

        private string TrimDocumentType(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return raw.Trim().Substring(raw.Trim().Length - 1);
        }

        private string BuildReference(DataRow source)
        {
            string documentType = TrimDocumentType(source[0]?.ToString());
            string invoice = source[1]?.ToString();
            return invoice?.Replace("-", documentType);
        }
    }
}
