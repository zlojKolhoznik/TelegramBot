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

Task OnErrors(ITelegramBotClient bot, Exception ex, CancellationToken cancellationToken)
{
    Console.WriteLine(ex.Message);
    return Task.CompletedTask;
}

async Task OnUpdates(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    try
    {
        await CheckUpdate(bot, update);
    }
    catch 
    { 
        return; 
    }
    string uri = $"https://api.openweathermap.org/data/3.0/onecall?lat={update.Message!.Location!.Latitude}&lon={update.Message.Location.Longitude}&exclude=daily,minutely,hourly,alerts&appid={settings.WeatherApiKey}&units=metric&lang=en";
    string json = await WebUtils.GetTextResponseAsync(uri);
    WeatherInfo weatherInfo = JsonConvert.DeserializeObject<WeatherInfo>(json);
    if (weatherInfo == null)
    {
        await WeatherNotReceivedHandler(bot, update, uri, json);
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

static async Task CheckUpdate(ITelegramBotClient bot, Update update)
{
    if (update.Type != UpdateType.Message)
    {
        throw new Exception();
    }
    switch (update.Message!.Text)
    {
        case "/start":
            await StartHandler(bot, update);
            Console.WriteLine(update.Message!.From!.Username + " - " + update.Message.Text);
            throw new Exception();
        case "/help":
            await HelpHandler(bot, update);
            Console.WriteLine(update.Message!.From!.Username + " - " + update.Message.Text);
            throw new Exception();
        default:
            Console.WriteLine(update.Message!.From!.Username + " - " + update.Message.Text);
            break;
    }
    if (update.Message!.Location == null)
    {
        await UnknownCommandHandler(bot, update);
        throw new Exception();
    }
}

static async Task StartHandler(ITelegramBotClient bot, Update update)
{
    await bot.SendTextMessageAsync(
                chatId: update.Message!.Chat.Id,
                text: "Hello\\! I am *GeoWeatherBot*\\. I will send you the current weather at the point you send to me",
                parseMode: ParseMode.MarkdownV2);
    return;
}

static async Task HelpHandler(ITelegramBotClient bot, Update update)
{
    for (int i = 1; i <= 3; i++)
    {
        string path = $"{i}.jpg";
        await SendPhoto(bot, update, path);
    }
    await bot.SendTextMessageAsync(
        chatId: update.Message!.Chat.Id,
        text: "To send me the geolocation, you should:\n1\\. Click `Attachment` button near the message input\n2\\. Choose `geolocation` option\n3\\. Select desired location and click `send` button",
        parseMode: ParseMode.MarkdownV2);
    return;
}

static async Task SendPhoto(ITelegramBotClient bot, Update update, string path)
{
    InputOnlineFile input;
    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
        input = new InputOnlineFile(fs);
        await bot.SendPhotoAsync(
            chatId: update.Message!.Chat.Id,
            photo: input);
    }
}

static async Task UnknownCommandHandler(ITelegramBotClient bot, Update update)
{
    await bot.SendTextMessageAsync(
                chatId: update.Message!.Chat.Id,
                text: "I understand only messages with a location, commands `/start` and `/help`\\!",
                parseMode: ParseMode.MarkdownV2);
}

static async Task WeatherNotReceivedHandler(ITelegramBotClient bot, Update update, string uri, string json)
{
    await bot.SendTextMessageAsync(
                chatId: update.Message!.Chat.Id,
                text: "Oops... Something went wrong while getting weather. Try again later!");
    System.IO.File.WriteAllText($"{DateTime.Now.ToString("ddMMyyyyhhmmss")}-log.log", $"{uri}\n\n\n\n{json}");
}

client.StartReceiving(
    updateHandler: OnUpdates,
    pollingErrorHandler: OnErrors,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token);

Console.ReadLine();
