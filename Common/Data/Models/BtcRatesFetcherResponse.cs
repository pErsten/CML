using Newtonsoft.Json;

namespace Common.Data.Models
{
    public class BtcRatesFetcherResponse
    {
        [JsonProperty("15m")]
        public decimal fifteenMin { get; set; }
        public decimal last { get; set; }
        public decimal buy { get; set; }
        public decimal sell { get; set; }
        public string symbol { get; set; }
    }
}
