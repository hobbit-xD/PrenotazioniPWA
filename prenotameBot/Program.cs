using System.Globalization;
using Newtonsoft.Json;
using prenotameBot;
using prenotameBot.Models;
using prenotameBot.SyncDataServices.Http;
using Telegram.Bot;
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
InlineKeyboardMarkup emptyMarkup = new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>());
InlineKeyboardMarkup firstMenuMarkup = new(InlineKeyboardButton.WithCallbackData(nextButton));
InlineKeyboardMarkup secondMenuMarkup = new(
    new[] {
        new[] { InlineKeyboardButton.WithCallbackData(backButton) },
        new[] { InlineKeyboardButton.WithUrl(tutorialButton, "https://core.telegram.org/bots/tutorial") }
    }
);

InlineKeyboardMarkup secondMenuMarkup2 = new(
    new[] {
         new[] { InlineKeyboardButton.WithCallbackData(backButton) },

    }
);

ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
{
    new KeyboardButton[] { "➕Prenota","➖Lista"  },
    new KeyboardButton[] { "🗑️Rimuovi","❓Aiuto" },
})
{
    ResizeKeyboard = true
};


var bot = new TelegramBotClient("");
string json = System.IO.File.ReadAllText("commandsJson.json");

List<BotCommand> commands = JsonConvert.DeserializeObject<List<BotCommand>>(json)!;
Message msg = new Message();
await bot.SetMyCommandsAsync(commands);

using var cts = new CancellationTokenSource();
HttpClient httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://localhost:5082/api/v1/"),
};

IPrenotazioneDataClient dataClient = new HttpDataClient(httpClient);
PrenotazioneCreate prenotazione = new PrenotazioneCreate();

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
        Console.WriteLine(update.InlineQuery);
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

    if (text.Substring(1).Trim().Equals("Prenota"))
    {
        text = "/book";
    }
    else if (text.Substring(1).Trim().Equals("Aiuto"))
    {
        text = "/help";
    }
    else if (text.Substring(1).Trim().Equals("Lista"))
    {
        text = "/list";
    }

    Console.WriteLine(text);

    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
        await HandleCommand(user, text);
    }
    else if (screaming && text.Length > 0)
    {
        // To preserve the markdown, we attach entities (bold, italic..)

        await PostRequest(user.Id);
        //await bot.SendTextMessageAsync();

        //await bot.SendTextMessageAsync(user.Id, text.ToUpper(), entities: msg.Entities);
    }
    else
    {   // This is equivalent to forwarding, without the sender's name
        await bot.CopyMessageAsync(user.Id, user.Id, msg.MessageId);
    }
}


async Task HandleCommand(User user, string command)
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
        case "/book":
            msg = await Book(user);
            break;
        case "/list":
            await GetRequest(user.Id);
            break;
        case "/start":
            await StartMenu(user);
            break;
        case "/help":
            textMessage =
                        "⚙️ <b>Comandi:</b>\n\n" +
                        "- /help - Visualizza messaggio di aiuto con tutte le funzione del Bot\n" +
                        "- /list - Visualizza le prenotazioni che hai attive\n" +
                        "- /book - Aggiunge una prenotazione\n" +
                        "- /remove - Rimuove una prenotazione\n" +
                        "- /cancel - Annulla il comando in corso\n";

            await SendMessage(user.Id, textMessage);
            break;
        case "/cancel":
            
            textMessage = "👍Operazione annullata.";
            Console.WriteLine(msg.MessageId);
            //await SendMessage(user.Id, textMessage);
            await bot.EditMessageTextAsync(
                    chatId: msg!.Chat.Id,
                    messageId: msg.MessageId,
                    text: textMessage,
                    parseMode: ParseMode.Html,
                    replyMarkup: emptyMarkup
                );
            break;
    }

    await Task.CompletedTask;
}


async Task<Message> Book(User user)
{
    var startDate = DateTime.Today;
    var endDate = startDate.AddDays(7);
    //the number of days in our range of dates
    var numDays = (int)(endDate - startDate).TotalDays;
    List<DateTime> myDates = Enumerable
               //creates an IEnumerable of ints from 0 to numDays
               .Range(0, numDays)
               //now for each of those numbers (0..numDays), 
               //select startDate plus x number of days
               .Select(x => startDate.AddDays(x))
               //and make a list
               .ToList();

    InlineKeyboardButton[][] listButton = new InlineKeyboardButton[myDates.Count][];
    int i = 0;
    foreach (DateTime day in myDates)
    {
        listButton[i] = new[] { InlineKeyboardButton.WithCallbackData($"{day.ToString("dd/MM/yyyy")} ({day.ToString("dddd", new CultureInfo("it-IT"))})", $"date_{day.ToString("dd/MM/yyyy")}") };
        i++;
    }

    InlineKeyboardMarkup dayMarkup = new InlineKeyboardMarkup(listButton);
    Message message = await bot.SendTextMessageAsync(
        chatId: user.Id,
        text: "🗓️ Scegli il giorno per cui vuoi effettuare la prenotazione.\n\nPer annullare la prenotazione /cancel",
        parseMode: ParseMode.Html,
        replyMarkup: dayMarkup
    );
    return message;
}


async Task SendMessage(long userId, string textMessage)
{
    Message sentMessage = await bot.SendTextMessageAsync(chatId: userId,
     parseMode: ParseMode.Html,
     text: textMessage
     );
}
async Task PostRequest(long userId)
{
    await dataClient.SendPrenotazione(prenotazione);
}
async Task GetRequest(long userId)
{
    try
    {
        string jsonResponse = await dataClient.GetPrenotazioni();

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

async Task StartMenu(User user)
{
    string textMessage = $"👋Ciao {user.FirstName}, benvenuto in <b>PrenotAme</b>, un comodo bot per poter prenotare campi in ogni momento 🎾\n\n";
    textMessage += "✅ Le cose da sapere:\n\n";
    textMessage += "1️⃣ Per prenotare un campo, premi ➕<b>Prenota</b> qua sotto.\n";
    textMessage += "2️⃣ Se non capisci qualcosa o hai un problema con il bot, premi ❓<b>Aiuto</b>.\n";

    await bot.SendTextMessageAsync(
        chatId: user.Id,
        text: textMessage,
        parseMode: ParseMode.Html,
        replyMarkup: replyKeyboardMarkup
    );
}

async Task HandleButton(CallbackQuery query)
{
    string text = string.Empty;
    Console.WriteLine("Handle button");
    Console.WriteLine($"{query.Data}");



    if (query.Data!.StartsWith("date_"))
    {
        // text = secondMenu;
        //markup = firstMenuMarkup;
        Console.WriteLine($"Siamo dove dobbiamo cercare orari liberi - {query.Data.Substring(5)}");
        string jsonResponse = await dataClient.GetPrenotazioniByDate($"{query.Data.Substring(5)}");
        Console.WriteLine(jsonResponse);
        prenotazione.InizioPrenotazione=DateTime.Parse(query.Data.Substring(5));
        prenotazione.FinePrenotazione=DateTime.Parse(query.Data.Substring(5));
        text = $"Per il giorno <b>{query.Data.Substring(5)}</b> le fasce orarie disponibili sono:\n\n🕛 Inserisci l' orario desiderato nel formato HH:MM - HH:MM (es. 10:00 - 10:30)\n";
    }
    else if (query.Data == backButton)
    {
        text = firstMenu;
        //markup = firstMenuMarkup;
    }

    //Close the query to end the client-side loading animation
    await bot.AnswerCallbackQueryAsync(query.Id);

    // Replace menu text and keyboard
    await bot.EditMessageTextAsync(
        chatId: query.Message!.Chat.Id,
        messageId: query.Message.MessageId,
        text: text,
        parseMode: ParseMode.Html,
        replyMarkup: emptyMarkup
    );
}