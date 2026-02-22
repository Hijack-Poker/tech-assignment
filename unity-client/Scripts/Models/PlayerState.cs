using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HijackPoker.Models
{
    /// <summary>
    /// Player state from the holdem-processor API.
    /// Maps to each item in the "players" array of the /table/{tableId} response.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        [JsonProperty("playerId")]
        public int PlayerId;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("seat")]
        public int Seat;

        [JsonProperty("stack")]
        public float Stack;

        [JsonProperty("bet")]
        public float Bet;

        [JsonProperty("totalBet")]
        public float TotalBet;

        /// <summary>
        /// Player status code as a string.
        /// "1" = Active, "11" = Folded, "12" = All-In, etc.
        /// </summary>
        [JsonProperty("status")]
        public string Status;

        /// <summary>
        /// Last action taken: "call", "check", "bet", "raise", "fold", "allin", or empty.
        /// </summary>
        [JsonProperty("action")]
        public string Action;

        /// <summary>
        /// Hole cards as strings, e.g. ["AH", "KD"].
        /// The API always returns the card values — it's the client's job
        /// to decide whether to display them face-up or face-down based on
        /// the current hand step (show at showdown or if player has winnings).
        /// </summary>
        [JsonProperty("cards")]
        public List<string> Cards;

        /// <summary>
        /// Hand rank description at showdown, e.g. "Full House", "Two Pair".
        /// Empty string before hands are evaluated.
        /// </summary>
        [JsonProperty("handRank")]
        public string HandRank;

        /// <summary>
        /// Amount won this hand. 0 if not a winner.
        /// </summary>
        [JsonProperty("winnings")]
        public float Winnings;

        // ── Convenience properties ─────────────────────────────────

        public bool IsActive => Status == PlayerStatusCode.Active;
        public bool IsFolded => Status == PlayerStatusCode.Folded;
        public bool IsAllIn => Status == PlayerStatusCode.AllIn;
        public bool IsWinner => Winnings > 0;
        public bool HasCards => Cards != null && Cards.Count > 0;
    }

    /// <summary>
    /// Player status codes returned by the API.
    /// </summary>
    public static class PlayerStatusCode
    {
        public const string Active = "1";
        public const string SittingOut = "2";
        public const string Leaving = "3";
        public const string ShowCards = "4";
        public const string PostBlind = "5";
        public const string WaitForBB = "6";
        public const string Folded = "11";
        public const string AllIn = "12";
    }
}
