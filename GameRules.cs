using System;

namespace FLAGGED
{
    // =====================================================================
    //  SHARED SETTINGS
    //  Every number in the design lives here, so the group can tune the game
    //  in one place. Difficulty and costs are the SAME on all 5 days.
    // =====================================================================
    public static class GameRules
    {
        public static readonly Random Rng = new Random();

        // ----- Day / Time -----
        public const int TotalDays = 5;
        public const int ShiftSeconds = 180;          // 3-minute shift
        public const int ArticlesPerShift = 6;        // same every day (their pool caps a shift at 6)
        public const int DatabaseCheckSeconds = 5;    // time taken off the clock per database lookup

        // ----- Coins -----
        public const int StartingMoney = 20;
        public const int RewardCorrect = 10;          // correct accept/reject
        public const int PenaltyWrong = 5;            // wrong accept/reject

        // ----- Daily expenses (flat for all 5 days) -----
        public const int RentCost = 15;               // mandatory: cannot pay = game over
        public const int UtilitiesCost = 5;
        public const int FoodCostPerMember = 6;
        public const int MedicineCost = 10;           // per sick member, only when needed

        // ----- Family -----
        public const int FamilySize = 2;
        public const int HungerDeathDays = 3;         // 3 days without food = death
        public const int SicknessDeathDays = 3;       // 3 days sick with no medicine = death
        public const int BaseSickChance = 10;         // % chance to fall sick overnight
        public const int SickChancePerMissedMeal = 25;
        public const int SickChanceNoUtilities = 10;
        public const int RandomEventChance = 20;      // % chance of a sickness event each night
        public const int BothSickChance = 30;         // % of those events that hit both members

        // ----- Final score -----
        public const int SurvivorBonus = 50;          // points per surviving family member
    }
}
