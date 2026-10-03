namespace FLAGGED;

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed class MainForm : Form
{
    private readonly Label _titleLabel = new();
    private readonly Panel _menuPanel = new();
    private readonly Button _startButton = new();
    private readonly Button _howToPlayButton = new();
    private readonly Button _settingsButton = new();
    private readonly Button _quitButton = new();
    private readonly GameSettings _settings = new();

    public MainForm()
    {
        InitializeComponent();
        ApplyBackground();
        PositionUi();
    }

    private void InitializeComponent()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

        Text = "FLAGGED!";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 720);
        MinimumSize = new Size(900, 700);
        BackColor = Color.FromArgb(8, 18, 18);
        BackgroundImageLayout = ImageLayout.Stretch;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        WindowState = FormWindowState.Normal;
        KeyPreview = true;

        _titleLabel.Text = "FLAGGED!";
        _titleLabel.Font = UiFonts.GameTitle(72f, FontStyle.Bold);
        _titleLabel.ForeColor = Color.FromArgb(146, 236, 220);
        _titleLabel.BackColor = Color.Transparent;
        _titleLabel.TextAlign = ContentAlignment.MiddleCenter;
        _titleLabel.AutoSize = false;
        _titleLabel.Size = new Size(860, 150);
        _titleLabel.Paint += TitleLabel_Paint;

        _menuPanel.BackColor = Color.FromArgb(12, 24, 25);
        _menuPanel.BorderStyle = BorderStyle.None;
        _menuPanel.Padding = new Padding(18, 14, 18, 16);
        _menuPanel.Size = new Size(360, 300);

        CreateMenuButton(_startButton, "START GAME", Color.FromArgb(30, 76, 66));
        CreateMenuButton(_howToPlayButton, "HOW TO PLAY", Color.FromArgb(30, 76, 66));
        CreateMenuButton(_settingsButton, "SETTINGS", Color.FromArgb(30, 76, 66));
        CreateMenuButton(_quitButton, "QUIT", Color.FromArgb(120, 25, 30));

        _startButton.Click += (_, _) => StartGame();
        _howToPlayButton.Click += (_, _) => OpenHowToPlay();
        _settingsButton.Click += (_, _) => OpenSettings();
        _quitButton.Click += (_, _) => ConfirmQuit();

        _menuPanel.Controls.Add(_startButton);
        _menuPanel.Controls.Add(_howToPlayButton);
        _menuPanel.Controls.Add(_settingsButton);
        _menuPanel.Controls.Add(_quitButton);

        _startButton.Location = new Point(18, 16);
        _howToPlayButton.Location = new Point(18, 80);
        _settingsButton.Location = new Point(18, 144);
        _quitButton.Location = new Point(18, 208);

        Controls.Add(_titleLabel);
        Controls.Add(_menuPanel);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                ConfirmQuit();
                e.Handled = true;
            }
        };

        Resize += (_, _) => PositionUi();
    }

    private void ApplyBackground()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var candidateDirectories = new[]
        {
            Path.Combine(baseDirectory, "assets"),
            baseDirectory,
            Path.Combine(baseDirectory, "bin", "Debug", "net10.0-windows"),
            Path.Combine(Environment.CurrentDirectory, "assets")
        };

        var candidateFiles = new[]
        {
            "Menu.png",
            "Menu.jpg",
            "Menu.jpeg",
            "Main_menu.png",
            "Main menu.png",
            "Main-menu.png",
            "Main menu.jpg",
            "Main_menu.jpg"
        };

        foreach (var directory in candidateDirectories.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var fileName in candidateFiles)
            {
                var fullPath = Path.Combine(directory, fileName);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                try
                {
                    using var original = Image.FromFile(fullPath);
                    BackgroundImage = new Bitmap(original);
                    BackgroundImageLayout = ImageLayout.Stretch;
                    return;
                }
                catch
                {
                    // Fall back to a dark background if the asset cannot be decoded.
                }
            }
        }

        BackgroundImage = null;
        BackColor = Color.FromArgb(8, 18, 18);
    }

    private void CreateMenuButton(Button button, string text, Color baseColor)
    {
        button.Text = text;
        button.Width = 320;
        button.Height = 52;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.ForeColor = Color.White;
        button.BackColor = baseColor;
        button.Font = UiFonts.MenuButton(15.5f, FontStyle.Bold);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.UseMnemonic = false;
        button.AutoEllipsis = false;
        button.Margin = new Padding(0, 0, 0, 0);
        button.Padding = new Padding(0);
        button.Cursor = Cursors.Hand;
        button.Visible = true;

        button.MouseEnter += (_, _) => button.BackColor = ControlPaint.Light(baseColor, 0.08f);
        button.MouseLeave += (_, _) => button.BackColor = baseColor;
        button.MouseDown += (_, _) => button.BackColor = ControlPaint.Dark(baseColor, 0.18f);
        button.MouseUp += (_, _) => button.BackColor = ControlPaint.Light(baseColor, 0.08f);
    }

    private void PositionUi()
    {
        _titleLabel.Location = new Point((ClientSize.Width - _titleLabel.Width) / 2, 30);

        var menuLeft = (ClientSize.Width - _menuPanel.Width) / 2;
        var menuTop = (ClientSize.Height - _menuPanel.Height) / 2 + 70;
        _menuPanel.Location = new Point(menuLeft, menuTop);
    }

    private void TitleLabel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Label label)
        {
            return;
        }

        var text = label.Text;
        var shadowColor = Color.FromArgb(4, 28, 24);
        var textColor = label.ForeColor;

        using var shadowBrush = new SolidBrush(shadowColor);
        using var textBrush = new SolidBrush(textColor);

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (var offset = 1; offset <= 4; offset++)
        {
            e.Graphics.DrawString(text, label.Font, shadowBrush, new RectangleF(0 + offset, 0 + offset, label.Width, label.Height), format);
        }

        e.Graphics.DrawString(text, label.Font, textBrush, new RectangleF(0, 0, label.Width, label.Height), format);
    }

    private void StartGame()
    {
        MessageBox.Show("Gameplay screen coming soon.", "FLAGGED!", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenHowToPlay()
    {
        using var form = new HowToPlayForm(_settings);
        form.ShowDialog(this);
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        form.ShowDialog(this);
    }

    private void ConfirmQuit()
    {
        var result = MessageBox.Show("Are you sure you want to quit?", "Quit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result == DialogResult.Yes)
        {
            Application.Exit();
        }
    }
}
