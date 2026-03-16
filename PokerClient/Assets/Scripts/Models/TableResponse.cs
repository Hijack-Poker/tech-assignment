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
}
