using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

namespace DocumentManagementApp.Core.Interfaces
{
    /// <summary>
    /// Defines the contract that all client exporters must implement.
    /// </summary>
    public interface IClientExporter
    {
        /// <summary>
        /// Transforms a raw imported DataTable into the client's required format.
        /// </summary>
        DataTable GenerateFormattedTable(DataTable rawData);

        /// <summary>
        /// Exports the formatted DataTable to a file at the specified path.
        /// </summary>
        void ExportToFile(DataTable formattedTable, string outputPath);

        /// <summary>
        /// Creates an empty DataTable with the columns required by this client.
        /// </summary>
        DataTable CreateResultTable();

        /// <summary>
        /// Sets the file paths needed for processing (main file, secondary, tertiary).
        /// </summary>
        void SetSourceFiles(string primaryPath, string secondaryPath, string tertiaryPath);
    }
}
