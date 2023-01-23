using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Globalization;
using GALYA.Keyboard;

namespace GALYA
{
    public class ClientQuery
    {
        ITelegramBotClient _botClient;       
        ClientMenu _clientMenu;
        Client _client;
        DateTime _myTime;        
        Stack<Func<Message, Task>> taskStack; // определяет способ обработки входных данных
        public long ChatId { get; set; }

        public ClientQuery(Client client, ITelegramBotClient botClient, Chat chat)
        {
            _client = client;
            _botClient = botClient;
            ChatId = chat.Id;
            _clientMenu = new ClientMenu();            
            taskStack = new Stack<Func<Message, Task>>();
        }      

        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            string str = callbackQuery.Data;
            string[] strInfo = str.Split(" ");
            InlineKeyboardMarkup keyboard;
            switch (strInfo[0])
            {
                case "MenuDays":

                    keyboard = strInfo.Length == 1 ? _clientMenu.DaysOfMonthMenuKeyboard() : _clientMenu.DaysOfMonthMenuKeyboard(strInfo[1]);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, "Выберете свободную запись", replyMarkup: keyboard);                  
                    break;

                case "MenuHours":
                    
                    keyboard = _clientMenu.HoursOfDayKeyboard(strInfo[1]);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы выбрали {strInfo[1]}.");
                    await _botClient.EditMessageReplyMarkupAsync(chatId: ChatId, callbackQuery.Message.MessageId, keyboard);
                    break;

                case "SelectedEntry":

                    _myTime = DateTime.Parse(strInfo[1] + " " + strInfo[2]);                   
                    await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, $"Вы выбрали {_myTime.ToString("g")}. \n" +
                            $"Напишите полность фамилию, имя, отчество и телефон для связи, например: Иванов Иван Иванович 89999999999");
                    taskStack.Push(MakeEntryAsync);
                    break;

                case "Unsubcribe":

                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "");
                    await _botClient.SendTextMessageAsync(ChatId, "Напишите фамилию, имя и время записи \n" +
                        "Например: Иванов Иван 25.11.2022 15:30");                    
                    taskStack.Push(DeleteEntryAsync);
                    break;

                case "BackMainMenu":
                    
                    keyboard = _clientMenu.StartMenuKeyboard();
                    await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, "Вы можете:", replyMarkup: keyboard);
                    break;
            }
        }
       
        public async Task HandleMessage(Message message)
        {
            string str = message.Text;

            if (taskStack.Count> 0)
            {
                await taskStack.Pop().Invoke(message);
            } 
            else
            {
                switch (str)
                {
                    case "/start":

                        InlineKeyboardMarkup keyboard = _clientMenu.StartMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: keyboard);
                        break;

                    default:

                        await _botClient.SendTextMessageAsync(ChatId, "Галина еще не знает такой команды");
                        break;
                }
            }            
        }

        async Task MakeEntryAsync(Message message)
        {
            string[] str = message.Text.Split(" ");
            if (str.Count() != 4)
            {
                await _botClient.SendTextMessageAsync(chatId: ChatId, $"Данные введены неверно! Пример: Иванов Иван Иванович 89999999999");
                return;
            }
            _client.FIO = $"{str[0]} {str[1]} {str[2]}";
            _client.PhoneNumber = str[3];            
            DateTime start = _myTime;
            DateTime end = _myTime.AddMinutes(30);

            Calendar.AddEvent($"{str[0]} {str[1]}", "Описание", start, end);
            await _botClient.SendTextMessageAsync(chatId: ChatId, $"{_client.FIO}, Вы записались на {_myTime.ToString("g")}. Будем ждать! Спасибо!");
            DataBaseInfo.ClientList.Add(start, new string[] { _client.FIO, _client.PhoneNumber });
            DataBaseInfo.FreeEntry.Remove(start);
        }

        async Task DeleteEntryAsync(Message message)
        {
            string str = message.Text;
            int countWords = str.Split(" ").Length;
            if (string.IsNullOrWhiteSpace(str) || countWords != 4)
            {
                await _botClient.SendTextMessageAsync(ChatId, $"Данные введены неверно!");
                return;
            }

            string[] data = str.Split(" ");
            string surName = $"{data[0]} {data[1]}";
            DateTime deleteDate;
            bool isCorrectDate = DateTime.TryParse(data[2] + " " + data[3], out deleteDate);

            if (!isCorrectDate || deleteDate < DateTime.Now)
            {
                await _botClient.SendTextMessageAsync(ChatId, $"Актуальные записи не обнаружены");
                return;
            }

            if (DataBaseInfo.ClientList.ContainsKey(deleteDate))
            {
                string[] FIO2 = DataBaseInfo.ClientList[deleteDate][0].Split(" ");
                if ($"{FIO2[0]} {FIO2[1]}" == surName)
                {                    
                    string FIO = $"{FIO2[0]} {FIO2[1]} {FIO2[2]}";
                    Calendar.DeleteEvent(deleteDate);                    
                    DataBaseInfo.ClientList.Remove(deleteDate);
                    DataBaseInfo.FreeEntry.Add(deleteDate);
                    DataBaseInfo.FreeEntry.Sort();
                    await _botClient.SendTextMessageAsync(ChatId, $"Запись успешно удалена");                   
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(ChatId, $"Такое время записи отсутствует!");
                return;
            }
        }
    }
}
