namespace DssLabelTemplate.Tests;

/// <summary>The logo and media\app.ico, which is made from it (--write-icon).</summary>
public class AppLogoTests
{
    [Fact]
    public void WriteIco_GivesAnIconWindowsCanRead_InEverySize()
    {
        using var folder = new TempFolder();
        string path = folder.File("app.ico");

        IconFileWriter.WriteIco(path);

        // The sizes below 256 are bitmaps: System.Drawing reads them like Windows does.
        foreach (int size in IconFileWriter.Sizes.Where(size => size < 256))
        {
            using var icon = new Icon(path, size, size);
            Assert.Equal(new Size(size, size), icon.Size);
            using Bitmap picture = icon.ToBitmap();
            CheckLogo(picture, size);
        }

        // 256 is a PNG inside the .ico (like Windows' own icons). System.Drawing.Icon cannot read that one, Windows can:
        // so the entry is found in the file's directory and the PNG itself is checked.
        byte[] file = File.ReadAllBytes(path);
        int count = BitConverter.ToInt16(file, 4);
        Assert.Equal(IconFileWriter.Sizes.Length, count);
        int last = 6 + 16 * (count - 1);
        Assert.Equal(0, file[last]); // width 0 = 256
        int length = BitConverter.ToInt32(file, last + 8), offset = BitConverter.ToInt32(file, last + 12);
        using var png = new Bitmap(new MemoryStream(file, offset, length));
        CheckLogo(png, 256);
    }

    private static void CheckLogo(Bitmap picture, int size)
    {
        Assert.Equal(new Size(size, size), picture.Size);
        Assert.True(picture.GetPixel(size / 2, size / 2).A > 200, $"the middle of the {size} px icon is not visible");
        Assert.Equal(0, picture.GetPixel(0, 0).A); // the corner outside the rounded square is transparent
    }

    [Fact]
    public void TheIconInMedia_IsTheIconOfTheLogo()
    {
        // If this fails, the logo was changed without writing the icon again:
        //   dotnet run --project src/DssLabelTemplate -- --write-icon media
        using var folder = new TempFolder();
        string fresh = folder.File("app.ico");
        IconFileWriter.WriteIco(fresh);

        Assert.Equal(File.ReadAllBytes(fresh), File.ReadAllBytes(Path.Combine(Samples.RepositoryFolder, "media", "app.ico")));
    }

    [Fact]
    public void TheBanner_HasTheLogoAndIsDrawnOnTheBrandColour()
    {
        using Image banner = AppIcons.Banner();
        using var picture = new Bitmap(banner);

        Assert.Equal(Theme.Brand.ToArgb(), picture.GetPixel(picture.Width - 2, 2).ToArgb()); // plain brand blue on the right
        Assert.Equal(Theme.White.ToArgb(), picture.GetPixel(60, 40).ToArgb());               // the white label of the logo
    }
}
