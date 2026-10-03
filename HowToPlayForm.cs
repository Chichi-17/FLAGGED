namespace FLAGGED;

using System.Drawing;
using System.Windows.Forms;

public class HowToPlayForm : Form
{
    public HowToPlayForm(GameSettings settings)
    {
        Text = "How to Play";
        StartPosition = FormStartPosition.CenterParent;
        Width = 920;
        Height = 680;
        MinimumSize = new Size(860, 620);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        BackColor = Color.FromArgb(8, 18, 18);
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = false;

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(8, 18, 18),
            Padding = new Padding(24, 16, 24, 18)
        };

        var title = new Label
        {
            Text = "HOW TO PLAY",
            Font = UiFonts.Heading(36f),
            ForeColor = Color.FromArgb(150, 220, 170),
            Dock = DockStyle.Top,
            Height = 70,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var subtitle = new Label
        {
            Text = "Be a digital content moderator. Verify the truth. Keep your family alive.",
            Font = UiFonts.Body(12f),
            ForeColor = Color.FromArgb(220, 220, 220),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 22, 22),
            Padding = new Padding(18)
        };

        var textBox = new RichTextBox
        {
            ReadOnly = true,
            BackColor = Color.FromArgb(10, 22, 22),
            ForeColor = Color.FromArgb(235, 235, 220),
            Font = UiFonts.Body(12.5f),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0),
            WordWrap = true,
            Text = "OBJECTIVE\n\n" +
                   "You are a digital content moderator. Review the latest news and determine whether the information is truthful or misleading.\n\n" +
                   "CONTROLS\n\n" +
                   "• Accept: the article is credible and supported by reliable evidence.\n" +
                   "• Reject: the article contains false or misleading information.\n" +
                   "• Check Database: investigate the sources before deciding.\n\n" +
                   "GAME RULES\n\n" +
                   "• Correct choices reward money and trust.\n" +
                   "• Wrong choices cost money and hurt your family's stability.\n" +
                   "• Complete each shift before time runs out.\n" +
                   "• Manage rent, food, medicine, and safety expenses carefully.\n\n" +
                   "WIN CONDITION\n\n" +
                   "Complete all five days, make accurate decisions, and keep your family safe."
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(12, 21, 21),
            Padding = new Padding(12, 12, 12, 12)
        };

        var backButton = new AnimatedButton
        {
            Text = "BACK TO MAIN MENU",
            Width = 220,
            Height = 46,
            Font = UiFonts.MenuButton(15.5f),
            BackColor = Color.FromArgb(29, 82, 69),
            ForeColor = Color.White
        };

        backButton.Click += (_, _) => Close();

        footer.Controls.Add(backButton);
        content.Controls.Add(textBox);
        root.Controls.Add(title);
        root.Controls.Add(subtitle);
        root.Controls.Add(content);
        root.Controls.Add(footer);
        Controls.Add(root);
    }
}
