namespace FLAGGED;

using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;

public static class UiFonts
{
    private static readonly PrivateFontCollection PrivateFonts = new();
    private static readonly FontFamily GameFontFamily = LoadFontsAndResolveFamily();

    public static Font GetGameFont(float size, FontStyle style = FontStyle.Bold)
    {
        return new Font(GameFontFamily, size, style, GraphicsUnit.Pixel);
    }

    public static Font GetBodyFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font(GameFontFamily, size, style, GraphicsUnit.Pixel);
    }

    public static Font GameTitle(float size = 58f, FontStyle style = FontStyle.Bold)
    {
        return GetGameFont(size, style);
    }

    public static Font Heading(float size = 28f, FontStyle style = FontStyle.Bold)
    {
        return GetGameFont(size, style);
    }

    public static Font MenuButton(float size = 18f, FontStyle style = FontStyle.Bold)
    {
        return GetGameFont(size, style);
    }

    public static Font Body(float size = 12f, FontStyle style = FontStyle.Regular)
    {
        return GetBodyFont(size, style);
    }

    public static Font Small(float size = 10f, FontStyle style = FontStyle.Regular)
    {
        return GetBodyFont(size, style);
    }

    private static FontFamily LoadFontsAndResolveFamily()
    {
        LoadEmbeddedFonts();
        return ResolveGameFontFamily();
    }

    private static void LoadEmbeddedFonts()
    {
        var candidateDirectories = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "assets", "Fonts"),
            Path.Combine(AppContext.BaseDirectory, "assets"),
            AppContext.BaseDirectory
        };

        foreach (var directory in candidateDirectories.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.ttf", SearchOption.AllDirectories).OrderBy(path => path))
            {
                try
                {
                    PrivateFonts.AddFontFile(file);
                }
                catch
                {
                    // Ignore invalid or unsupported font files and fall back gracefully.
                }
            }

            if (PrivateFonts.Families.Length > 0)
            {
                break;
            }
        }
    }

    private static FontFamily ResolveGameFontFamily()
    {
        try
        {
            return new FontFamily("Consolas");
        }
        catch
        {
        }

        if (PrivateFonts.Families.Length > 0)
        {
            return PrivateFonts.Families[0];
        }

        var fallbackFamilies = new[]
        {
            "Cascadia Mono",
            "Courier New",
            "Lucida Console"
        };

        foreach (var familyName in fallbackFamilies)
        {
            try
            {
                return new FontFamily(familyName);
            }
            catch
            {
                // Continue to next fallback.
            }
        }

        return FontFamily.GenericSansSerif;
    }
}
