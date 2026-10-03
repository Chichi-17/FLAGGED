namespace FLAGGED;

public class HowToPlay
{
    private readonly GameSettings _settings;
    private int _scrollOffset;

    public HowToPlay(GameSettings settings)
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
                    _scrollOffset = Math.Max(0, _scrollOffset - 1);
                    break;
                case ConsoleKey.DownArrow:
                    _scrollOffset = Math.Max(0, _scrollOffset + 1);
                    break;
                case ConsoleKey.PageUp:
                    _scrollOffset = Math.Max(0, _scrollOffset - 4);
                    break;
                case ConsoleKey.PageDown:
                    _scrollOffset = Math.Max(0, _scrollOffset + 4);
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
        ConsoleUI.DrawHeader("FLAGGED!", "HOW TO PLAY");

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - 76) / 2), 5);
        Console.Write("Be a digital content moderator. Verify the truth. Keep your family alive.");

        var sections = new[]
        {
            new Section("SECTION 1: THE BASICS", new[]
            {
                "You are a digital content moderator.",
                "You will receive news articles to review.",
                "Your job is to fact-check each article by comparing information with the database.",
                "Your goal is to identify credible news and misinformation."
            }),
            new Section("SECTION 2: MAKE A DECISION", new[]
            {
                "[1] ACCEPT",
                "Use when the article is credible and its claims are supported by reliable evidence.",
                "[2] REJECT",
                "Use when the article contains false or misleading information.",
                "[3] CHECK DATABASE",
                "Investigate the article using the fact-checking database before making a decision."
            }),
            new Section("SECTION 3: EARN MONEY", new[]
            {
                "Correct decisions earn $10.",
                "Incorrect decisions deduct $5.",
                "Complete each shift within 3 minutes.",
                "Your earnings help pay for your family's basic needs."
            }),
            new Section("SECTION 4: TAKE CARE OF YOUR FAMILY", new[]
            {
                "You must provide rent, utilities, food, and medicine.",
                "Your mother and dog depend on your earnings.",
                "Neglecting food and medicine can cause health problems.",
                "Manage your money carefully to survive all five days."
            }),
            new Section("SECTION 5: WIN THE GAME", new[]
            {
                "Complete all five days.",
                "Make accurate fact-checking decisions.",
                "Earn enough money to pay mandatory expenses.",
                "Keep your mother and dog alive."
            })
        };

        const int panelWidth = 96;
        var panelLeft = Math.Max(2, (Console.WindowWidth - panelWidth) / 2);
        var y = 7;
        var panelHeight = 9;

        for (var sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            var currentSection = sections[sectionIndex];
            var drawY = y + sectionIndex * panelHeight - _scrollOffset * 2;
            if (drawY < 7 || drawY > Console.WindowHeight - 10)
            {
                continue;
            }

            ConsoleUI.DrawPanel(panelLeft, drawY, panelWidth, panelHeight, currentSection.Title, ConsoleColor.DarkGreen);

            var row = drawY + 2;
            foreach (var line in currentSection.Content)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.SetCursorPosition(panelLeft + 3, row);
                Console.Write(line.Length > panelWidth - 8 ? line.Substring(0, panelWidth - 10) : line);
                row++;
                if (row >= drawY + panelHeight - 1)
                {
                    break;
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.SetCursorPosition(Math.Max(0, (Console.WindowWidth - 31) / 2), Console.WindowHeight - 6);
        Console.Write("THINK CRITICALLY. VERIFY ALWAYS.");

        ConsoleUI.DrawButton(Console.WindowHeight - 4, "BACK TO MAIN MENU", true, ConsoleColor.DarkGreen);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.SetCursorPosition(2, Console.WindowHeight - 2);
        Console.Write("Scroll with ↑ ↓ or PgUp / PgDn • Esc to return");
    }

    private sealed class Section
    {
        public Section(string title, string[] content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; }
        public string[] Content { get; }
    }
}
