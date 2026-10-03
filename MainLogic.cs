using System;
using System.Collections.Generic;
using System.Linq;

namespace FLAGGED
{
    public enum Judgment { Accept, Reject }

    public class Article
    {
        public string Headline { get; set; } = "";
        public string Author { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsFake { get; set; }
        public int Difficulty { get; set; } = 1;
        public string Explanation { get; set; } = "";
    }

    public class DatabaseRecord
    {
        public string SourceType { get; set; } = "";
        public string Credibility { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public class JudgmentResult
    {
        public bool IsCorrect { get; set; }
        public int PointsDelta { get; set; } // eto na gamitin sa point update
        public string Message { get; set; } = "";
        public bool ShiftEnded { get; set; }
    }

    public class DatabaseCheckResult
    {
        public bool Found { get; set; }
        public string Report { get; set; } = "";
    }

    // core logic
    public class CoreGameLogic
    {
        private readonly Random _rng = new Random();
        private readonly List<Article> _articlePool;
        private readonly Dictionary<string, DatabaseRecord> _database;
        private readonly Queue<Article> _shiftQueue = new Queue<Article>();
        
        private Article _currentArticle;

        public CoreGameLogic()
        {
            _articlePool = BuildArticlePool();
            _database = BuildDatabase();
        }

        public void StartShift(int day)
        {
            _shiftQueue.Clear();
            LoadArticlesForDay(day);
            NextArticle();
        }

        public Article GetCurrentArticle() => _currentArticle;

        public DatabaseCheckResult CheckDatabase()
        {
            if (_currentArticle == null)
                return new DatabaseCheckResult { Found = false, Report = "No article loaded." };

            var lines = new List<string>();
            bool found = false;

            if (_database.TryGetValue("author:" + _currentArticle.Author, out var author))
            {
                found = true;
                lines.Add($"AUTHOR [{author.Credibility}] {_currentArticle.Author}: {author.Notes}");
            }
            if (_database.TryGetValue("domain:" + _currentArticle.Domain, out var domain))
            {
                found = true;
                lines.Add($"DOMAIN [{domain.Credibility}] {_currentArticle.Domain}: {domain.Notes}");
            }

            if (!found)
                lines.Add("No matching database record found. Treat with caution.");

            return new DatabaseCheckResult
            {
                Found = found,
                Report = string.Join(Environment.NewLine, lines)
            };
        }

        public JudgmentResult SubmitJudgment(Judgment choice)
        {
            if (_currentArticle == null)
                return new JudgmentResult { IsCorrect = false, Message = "No article to judge." };

            bool isCorrect = (choice == Judgment.Accept && !_currentArticle.IsFake) ||
                             (choice == Judgment.Reject && _currentArticle.IsFake);

            // basahin nalang to tas iapply sa points
            int pointsDelta = isCorrect ? 10 : -5; 

            string message = isCorrect 
                ? $"Correct! {_currentArticle.Explanation}" 
                : $"Incorrect. {_currentArticle.Explanation}";

            NextArticle();
            bool shiftEnded = (_currentArticle == null);

            return new JudgmentResult
            {
                IsCorrect = isCorrect,
                PointsDelta = pointsDelta,
                Message = message,
                ShiftEnded = shiftEnded
            };
        }

        private void NextArticle()
        {
            if (_shiftQueue.Count > 0)
                _currentArticle = _shiftQueue.Dequeue();
            else
                _currentArticle = null; // Signals shift is over
        }

        private void LoadArticlesForDay(int day)
        {
            int count = Math.Min(3 + day, 8);
            int maxDifficulty = Math.Min(3, day);

            var picks = _articlePool
                .Where(a => a.Difficulty <= maxDifficulty)
                .OrderBy(a => _rng.Next())
                .Take(count)
                .ToList();

            foreach (var a in picks) _shiftQueue.Enqueue(a);
        }

                private List<Article> BuildArticlePool() => new List<Article>
        {
            // easy
            new Article { Headline="DOH advises public to practice 4S strategy amid rising Dengue cases", Author="DOH Press Office", Domain="doh.gov.ph", Body="The Department of Health urges the public to eliminate mosquito breeding sites to prevent the spread of Dengue.", IsFake=false, Difficulty=1, Explanation="Official government health department providing standard medical advisories." },
            new Article { Headline="Boiling guava leaves cures Dengue instantly, hospitals are lying to you!", Author="Albularyo Juan", Domain="herbalsecrets.ph", Body="A local healer claims a simple guava leaf boil can cure Dengue in hours, advising people to avoid hospitals.", IsFake=true, Difficulty=1, Explanation="Dangerous medical misinformation from an unverified source; discourages seeking real medical help." },
            new Article { Headline="LTFRB sets new deadline for transport cooperative consolidation", Author="LTFRB Public Affairs", Domain="ltfrb.gov.ph", Body="The Land Transportation Franchising and Regulatory Board announces the final extension for PUV consolidation.", IsFake=false, Difficulty=1, Explanation="Official transport regulatory agency announcing policy updates." },
            new Article { Headline="All traditional jeepneys to be crushed by government next week", Author="Viral Manila", Domain="truthviral.ph", Body="Social media posts claim the military will crush all unconsolidated jeepneys by Monday.", IsFake=true, Difficulty=1, Explanation="Sensationalized and exaggerated claim not supported by actual government policy." },
            
            // medium
            new Article { Headline="PCG successfully resupplies BRP Sierra Madre at Ayungin Shoal", Author="PCG Spokesperson", Domain="coastguard.gov.ph", Body="The Philippine Coast Guard confirms the successful rotation and reprovisioning mission in the West Philippine Sea.", IsFake=false, Difficulty=2, Explanation="Verified official report from the Philippine Coast Guard regarding maritime operations." },
            new Article { Headline="Secret treaty signed: Philippines to surrender Palawan to foreign powers", Author="Anonymous Insider", Domain="pinasconspiracy.net", Body="An unnamed government insider claims a secret document gives away sovereign territory.", IsFake=true, Difficulty=2, Explanation="Baseless conspiracy theory with anonymous sourcing and no credible evidence." },
            new Article { Headline="PhilHealth implements new premium contribution rates for 2024", Author="PhilHealth PR", Domain="philhealth.gov.ph", Body="The state health insurer announces standard rate adjustments in compliance with the UHC Act.", IsFake=false, Difficulty=2, Explanation="Official announcement from the state health insurer regarding standard rate adjustments." },
            new Article { Headline="PhilHealth is bankrupt, members will no longer get hospital discounts", Author="Health Watch PH", Domain="fakehealthalerts.com", Body="Viral posts warn that PhilHealth has run out of funds and will deny all hospital claims starting tomorrow.", IsFake=true, Difficulty=2, Explanation="False alarm designed to cause panic; PhilHealth benefits remain active and funded." },
            new Article { Headline="PAGASA tracks Typhoon Enteng, raises Signal No. 2 in Northern Luzon", Author="PAGASA", Domain="bagong.pagasa.gov.ph", Body="The national weather agency advises residents in Cagayan and Isabela to prepare for strong winds and heavy rain.", IsFake=false, Difficulty=2, Explanation="Standard, verifiable weather bulletin from the national meteorological agency." },
            
            // hard
            new Article { Headline="PAGASA warns of super typhoon hitting Manila tomorrow, prepare now!", Author="PAGASA", Domain="pagasa-updates.com", Body="A viral screenshot shows a PAGASA warning for a Category 5 super typhoon directly hitting Metro Manila.", IsFake=true, Difficulty=3, Explanation="The author name looks official, but the domain is an unofficial imitation site spreading false alarms." },
            new Article { Headline="DepEd announces removal of Mother Tongue as separate subject in early grades", Author="DepEd Secretary", Domain="deped.gov.ph", Body="The Department of Education implements recent legislative changes regarding the medium of instruction in basic education.", IsFake=false, Difficulty=3, Explanation="Official policy update from the Department of Education based on recent legislative changes." },
            new Article { Headline="DepEd secretly banning Tagalog in all schools to force English-only policy", Author="EduLeaks", Domain="educorrupt.net", Body="Leaks suggest the government is erasing Filipino culture by banning the national language in classrooms.", IsFake=true, Difficulty=3, Explanation="Misrepresentation of the Mother Tongue policy to incite outrage; Filipino remains a core subject." },
            new Article { Headline="PBBM orders total ban on all POGOs, effective immediately", Author="Presidential Communications", Domain="pco.gov.ph", Body="The President directs PAGCOR to wind down Philippine Offshore Gaming Operators due to social and criminal costs.", IsFake=false, Difficulty=3, Explanation="Major official policy announcement directly from the Office of the President." },
            new Article { Headline="POGOs secretly buying all land in BGC, locals to be evicted next month", Author="Buzz Clicker", Domain="shockfeed.ph", Body="Insiders claim foreign gaming syndicates are forcing residents out of Bonifacio Global City.", IsFake=true, Difficulty=3, Explanation="Sensational clickbait with no basis in property laws or recent POGO shutdowns." },
            new Article { Headline="DA reports stabilization of rice prices following tariff reductions", Author="DA News Desk", Domain="da.gov.ph", Body="The Department of Agriculture monitors local markets to ensure imported rice supply reaches consumers.", IsFake=false, Difficulty=3, Explanation="Economic update from the Department of Agriculture regarding food security measures." },
            new Article { Headline="Golden Rice is made of plastic and causes cancer, whistleblower claims", Author="Anonymous Whistleblower", Domain="pinasconspiracy.net", Body="A viral video claims biofortified rice distributed in provinces is synthetic and toxic.", IsFake=true, Difficulty=3, Explanation="Dangerous agricultural misinformation; Golden Rice is a biofortified crop approved by the DA and FDA." }
        };

        private Dictionary<string, DatabaseRecord> BuildDatabase() =>
            new Dictionary<string, DatabaseRecord>(StringComparer.OrdinalIgnoreCase)
        {
            // authors
            ["author:DOH Press Office"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Official health department communications." },
            ["author:Albularyo Juan"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Unverified traditional healer with no medical license." },
            ["author:LTFRB Public Affairs"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Official transport regulatory agency." },
            ["author:Viral Manila"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Clickbait page known for sensationalizing local news." },
            ["author:PCG Spokesperson"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Official maritime and coast guard statements." },
            ["author:Anonymous Insider"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Unverified identity, frequently posts political conspiracies." },
            ["author:PhilHealth PR"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Official state health insurer announcements." },
            ["author:Health Watch PH"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="History of spreading medical panic and fake alerts." },
            ["author:PAGASA"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="National meteorological agency." },
            ["author:Freedom Truth"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Pseudoscience and conspiracy blogger." },
            ["author:DepEd Secretary"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Head of the Department of Education." },
            ["author:EduLeaks"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Known for misrepresenting educational policies for outrage." },
            ["author:Presidential Communications"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Official statements from the Office of the President." },
            ["author:Buzz Clicker"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Sensationalist writer with exaggerated stories." },
            ["author:DA News Desk"] = new DatabaseRecord { SourceType="Author", Credibility="High", Notes="Department of Agriculture official updates." },
            ["author:Anonymous Whistleblower"] = new DatabaseRecord { SourceType="Author", Credibility="Low", Notes="Unverified claims, commonly linked to agricultural hoaxes." },

            // domains
            ["domain:doh.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Official Department of Health website." },
            ["domain:herbalsecrets.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Promotes unverified alternative medicine." },
            ["domain:ltfrb.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Land Transportation Franchising and Regulatory Board." },
            ["domain:truthviral.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Frequently publishes exaggerated local rumors." },
            ["domain:coastguard.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Philippine Coast Guard official portal." },
            ["domain:pinasconspiracy.net"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Hub for political and national security conspiracies." },
            ["domain:philhealth.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Philippine Health Insurance Corporation." },
            ["domain:fakehealthalerts.com"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Known for generating medical panic." },
            ["domain:bagong.pagasa.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Official new portal of the national weather agency." },
            ["domain:pinaslibre.xyz"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Fringe blog promoting pseudoscience." },
            ["domain:deped.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Department of Education official website." },
            ["domain:educorrupt.net"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Biased site known for twisting educational news." },
            ["domain:pco.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Presidential Communications Office." },
            ["domain:shockfeed.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="Low", Notes="Clickbait and sensationalist content." },
            ["domain:da.gov.ph"] = new DatabaseRecord { SourceType="Domain", Credibility="High", Notes="Department of Agriculture." },
            ["domain:pagasa-updates.com"] = new DatabaseRecord { SourceType="Domain", Credibility="Unknown", Notes="Unofficial imitation site. Not affiliated with the real PAGASA." }
        };
    }
}