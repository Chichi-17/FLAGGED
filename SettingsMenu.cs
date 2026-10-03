namespace FLAGGED;

public class SettingsMenu
{
    private readonly GameSettings _settings;
    private readonly string[] _labels =
    {
        "Music Volume",
        "Sound Effects Volume",
        "Mute All Audio",
        "Console Theme",
        "Text Size",
        "Clear Screen Before Redrawing",
        "Show Hints",
        "Confirm Before Spending",
        "Difficulty",
        "Reset Game Progress",
        "SAVE SETTINGS"
    };

    private int _selectedIndex;
    private bool _showSavedMessage;

    public SettingsMenu(GameSettings settings)
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
                    _selectedIndex = (_selectedIndex == 0) ? _labels.Length - 1 : _selectedIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    _selectedIndex = (_selectedIndex + 1) % _labels.Length;
                    break;
                case ConsoleKey.LeftArrow:
                    AdjustSelected(-1);
                    break;
                case ConsoleKey.RightArrow:
                    AdjustSelected(1);
                    break;
                case ConsoleKey.Enter:
                    ToggleSelected();
                    break;
                case ConsoleKey.Escape:
                    return;
            }
        }
    }

    private void Render()
    {
        Console.Clear();
        ConsoleUI.ApplyTheme(_settings);
        ConsoleUI.DrawHeader("FLAGGED!", "SETTINGS");

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - 42) / 2), 5);
        Console.Write("Customize your game experience.");

        var row = 8;
        for (var i = 0; i < _labels.Length; i++)
        {
            var label = _labels[i];
            var currentRow = row + i * 2;
            var value = GetValueText(label);

            var left = 14;
            Console.SetCursorPosition(left, currentRow);

            if (_selectedIndex == i)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write($"> {label.PadRight(28)} {value}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"  {label.PadRight(28)} {value}");
            }
        }

        if (_showSavedMessage)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - 18) / 2), Console.WindowHeight - 5);
            Console.Write("Settings saved!");
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.SetCursorPosition(2, Console.WindowHeight - 2);
        Console.Write("Arrow keys to adjust • Enter to toggle • Esc to return");
    }

    private string GetValueText(string label)
    {
        return label switch
        {
            "Music Volume" => $"{_settings.MusicVolume}%",
            "Sound Effects Volume" => $"{_settings.SoundEffectsVolume}%",
            "Mute All Audio" => _settings.MuteAllAudio ? "ON" : "OFF",
            "Console Theme" => _settings.GetThemeName(),
            "Text Size" => _settings.GetTextSizeName() + " (Windows Terminal)",
            "Clear Screen Before Redrawing" => _settings.ClearScreenBeforeRedrawing ? "ON" : "OFF",
            "Show Hints" => _settings.ShowHints ? "ON" : "OFF",
            "Confirm Before Spending" => _settings.ConfirmBeforeSpending ? "ON" : "OFF",
            "Difficulty" => _settings.GetDifficultyName(),
            "Reset Game Progress" => "Placeholder",
            "SAVE SETTINGS" => "Save",
            _ => ""
        };
    }

    private void AdjustSelected(int direction)
    {
        switch (_labels[_selectedIndex])
        {
            case "Music Volume":
                _settings.MusicVolume = _settings.ClampVolume(_settings.MusicVolume + direction * 5);
                break;
            case "Sound Effects Volume":
                _settings.SoundEffectsVolume = _settings.ClampVolume(_settings.SoundEffectsVolume + direction * 5);
                break;
            case "Console Theme":
                _settings.CycleTheme(direction);
                ConsoleUI.ApplyTheme(_settings);
                break;
            case "Text Size":
                _settings.CycleTextSize(direction);
                break;
            case "Difficulty":
                _settings.CycleDifficulty(direction);
                break;
        }
    }

    private void ToggleSelected()
    {
        switch (_labels[_selectedIndex])
        {
            case "Mute All Audio":
                _settings.MuteAllAudio = !_settings.MuteAllAudio;
                break;
            case "Clear Screen Before Redrawing":
                _settings.ClearScreenBeforeRedrawing = !_settings.ClearScreenBeforeRedrawing;
                break;
            case "Show Hints":
                _settings.ShowHints = !_settings.ShowHints;
                break;
            case "Confirm Before Spending":
                _settings.ConfirmBeforeSpending = !_settings.ConfirmBeforeSpending;
                break;
            case "Console Theme":
                _settings.CycleTheme(1);
                ConsoleUI.ApplyTheme(_settings);
                break;
            case "Text Size":
                _settings.CycleTextSize(1);
                break;
            case "Difficulty":
                _settings.CycleDifficulty(1);
                break;
            case "Reset Game Progress":
                if (ConfirmReset())
                {
                    ConsoleUI.ShowTemporaryMessage("Progress reset placeholder. Gameplay not implemented yet.");
                }
                break;
            case "SAVE SETTINGS":
                _showSavedMessage = true;
                ConsoleUI.ShowTemporaryMessage("Settings saved!");
                break;
        }
    }

    private bool ConfirmReset()
    {
        var selectedIndex = 0;
        while (true)
        {
            Console.Clear();
            ConsoleUI.ApplyTheme(_settings);
            ConsoleUI.DrawBox(20, 12, 50, 8, "RESET PROGRESS", ConsoleColor.Yellow);
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(25, 14);
            Console.Write("Gameplay is not implemented yet.");
            Console.SetCursorPosition(25, 15);
            Console.Write("Proceed with placeholder reset?");

            var options = new[] { "YES", "NO" };
            for (var i = 0; i < options.Length; i++)
            {
                var left = 32 + i * 12;
                Console.SetCursorPosition(left, 17);
                if (selectedIndex == i)
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
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
