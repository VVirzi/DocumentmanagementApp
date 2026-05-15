using System.Data;
using System.Linq;
using DocumentManagementApp.Infrastructure.Export;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace DocumentManagementApp.Infrastructure.Export
{
    /// <summary>
    /// Exxports a Datatable as a PDF file where each row is rendered as a QR code.
    /// </summary>
    public class QrPdfExporter
    {
        private readonly QrGenerator _qrGenerator;

        private const float QrSizeInPoints = 56.7f;
        private const int QrsPerPage = 1;
        private const float PageWidthInMm = 210f;
        private const float MarginInMm = 20f;

        public QrPdfExporter()
        {
            _qrGenerator = new QrGenerator();
        }

        /// <summary>
        /// Exports each row of the DataTable as a QR code into a paginated PDF file.
        /// </summary>
        /// <param name="table">The DataTable containing the rows to encode.</param>
        /// <param name="outputPath">Full path where the PDF will be saved.</param>
        public void Export(DataTable table, string outputPath)
        {
            using (PdfWriter writer = new PdfWriter(outputPath))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document document = new Document(pdf, PageSize.A4))
            {
                float marginInPoints = MmToPoints(MarginInMm);
                document.SetMargins(marginInPoints, marginInPoints, marginInPoints, marginInPoints);

                float uasbleWidth = MmToPoints(PageWidthInMm);
                Table pdfTable = CreatePdfTable(uasbleWidth);
                int counter = 0;

                foreach (DataRow row in table.Rows)
                {
                    if (counter > 0 && counter % QrsPerPage == 0)
                    {
                        document.Add(pdfTable);
                        document.Add(new AreaBreak());
                        pdfTable = CreatePdfTable(uasbleWidth);
                    }

                    pdfTable.AddCell(BuildQrCell(row));
                    counter++;
                }

                document.Add(pdfTable)
            }
        }

        private Table CreatePdfTable(float usableWith)
        {
            return new Table(UnitValue.CreatePointArray(
                Enumerable.Repeat(usableWidth, 1).ToArray()
            ));
        }

        private Cell BuildQrCell(DataRow row)
        {
            byte[] qrBytes = _qrGenerator.GenerateFromDataRow(row);
            Image qrImage = new Image(ImageDataFactory.Create(qrBytes))
                .ScaleAbsolute(QrSizeInPoints, QrSizeInPoints)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            Paragraph label = new Paragraph(row[1]?.ToString())
                .SetFontSize(8)
                .SetMargin(0)
                .SetTextAlignment(TextAlignment.CENTER);

            return new Cell()
                .Add(qrImage)
                .Add(label)
                .SetPadding(1)
                .SetTextAlignment(TextAlignment.CENTER);
        }

        private float MmToPoints(float mm)
        {
            return mm * 72f / 25.4f;
        }
    }
}
