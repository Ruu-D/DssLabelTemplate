using System.Text;

namespace DssLabelTemplate;

/// <summary>How the lines of a file end: Windows (CR LF) or Unix (LF only).</summary>
internal enum LineEnding
{
    CrLf,
    Lf,
}

/// <summary>
/// The text of a file, plus what is needed to save it back exactly the way it was: the encoding (with or without
/// BOM) and the line endings. The text itself always has CR LF line endings, because the text box needs them.
/// </summary>
internal record TextFileContent(string Text, Encoding Encoding, LineEnding LineEnding);

/// <summary>
/// Reads and writes the template files. A template may come from any program (Notepad, Duivestein's own tools, an old
/// editor), so the encoding is detected, and Save writes the file back in the same encoding and with the same
/// line endings: a file that is only opened and saved stays byte for byte the same.
/// </summary>
internal static class TextFile
{
    public static TextFileContent Read(string path)
    {
        // FileShare.ReadWrite: another program may have the file open.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Decode(memory.ToArray());
    }

    /// <summary>
    /// A byte order mark (UTF-8, UTF-16) says the encoding. Without one: UTF-8 if the bytes are valid UTF-8, otherwise
    /// Windows-1252 ("ANSI", what old Windows programs save). "Chêne" survives every way.
    /// </summary>
    public static TextFileContent Decode(byte[] bytes)
    {
        Encoding encoding;
        int start;

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            start = 3;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            start = 2;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            start = 2;
        }
        else if (IsValidUtf8(bytes))
        {
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            start = 0;
        }
        else
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            encoding = Encoding.GetEncoding(1252);
            start = 0;
        }

        string text = encoding.GetString(bytes, start, bytes.Length - start);

        // A file with any CR LF is a Windows file; a file with only LF a Unix file. The text box shows both as CR LF.
        LineEnding lineEnding = !text.Contains("\r\n") && text.Contains('\n') ? LineEnding.Lf : LineEnding.CrLf;
        string forTextBox = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        return new TextFileContent(forTextBox, encoding, lineEnding);
    }

    /// <summary>
    /// Writes the text in the given encoding and line endings: first as name.tmp in the same folder, then renamed, so a
    /// problem halfway never leaves a half-written template. May throw IOException / UnauthorizedAccessException.
    /// </summary>
    public static void Write(string path, string text, Encoding encoding, LineEnding lineEnding)
    {
        string normalised = text.Replace("\r\n", "\n");
        if (lineEnding == LineEnding.CrLf)
        {
            normalised = normalised.Replace("\n", "\r\n");
        }

        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, normalised, encoding); // writes the BOM if the encoding has one
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            File.Delete(temporary); // never leave the .tmp behind (Delete does nothing if it is not there)
            throw;
        }
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            // throwOnInvalidBytes: true -> invalid UTF-8 throws instead of silently giving '?' characters.
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
