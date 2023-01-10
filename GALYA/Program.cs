using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Net;

namespace GALYA
{
    internal class Program
    {
        static readonly string _token = "ops"; /// "5510668039:AAFY6YQPZY2XDcqxfVRZjphccmrcfIKHs4o";
        static List<Client> _clients = new List<Client>();
        static List<Admin> _admins = new List<Admin>();
        static async Task Main(string[] args)
        {
            var botClient = new TelegramBotClient(_token); // для соединения с Телеграммом
            var me = await botClient.GetMeAsync(); // запрос информации о боте

            Console.WriteLine($"My name is {me.Username} {me.Id}");
            
            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync,
                new Telegram.Bot.Polling.ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                });
            Console.ReadKey();
        }

        // Обработка ошибок
        private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cts)
        {
            var ErrorMessage = exception.Message;
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
       
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cts)
        {
            try
            {
                switch (update.Type)
                {                    
                    case UpdateType.Message:
                        await BotOnMessageReceived(botClient, update.Message);
                        break;
                    case UpdateType.CallbackQuery:
                        await BotOnCallbackQueryRecevied(botClient, update.CallbackQuery);
                        break;
                }

            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cts);
            }
        }

        private static async Task BotOnCallbackQueryRecevied(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (callbackQuery == null)
            {
                return;
            }
            var client = _clients.Find(x => x.ChatId == callbackQuery.Message.Chat.Id);
            var admin = _admins.Find(x => x.ChatId == callbackQuery.Message.Chat.Id);
            
            if (client == null && admin == null)
            {
                return;
            }
           
            if (client != null)
            {               
                await client.OnAnswerCallbackQueryAsync(callbackQuery);
                return;
            }
            if (admin != null && !admin.IsFinished)
            {
                await admin.OnAnswerCallbackQueryAsync(callbackQuery);
                return;
            }
        }
        
        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            
            if (message.Type != MessageType.Text)
            {
                return;
            }

            var action = message.Text;

            switch (action)
            {
                case "/start": // для обычных пользователей                    
                    await StartClientAsync(botClient, message);
                    break;

                case "/admin": // команда для администратора
                    await CheckAuthorisationAsync(botClient, message);
                    break;

                default:

                    var client = _clients.Find(x => x.ChatId == message.Chat.Id);

                    if (client != null)
                    {
                        await client.OnAnswerMessageAsync(message);
                        break;                        
                    }

                    var admin = _admins.Find(x => x.ChatId == message.Chat.Id);
                    
                    if (admin != null && !admin.IsFinished)
                    {                       
                        await admin.OnAnswerMessageAsync(message);
                    }                    
                    break;
            }
        }

        private static async Task CheckAuthorisationAsync(ITelegramBotClient botClient, Message message)
        {
            var admin = _admins.Find(x => x.ChatId == message.Chat.Id);
            var client = _clients.Find(x => x.ChatId == message.Chat.Id);
            if (admin == null)
            {
                admin = new Admin(botClient, message.Chat);                
                _admins.Add(admin);

                if (client != null) 
                {
                    _clients.Remove(client); // для выхода из режима клиента
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Введите пароль");
            }
            else if (admin != null && !admin.IsFinished)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Вы уже авторизованы");
            }
            else if (admin != null && admin.IsFinished)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Введите пароль");
                admin.IsFinished = false;
            }
        }
        
        private static async Task StartClientAsync(ITelegramBotClient botClient, Message message)
        {
            var admin = _admins.Find(x => x.ChatId == message.Chat.Id);
            var client = _clients.Find(x => x.ChatId == message.Chat.Id);
            
            if (client == null)
            {
                client = new Client(botClient, message.Chat);
                _clients.Add(client);
                if (admin != null)
                {
                    _admins.Remove(admin); // для выхода из режима админа
                }

                var user = $"{message.From.LastName} {message.From.FirstName}";
                await botClient.SendTextMessageAsync(message.Chat.Id, $"Здравствуйте, {user}! Чем могу помочь?");
            }                       
            await client.OnAnswerMessageAsync(message);
        }
    }
}