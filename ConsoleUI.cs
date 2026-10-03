namespace FLAGGED;

public static class ConsoleUI
{
    public static void ApplyTheme(GameSettings settings)
    {
        Console.Clear();

        switch (settings.ConsoleTheme)
        {
            case ConsoleThemeOption.DarkGreenTerminal:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                break;
            case ConsoleThemeOption.ClassicMonochrome:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case ConsoleThemeOption.HighContrast:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                break;
            default:
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                break;
        }
    }

    public static void ResetColors()
    {
        Console.ResetColor();
    }

    public static void CenterText(string text, int row, ConsoleColor color = ConsoleColor.Gray)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        var x = Math.Max(0, (Console.WindowWidth - text.Length) / 2);
        Console.SetCursorPosition(x, row);
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public static void DrawBox(int left, int top, int width, int height, string title = "", ConsoleColor borderColor = ConsoleColor.DarkGreen)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = borderColor;

        var titleText = title.Length > 0 ? $" {title} " : string.Empty;

        for (var y = top; y < top + height; y++)
        {
            Console.SetCursorPosition(left, y);
            Console.Write(y == top || y == top + height - 1 ? "┌" : "│");

            Console.SetCursorPosition(left + width - 1, y);
            Console.Write(y == top || y == top + height - 1 ? "┐" : "│");

            if (y > top && y < top + height - 1)
            {
                Console.SetCursorPosition(left + 1, y);
                Console.Write(new string(' ', width - 2));
            }
        }

        Console.SetCursorPosition(left, top);
        Console.Write("┌");
        Console.SetCursorPosition(left + width - 1, top);
        Console.Write("┐");
        Console.SetCursorPosition(left, top + height - 1);
        Console.Write("└");
        Console.SetCursorPosition(left + width - 1, top + height - 1);
        Console.Write("┘");

        for (var x = left + 1; x < left + width - 1; x++)
        {
            Console.SetCursorPosition(x, top);
            Console.Write("─");
            Console.SetCursorPosition(x, top + height - 1);
            Console.Write("─");
        }

        if (!string.IsNullOrEmpty(titleText))
        {
            Console.SetCursorPosition(left + 2, top);
            Console.Write(titleText);
        }

        Console.ForegroundColor = originalColor;
    }

    public static void DrawPanel(int left, int top, int width, int height, string title = "", ConsoleColor borderColor = ConsoleColor.DarkGreen)
    {
        DrawBox(left, top, width, height, title, borderColor);
    }

    public static void DrawMenuOption(int row, string text, bool selected, bool isAction = true)
    {
        var left = Console.WindowWidth / 2 - 18;
        var indicator = selected ? ">" : " ";
        var pad = selected ? " " : "  ";
        var originalBg = Console.BackgroundColor;
        var originalFg = Console.ForegroundColor;

        Console.SetCursorPosition(left, row);
        Console.Write(indicator + " ");

        if (selected)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        else if (isAction)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        Console.Write(pad + text.PadRight(26));
        Console.BackgroundColor = originalBg;
        Console.ForegroundColor = originalFg;
    }

    public static void DrawButton(int row, string text, bool selected, ConsoleColor borderColor = ConsoleColor.DarkGreen)
    {
        var width = Math.Max(24, text.Length + 8);
        var left = Console.WindowWidth / 2 - width / 2;
        var originalBg = Console.BackgroundColor;
        var originalFg = Console.ForegroundColor;

        DrawBox(left - 2, row - 1, width + 4, 3, selected ? "[ SELECTED ]" : string.Empty, borderColor);

        Console.SetCursorPosition(left, row);
        if (selected)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(text.PadRight(width));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(text.PadRight(width));
        }

        Console.BackgroundColor = originalBg;
        Console.ForegroundColor = originalFg;
    }

    public static void ShowTemporaryMessage(string message)
    {
        var row = Console.WindowHeight - 3;
        var originalBg = Console.BackgroundColor;
        var originalFg = Console.ForegroundColor;

        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.ForegroundColor = ConsoleColor.White;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - message.Length) / 2), row);
        Console.Write(message.PadRight(Console.WindowWidth - 2));
        Console.BackgroundColor = originalBg;
        Console.ForegroundColor = originalFg;
        Thread.Sleep(1200);
    }

    public static void DrawHeader(string title, string subtitle)
    {
        var mainTitle = title;
        var mainY = 1;

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - mainTitle.Length) / 2), mainY);
        Console.Write(mainTitle);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - subtitle.Length) / 2), mainY + 2);
        Console.Write(subtitle);

        Console.WriteLine();
    }

    public static void DrawDivider(int row, int width)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.SetCursorPosition(0, row);
        Console.Write(new string('─', width));
        Console.ForegroundColor = original;
    }

    public static void WriteLineAt(int x, int y, string text, ConsoleColor color = ConsoleColor.Gray)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.SetCursorPosition(x, y);
        Console.Write(text.Length > Console.WindowWidth - x ? text.Substring(0, Math.Max(0, Console.WindowWidth - x - 1)) : text);
        Console.ForegroundColor = original;
    }
}
