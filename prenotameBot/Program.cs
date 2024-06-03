using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using prenotameBot;
using prenotameBot.Models;
using prenotameBot.SyncDataServices.Http;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


var isTime = false;
const string regexValue = @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]\s?-\s?([0-1]?[0-9]|2[0-3]):[0-5][0-9]$";


// Build keyboards
InlineKeyboardMarkup emptyMarkup = new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>());

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
List<TimeBlock> freePrenotazioni = new List<TimeBlock>();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool, so we use cancellation token
bot.StartReceiving(
    updateHandler: HandleUpdate,
    pollingErrorHandler: HandleError,
    cancellationToken: cts.Token
);

var me = await bot.GetMeAsync();
Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");
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
    cts.Cancel();
}

async Task HandleMessage(Message msg)
{
    var user = msg.From;
    var text = msg.Text ?? string.Empty;

    if (user is null)
        return;

    // Print to console
    Console.WriteLine($"{user.FirstName} ({user.Id}) wrote {text}");

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
    else if (text.Substring(3).Trim().Equals("Rimuovi"))
    {
        text = "/remove";
    }

    isTime = Regex.Match(text, regexValue, RegexOptions.IgnoreCase).Success;
    Console.WriteLine(isTime);


    // When we get a command, we react accordingly
    if (text.StartsWith("/"))
    {
        await HandleCommand(user, text);
    }
    else if (isTime && text.Length > 0)
    {
        string[] orarioPrenotazione = text.Split("-");
        Console.WriteLine(orarioPrenotazione[0]);
        prenotazione.InizioPrenotazione = prenotazione.InizioPrenotazione.AddHours(Double.Parse(orarioPrenotazione[0].Split(":")[0]));
        prenotazione.InizioPrenotazione = prenotazione.InizioPrenotazione.AddMinutes(Double.Parse(orarioPrenotazione[0].Split(":")[1]));

        prenotazione.FinePrenotazione = prenotazione.FinePrenotazione.AddHours(Double.Parse(orarioPrenotazione[1].Split(":")[0]));
        prenotazione.FinePrenotazione = prenotazione.FinePrenotazione.AddMinutes(Double.Parse(orarioPrenotazione[1].Split(":")[1]));
        prenotazione.NomePrenotazione = user.FirstName;
        prenotazione.TelegramUserId = user.Id;
        bool isPrenotazioneSuccess = await dataClient.SendPrenotazione(prenotazione);
        if (isPrenotazioneSuccess)
            await SendMessage(user.Id, "Prenotazione effettuata con successo\n\nPer vedere le tue prenotazioni /list");
        else
            await SendMessage(user.Id, "Prenotazione non andata a buon fine\n\nPer effettuare una nuova prenotazione /book");
    }
    else
    {
        await SendMessage(user.Id, "Comando non riconosciuto");
    }
}

async Task HandleCommand(User user, string command)
{
    try
    {

        string textMessage = "";
        switch (command)
        {
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

            case "/remove":
                msg = await Delete(user.Id);
                break;

        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        await SendMessage(user.Id, e.Message);
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

async Task GetRequest(long userId)
{
    try
    {
        string jsonResponse = await dataClient.GetPrenotazioniByUser(userId);

        List<Prenotazione> data = JsonConvert.DeserializeObject<List<Prenotazione>>(jsonResponse) ?? throw new ArgumentException("Nessuna prenotazione trovata");

        string text = "";
        if (data.Count > 0)
        {
            text = "<b>Nome</b>\t<b>Data</b>\t<b>Ora Inizio</b>\t<b>Ora Fine</b>\n\n";
            foreach (Prenotazione item in data)
            {
                //Console.WriteLine($"{item.id} - {item.nomePrenotazione}");
                text += item.ToString() + "\n\n";
            }
        }
        else
        {
            text = "Nessuna Prenotazione Trovata";
        }
        await SendMessage(userId, text);
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

    try
    {
        string text = string.Empty;
        Console.WriteLine("Handle button");
        Console.WriteLine($"{query.Data}");



        if (query.Data!.StartsWith("date_"))
        {
            // text = secondMenu;
            //markup = firstMenuMarkup;
            Console.WriteLine($"Siamo dove dobbiamo cercare orari liberi - {query.Data.Substring(5)} - {query.From.Id}");
            string jsonResponse = await dataClient.GetPrenotazioniByDate($"{query.Data.Substring(5)}");
            Console.WriteLine(jsonResponse);
            prenotazione.InizioPrenotazione = DateTime.Parse(query.Data.Substring(5));
            prenotazione.FinePrenotazione = DateTime.Parse(query.Data.Substring(5));

            text = $"Per il giorno <b>{query.Data.Substring(5)}</b> le fasce orarie disponibili sono:\n\n";


            List<Prenotazione> prenotazioni = JsonConvert.DeserializeObject<List<Prenotazione>>(jsonResponse);
            prenotazioni.ForEach(p => Console.WriteLine(p.ToString()));
            if (prenotazioni.Count != 0)
            {
                var prenotazioniOrdinate = prenotazioni.OrderBy(p => p.InizioPrenotazione).ToArray();
                freePrenotazioni.Clear();

                Console.WriteLine(prenotazioniOrdinate.Length);
                for (int i = 0; i < prenotazioniOrdinate.Length - 1; i++)
                {
                    freePrenotazioni.Add(new TimeBlock() { Start = prenotazioniOrdinate[i].FinePrenotazione, End = prenotazioniOrdinate[i + 1].InizioPrenotazione });
                }

                Prenotazione primaPrenotazione = prenotazioniOrdinate.First();
                Prenotazione ultimaPrenotazione = prenotazioniOrdinate.Last();

                if (primaPrenotazione.InizioPrenotazione.Hour > 8)
                    freePrenotazioni.Add(new TimeBlock() { Start = DateTime.Parse(query.Data.Substring(5)).AddHours(8), End = primaPrenotazione.InizioPrenotazione });

                if (ultimaPrenotazione.FinePrenotazione.Hour < 23)
                    freePrenotazioni.Add(new TimeBlock() { Start = ultimaPrenotazione.FinePrenotazione, End = DateTime.Parse(query.Data.Substring(5)).AddHours(23) });



                foreach (TimeBlock data in freePrenotazioni)
                {
                    text += data.Start.ToShortDateString() + " " + data.Start.ToShortTimeString() + " - " + data.End.ToShortTimeString() + "\n";
                }
            }
            else
            {

                TimeBlock free = new TimeBlock() { Start = DateTime.Parse(query.Data.Substring(5)).AddHours(8), End = DateTime.Parse(query.Data.Substring(5)).AddHours(23) };
                text += free.Start.ToShortDateString() + " " + free.Start.ToShortTimeString() + " - " + free.End.ToShortTimeString() + "\n";
            }
            text += "\n🕛Inserisci l' orario desiderato nel formato HH:MM - HH:MM (es. 10:00 - 10:30)\n";
        }
        else if (query.Data.StartsWith("id_"))
        {
            Console.WriteLine($"Devo cancellare prenotazione con Id: {query.Data.Substring(3)}");
            bool isDeleted = await dataClient.DeletePrenotazioni(query.Data.Substring(3));
            if (isDeleted)
            {
                text = "Prenotazione cancellata con successo";
            }
            else
            {
                text = "Cancellazione fallita";
            }
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
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        //await SendMessage(userId, e.Message);
    }
}

async Task<Message> Delete(long userId)
{

    string jsonResponse = await dataClient.GetPrenotazioniByUser(userId);
    InlineKeyboardMarkup markup = new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>());
    Message message = new();
    string textMessage = "Nessuna prenotazione trovata";
    List<Prenotazione> data = JsonConvert.DeserializeObject<List<Prenotazione>>(jsonResponse) ?? throw new ArgumentException("Nessuna prenotazione trovata");
    if (data.Count > 0)
    {
        InlineKeyboardButton[][] buttonList = new InlineKeyboardButton[data.Count][];
        for (int i = 0; i < data.Count; i++)
            buttonList[i] = new[] { InlineKeyboardButton.WithCallbackData($"{data[i].ToString()}", $"id_{data[i].Id}") };

        markup = new InlineKeyboardMarkup(buttonList);
        textMessage = "Quale prenotazione vuoi cancellare?\nPer annullare l'operazione /cancel";


    }
    message = await bot.SendTextMessageAsync(
chatId: userId,
text: textMessage,
parseMode: ParseMode.Html,
replyMarkup: markup
);

    return message;


}