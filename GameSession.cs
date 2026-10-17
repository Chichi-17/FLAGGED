using System;
using System.Collections.Generic;

namespace FLAGGED
{
    public enum GameEnding { None, Best, Partial, Worst, RentUnpaid }

    // What the player chooses to buy at the end of the day.
    // Index 0 / 1 in the arrays = the first / second family member.
    public class EndOfDayChoices
    {
        public bool PayUtilities { get; set; }
        public bool[] FeedMember { get; set; } = new bool[GameRules.FamilySize];
        public bool[] GiveMedicine { get; set; } = new bool[GameRules.FamilySize];
    }

    public class ShiftSummary
    {
        public int Day { get; set; }
        public int Correct { get; set; }
        public int Wrong { get; set; }
        public int Money { get; set; }
        public bool TimeUp { get; set; }
    }

    public class EndOfDayReport
    {
        public int Day { get; set; }
        public bool RentPaid { get; set; }
        public int MoneyLeft { get; set; }
        public bool GameOver { get; set; }
        public List<string> Events { get; set; } = new List<string>();
        public List<string> FamilyStatus { get; set; } = new List<string>();
    }

    public class FinalResult
    {
        public GameEnding Ending { get; set; } = GameEnding.None;
        public int Survivors { get; set; }
        public int Savings { get; set; }
        public int Score { get; set; }
        public string Message { get; set; } = "";
    }

    // =====================================================================
    //  DAY LOGIC
    //  Wraps CoreGameLogic and ties the shift (time), coins and family
    //  together across the 5 days. It never uses Console, so it works the
    //  same with a console UI or a graphical UI.
    //
    //  The UI should call GameSession instead of CoreGameLogic:
    //    StartShift() -> GetCurrentArticle() / SearchDatabase() /
    //    InvestigateField() / SubmitJudgment() -> EndShift() ->
    //    ResolveEndOfDay() -> repeat until IsGameOver -> GetFinalResult()
    // =====================================================================
    public class GameSession
    {
        private readonly CoreGameLogic _core;
        private int _correct;
        private int _wrong;
        private bool _shiftFinished;
        private bool _rentFailed;
        private bool _allDead;

        public Wallet Wallet { get; private set; }
        public ShiftTimer Timer { get; private set; }
        public List<FamilyMember> Family { get; private set; }
        public int CurrentDay { get; private set; } = 1;

        public GameSession(CoreGameLogic core,
                           string firstMemberName = "Family Member 1",
                           string secondMemberName = "Family Member 2")
        {
            _core = core;
            Wallet = new Wallet(GameRules.StartingMoney);
            Timer = new ShiftTimer();
            Family = new List<FamilyMember>
            {
                new FamilyMember(firstMemberName),
                new FamilyMember(secondMemberName)
            };
        }

        public bool IsGameOver => _rentFailed || _allDead || CurrentDay > GameRules.TotalDays;

        // True when time ran out or the article queue is empty.
        public bool IsShiftOver => _shiftFinished || Timer.IsTimeUp;

        public int Money => Wallet.Money;

        // =================================================================
        //  SHIFT (TIME + COINS)
        // =================================================================
        public void StartShift()
        {
            if (IsGameOver) return;

            _correct = 0;
            _wrong = 0;
            _shiftFinished = false;

            _core.StartShift(CurrentDay);
            Timer.Start();
        }

        // Returns null when the shift is over.
        public Article GetCurrentArticle()
        {
            return IsShiftOver ? null : _core.GetCurrentArticle();
        }

        // Database lookups cost time (GameRules.DatabaseCheckSeconds each).
        public DatabaseSearchResult SearchDatabase(string query)
        {
            if (IsShiftOver)
                return new DatabaseSearchResult { Query = query, Message = "The shift is over." };

            Timer.AddPenalty(GameRules.DatabaseCheckSeconds);
            return _core.SearchDatabase(query);
        }

        public DatabaseSearchResult InvestigateField(int fieldIndex)
        {
            if (IsShiftOver)
                return new DatabaseSearchResult { Message = "The shift is over." };

            Timer.AddPenalty(GameRules.DatabaseCheckSeconds);
            return _core.InvestigateField(fieldIndex);
        }

        // Rulebook-style browsing stays free.
        public List<DatabaseRecord> BrowseDatabase(string category = null)
        {
            return _core.BrowseDatabase(category);
        }

        // Accept or Reject the current article. Updates the wallet:
        // +GameRules.RewardCorrect when correct, -GameRules.PenaltyWrong when wrong.
        public JudgmentResult SubmitJudgment(Judgment choice)
        {
            if (IsShiftOver)
            {
                return new JudgmentResult { Message = "The shift is over.", ShiftEnded = true };
            }

            if (_core.GetCurrentArticle() == null)
            {
                _shiftFinished = true;
                Timer.Stop();
                return new JudgmentResult { Message = "No article to judge.", ShiftEnded = true };
            }

            JudgmentResult result = _core.SubmitJudgment(choice);

            if (result.IsCorrect)
            {
                result.PointsDelta = Wallet.Add(GameRules.RewardCorrect);
                _correct++;
            }
            else
            {
                result.PointsDelta = -Wallet.Deduct(GameRules.PenaltyWrong);
                _wrong++;
            }

            if (result.ShiftEnded)
            {
                _shiftFinished = true;
                Timer.Stop();
            }

            result.ShiftEnded = result.ShiftEnded || Timer.IsTimeUp;
            return result;
        }

        // Call when the shift is over (time up or no articles left).
        public ShiftSummary EndShift()
        {
            bool timeUp = Timer.IsTimeUp;
            _shiftFinished = true;
            Timer.Stop();

            return new ShiftSummary
            {
                Day = CurrentDay,
                Correct = _correct,
                Wrong = _wrong,
                Money = Wallet.Money,
                TimeUp = timeUp
            };
        }

        // =================================================================
        //  END OF DAY (COINS + FAMILY)
        // =================================================================
        public bool CanPayRent => Wallet.CanAfford(GameRules.RentCost);

        // Money the player will have to spend on utilities/food/medicine after rent.
        public int MoneyAfterRent => Math.Max(0, Wallet.Money - GameRules.RentCost);

        // Total cost of a set of choices (for showing a running total in the UI).
        public int CostOf(EndOfDayChoices choices)
        {
            if (choices == null) return 0;

            int total = choices.PayUtilities ? GameRules.UtilitiesCost : 0;
            for (int i = 0; i < Family.Count; i++)
            {
                if (Family[i].IsAlive && Flag(choices.FeedMember, i))
                    total += GameRules.FoodCostPerMember;
                if (Family[i].IsAlive && Family[i].IsSick && Flag(choices.GiveMedicine, i))
                    total += GameRules.MedicineCost;
            }
            return total;
        }

        // Pays rent, then buys what the player chose (utilities, food, medicine, in that
        // order; anything unaffordable is skipped), then works out the night.
        public EndOfDayReport ResolveEndOfDay(EndOfDayChoices choices)
        {
            var report = new EndOfDayReport { Day = CurrentDay };

            if (IsGameOver)
            {
                report.GameOver = true;
                report.MoneyLeft = Wallet.Money;
                report.Events.Add("The game is already over.");
                return report;
            }

            if (choices == null) choices = new EndOfDayChoices();

            // 1. Rent is mandatory: cannot pay = game over
            if (!CanPayRent)
            {
                _rentFailed = true;
                report.GameOver = true;
                report.MoneyLeft = Wallet.Money;
                report.Events.Add("You cannot afford the rent ($" + GameRules.RentCost + ").");
                return report;
            }

            Wallet.Deduct(GameRules.RentCost);
            report.RentPaid = true;
            report.Events.Add("Rent paid: -$" + GameRules.RentCost + ".");

            // 2. Spend on utilities, food, medicine
            bool utilitiesPaid = Purchase(choices.PayUtilities, GameRules.UtilitiesCost, "utilities", report);

            bool[] fed = new bool[Family.Count];
            bool[] treated = new bool[Family.Count];

            for (int i = 0; i < Family.Count; i++)
            {
                if (Family[i].IsAlive)
                    fed[i] = Purchase(Flag(choices.FeedMember, i), GameRules.FoodCostPerMember,
                                      "food for " + Family[i].Name, report);
            }

            for (int i = 0; i < Family.Count; i++)
            {
                if (Family[i].IsAlive && Family[i].IsSick)
                    treated[i] = Purchase(Flag(choices.GiveMedicine, i), GameRules.MedicineCost,
                                          "medicine for " + Family[i].Name, report);
            }

            // 3. Work out what happened to each family member
            for (int i = 0; i < Family.Count; i++)
            {
                FamilyMember member = Family[i];
                if (!member.IsAlive) continue;

                if (treated[i])
                {
                    member.GiveMedicine();
                    report.Events.Add(member.Name + " took medicine and recovered.");
                }

                if (fed[i])
                {
                    member.Feed();
                }
                else
                {
                    member.SkipMeal();
                    report.Events.Add(member.Name + " went without food.");
                }

                member.UntreatedSicknessDay();

                if (!member.IsAlive)
                {
                    report.Events.Add(member.Name + " has died from " + member.CauseOfDeath + ".");
                }
            }

            if (CountAlive() == 0)
            {
                _allDead = true;
                report.GameOver = true;
                report.MoneyLeft = Wallet.Money;
                AddFamilyStatus(report);
                return report;
            }

            // 4. Overnight sickness and random events (no night follows the last day)
            if (CurrentDay < GameRules.TotalDays)
            {
                RollOvernightSickness(utilitiesPaid, report);
                RandomEvent(report);
            }

            report.MoneyLeft = Wallet.Money;
            AddFamilyStatus(report);

            CurrentDay++;
            report.GameOver = IsGameOver;
            return report;
        }

        // =================================================================
        //  ENDINGS AND FINAL SCORE
        // =================================================================
        public FinalResult GetFinalResult()
        {
            var result = new FinalResult();
            if (!IsGameOver) return result;

            result.Survivors = CountAlive();
            result.Savings = Wallet.Money;
            result.Score = Wallet.Money + (result.Survivors * GameRules.SurvivorBonus);

            if (_rentFailed)
            {
                result.Ending = GameEnding.RentUnpaid;
                result.Message = "GAME OVER: You could not pay the rent.";
            }
            else if (result.Survivors == 0)
            {
                result.Ending = GameEnding.Worst;
                result.Message = "WORST ENDING: Your whole family did not survive.";
            }
            else if (result.Survivors == Family.Count)
            {
                result.Ending = GameEnding.Best;
                result.Message = "BEST ENDING: You survived all " + GameRules.TotalDays +
                                 " days and kept your family alive!";
            }
            else
            {
                result.Ending = GameEnding.Partial;
                result.Message = "PARTIAL ENDING: You made it through, but not everyone survived.";
            }

            return result;
        }

        public int CountAlive()
        {
            int count = 0;
            foreach (FamilyMember member in Family)
            {
                if (member.IsAlive) count++;
            }
            return count;
        }

        // =================================================================
        //  PRIVATE HELPERS
        // =================================================================
        private static bool Flag(bool[] flags, int index)
        {
            return flags != null && index < flags.Length && flags[index];
        }

        // Buys something if the player wanted it and can afford it.
        private bool Purchase(bool wanted, int cost, string what, EndOfDayReport report)
        {
            if (!wanted) return false;

            if (!Wallet.CanAfford(cost))
            {
                report.Events.Add("Could not afford " + what + " ($" + cost + ").");
                return false;
            }

            Wallet.Deduct(cost);
            return true;
        }

        private void RollOvernightSickness(bool utilitiesPaid, EndOfDayReport report)
        {
            foreach (FamilyMember member in Family)
            {
                if (!member.IsAlive || member.IsSick) continue;

                int chance = GameRules.BaseSickChance +
                             (GameRules.SickChancePerMissedMeal * member.MissedMeals);
                if (!utilitiesPaid) chance += GameRules.SickChanceNoUtilities;
                if (chance > 100) chance = 100;

                if (GameRules.Rng.Next(100) < chance)
                {
                    member.GetSick();
                    report.Events.Add(member.Name + " fell sick overnight.");
                }
            }
        }

        // Random event: makes one or both living members sick.
        // Same chance every day, so it is not a hidden difficulty ramp.
        private void RandomEvent(EndOfDayReport report)
        {
            if (GameRules.Rng.Next(100) >= GameRules.RandomEventChance) return;

            var living = new List<FamilyMember>();
            foreach (FamilyMember member in Family)
            {
                if (member.IsAlive) living.Add(member);
            }
            if (living.Count == 0) return;

            report.Events.Add("EVENT: A sickness spreads through the neighborhood!");

            if (living.Count > 1 && GameRules.Rng.Next(100) < GameRules.BothSickChance)
            {
                foreach (FamilyMember member in living)
                {
                    member.GetSick();
                    report.Events.Add(member.Name + " caught the illness.");
                }
            }
            else
            {
                FamilyMember victim = living[GameRules.Rng.Next(living.Count)];
                victim.GetSick();
                report.Events.Add(victim.Name + " caught the illness.");
            }
        }

        private void AddFamilyStatus(EndOfDayReport report)
        {
            foreach (FamilyMember member in Family)
            {
                report.FamilyStatus.Add(member.Name + " - " + member.Status);
            }
        }
    }
}
