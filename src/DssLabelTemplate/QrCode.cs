using QRCoder;

namespace DssLabelTemplate;

/// <summary>
/// The squares of a real, scannable QR code, made by QRCoder (NuGet, MIT licence, approved by Daan 2026-10-25).
/// The manual's QRCode object has Type="AUTO" and says nothing about the error correction: level M (about 15% of the code
/// may be damaged) is the usual choice and keeps the code small enough for an 8 mm square on a 60 x 60 label.
/// </summary>
internal static class QrCode
{
    // QRCoder puts a white border of 4 squares around the code (the "quiet zone"). The label template decides where the
    // code sits, so that border is left out here.
    private const int QuietZone = 4;

    /// <summary>
    /// The squares, [row, column], true = black, without the white border. Null when the text is too long for any QR code.
    /// </summary>
    public static bool[,]? Modules(string text)
    {
        QRCodeData data;
        try
        {
            using var generator = new QRCodeGenerator();
            data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        }
        catch (QRCoder.Exceptions.DataTooLongException)
        {
            return null;
        }

        using (data)
        {
            int size = data.ModuleMatrix.Count - 2 * QuietZone;
            var modules = new bool[size, size];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    modules[row, column] = data.ModuleMatrix[row + QuietZone][column + QuietZone];
                }
            }

            return modules;
        }
    }
}
