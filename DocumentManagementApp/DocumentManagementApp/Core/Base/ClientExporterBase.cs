using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DocumentManagementApp.Core.Interfaces;
using DocumentManagementApp.Infrastructure.Import;

namespace DocumentManagementApp.Core.Base
{
    /// <summary>
    /// Provides base functionality shared across all client exporters.
    /// </summary>
    public abstract class ClientExporterBase: IClientExporter
    {
        private string _primaryPath;
        private string _secondaryPath;
        private string _tertiaryPath;

        public string PrimaryPath => _primaryPath;
        public string SecondaryPath => _secondaryPath;
        public string TertiaryPath => _tertiaryPath;

        /// <inheritdoc/>
        public abstract DataTable GenerateFormattedTable(DataTable rawData);

        /// <inheritdoc/>
        public abstract DataTable CreateResultTable();

        ///<inheritdoc/>
        public virtual void SetSourceFiles(string primaryPath, string secondaryPath, string tertiaryPath)
        {
            _primaryPath = primaryPath;
            _secondaryPath = secondaryPath;
            _tertiaryPath = tertiaryPath;
        }

        ///<inheritdoc/>
        public virtual void ExportToFile(DataTable formattedTable, string outputPath)
        {
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (DataRow row in formattedTable.Rows)
                {
                    var values = row.ItemArray.Select(c => c.ToString());
                    writer.WriteLine(string.Join("\t", values));
                }
            }
        }

        ///<summary>
        /// Imported an HTML-based Excel file into a Datatable.
        /// </summary>
        protected DataTable ImportFileIntoDataTable(string filePath)
        {
            try
            {
                return HTMLExcelImporter.Import(filePath);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error importing file: {ex.Message}");
                //MessageBox.Show($"Error importing file: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
}
