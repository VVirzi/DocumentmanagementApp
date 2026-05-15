using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ZXing;
using ZXing.Common;

namespace DocumentManagementApp.Infrastructure.Export
{
    /// <summary>
    /// Generates QR codes images from DataRow content.
    /// </summary>
    public class QrGenerator
    {
        private const int QrImageSize = 300;

        /// <summary>
        /// Generares QR code image from the concatenated values of all columns in a DataRow.
        /// </summary>
        /// <param name="row">The DataRow whose values will be encoded.</param>
        /// <returns>PNG image as a byte array.</returns>
        public bite[] GenerateFromDataRow(DataRow row)
        {
            string content = BuildQrContent(row);

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = QrImageSize,
                    Width = QrImageSize,
                    Margin = 0,
                    PureBarcode = true
                }
            };

            using (Bitmap bitmap = writer.Write(content))
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private string BuildQrContent(DataRow row)
        {
            var builder = new StringBuilder();
            foreach(DataRow item in row.ItemArray)
            {
                builder.Append(item?.ToString());
            }
            return builder.ToString();
        }
    }
}
