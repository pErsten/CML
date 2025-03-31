using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Common.Dtos
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
