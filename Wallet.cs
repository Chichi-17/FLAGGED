using System;

namespace FLAGGED
{
    // =====================================================================
    //  COINS LOGIC
    //  Holds the player's money. Money never goes below zero.
    // =====================================================================
    public class Wallet
    {
        public int Money { get; private set; }

        public Wallet(int startingMoney)
        {
            Money = startingMoney;
        }

        // Adds money. Returns the amount actually added.
        public int Add(int amount)
        {
            if (amount < 0) amount = 0;
            Money += amount;
            return amount;
        }

        // Takes money away (never below 0). Returns the amount actually taken.
        public int Deduct(int amount)
        {
            if (amount < 0) amount = 0;
            int actual = Math.Min(amount, Money);
            Money -= actual;
            return actual;
        }

        public bool CanAfford(int cost) => Money >= cost;

        // For the on-screen label: "Money: $##"
        public string Display() => "Money: $" + Money;
    }
}
