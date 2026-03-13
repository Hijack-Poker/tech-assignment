using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HijackPoker.Models
{
    /// <summary>
    /// Top-level response from GET /table/{tableId}.
    /// </summary>
    [Serializable]
    public class TableResponse
    {
        [JsonProperty("game")]
        public GameState Game;

        [JsonProperty("players")]
        public List<PlayerState> Players;
    }

    /// <summary>
    /// Game state from the holdem-processor API.
    /// Maps to the "game" object in the /table/{tableId} response.
    /// </summary>
    [Serializable]
    public class GameState
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("tableId")]
        public int TableId;

        [JsonProperty("tableName")]
        public string TableName;

        [JsonProperty("gameNo")]
        public int GameNo;

        [JsonProperty("handStep")]
        public int HandStep;

        [JsonProperty("stepName")]
        public string StepName;

        [JsonProperty("dealerSeat")]
        public int DealerSeat;

        [JsonProperty("smallBlindSeat")]
        public int SmallBlindSeat;

        [JsonProperty("bigBlindSeat")]
        public int BigBlindSeat;

        /// <summary>
        /// Community cards as strings, e.g. ["JH", "7D", "2C"].
        /// Empty array before the flop is dealt.
        /// </summary>
        [JsonProperty("communityCards")]
        public List<string> CommunityCards;

        [JsonProperty("pot")]
        public float Pot;

        [JsonProperty("sidePots")]
        public List<SidePot> SidePots;

        /// <summary>
        /// Seat number of the player whose turn it is (0 if no action pending).
        /// </summary>
        [JsonProperty("move")]
        public int Move;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("smallBlind")]
        public float SmallBlind;

        [JsonProperty("bigBlind")]
        public float BigBlind;

        [JsonProperty("maxSeats")]
        public int MaxSeats;

        [JsonProperty("currentBet")]
        public float CurrentBet;

        [JsonProperty("winners")]
        public List<Winner> Winners;

        // ── Convenience properties ─────────────────────────────────

        /// <summary>
        /// True when cards should be revealed (AFTER_RIVER_BETTING_ROUND and beyond).
        /// </summary>
        public bool IsShowdown => HandStep >= 12;

        /// <summary>
        /// True when the hand has completed.
        /// </summary>
        public bool IsHandComplete => StepName == "RECORD_STATS_AND_NEW_HAND";
    }

    [Serializable]
    public class SidePot
    {
        [JsonProperty("amount")]
        public float Amount;

        [JsonProperty("eligibleSeats")]
        public List<int> EligibleSeats;
    }

    [Serializable]
    public class Winner
    {
        [JsonProperty("seat")]
        public int Seat;

        [JsonProperty("playerId")]
        public int PlayerId;
    }

    /// <summary>
    /// Response from POST /process.
    /// </summary>
    [Serializable]
    public class ProcessResponse
    {
        [JsonProperty("success")]
        public bool Success;

        [JsonProperty("result")]
        public ProcessResult Result;

        [JsonProperty("error")]
        public string Error;
    }

    [Serializable]
    public class ProcessResult
    {
        [JsonProperty("status")]
        public string Status;

        [JsonProperty("tableId")]
        public int TableId;

        [JsonProperty("step")]
        public int Step;

        [JsonProperty("stepName")]
        public string StepName;
    }

    /// <summary>
    /// Response from GET /health.
    /// </summary>
    [Serializable]
    public class HealthResponse
    {
        [JsonProperty("service")]
        public string Service;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("timestamp")]
        public string Timestamp;
    }
}
