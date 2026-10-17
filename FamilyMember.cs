using System;

namespace FLAGGED
{
    // =====================================================================
    //  FAMILY LOGIC
    //  One family member. Survival depends on food and medicine:
    //    - 3 days without food = death
    //    - sickness untreated for 3 days = death
    //    - medicine cures sickness
    // =====================================================================
    public class FamilyMember
    {
        public string Name { get; private set; }
        public bool IsAlive { get; private set; } = true;
        public bool IsSick { get; private set; }
        public int MissedMeals { get; private set; }
        public int SickDays { get; private set; }
        public string CauseOfDeath { get; private set; } = "";

        public FamilyMember(string name)
        {
            Name = name;
        }

        // Eating resets the hunger counter.
        public void Feed()
        {
            MissedMeals = 0;
        }

        public void SkipMeal()
        {
            MissedMeals++;
            if (MissedMeals >= GameRules.HungerDeathDays)
            {
                Die("starvation");
            }
        }

        public void GetSick()
        {
            if (IsAlive && !IsSick)
            {
                IsSick = true;
                SickDays = 0;
            }
        }

        public void GiveMedicine()
        {
            IsSick = false;
            SickDays = 0;
        }

        // Called once per day. If still sick (no medicine), the illness gets worse.
        public void UntreatedSicknessDay()
        {
            if (IsAlive && IsSick)
            {
                SickDays++;
                if (SickDays >= GameRules.SicknessDeathDays)
                {
                    Die("untreated illness");
                }
            }
        }

        private void Die(string cause)
        {
            IsAlive = false;
            IsSick = false;
            CauseOfDeath = cause;
        }

        // Text the UI can show, e.g. "Healthy", "Sick, hungry (1 missed meal/s)", "Dead (starvation)"
        public string Status
        {
            get
            {
                if (!IsAlive) return "Dead (" + CauseOfDeath + ")";

                string status = IsSick ? "Sick" : "Healthy";
                if (MissedMeals > 0)
                {
                    status += ", hungry (" + MissedMeals + " missed meal/s)";
                }
                return status;
            }
        }
    }
}
