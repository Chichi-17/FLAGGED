namespace FLAGGED;

public class MainMenu
{
    private readonly GameSettings _settings;
    private readonly string[] _options = { "START GAME", "HOW TO PLAY", "SETTINGS", "QUIT" };
    private int _selectedIndex;

    public MainMenu(GameSettings settings)
    {
        _settings = settings;
    }

    public void Show()
    {
        while (true)
        {
            Render();

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    _selectedIndex = (_selectedIndex == 0) ? _options.Length - 1 : _selectedIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    _selectedIndex = (_selectedIndex + 1) % _options.Length;
                    break;
                case ConsoleKey.Enter:
                    switch (_selectedIndex)
                    {
                        case 0:
                            ConsoleUI.ShowTemporaryMessage("Gameplay coming soon.");
                            break;
                        case 1:
                            new HowToPlay(_settings).Show();
                            break;
                        case 2:
                            new SettingsMenu(_settings).Show();
                            break;
                        case 3:
                            if (ConfirmQuit())
                            {
                                return;
                            }
                            break;
                    }
                    break;
                case ConsoleKey.Escape:
                    if (ConfirmQuit())
                    {
                        return;
                    }
                    break;
            }
        }
    }

    private void Render()
    {
        Console.Clear();
        ConsoleUI.ApplyTheme(_settings);

        if (Console.WindowHeight >= 16 && Console.WindowWidth >= 60)
        {
            DrawTerminalBackdrop();
        }

        DrawMenuWindow();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        SafeSetCursorPosition(2, Math.Max(0, Console.WindowHeight - 2));
        Console.Write("Use ↑ ↓ to navigate • Enter to confirm • Esc to quit");
    }

    private void DrawMenuWindow()
    {
        var left = Math.Max(12, (Console.WindowWidth - 52) / 2);
        var top = 9;
        var width = 52;
        var height = 18;

        ConsoleUI.DrawPanel(left, top, width, height, "SYSTEM MENU", ConsoleColor.DarkGreen);

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        SafeSetCursorPosition(Math.Max(0, (Console.WindowWidth - 11) / 2), 2);
        Console.Write("FLAGGED!");

        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(Math.Max(0, (Console.WindowWidth - 46) / 2), 4);
        Console.Write("THINK. VERIFY. MAKE A DIFFERENCE.");

        var optionTop = top + 3;
        for (var i = 0; i < _options.Length; i++)
        {
            var row = optionTop + i * 3;
            DrawMenuRow(row, _options[i], i == _selectedIndex);
        }
    }

    private static void DrawMenuRow(int row, string option, bool selected)
    {
        var left = Math.Max(12, (Console.WindowWidth - 52) / 2) + 4;

        Console.SetCursorPosition(left, row);
        Console.Write(selected ? "> " : "  ");

        if (selected)
        {
            var originalBg = Console.BackgroundColor;
            var originalFg = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(option.PadRight(26));
            Console.BackgroundColor = originalBg;
            Console.ForegroundColor = originalFg;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(option.PadRight(26));
        }
    }

    private static void DrawTerminalBackdrop()
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;

        var monitorLeft = Console.WindowWidth / 2 - 20;
        var monitorTop = 8;
        var monitorWidth = 38;
        var monitorHeight = 15;

        SafeSetCursorPosition(monitorLeft - 4, monitorTop - 1);
        Console.Write("╔══════════════════════════════════════╗");
        SafeSetCursorPosition(monitorLeft - 4, monitorTop);
        Console.Write("║");
        SafeSetCursorPosition(monitorLeft + monitorWidth + 2, monitorTop);
        Console.Write("║");
        for (var y = monitorTop + 1; y < monitorTop + monitorHeight - 1; y++)
        {
            SafeSetCursorPosition(monitorLeft - 4, y);
            Console.Write("║");
            SafeSetCursorPosition(monitorLeft + monitorWidth + 2, y);
            Console.Write("║");
        }

        SafeSetCursorPosition(monitorLeft - 4, monitorTop + monitorHeight - 1);
        Console.Write("╚══════════════════════════════════════╝");

        SafeSetCursorPosition(monitorLeft, monitorTop + 1);
        Console.Write(new string(' ', monitorWidth));

        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(monitorLeft + 4, monitorTop + 3);
        Console.Write("FLAGGED! TERMINAL");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        SafeSetCursorPosition(monitorLeft + 5, monitorTop + 6);
        Console.Write("VERIFY DATA");
        SafeSetCursorPosition(monitorLeft + 5, monitorTop + 8);
        Console.Write("CHECK SOURCE");
        SafeSetCursorPosition(monitorLeft + 5, monitorTop + 10);
        Console.Write("PROTECT FAMILY");
    }

    private static void SafeSetCursorPosition(int left, int top)
    {
        var safeLeft = Math.Clamp(left, 0, Math.Max(0, Console.WindowWidth - 1));
        var safeTop = Math.Clamp(top, 0, Math.Max(0, Console.WindowHeight - 1));
        Console.SetCursorPosition(safeLeft, safeTop);
    }

    private bool ConfirmQuit()
    {
        var selectedIndex = 0;
        while (true)
        {
            Console.Clear();
            ConsoleUI.ApplyTheme(_settings);
            ConsoleUI.DrawBox(15, 10, 60, 9, "QUIT", ConsoleColor.Red);

            Console.ForegroundColor = ConsoleColor.White;
            SafeSetCursorPosition(21, 12);
            Console.Write("Are you sure you want to quit?");

            var options = new[] { "YES", "NO" };
            for (var i = 0; i < options.Length; i++)
            {
                var left = 30 + i * 16;
                SafeSetCursorPosition(left, 15);
                if (selectedIndex == i)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write($"[{options[i]}]");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($" {options[i]} ");
                }
            }

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex == 0) ? 1 : 0;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex == 0) ? 1 : 0;
                    break;
                case ConsoleKey.Enter:
                    return selectedIndex == 0;
                case ConsoleKey.Escape:
                    return false;
            }
        }
    }
}
