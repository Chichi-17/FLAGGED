namespace FLAGGED;

public enum ConsoleThemeOption
{
    DarkGreenTerminal,
    ClassicMonochrome,
    HighContrast
}

public enum TextSizeOption
{
    Small,
    Medium,
    Large
}

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

public class GameSettings
{
    public int MusicVolume { get; set; } = 70;
    public int SoundEffectsVolume { get; set; } = 65;
    public bool MuteAllAudio { get; set; } = false;
    public ConsoleThemeOption ConsoleTheme { get; set; } = ConsoleThemeOption.DarkGreenTerminal;
    public TextSizeOption TextSize { get; set; } = TextSizeOption.Medium;
    public bool ClearScreenBeforeRedrawing { get; set; } = true;
    public bool ShowHints { get; set; } = true;
    public bool ConfirmBeforeSpending { get; set; } = true;
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

    public int ClampVolume(int value)
    {
        return Math.Clamp(value, 0, 100);
    }

    public void AdjustMusicVolume(int direction)
    {
        if (MuteAllAudio)
        {
            return;
        }

        MusicVolume = ClampVolume(MusicVolume + direction);
    }

    public void AdjustSoundEffectsVolume(int direction)
    {
        if (MuteAllAudio)
        {
            return;
        }

        SoundEffectsVolume = ClampVolume(SoundEffectsVolume + direction);
    }

    public ConsoleThemeOption CycleTheme(int direction)
    {
        var themeValues = Enum.GetValues<ConsoleThemeOption>();
        var currentIndex = Array.IndexOf(themeValues, ConsoleTheme);
        var nextIndex = (currentIndex + direction + themeValues.Length) % themeValues.Length;
        ConsoleTheme = themeValues[nextIndex];
        return ConsoleTheme;
    }

    public TextSizeOption CycleTextSize(int direction)
    {
        var sizes = Enum.GetValues<TextSizeOption>();
        var currentIndex = Array.IndexOf(sizes, TextSize);
        var nextIndex = (currentIndex + direction + sizes.Length) % sizes.Length;
        TextSize = sizes[nextIndex];
        return TextSize;
    }

    public DifficultyLevel CycleDifficulty(int direction)
    {
        var levels = Enum.GetValues<DifficultyLevel>();
        var currentIndex = Array.IndexOf(levels, Difficulty);
        var nextIndex = (currentIndex + direction + levels.Length) % levels.Length;
        Difficulty = levels[nextIndex];
        return Difficulty;
    }

    public string GetThemeName()
    {
        return ConsoleTheme switch
        {
            ConsoleThemeOption.DarkGreenTerminal => "Dark Green Terminal",
            ConsoleThemeOption.ClassicMonochrome => "Classic Monochrome",
            ConsoleThemeOption.HighContrast => "High Contrast",
            _ => "Dark Green Terminal"
        };
    }

    public string GetDifficultyName()
    {
        return Difficulty switch
        {
            DifficultyLevel.Easy => "Easy",
            DifficultyLevel.Normal => "Normal",
            DifficultyLevel.Hard => "Hard",
            _ => "Normal"
        };
    }

    public string GetTextSizeName()
    {
        return TextSize switch
        {
            TextSizeOption.Small => "Small",
            TextSizeOption.Medium => "Medium",
            TextSizeOption.Large => "Large",
            _ => "Medium"
        };
    }
}
