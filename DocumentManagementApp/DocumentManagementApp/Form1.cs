using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DocumentManagementApp.Core.Factory;
using DocumentManagementApp.Core.Interfaces;
using DocumentManagementApp.Infrastructure.Import;

namespace DocumentManagementApp
{
    public partial class Form1 : Form
    {
        // ─── Fields ───────────────────────────────────────────────
        private IClientExporter _currentExporter;
        private DataTable _formattedTable;

        // ─── Constructor ──────────────────────────────────────────
        public Form1()
        {
            InitializeComponent();
        }

        // ═════════════════════════════════════════════════════════
        // NAVIGATION
        // ═════════════════════════════════════════════════════════

        private void ShowMenuPanel()
        {
            panelMenu.Location = new System.Drawing.Point(12, 12);
            panelMenu.Visible = true;
            panelClient1.Visible = false;
            panelClient2.Visible = false;
            panelClient3.Visible = false;
        }

        private void ShowClient1Panel()
        {
            panelClient1.Location = new System.Drawing.Point(12, 12);
            panelMenu.Visible = false;
            panelClient1.Visible = true;
            panelClient2.Visible = false;
            panelClient3.Visible = false;
        }

        private void ShowClient2Panel()
        {
            panelClient2.Location = new System.Drawing.Point(12, 12);
            panelMenu.Visible = false;
            panelClient1.Visible = false;
            panelClient2.Visible = true;
            panelClient3.Visible = false;
        }

        private void ShowClient3Panel()
        {
            panelClient3.Location = new System.Drawing.Point(12, 12);
            panelMenu.Visible = false;
            panelClient1.Visible = false;
            panelClient2.Visible = false;
            panelClient3.Visible = true;
        }

        // ═════════════════════════════════════════════════════════
        // EXPORT — Table generation
        // ═════════════════════════════════════════════════════════

        private void GenerateClient1Table()
        {
            if (!ArePathsFilled(textBox_File1_Client1.Text, textBox_File2_Client1.Text))
                return;

            try
            {
                ClearGrid(dataGridView_Client1);
                DataTable rawData = ImportMainFile(textBox_File1_Client1.Text);
                _currentExporter = ClientExporterFactory.GetClientExporter("client1");
                _currentExporter.SetSourceFiles(textBox_File1_Client1.Text, textBox_File2_Client1.Text, null);
                _formattedTable = _currentExporter.GenerateFormattedTable(rawData);
                dataGridView_Client1.DataSource = _formattedTable;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void GenerateClient2Table()
        {
            if (!ArePathsFilled(textBox_File1_Client2.Text, textBox_File2_Client2.Text, textBox_File3_Client2.Text))
                return;

            try
            {
                ClearGrid(dataGridView_Client2);
                DataTable rawData = ImportMainFile(textBox_File1_Client2.Text);
                _currentExporter = ClientExporterFactory.GetClientExporter("client2");
                _currentExporter.SetSourceFiles(textBox_File1_Client2.Text, textBox_File2_Client2.Text, textBox_File3_Client2.Text);
                _formattedTable = _currentExporter.GenerateFormattedTable(rawData);
                dataGridView_Client2.DataSource = _formattedTable;
                LoadClient2InvoiceControls();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void GenerateClient3Table(string exportType)
        {
            if (!ArePathsFilled(textBox_File1_Client3.Text))
                return;

            try
            {
                ClearGrid(dataGridView_Client3);
                DataTable rawData = ImportMainFile(textBox_File1_Client3.Text);
                _currentExporter = ClientExporterFactory.GetClientExporter(exportType);
                _currentExporter.SetSourceFiles(textBox_File1_Client3.Text, null, null);
                _formattedTable = _currentExporter.GenerateFormattedTable(rawData);
                dataGridView_Client3.DataSource = _formattedTable;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SaveToFile(string filter)
        {
            if (_formattedTable == null || _currentExporter == null) return;

            using (SaveFileDialog dialog = new SaveFileDialog { Filter = filter })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _currentExporter.ExportToFile(_formattedTable, dialog.FileName);
                    MessageBox.Show("File exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // ═════════════════════════════════════════════════════════
        // CLIENT2 — Bulk edit controls
        // ═════════════════════════════════════════════════════════

        private void LoadClient2InvoiceControls()
        {
            if (_formattedTable == null) return;

            var invoices = _formattedTable.AsEnumerable()
                .Select(r => r["InvoiceNumber"].ToString())
                .Distinct()
                .ToList();

            comboBox_SelectInvoice_Client2.DataSource = invoices;

            string[] editableColumns = { "RecordType", "Date", "F5Number", "MemberNumber", "DeliveryDate" };
            comboBox_SelectRow_Client2.Items.Clear();
            comboBox_SelectRow_Client2.Items.AddRange(editableColumns);
        }

        private void ApplyBulkEdit()
        {
            if (comboBox_SelectInvoice_Client2.SelectedItem == null ||
                comboBox_SelectRow_Client2.SelectedItem == null)
            {
                MessageBox.Show("Please select an invoice and a column to edit.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string invoice = comboBox_SelectInvoice_Client2.SelectedItem.ToString();
            string columnName = comboBox_SelectRow_Client2.SelectedItem.ToString();
            string newValue = textBox_NewData_Client2.Text.Trim();

            if (string.IsNullOrWhiteSpace(newValue))
            {
                MessageBox.Show("Please enter a new value.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rows = _formattedTable.AsEnumerable()
                .Where(r => r["InvoiceNumber"].ToString() == invoice)
                .ToList();

            foreach (var row in rows)
            {
                if (_formattedTable.Columns[columnName].DataType == typeof(DateTime))
                {
                    if (DateTime.TryParse(newValue, out DateTime parsedDate))
                        row[columnName] = parsedDate;
                    else
                    {
                        MessageBox.Show("The value entered is not a valid date.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    row[columnName] = newValue;
                }
            }

            dataGridView_Client2.Refresh();
            MessageBox.Show($"{rows.Count} row(s) updated for invoice {invoice}.", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ═════════════════════════════════════════════════════════
        // UI HELPERS
        // ═════════════════════════════════════════════════════════

        private string PickExcelFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "HTML Excel files (*.xls)|*.xls|All files|*.*"
            })
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        private string PickTxtFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "Text files|*.txt" })
            {
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        private DataTable ImportMainFile(string path)
        {
            return HtmlExcelImporter.Import(path);
        }

        private void ClearGrid(DataGridView grid)
        {
            grid.DataSource = null;
            grid.Rows.Clear();
            grid.Columns.Clear();
        }

        private bool ArePathsFilled(params string[] paths)
        {
            return paths.All(p => !string.IsNullOrWhiteSpace(p));
        }

        private void ShowError(string message)
        {
            MessageBox.Show("Error: " + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ═════════════════════════════════════════════════════════
        // EVENT HANDLERS — Navigation
        // ═════════════════════════════════════════════════════════

        private void Form1_Load(object sender, EventArgs e) => ShowMenuPanel();
        private void button_Return_Client1_Click(object sender, EventArgs e) => ShowMenuPanel();
        private void button_Return_Client2_Click(object sender, EventArgs e) => ShowMenuPanel();
        private void button_Return_Client3_Click(object sender, EventArgs e) => ShowMenuPanel();
        private void button_Client1_Click(object sender, EventArgs e) => ShowClient1Panel();
        private void button_Client2_Click(object sender, EventArgs e) => ShowClient2Panel();
        private void button_Client3_Click(object sender, EventArgs e) => ShowClient3Panel();

        // ═════════════════════════════════════════════════════════
        // EVENT HANDLERS — Client1
        // ═════════════════════════════════════════════════════════

        private void button_ProcessData_Client1_Click(object sender, EventArgs e) => GenerateClient1Table();
        private void button_ExportQr_Client1_Click(object sender, EventArgs e) => SaveToFile("PDF|*.pdf");
        private void button_Clean_Client1_Click(object sender, EventArgs e) => ClearGrid(dataGridView_Client1);
        private void button_File1Search_Client1_Click(object sender, EventArgs e) => textBox_File1_Client1.Text = PickExcelFile();
        private void button_File2Search_Client1_Click(object sender, EventArgs e) => textBox_File2_Client1.Text = PickExcelFile();

        // ═════════════════════════════════════════════════════════
        // EVENT HANDLERS — Client2
        // ═════════════════════════════════════════════════════════

        private void button_ProcessData_Client2_Click(object sender, EventArgs e) => GenerateClient2Table();
        private void button_ExportTxt_Client2_Click(object sender, EventArgs e) => SaveToFile("Text files|*.txt");
        private void button_Clean_Client2_Click(object sender, EventArgs e) => ClearGrid(dataGridView_Client2);
        private void button_Refresh_Client2_Click(object sender, EventArgs e) => ApplyBulkEdit();
        private void button_File1Search_Client2_Click(object sender, EventArgs e) => textBox_File1_Client2.Text = PickExcelFile();
        private void button_File2Search_Client2_Click(object sender, EventArgs e) => textBox_File2_Client2.Text = PickExcelFile();
        private void button_File3Search_Client2_Click(object sender, EventArgs e) => textBox_File3_Client2.Text = PickTxtFile();

        // ═════════════════════════════════════════════════════════
        // EVENT HANDLERS — Client3
        // ═════════════════════════════════════════════════════════

        private void button_Billing_Client3_Click(object sender, EventArgs e) => GenerateClient3Table("client3-billing");
        private void button_Settlements_Client3_Click(object sender, EventArgs e) => GenerateClient3Table("client3-settlements");
        private void button_ExportTxt_Client3_Click(object sender, EventArgs e) => SaveToFile("Text files|*.txt");
        private void button_Clean_Client3_Click(object sender, EventArgs e) => ClearGrid(dataGridView_Client3);
        private void button_File1Search_Client3_Click(object sender, EventArgs e) => textBox_File1_Client3.Text = PickExcelFile();
    }
}