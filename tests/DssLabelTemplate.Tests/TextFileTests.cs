using System.Text;

namespace DssLabelTemplate.Tests;

/// <summary>A template that is only opened and saved must stay byte for byte the same, whatever program made it.</summary>
public class TextFileTests
{
    private static byte[] RoundTrip(byte[] original)
    {
        using var folder = new TempFolder();
        string path = folder.File("template.xml");
        File.WriteAllBytes(path, original);

        TextFileContent content = TextFile.Read(path);
        TextFile.Write(path, content.Text, content.Encoding, content.LineEnding);

        Assert.False(File.Exists(path + ".tmp"));
        return File.ReadAllBytes(path);
    }

    public static IEnumerable<object[]> Files()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        const string text = "<PrintLabel ROTATION=\"$ROTATION$\">\r\n  Chêne é à\r\n</PrintLabel>\r\n";

        yield return new object[] { "UTF-8 without BOM", Encoding.UTF8.GetBytes(text) };
        yield return new object[] { "UTF-8 with BOM", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray() };
        yield return new object[] { "Windows-1252", Encoding.GetEncoding(1252).GetBytes(text) };
        yield return new object[] { "UTF-16", Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray() };
        yield return new object[] { "LF only", Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n")) };
        yield return new object[] { "No line break", Encoding.UTF8.GetBytes("one line") };
        yield return new object[] { "Empty", Array.Empty<byte>() };
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void OpenAndSave_KeepsTheFileByteForByte(string kind, byte[] original)
    {
        Assert.True(original.SequenceEqual(RoundTrip(original)), kind);
    }

    [Fact]
    public void AccentsSurvive_InEveryEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Assert.Equal("Chêne", TextFile.Decode(Encoding.UTF8.GetBytes("Chêne")).Text);
        Assert.Equal("Chêne", TextFile.Decode(Encoding.GetEncoding(1252).GetBytes("Chêne")).Text);
    }

    [Fact]
    public void TheEditorAlwaysGetsWindowsLineEndings()
    {
        TextFileContent content = TextFile.Decode(Encoding.UTF8.GetBytes("a\nb\nc"));

        Assert.Equal("a\r\nb\r\nc", content.Text);
        Assert.Equal(LineEnding.Lf, content.LineEnding);
    }

    [Fact]
    public void AnEditedText_IsWrittenWithTheLineEndingsOfTheFile()
    {
        using var folder = new TempFolder();
        string path = folder.File("unix.txt");

        TextFile.Write(path, "a\r\nb", new UTF8Encoding(false), LineEnding.Lf);

        Assert.Equal("a\nb", File.ReadAllText(path));
    }
}
