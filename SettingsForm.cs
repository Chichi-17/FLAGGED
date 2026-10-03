namespace FLAGGED;

using System.Drawing;
using System.Windows.Forms;

public class SettingsForm : Form
{
    private readonly GameSettings _settings;

    public SettingsForm(GameSettings settings)
    {
        _settings = settings;
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        Width = 900;
        Height = 620;
        MinimumSize = new Size(760, 540);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BackColor = Color.FromArgb(8, 18, 18);
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = false;

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(8, 18, 18),
            Padding = new Padding(20, 16, 20, 18)
        };

        var title = new Label
        {
            Text = "SETTINGS",
            Font = UiFonts.Heading(34f),
            ForeColor = Color.FromArgb(150, 220, 170),
            Dock = DockStyle.Top,
            Height = 70,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var subtitle = new Label
        {
            Text = "Customize your game experience.",
            Font = UiFonts.Body(12f),
            ForeColor = Color.FromArgb(220, 220, 220),
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var settingsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = Color.FromArgb(10, 22, 22),
            ColumnCount = 2,
            RowCount = 9,
            AutoSize = false
        };

        settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));

        var musicTrack = new TrackBar { Minimum = 0, Maximum = 100, Value = _settings.MusicVolume, TickStyle = TickStyle.None, Width = 220, Height = 30, LargeChange = 10 };
        var soundTrack = new TrackBar { Minimum = 0, Maximum = 100, Value = _settings.SoundEffectsVolume, TickStyle = TickStyle.None, Width = 220, Height = 30, LargeChange = 10 };
        var muteCheck = new CheckBox { Checked = _settings.MuteAllAudio, Text = "Mute All Audio", ForeColor = Color.White, AutoSize = true, Font = UiFonts.Body(12f) };
        var themeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = UiFonts.Body(12f), Items = { "Dark Green Terminal", "Classic Monochrome", "High Contrast" }, SelectedIndex = (int)_settings.ConsoleTheme };
        var textSizeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = UiFonts.Body(12f), Items = { "Small", "Medium", "Large" }, SelectedIndex = (int)_settings.TextSize };
        var clearScreenCheck = new CheckBox { Checked = _settings.ClearScreenBeforeRedrawing, Text = "Clear Screen Before Redrawing", ForeColor = Color.White, AutoSize = true, Font = UiFonts.Body(12f) };
        var hintsCheck = new CheckBox { Checked = _settings.ShowHints, Text = "Show Hints", ForeColor = Color.White, AutoSize = true, Font = UiFonts.Body(12f) };
        var confirmCheck = new CheckBox { Checked = _settings.ConfirmBeforeSpending, Text = "Confirm Before Spending", ForeColor = Color.White, AutoSize = true, Font = UiFonts.Body(12f) };
        var difficultyBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = UiFonts.Body(12f), Items = { "Easy", "Normal", "Hard" }, SelectedIndex = (int)_settings.Difficulty };

        AddSetting(settingsPanel, 0, "Music Volume", musicTrack);
        AddSetting(settingsPanel, 1, "Sound Effects Volume", soundTrack);
        AddSetting(settingsPanel, 2, "Mute All Audio", muteCheck);
        AddSetting(settingsPanel, 3, "Console Theme", themeBox);
        AddSetting(settingsPanel, 4, "Text Size", textSizeBox);
        AddSetting(settingsPanel, 5, "Clear Screen Before Redrawing", clearScreenCheck);
        AddSetting(settingsPanel, 6, "Show Hints", hintsCheck);
        AddSetting(settingsPanel, 7, "Confirm Before Spending", confirmCheck);

        var difficultyLabel = new Label { Text = "Difficulty", ForeColor = Color.White, Font = UiFonts.Body(12f), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 0, 8) };
        settingsPanel.Controls.Add(difficultyLabel, 0, 8);
        settingsPanel.Controls.Add(difficultyBox, 1, 8);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(12, 21, 21),
            Padding = new Padding(12, 12, 12, 12)
        };

        var saveButton = new AnimatedButton
        {
            Text = "SAVE SETTINGS",
            Width = 170,
            Height = 46,
            Font = UiFonts.MenuButton(15.5f),
            BackColor = Color.FromArgb(29, 82, 69)
        };

        var closeButton = new AnimatedButton
        {
            Text = "CLOSE",
            Width = 120,
            Height = 46,
            Font = UiFonts.MenuButton(15.5f),
            BackColor = Color.FromArgb(74, 78, 82)
        };

        saveButton.Click += (_, _) =>
        {
            _settings.MusicVolume = musicTrack.Value;
            _settings.SoundEffectsVolume = soundTrack.Value;
            _settings.MuteAllAudio = muteCheck.Checked;
            _settings.ConsoleTheme = (ConsoleThemeOption)themeBox.SelectedIndex;
            _settings.TextSize = (TextSizeOption)textSizeBox.SelectedIndex;
            _settings.ClearScreenBeforeRedrawing = clearScreenCheck.Checked;
            _settings.ShowHints = hintsCheck.Checked;
            _settings.ConfirmBeforeSpending = confirmCheck.Checked;
            _settings.Difficulty = (DifficultyLevel)difficultyBox.SelectedIndex;

            MessageBox.Show("Settings saved!", "FLAGGED!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        closeButton.Click += (_, _) => Close();

        footer.Controls.Add(closeButton);
        footer.Controls.Add(saveButton);

        root.Controls.Add(title);
        root.Controls.Add(subtitle);
        root.Controls.Add(settingsPanel);
        root.Controls.Add(footer);
        Controls.Add(root);
    }

    private static void AddSetting(TableLayoutPanel panel, int row, string labelText, Control valueControl)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = Color.White,
            Font = UiFonts.Body(12f),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 0, 8),
            BackColor = Color.Transparent
        };

        valueControl.Anchor = AnchorStyles.Left;
        valueControl.Margin = new Padding(0, 6, 0, 6);
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(valueControl, 1, row);
    }
}
