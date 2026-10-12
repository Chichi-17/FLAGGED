using System;
using System.Collections.Generic;
using System.Linq;

namespace FLAGGED
{
    public enum Judgment { Accept, Reject }

    public class ArticleField
    {
        public string Name { get; set; } = "";
        public string ArticleValue { get; set; } = "";

        // internal only, never show
        public string OfficialValue { get; set; } = "";

        // terms that player would search for
        public List<string> SearchTerms { get; set; } = new List<string>();

        public bool HasDiscrepancy => // INTERNAL
            !string.Equals(ArticleValue, OfficialValue, StringComparison.OrdinalIgnoreCase);
    }

    public class Article
    {
        public string Headline { get; set; } = "";
        public string Author { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Body { get; set; } = "";
        public int Difficulty { get; set; } = 1;
        public List<ArticleField> Fields { get; set; } = new List<ArticleField>();

        // internal only, never show
        public string Explanation { get; set; } = "";
        public bool IsFake => Fields.Any(f => f.HasDiscrepancy);
    }

    public class DatabaseRecord
    {
        public string Category { get; set; } = "";     // author | domain | statement
        public string Name { get; set; } = "";         // what the player looks up
        public string Credibility { get; set; } = "";  // High/Low/Unknown, or issuing office
        public string Notes { get; set; } = "";
    }

    public class DatabaseSearchResult
    {
        public string Query { get; set; } = "";
        public bool Found { get; set; }
        public string Message { get; set; } = "";
        public List<DatabaseRecord> Records { get; set; } = new List<DatabaseRecord>();
    }

    // never show these before SubmitJudgment()
    public class ComparisonRow
    {
        public string Field { get; set; } = "";
        public string ArticleSays { get; set; } = "";
        public string DatabaseSays { get; set; } = "";
        public bool Mismatch { get; set; }
    }

    public class JudgmentResult
    {
        public bool IsCorrect { get; set; }
        public int PointsDelta { get; set; }  // Derek: +10 correct, -5 wrong
        public string Message { get; set; } = "";
        public bool ShiftEnded { get; set; }
        public bool WasFake { get; set; }     // for coloring the reveal screen
        public List<ComparisonRow> Reveal { get; set; } = new List<ComparisonRow>();
    }

    public class CoreGameLogic
    {
        private readonly Random _rng = new Random();
        private readonly List<Article> _articlePool;
        private readonly List<DatabaseRecord> _database;
        private readonly Queue<Article> _shiftQueue = new Queue<Article>();
        private readonly HashSet<string> _usedHeadlines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Article _currentArticle;

        public CoreGameLogic()
        {
            _articlePool = BuildArticlePool();
            _database = BuildDatabase();
        }

        public void StartShift(int day)
        {
            if (day <= 1) _usedHeadlines.Clear(); // fresh playthrough
            _shiftQueue.Clear();
            LoadArticlesForDay(day);
            NextArticle();
        }

        public Article GetCurrentArticle() => _currentArticle;

        private static bool ContainsIgnoreCase(string text, string query) =>
            text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        public DatabaseSearchResult SearchDatabase(string query)
        {
            query = (query ?? "").Trim();
            var result = new DatabaseSearchResult { Query = query };

            if (query.Length < 2)
            {
                result.Message = "Enter a search term (author, domain, or topic).";
                return result;
            }

            result.Records = _database
                .Where(r => ContainsIgnoreCase(r.Name, query) || ContainsIgnoreCase(r.Notes, query))
                .ToList();

            result.Found = result.Records.Count > 0;
            result.Message = result.Found
                ? $"{result.Records.Count} record(s) found."
                : "NO RECORD FOUND. An unregistered source is itself a red flag — but weigh the whole article.";
            return result;
        }

        public DatabaseSearchResult InvestigateField(int fieldIndex)
        {
            var field = _currentArticle?.Fields.ElementAtOrDefault(fieldIndex);
            if (field == null)
                return new DatabaseSearchResult { Message = "No such field on this article." };

            var terms = field.SearchTerms.Count > 0
                ? field.SearchTerms
                : new List<string> { field.ArticleValue };

            var records = new List<DatabaseRecord>();
            foreach (var term in terms)
                foreach (var hit in SearchDatabase(term).Records)
                    if (!records.Contains(hit)) records.Add(hit);

            return new DatabaseSearchResult
            {
                Query = field.Name,
                Found = records.Count > 0,
                Records = records,
                Message = records.Count > 0
                    ? $"Records related to: {field.Name}"
                    : "No records matched this field. No record found is itself a clue."
            };
        }

        // optional rulebook-style browsing
        public List<DatabaseRecord> BrowseDatabase(string category = null) =>
            string.IsNullOrEmpty(category)
                ? _database
                : _database.Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();


        public JudgmentResult SubmitJudgment(Judgment choice)
        {
            if (_currentArticle == null)
                return new JudgmentResult { IsCorrect = false, Message = "No article to judge." };

            var article = _currentArticle;

            bool isCorrect = (choice == Judgment.Accept && !article.IsFake) ||
                             (choice == Judgment.Reject && article.IsFake);

            var reveal = article.Fields
                .Select(f => new ComparisonRow
                {
                    Field = f.Name,
                    ArticleSays = f.ArticleValue,
                    DatabaseSays = f.OfficialValue,
                    Mismatch = f.HasDiscrepancy
                })
                .ToList();

            var mismatches = reveal.Where(r => r.Mismatch).Select(r => r.Field).ToList();
            string detail = mismatches.Count == 0
                ? "All details matched the official record."
                : $"Discrepancies found in: {string.Join(", ", mismatches)}.";

            string message = (isCorrect ? "Correct! " : "Incorrect. ") + detail + " " + article.Explanation;

            NextArticle();

            return new JudgmentResult
            {
                IsCorrect = isCorrect,
                PointsDelta = isCorrect ? 10 : -5,
                Message = message,
                ShiftEnded = _currentArticle == null,
                WasFake = article.IsFake,
                Reveal = reveal
            };
        }

        private void NextArticle()
        {
            if (_shiftQueue.Count > 0) _currentArticle = _shiftQueue.Dequeue();
            else _currentArticle = null;
        }

        private void LoadArticlesForDay(int day)
        {
            int count = Math.Min(2 + day, 6);
            int maxDifficulty = Math.Min(3, day);

            var available = _articlePool.Where(a => a.Difficulty <= maxDifficulty).ToList();

            // prefer articles player hasn't seen
            var picks = available
                .Where(a => !_usedHeadlines.Contains(a.Headline))
                .OrderBy(a => _rng.Next())
                .Take(count)
                .ToList();

            // repeat if not enough unseen articles
            if (picks.Count < count)
                picks.AddRange(available
                    .Where(a => !picks.Contains(a))
                    .OrderBy(a => _rng.Next())
                    .Take(count - picks.Count));

            foreach (var a in picks)
            {
                _usedHeadlines.Add(a.Headline);
                _shiftQueue.Enqueue(a);
            }
        }

        // Database, 10 real 9 fakes
        private List<Article> BuildArticlePool() => new List<Article>
        {
            // ---------- MAYON VOLCANO ----------
            new Article {
                Headline="PHIVOLCS raises Mayon to Alert Level 3; six-kilometer Permanent Danger Zone strictly off-limits",
                Author="PHIVOLCS-DOST", Domain="phivolcs.dost.gov.ph",
                Body="State volcanologists detect intensified magmatic unrest, pyroclastic density currents, and sustained rockfall at Mayon Volcano in Albay. Thousands within the permanent danger zone move to evacuation centers as local governments declare states of calamity.",
                Difficulty=1,
                Explanation="Matches the official PHIVOLCS advisory: Alert Level 3, six-kilometer PDZ.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="phivolcs.dost.gov.ph", OfficialValue="phivolcs.dost.gov.ph", SearchTerms=new List<string>{ "phivolcs.dost.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="PHIVOLCS-DOST", OfficialValue="PHIVOLCS-DOST", SearchTerms=new List<string>{ "PHIVOLCS" } },
                    new ArticleField { Name="Alert Status", ArticleValue="Alert Level 3 — intensified magmatic unrest", OfficialValue="Alert Level 3 — intensified magmatic unrest", SearchTerms=new List<string>{ "Mayon", "Alert Level" } },
                    new ArticleField { Name="Exclusion Zone", ArticleValue="Six-kilometer-radius Permanent Danger Zone; entry strictly prohibited", OfficialValue="Six-kilometer-radius Permanent Danger Zone; entry strictly prohibited", SearchTerms=new List<string>{ "Mayon", "Permanent Danger Zone" } }
                }
            },
            new Article {
                Headline="ALERT LEVEL 5 DECLARED OVER MAYON — entire Bicol region ordered to evacuate immediately",
                Author="PHIVOLCS Monitoring Team", Domain="phivolcs-alerts.com",
                Body="A post on a copycat volcanology site claims Mayon has been raised to Alert Level 5 and that every resident of Bicol must evacuate at once ahead of a catastrophic eruption.",
                Difficulty=3,
                Explanation="Imitation-domain trap: inflated alert level and evacuation scope. PHIVOLCS has Mayon at Alert Level 3 with only the six-kilometer PDZ off-limits.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="phivolcs-alerts.com", OfficialValue="phivolcs.dost.gov.ph", SearchTerms=new List<string>{ "phivolcs-alerts.com" } },
                    new ArticleField { Name="Byline", ArticleValue="PHIVOLCS Monitoring Team", OfficialValue="PHIVOLCS-DOST", SearchTerms=new List<string>{ "PHIVOLCS" } },
                    new ArticleField { Name="Alert Status", ArticleValue="Alert Level 5 — hazardous explosive eruption imminent", OfficialValue="Alert Level 3 — intensified magmatic unrest", SearchTerms=new List<string>{ "Mayon", "Alert Level" } },
                    new ArticleField { Name="Evacuation Scope", ArticleValue="Entire Bicol region under mandatory evacuation", OfficialValue="Evacuation limited to residents within the six-kilometer Permanent Danger Zone", SearchTerms=new List<string>{ "Mayon", "Bicol" } }
                }
            },

            // ---------- DICT GROK BLOCK ----------
            new Article {
                Headline="DICT directs ISPs to block xAI's Grok over non-consensual deepfake imagery",
                Author="DICT Public Affairs", Domain="dict.gov.ph",
                Body="The DICT orders internet service providers to block access to xAI's Grok after evidence showed the tool being used to generate non-consensual sexually explicit imagery and deepfakes targeting women and minors.",
                Difficulty=1,
                Explanation="Matches the official DICT order: the block targets xAI's Grok specifically.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="dict.gov.ph", OfficialValue="dict.gov.ph", SearchTerms=new List<string>{ "dict.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DICT Public Affairs", OfficialValue="DICT Public Affairs", SearchTerms=new List<string>{ "DICT Public Affairs" } },
                    new ArticleField { Name="Action", ArticleValue="ISPs directed to block access to xAI's Grok platform", OfficialValue="ISPs directed to block access to xAI's Grok platform", SearchTerms=new List<string>{ "Grok" } },
                    new ArticleField { Name="Reason", ArticleValue="Generation of non-consensual sexually explicit imagery and deepfakes targeting women and minors", OfficialValue="Generation of non-consensual sexually explicit imagery and deepfakes targeting women and minors", SearchTerms=new List<string>{ "Grok", "deepfake" } }
                }
            },
            new Article {
                Headline="AI BANNED! ChatGPT, Gemini, and ALL AI tools blocked nationwide starting Monday!",
                Author="Viral Manila", Domain="truthviral.ph",
                Body="Viral posts claim the DICT will block every AI platform in the country and make simply using AI illegal, with fines and jail time for ordinary users.",
                Difficulty=1,
                Explanation="Overgeneralization: the block applies only to xAI's Grok; there is no nationwide AI ban.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="truthviral.ph", OfficialValue="Not a registered news domain", SearchTerms=new List<string>{ "truthviral.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="Viral Manila", OfficialValue="Unverified page; clickbait content", SearchTerms=new List<string>{ "Viral Manila" } },
                    new ArticleField { Name="Claim", ArticleValue="All AI tools blocked nationwide; AI use criminalized", OfficialValue="DICT: block applies only to xAI's Grok; no nationwide AI ban exists", SearchTerms=new List<string>{ "Grok", "AI" } }
                }
            },

            // ---------- TRILLION PESO MARCH ----------
            new Article {
                Headline="Palace acknowledges public frustration as 'Trillion Peso March' fills Luneta and EDSA Shrine",
                Author="PCO Media Affairs", Domain="pco.gov.ph",
                Body="Officials recognize public anger over anomalous flood control projects after tens of thousands join the 'Baha sa Luneta' and 'Trillion Peso March' protests, affirming commitment to administrative overhauls and cooperation with newly formed independent investigative bodies.",
                Difficulty=2,
                Explanation="Matches the official Palace response to the flood-control protests.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="pco.gov.ph", OfficialValue="pco.gov.ph", SearchTerms=new List<string>{ "pco.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="PCO Media Affairs", OfficialValue="PCO Media Affairs", SearchTerms=new List<string>{ "PCO" } },
                    new ArticleField { Name="Event", ArticleValue="Tens of thousands rally at Luneta and the EDSA Shrine over ghost and substandard flood control projects", OfficialValue="Tens of thousands rally at Luneta and the EDSA Shrine over ghost and substandard flood control projects", SearchTerms=new List<string>{ "Trillion Peso March", "protest" } },
                    new ArticleField { Name="Response", ArticleValue="Commitment to administrative overhauls and cooperation with independent investigative bodies", OfficialValue="Commitment to administrative overhauls and cooperation with independent investigative bodies", SearchTerms=new List<string>{ "Trillion Peso March", "flood control" } }
                }
            },
            new Article {
                Headline="MARTIAL LAW TONIGHT: Military to disperse Trillion Peso March protesters, mass arrests ordered!",
                Author="Anonymous Insider", Domain="pinasconspiracy.net",
                Body="A viral post claims Malacañang has secretly ordered the military to break up the Luneta and EDSA protests with mass arrests before dawn.",
                Difficulty=2,
                Explanation="Fabricated crackdown; the Palace acknowledged the protests and committed to overhauls — no martial law declaration exists.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="pinasconspiracy.net", OfficialValue="Not a registered news domain", SearchTerms=new List<string>{ "pinasconspiracy.net" } },
                    new ArticleField { Name="Byline", ArticleValue="Anonymous Insider", OfficialValue="Unverified identity", SearchTerms=new List<string>{ "Anonymous Insider" } },
                    new ArticleField { Name="Claim", ArticleValue="Martial law declared; military to disperse protesters and conduct mass arrests", OfficialValue="PCO: no martial law or dispersal order; protests acknowledged, right to peaceful assembly respected", SearchTerms=new List<string>{ "Trillion Peso March", "martial law" } }
                }
            },

            // ---------- SANDIGANBAYAN WARRANTS ----------
            new Article {
                Headline="PNP serves Sandiganbayan warrants in flood control plunder probe; Revilla surrenders at Camp Crame",
                Author="PNP Public Information Office", Domain="pnp.gov.ph",
                Body="The PNP confirms receipt of court orders and the surrender of former senator Bong Revilla at Camp Crame, stating all valid warrants will be executed without fear or favor, with due process and custody safety assured.",
                Difficulty=2,
                Explanation="Matches the PNP statement on the flood-control plunder warrants.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="pnp.gov.ph", OfficialValue="pnp.gov.ph", SearchTerms=new List<string>{ "pnp.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="PNP Public Information Office", OfficialValue="PNP Public Information Office", SearchTerms=new List<string>{ "PNP" } },
                    new ArticleField { Name="Action", ArticleValue="Arrest warrants served on lawmakers and public works officials; Revilla surrenders at Camp Crame", OfficialValue="Arrest warrants served on lawmakers and public works officials; Revilla surrenders at Camp Crame", SearchTerms=new List<string>{ "Revilla", "Camp Crame" } },
                    new ArticleField { Name="Policy", ArticleValue="All valid warrants executed without fear or favor, ensuring due process and safe custody", OfficialValue="All valid warrants executed without fear or favor, ensuring due process and safe custody", SearchTerms=new List<string>{ "warrant", "PNP" } }
                }
            },

            // ---------- BINALIW LANDFILL ----------
            new Article {
                Headline="Cebu City declares state of calamity after Binaliw landfill collapse kills 36 workers",
                Author="DENR Public Affairs", Domain="denr.gov.ph",
                Body="A catastrophic slope failure at the Binaliw sanitary landfill in Cebu City buried operational facilities and personnel, leaving 36 workers dead. The city is under a state of calamity as an emergency environmental review of private solid waste concessions begins.",
                Difficulty=2,
                Explanation="Matches the official casualty figure and calamity declaration.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="denr.gov.ph", OfficialValue="denr.gov.ph", SearchTerms=new List<string>{ "denr.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DENR Public Affairs", OfficialValue="DENR Public Affairs", SearchTerms=new List<string>{ "DENR" } },
                    new ArticleField { Name="Casualties", ArticleValue="36 workers killed", OfficialValue="36 workers killed", SearchTerms=new List<string>{ "Binaliw", "death toll" } },
                    new ArticleField { Name="Declaration", ArticleValue="State of calamity in Cebu City; independent engineering inspections of waste facility slopes ordered", OfficialValue="State of calamity in Cebu City; independent engineering inspections of waste facility slopes ordered", SearchTerms=new List<string>{ "Binaliw", "state of calamity" } }
                }
            },
            new Article {
                Headline="EXPOSED: 'Real' Binaliw death toll is 300+ — government hiding bodies, claims insider",
                Author="Cebu Truth Channel", Domain="cebu-truthchannel.net",
                Body="An anonymous post insists the officially reported deaths at the Binaliw landfill collapse are just a fraction of the real figure, accusing officials of concealing additional casualties.",
                Difficulty=2,
                Explanation="Casualty inflation; the confirmed figure of 36 fatalities has been publicly reported since the incident.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="cebu-truthchannel.net", OfficialValue="Not a registered news domain", SearchTerms=new List<string>{ "cebu-truthchannel.net" } },
                    new ArticleField { Name="Byline", ArticleValue="Cebu Truth Channel", OfficialValue="Unverified page; anonymous operators", SearchTerms=new List<string>{ "Cebu Truth Channel" } },
                    new ArticleField { Name="Claim", ArticleValue="Over 300 dead; casualty figures covered up by officials", OfficialValue="DENR/CCDRRMO: 36 confirmed fatalities, publicly reported; state of calamity declared", SearchTerms=new List<string>{ "Binaliw", "death toll" } }
                }
            },

            // ---------- SUPREME COURT BANK RULING ----------
            new Article {
                Headline="Supreme Court bars banks from unilaterally seizing deceased clients' deposits",
                Author="Supreme Court Public Information Office", Domain="sc.judiciary.gov.ph",
                Body="The High Court en banc prohibits banks from unilaterally withholding or transferring funds from deceased depositors' accounts, ruling that balances must be preserved for estate proceedings and lawful heir adjudication.",
                Difficulty=2,
                Explanation="Matches the actual ruling: funds are preserved for heirs under estate settlement laws.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="sc.judiciary.gov.ph", OfficialValue="sc.judiciary.gov.ph", SearchTerms=new List<string>{ "sc.judiciary.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="Supreme Court Public Information Office", OfficialValue="Supreme Court Public Information Office", SearchTerms=new List<string>{ "Supreme Court" } },
                    new ArticleField { Name="Ruling", ArticleValue="Banks prohibited from unilaterally withholding or transferring deceased depositors' funds", OfficialValue="Banks prohibited from unilaterally withholding or transferring deceased depositors' funds", SearchTerms=new List<string>{ "bank", "deceased" } },
                    new ArticleField { Name="Reason", ArticleValue="Balances preserved for estate proceedings and legal heir adjudication", OfficialValue="Balances preserved for estate proceedings and legal heir adjudication", SearchTerms=new List<string>{ "estate", "heir" } }
                }
            },
            new Article {
                Headline="Supreme Court orders banks to hand over deceased clients' deposits to the government",
                Author="Anonymous Whistleblower", Domain="sc-bulletin.ph",
                Body="A viral 'bulletin' claims the High Court ruled that the accounts of deceased depositors escheat to the State immediately, with heirs required to re-apply for their inheritance.",
                Difficulty=3,
                Explanation="Fabricated ruling on an imitation SC domain; the actual decision does the opposite — funds are preserved for lawful heirs under estate settlement laws.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="sc-bulletin.ph", OfficialValue="sc.judiciary.gov.ph", SearchTerms=new List<string>{ "sc-bulletin.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="Anonymous Whistleblower", OfficialValue="Unverified identity", SearchTerms=new List<string>{ "Anonymous Whistleblower" } },
                    new ArticleField { Name="Ruling", ArticleValue="Deposits of deceased clients turned over to the government; heirs must re-apply", OfficialValue="SC: banks cannot unilaterally withhold or transfer funds; balances preserved for estate settlement and lawful heirs", SearchTerms=new List<string>{ "bank", "deceased" } }
                }
            },

            // ---------- PH-UAE CEPA ----------
            new Article {
                Headline="Philippines, UAE sign CEPA — country's first free trade accord with a Middle East nation",
                Author="DTI Communications", Domain="dti.gov.ph",
                Body="Trade officials sign the Comprehensive Economic Partnership Agreement in Abu Dhabi, cutting tariffs on agricultural exports, expanding bilateral investment, and safeguarding labor protections for overseas Filipino workers.",
                Difficulty=2,
                Explanation="Matches the DTI announcement on the CEPA signing.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="dti.gov.ph", OfficialValue="dti.gov.ph", SearchTerms=new List<string>{ "dti.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DTI Communications", OfficialValue="DTI Communications", SearchTerms=new List<string>{ "DTI" } },
                    new ArticleField { Name="Agreement", ArticleValue="Comprehensive Economic Partnership Agreement (CEPA), first PH trade deal with a Middle Eastern nation", OfficialValue="Comprehensive Economic Partnership Agreement (CEPA), first PH trade deal with a Middle Eastern nation", SearchTerms=new List<string>{ "CEPA", "UAE" } },
                    new ArticleField { Name="Coverage", ArticleValue="Lower tariffs on agricultural exports, expanded investments, OFW labor protections", OfficialValue="Lower tariffs on agricultural exports, expanded investments, OFW labor protections", SearchTerms=new List<string>{ "CEPA", "tariff" } }
                }
            },
            new Article {
                Headline="₱100,000 cash aid for every OFW in UAE — register via GCash link before Friday!",
                Author="OFW Assistance Bureau", Domain="dole-aid.online",
                Body="A circulating message promises every overseas Filipino worker in the UAE a ₱100,000 payout under the new trade deal, available by registering through a GCash link within 24 hours.",
                Difficulty=1,
                Explanation="Phishing bait riding the CEPA signing; no such payout program exists.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="dole-aid.online", OfficialValue="dole.gov.ph (only official DOLE channel)", SearchTerms=new List<string>{ "dole-aid.online" } },
                    new ArticleField { Name="Byline", ArticleValue="OFW Assistance Bureau", OfficialValue="Not a registered DOLE office", SearchTerms=new List<string>{ "OFW Assistance Bureau" } },
                    new ArticleField { Name="Program", ArticleValue="₱100,000 cash aid for every OFW in the UAE via GCash registration", OfficialValue="DTI: CEPA covers tariffs and investments; no cash aid program exists; never enter credentials via links", SearchTerms=new List<string>{ "CEPA", "OFW" } }
                }
            },

            // ---------- MALAMPAYA EAST-1 ----------
            new Article {
                Headline="DOE confirms 98 billion cubic feet of recoverable gas at Malampaya East-1",
                Author="DOE Communications", Domain="doe.gov.ph",
                Body="Exploration drilling at the offshore Malampaya East-1 field confirms an estimated 98 billion cubic feet of recoverable natural gas, extending the Malampaya gas-to-power project and reducing reliance on imported LNG.",
                Difficulty=3,
                Explanation="Matches the DOE announcement: 98 billion cubic feet supporting domestic power supply.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="doe.gov.ph", OfficialValue="doe.gov.ph", SearchTerms=new List<string>{ "doe.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DOE Communications", OfficialValue="DOE Communications", SearchTerms=new List<string>{ "DOE Communications" } },
                    new ArticleField { Name="Reserve Figure", ArticleValue="98 billion cubic feet of recoverable natural gas", OfficialValue="98 billion cubic feet of recoverable natural gas", SearchTerms=new List<string>{ "Malampaya", "cubic feet" } },
                    new ArticleField { Name="Impact", ArticleValue="Extends the Malampaya gas-to-power project; lowers reliance on imported LNG", OfficialValue="Extends the Malampaya gas-to-power project; lowers reliance on imported LNG", SearchTerms=new List<string>{ "Malampaya", "LNG" } }
                }
            },
            new Article {
                Headline="98 TRILLION cubic feet! PH becomes world's top natural gas exporter under new DOE find",
                Author="DOE Communications", Domain="doe-updates.com",
                Body="A post on an unofficial mirror of the DOE site hails the Malampaya East-1 find as instantly transforming the Philippines into the world's leading natural gas exporter.",
                Difficulty=3,
                Explanation="Imitation-domain trap: real byline, fake site. The figure is inflated a thousandfold, and the discovery supports domestic supply — no export program exists.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="doe-updates.com", OfficialValue="doe.gov.ph", SearchTerms=new List<string>{ "doe-updates.com" } },
                    new ArticleField { Name="Byline", ArticleValue="DOE Communications", OfficialValue="DOE Communications", SearchTerms=new List<string>{ "DOE Communications" } },
                    new ArticleField { Name="Reserve Figure", ArticleValue="98 trillion cubic feet of recoverable natural gas", OfficialValue="98 billion cubic feet of recoverable natural gas", SearchTerms=new List<string>{ "Malampaya", "cubic feet" } },
                    new ArticleField { Name="Claim", ArticleValue="PH becomes the world's top natural gas exporter", OfficialValue="DOE: discovery extends domestic gas-to-power supply; no export program announced", SearchTerms=new List<string>{ "Malampaya", "export" } }
                }
            },

            // ---------- SABUNGEROS CASE ----------
            new Article {
                Headline="DOJ, CIDG arrest 21 — including 14 cops — in missing sabungeros abductions",
                Author="DOJ Public Affairs", Domain="doj.gov.ph",
                Body="Coordinated raids net 21 individuals, among them 14 active and former police officers, as warrants are served against gaming operators accused of orchestrating the abduction and killing of dozens of cockfighting enthusiasts.",
                Difficulty=3,
                Explanation="Matches the DOJ announcement on the raids and arrests.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="doj.gov.ph", OfficialValue="doj.gov.ph", SearchTerms=new List<string>{ "doj.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DOJ Public Affairs", OfficialValue="DOJ Public Affairs", SearchTerms=new List<string>{ "DOJ" } },
                    new ArticleField { Name="Operation", ArticleValue="21 individuals arrested, including 14 active and former police officers", OfficialValue="21 individuals arrested, including 14 active and former police officers", SearchTerms=new List<string>{ "sabungeros", "arrest" } },
                    new ArticleField { Name="Stance", ArticleValue="Justice will spare no one regardless of wealth or rank within the police force", OfficialValue="Justice will spare no one regardless of wealth or rank within the police force", SearchTerms=new List<string>{ "sabungeros", "justice" } }
                }
            },
            new Article {
                Headline="ALL 21 arrested in sabungeros case are police generals — top brass ran the abductions, DOJ 'confirms'",
                Author="Marites Ng Bayan", Domain="totoongbalita.ph",
                Body="A gossip account claims every person arrested in the sabungeros manhunt was a police general and that the PNP leadership itself orchestrated the disappearances.",
                Difficulty=3,
                Explanation="Distorted composition: the 21 arrested include 14 active and former police officers — not generals — alongside gaming operators.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="totoongbalita.ph", OfficialValue="Not a registered news domain", SearchTerms=new List<string>{ "totoongbalita.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="Marites Ng Bayan", OfficialValue="Unverified gossip account", SearchTerms=new List<string>{ "Marites" } },
                    new ArticleField { Name="Arrests", ArticleValue="21 police generals arrested; PNP leadership ran the operation", OfficialValue="DOJ/CIDG: 21 individuals arrested, including 14 active and former police officers; gaming operators accused of orchestrating the abductions", SearchTerms=new List<string>{ "sabungeros", "arrest" } }
                }
            },

            // ---------- PH-JAPAN DEFENSE ----------
            new Article {
                Headline="PH, Japan sign defense logistics pacts; ACSA builds on Reciprocal Access Agreement",
                Author="DND Public Affairs", Domain="dnd.gov.ph",
                Body="Manila and Tokyo formalize an Acquisition and Cross-Servicing Agreement allowing the AFP and Japan Self-Defense Forces to exchange fuel, provisions, and logistical support, building on the RAA's framework for a rules-based maritime order.",
                Difficulty=3,
                Explanation="Matches the DND statement: ACSA is a logistics reciprocity agreement on top of the RAA.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="dnd.gov.ph", OfficialValue="dnd.gov.ph", SearchTerms=new List<string>{ "dnd.gov.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="DND Public Affairs", OfficialValue="DND Public Affairs", SearchTerms=new List<string>{ "DND" } },
                    new ArticleField { Name="Agreement", ArticleValue="Acquisition and Cross-Servicing Agreement (ACSA) — fuel, provisions, logistics exchange between AFP and JSDF", OfficialValue="Acquisition and Cross-Servicing Agreement (ACSA) — fuel, provisions, logistics exchange between AFP and JSDF", SearchTerms=new List<string>{ "ACSA", "Japan" } },
                    new ArticleField { Name="Context", ArticleValue="Builds on the Reciprocal Access Agreement (RAA); supports interoperability and a rules-based maritime order", OfficialValue="Builds on the Reciprocal Access Agreement (RAA); supports interoperability and a rules-based maritime order", SearchTerms=new List<string>{ "RAA", "Japan" } }
                }
            },
            new Article {
                Headline="SECRET DEAL: Japanese troops to permanently occupy PH bases; war with China 'within months'",
                Author="Buzz Clicker", Domain="shockfeed.ph",
                Body="A sensational post claims the new defense pacts allow Japan to station forces permanently in the Philippines and that conflict with China is imminent.",
                Difficulty=2,
                Explanation="Sovereignty panic; the ACSA is a logistics exchange agreement and the RAA governs visits — neither allows permanent basing.",
                Fields = new List<ArticleField> {
                    new ArticleField { Name="Publisher Domain", ArticleValue="shockfeed.ph", OfficialValue="Not a registered news domain", SearchTerms=new List<string>{ "shockfeed.ph" } },
                    new ArticleField { Name="Byline", ArticleValue="Buzz Clicker", OfficialValue="Unverified identity", SearchTerms=new List<string>{ "Buzz Clicker" } },
                    new ArticleField { Name="Claim", ArticleValue="Permanent Japanese troop presence in PH bases; war with China imminent", OfficialValue="DND: ACSA covers fuel, provisions, and logistics exchange; no permanent basing; aimed at deterrence and interoperability", SearchTerms=new List<string>{ "Japan", "ACSA" } }
                }
            }
        };

        private List<DatabaseRecord> BuildDatabase() => new List<DatabaseRecord>
        {
            // ---------- AUTHORS: official ----------
            new DatabaseRecord { Category="Author", Name="PHIVOLCS-DOST", Credibility="High", Notes="Official volcanology and seismology institute under DOST; issues all Mayon advisories." },
            new DatabaseRecord { Category="Author", Name="DICT Public Affairs", Credibility="High", Notes="Official ICT department communications." },
            new DatabaseRecord { Category="Author", Name="PCO Media Affairs", Credibility="High", Notes="Presidential Communications Office press office." },
            new DatabaseRecord { Category="Author", Name="PNP Public Information Office", Credibility="High", Notes="Official Philippine National Police press office." },
            new DatabaseRecord { Category="Author", Name="DENR Public Affairs", Credibility="High", Notes="Department of Environment and Natural Resources official updates." },
            new DatabaseRecord { Category="Author", Name="Supreme Court Public Information Office", Credibility="High", Notes="Official Supreme Court press office." },
            new DatabaseRecord { Category="Author", Name="DTI Communications", Credibility="High", Notes="Department of Trade and Industry official updates." },
            new DatabaseRecord { Category="Author", Name="DOE Communications", Credibility="High", Notes="Department of Energy official updates." },
            new DatabaseRecord { Category="Author", Name="DND Public Affairs", Credibility="High", Notes="Department of National Defense official updates." },
            new DatabaseRecord { Category="Author", Name="DOJ Public Affairs", Credibility="High", Notes="Department of Justice official updates." },

            // ---------- AUTHORS: unreliable / imitation ----------
            new DatabaseRecord { Category="Author", Name="Viral Manila", Credibility="Low", Notes="Clickbait page known for sensationalizing local news." },
            new DatabaseRecord { Category="Author", Name="OFW Assistance Bureau", Credibility="Unknown", Notes="Not a registered DOLE office; common scam nomenclature." },
            new DatabaseRecord { Category="Author", Name="Anonymous Insider", Credibility="Low", Notes="Unverified identity; frequently posts political conspiracies." },
            new DatabaseRecord { Category="Author", Name="Cebu Truth Channel", Credibility="Low", Notes="Anonymous local page; known for casualty and crime fabrications." },
            new DatabaseRecord { Category="Author", Name="Buzz Clicker", Credibility="Low", Notes="Sensationalist writer with exaggerated stories." },
            new DatabaseRecord { Category="Author", Name="Marites Ng Bayan", Credibility="Low", Notes="Viral gossip account known for twisting crime stories." },
            new DatabaseRecord { Category="Author", Name="Anonymous Whistleblower", Credibility="Low", Notes="Unverified claims; commonly linked to hoaxes." },
            new DatabaseRecord { Category="Author", Name="PHIVOLCS Monitoring Team", Credibility="Unknown", Notes="Imitation byline on copycat sites; official advisories are issued as PHIVOLCS-DOST." },

            // ---------- DOMAINS: official ----------
            new DatabaseRecord { Category="Domain", Name="phivolcs.dost.gov.ph", Credibility="High", Notes="DOST-PHIVOLCS official site." },
            new DatabaseRecord { Category="Domain", Name="dict.gov.ph", Credibility="High", Notes="Department of Information and Communications Technology." },
            new DatabaseRecord { Category="Domain", Name="pco.gov.ph", Credibility="High", Notes="Presidential Communications Office." },
            new DatabaseRecord { Category="Domain", Name="pnp.gov.ph", Credibility="High", Notes="Philippine National Police official portal." },
            new DatabaseRecord { Category="Domain", Name="denr.gov.ph", Credibility="High", Notes="Department of Environment and Natural Resources." },
            new DatabaseRecord { Category="Domain", Name="sc.judiciary.gov.ph", Credibility="High", Notes="Supreme Court of the Philippines official portal." },
            new DatabaseRecord { Category="Domain", Name="dti.gov.ph", Credibility="High", Notes="Department of Trade and Industry." },
            new DatabaseRecord { Category="Domain", Name="doe.gov.ph", Credibility="High", Notes="Department of Energy." },
            new DatabaseRecord { Category="Domain", Name="dnd.gov.ph", Credibility="High", Notes="Department of National Defense." },
            new DatabaseRecord { Category="Domain", Name="doj.gov.ph", Credibility="High", Notes="Department of Justice." },

            // ---------- DOMAINS: unregistered / imitation ----------
            new DatabaseRecord { Category="Domain", Name="truthviral.ph", Credibility="Low", Notes="Frequently publishes exaggerated local rumors." },
            new DatabaseRecord { Category="Domain", Name="dole-aid.online", Credibility="Unknown", Notes="Recently created imitation domain; not affiliated with DOLE. Official DOLE domain: dole.gov.ph." },
            new DatabaseRecord { Category="Domain", Name="pinasconspiracy.net", Credibility="Low", Notes="Hub for political and national security conspiracies." },
            new DatabaseRecord { Category="Domain", Name="cebu-truthchannel.net", Credibility="Low", Notes="Unregistered anonymous page; not a news organization." },
            new DatabaseRecord { Category="Domain", Name="shockfeed.ph", Credibility="Low", Notes="Clickbait and sensationalist content." },
            new DatabaseRecord { Category="Domain", Name="totoongbalita.ph", Credibility="Low", Notes="Gossip-hybrid site; stories routinely distorted." },
            new DatabaseRecord { Category="Domain", Name="doe-updates.com", Credibility="Unknown", Notes="Unofficial imitation site; not affiliated with the DOE. Official domain: doe.gov.ph." },
            new DatabaseRecord { Category="Domain", Name="phivolcs-alerts.com", Credibility="Unknown", Notes="Copycat volcanology site; not affiliated with DOST-PHIVOLCS. Official domain: phivolcs.dost.gov.ph." },
            new DatabaseRecord { Category="Domain", Name="sc-bulletin.ph", Credibility="Unknown", Notes="Imitation site; not affiliated with the Supreme Court. Official domain: sc.judiciary.gov.ph." },

            // ---------- OFFICIAL STATEMENTS: verify claims here ----------
            new DatabaseRecord { Category="Official Statement", Name="Mayon Volcano Alert", Credibility="DOST-PHIVOLCS",
                Notes="DOST-PHIVOLCS has Mayon Volcano at Alert Level 3 (intensified magmatic unrest, pyroclastic density currents, rockfall). The six-kilometer-radius Permanent Danger Zone is strictly off-limits; residents within it are evacuated and local governments have declared states of calamity. There is NO Alert Level 5 and NO regionwide Bicol evacuation order." },
            new DatabaseRecord { Category="Official Statement", Name="Grok Platform Block", Credibility="DICT",
                Notes="The DICT directed internet service providers to block xAI's Grok after evidence it was used to generate non-consensual sexually explicit imagery and deepfakes of women and minors. The block applies ONLY to Grok. There is no nationwide ban on AI tools such as ChatGPT or Gemini, and AI use is not criminalized." },
            new DatabaseRecord { Category="Official Statement", Name="Trillion Peso March Protests", Credibility="PCO / Malacañang",
                Notes="Tens of thousands gathered at Luneta and the EDSA Shrine ('Baha sa Luneta' / 'Trillion Peso March') over ghost and substandard flood control projects. The Palace acknowledged public frustration and committed to administrative overhauls and cooperation with independent investigative bodies. There is NO martial law or dispersal order." },
            new DatabaseRecord { Category="Official Statement", Name="Binaliw Landfill Collapse", Credibility="DENR / CCDRRMO",
                Notes="The Binaliw sanitary landfill slope collapse in Cebu City killed 36 workers. Cebu City is under a state of calamity, and independent engineering inspections of waste facility slopes are underway. The death toll of 36 has been publicly reported since the incident; there is no cover-up." },
            new DatabaseRecord { Category="Official Statement", Name="Malampaya East-1 Gas Discovery", Credibility="DOE",
                Notes="The Department of Energy (DOE) confirmed an estimated 98 billion cubic feet of recoverable natural gas at Malampaya East-1, extending the Malampaya gas-to-power project and reducing reliance on imported LNG for Luzon's power supply. There is NO gas export program and NO free electricity program." },
            new DatabaseRecord { Category="Official Statement", Name="Sabungeros Abduction Case", Credibility="DOJ / CIDG",
                Notes="Coordinated DOJ and CIDG raids arrested 21 individuals — including 14 active and former police officers — over the abduction and killing of cockfighting enthusiasts ('sabungeros'). Warrants also targeted gaming operators accused of orchestrating the disappearances. The DOJ says the pursuit of justice will spare no one regardless of wealth or rank." },
            new DatabaseRecord { Category="Official Statement", Name="PH-Japan Defense Agreements", Credibility="DND",
                Notes="The Acquisition and Cross-Servicing Agreement (ACSA) allows the AFP and Japan Self-Defense Forces to exchange fuel, provisions, and logistical support, building on the Reciprocal Access Agreement (RAA). The pacts strengthen interoperability and deterrence; they do NOT allow permanent foreign basing or occupation of Philippine bases." },
            new DatabaseRecord { Category="Official Statement", Name="Deceased Depositors' Bank Accounts", Credibility="Supreme Court",
                Notes="The Supreme Court en banc prohibited banks from unilaterally withholding or transferring funds of deceased depositors. Balances must be preserved for estate settlement and lawful heir adjudication. Deposits are NOT turned over to the government, and banks do not gain ownership of deceased clients' funds." },
            new DatabaseRecord { Category="Official Statement", Name="PH-UAE CEPA Trade Deal", Credibility="DTI",
                Notes="The Comprehensive Economic Partnership Agreement (CEPA), signed in Abu Dhabi, is the Philippines' first free trade accord with a Middle Eastern nation. It cuts tariffs on agricultural exports, expands bilateral investment, and safeguards OFW labor protections. There is NO cash aid or GCash registration program attached to the deal." },
            new DatabaseRecord { Category="Official Statement", Name="Flood Control Plunder Probe", Credibility="Sandiganbayan / PNP",
                Notes="The Sandiganbayan issued arrest warrants for lawmakers and public works officials implicated in the multi-billion-peso flood control budget scandal; former senator Bong Revilla surrendered to the PNP at Camp Crame. The PNP states all valid warrants will be executed without fear or favor, with due process observed." }
        };
    }
}