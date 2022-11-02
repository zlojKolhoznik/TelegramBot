using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    internal class AppSettings
    {
        [JsonProperty("botToken")]
        public string BotToken { get; set; }

        [JsonProperty("weatherApiKey")]
        public string WeatherApiKey { get; set; }

        public static AppSettings ReadFromFile(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AppSettings>(json);
        }
    }
}
