using Newtonsoft.Json;
using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TelegramBot;
using TelegramBot.Models;
using TelegramBot.Utils;

var settings = AppSettings.ReadFromFile("appsettings.json");
var client = new TelegramBotClient(settings.BotToken);
var cts = new CancellationTokenSource();
var receiverOptions = new Telegram.Bot.Polling.ReceiverOptions() { AllowedUpdates = Array.Empty<UpdateType>() };

client.StartReceiving(
    updateHandler: OnUpdates,
    pollingErrorHandler: OnErrors,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token);

Task OnErrors(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
{
    Console.WriteLine(ex.Message);
    return Task.CompletedTask;
}

async Task OnUpdates(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Type != UpdateType.Message)
    {
        await bot.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Oops... I don't know such commands");
    }
    switch (update.Message!.Text)
    {
        case "/start":
            await bot.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Hello\\! I am *GeoWeatherBot*\\. I will send you the current weather at the point you send to me",
            parseMode: ParseMode.MarkdownV2);
            return;
        case "/help":
            for (int i = 1; i <= 3; i++)
            {
                InputOnlineFile input;
                using (FileStream fs = new FileStream($"{i}.jpg", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    input = new InputOnlineFile(fs);

                    await bot.SendPhotoAsync(
                        chatId: update.Message!.Chat.Id,
                        photo: input);
                }
            }
            await bot.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: "To send me the geolocation, you should:\n1\\. Click `Attachment` button near the message input\n2\\. Choose `geolocation` option\n3\\. Select desired location and click `send` button",
                parseMode: ParseMode.MarkdownV2);
            return;
        default:
            break;
    }
    if (update.Message!.Location == null)
    {
        await bot.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "I understand only messages with a location, commands `/start` and `/help`\\!",
            parseMode: ParseMode.MarkdownV2);
    }

    string uri = $"https://api.openweathermap.org/data/3.0/onecall?lat={update.Message.Location!.Latitude}&lon={update.Message.Location.Longitude}&exclude=daily,minutely,hourly,alerts&appid={settings.WeatherApiKey}&units=metric&lang=en";
    string json = await WebUtils.GetTextResponseAsync(uri);
    WeatherInfo weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(json);
    if (weatherInfo == null)
    {
        await bot.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "Oops... Something went wrong while getting weather. Try again later!");
        System.IO.File.WriteAllText($"{DateTime.Now.ToString("ddMMyyyyhhmmss")}-log.log", $"{uri}\n\n\n\n{json}");
    }
    Current current = weatherInfo!.Current;
    string iconCode = current.Weather.First().Icon;
    Image icon = await WebUtils.GetImageResponseAsync($"http://openweathermap.org/img/wn/{iconCode}@2x.png");
    icon.Save(iconCode + ".png", System.Drawing.Imaging.ImageFormat.Png);
    InputOnlineFile iof;
    string caption = $"Temperature: {Convert.ToInt32(current.Temp)}\nWeather: {current.Weather.First().Main}\nWind: {current.WindSpeed}\nPressure: {current.Pressure}\nHumidity: {current.Humidity}";
    using (FileStream fs = new FileStream(iconCode + ".png", FileMode.Open, FileAccess.Read, FileShare.Read))
    {
        iof = new InputOnlineFile(fs);
        await bot.SendPhotoAsync(
            chatId: update.Message!.Chat.Id,
            photo: iof,
            caption: caption);
    }
}

Console.ReadLine();
