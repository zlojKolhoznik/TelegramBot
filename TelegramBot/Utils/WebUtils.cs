using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TelegramBot.Utils
{
    internal static class WebUtils
    {
        public static async Task<string> GetTextResponseAsync(string uri)
        {
            string text;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(uri);
                text = await response.Content.ReadAsStringAsync();
            }
            return text;
        }

        public static async Task<Image> GetImageResponseAsync(string uri)
        {
            Image result;
            byte[] buff;
            using (var client = new HttpClient())
            {
                buff = await client.GetByteArrayAsync(uri);
            }
            using (var ms = new MemoryStream(buff))
            {
                result = new Bitmap(ms);
            }
            return result;
        }
    }
}
