using Newtonsoft.Json;
using prenotameBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// Store bot screaming status
var screaming = false;

// Pre-assign menu text
const string firstMenu = "<b>Menu 1</b>\n\nA beautiful menu with a shiny inline button.";
const string secondMenu = "<b>Menu 2</b>\n\nA better menu with even more shiny inline buttons.";

// Pre-assign button text
const string nextButton = "Next";
const string backButton = "Back";
const string tutorialButton = "Tutorial";


// Build keyboards
InlineKeyboardMarkup firstMenuMarkup = new(InlineKeyboardButton.WithCallbackData(nextButton));
InlineKeyboardMarkup secondMenuMarkup = new(
    new[] {
        new[] { InlineKeyboardButton.WithCallbackData(backButton) },
        new[] { InlineKeyboardButton.WithUrl(tutorialButton, "https://core.telegram.org/bots/tutorial") }
    }
);

ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
{
    new KeyboardButton[] { "Help me" },
    new KeyboardButton[] { "Call me ☎️" },
})
{
    ResizeKeyboard = true
};


var bot = new TelegramBotClient("6620766124:AAFm3ZCB640p7bJ3doOyv4zZQUSdYTx2hxo");
string json = System.IO.File.ReadAllText("commandsJson.json");

List<BotCommand> commands = JsonConvert.DeserializeObject<List<BotCommand>>(json)!;

await bot.SetMyCommandsAsync(commands);

using var cts = new CancellationTokenSource();
HttpClient httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://localhost:5082"),
};

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(
    updateHandler: HandleUpdate,
    pollingErrorHandler: HandleError,
    cancellationToken: cts.Token
);

var me = await bot.GetMeAsync();
// Tell the user the bot is online
Console.WriteLine("Start listening for updates. Press enter to stop");
Console.ReadKey();

// Send cancellation request to stop the bot
cts.Cancel();

// Each time a user interacts with the bot, this method is called
async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
{
    switch (update.Type)
    {
        // A message was received
        case UpdateType.Message:
            await HandleMessage(update.Message!);
            break;

        // A button was pressed
        case UpdateType.CallbackQuery:
            await HandleButton(update.CallbackQuery!);
            break;
    }
}

async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
{
    await Console.Error.WriteLineAsync(exception.Message);
}

async Task HandleMessage(Message msg)
{
    var user = msg.From;
    var text = msg.Text ?? string.Empty;

    if (user is null)
        return;

    // Print to console
    Console.WriteLine($"{user.FirstName} wrote {text}");

    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
        await HandleCommand(user.Id, text);
    }
    else if (screaming && text.Length > 0)
    {
        // To preserve the markdown, we attach entities (bold, italic..)
        await bot.SendTextMessageAsync(user.Id, text.ToUpper(), entities: msg.Entities);
    }
    else
    {   // This is equivalent to forwarding, without the sender's name
        await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
    }
}


async Task HandleCommand(long userId, string command)
{
    string textMessage = "";
    switch (command)
    {
        case "/scream":
            screaming = true;
            break;

        case "/whisper":
            screaming = false;
            break;

        case "/menu":
            await SendMenu(userId);
            break;
        case "/list":
            await GetRequest(userId);
            break;
        case "/start":
        case "/help":
            textMessage =
                        "Benvenuto, in <b>PrenotAme</b> bot, un comodo bot per poter prenotare campi in ogni momento\n\n⚙️ <b>Comandi:</b>\n\n" +
                        "- /help - Visualizza messaggio di aiuto con tutte le funzione del Bot\n" +
                        "- /list - Visualizza le prenotazioni che hai attive\n" +
                        "- /book - Aggiunge una prenotazione\n" +
                        "- /remove - Rimuove una prenotazione\n" +
                        "- /modify - Modifica una prenotazione\n" +
                        "- /cancel - Annulla il comando in corso\n";

            await SendMessage(userId, textMessage);
            break;
        case "/cancel":
            textMessage = "👍Operazione annullata.";
            await SendMessage(userId, textMessage);
            break;
    }

    await Task.CompletedTask;
}

async Task SendMessage(long userId, string textMessage)
{
    Message sentMessage = await bot.SendTextMessageAsync(chatId: userId,
     parseMode: ParseMode.Html,
     text: textMessage,
     replyMarkup: replyKeyboardMarkup);
}

async Task GetRequest(long userId)
{
    try
    {
        using HttpResponseMessage response = await httpClient.GetAsync("api/v1/commands");
        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine(jsonResponse);
        List<Prenotazione> data = JsonConvert.DeserializeObject<List<Prenotazione>>(jsonResponse) ?? throw new ArgumentException("Nessuna prenotazione trovata");

        string text = "";
        if (data.Count > 0)
        {
            foreach (Prenotazione item in data)
            {
                //Console.WriteLine($"{item.id} - {item.nomePrenotazione}");
                text += "" + item.Id + " - " + item.NomePrenotazione + "\n";
            }
        }
        else
        {
            text = "Nessuna Prenotazione Trovata";
        }
        await SendMessage(userId, text);
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine(e.Message);
        await SendMessage(userId, e.Message);
    }
    catch (ArgumentException e)
    {
        Console.WriteLine(e.Message);
        await SendMessage(userId, e.Message);
    }
}

async Task SendMenu(long userId)
{
    await bot.SendTextMessageAsync(
        userId,
        firstMenu,
        replyMarkup: secondMenuMarkup
    );
}

async Task HandleButton(CallbackQuery query)
{
    string text = string.Empty;
    InlineKeyboardMarkup markup = new(Array.Empty<InlineKeyboardButton>());

    if (query.Data == nextButton)
    {
        text = secondMenu;
        markup = secondMenuMarkup;
    }
    else if (query.Data == backButton)
    {
        text = firstMenu;
        markup = firstMenuMarkup;
    }

    // Close the query to end the client-side loading animation
    await bot.AnswerCallbackQueryAsync(query.Id);

    // Replace menu text and keyboard
    await bot.EditMessageTextAsync(
        query.Message!.Chat.Id,
        query.Message.MessageId,
        text,
        ParseMode.Html,
        replyMarkup: markup
    );
}