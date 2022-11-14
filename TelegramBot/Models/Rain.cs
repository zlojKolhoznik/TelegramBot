using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Models
{
    public class Rain
    {
        [JsonProperty("1h")]
        public double OneH { get; set; }
    }
}
