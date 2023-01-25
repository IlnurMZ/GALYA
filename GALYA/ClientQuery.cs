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
using GALYA.Model;
using GALYA.Service;
using Calendar = GALYA.Service.Calendar;
using GALYA.Table;
using Npgsql;
using Dapper;
using GALYA.Repositories;

namespace GALYA
{
    public class ClientQuery
    {
        ITelegramBotClient _botClient;       
        ClientMenu _clientMenu;
        Client _client;
        DateTime _myTime;        
        Stack<Action<Message>> taskStack;
        public long ChatId { get; set; }

        public ClientQuery(Client client, ITelegramBotClient botClient, Chat chat)
        {
            _client = client;
            _botClient = botClient;
            ChatId = chat.Id;
            _clientMenu = new ClientMenu();            
            taskStack = new Stack<Action<Message>>();
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
                    taskStack.Push(MakeEntry);
                    break;

                case "Unsubcribe":

                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "");
                    await _botClient.SendTextMessageAsync(ChatId, "Напишите фамилию, имя и время записи \n" +
                        "Например: Иванов Иван 25.11.2022 15:30");                    
                    taskStack.Push(DeleteEntry);
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
                taskStack.Pop().Invoke(message);
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

        void MakeEntry(Message message)
        {
            string[] str = message.Text.Split(" ");
            if (str.Count() != 4)
            {
                _botClient.SendTextMessageAsync(chatId: ChatId, $"Данные введены неверно! Пример: Иванов Иван Иванович 89999999999");
                return;
            }

            ClientDB _client = new ClientDB() { Entry = _myTime, LastName = str[0], FirstName = str[1], MiddleName = str[2], Phone = str[3] };
            try
            {
                ClientRepository.AddClient(_client);
                EntryRepository.RemoveEntry(_myTime);
                _botClient.SendTextMessageAsync(ChatId, $"{_client.LastName}, Вы записались на {_myTime.ToString("g")}. Будем ждать! Спасибо!");            
            }
            catch (Exception e)
            {
                _botClient.SendTextMessageAsync(ChatId, $"Произошла ошибка записи!");
                Console.WriteLine(e.Message);
            }

            DateTime start = _myTime;
            DateTime end = _myTime.AddMinutes(30);
            Calendar.AddEvent($"{str[0]} {str[1]}", "Описание", start, end);           
        }

        void DeleteEntry(Message message)
        {
            string str = message.Text;
            int countWords = str.Split(" ").Length;
            if (string.IsNullOrWhiteSpace(str) || countWords != 4)
            {
                 _botClient.SendTextMessageAsync(ChatId, $"Данные введены неверно!");
                return;
            }

            string[] data = str.Split(" "); 
            string lastName = data[0];
            string firstName = data[1];            
            DateTime deleteDate;
            bool isCorrectDate = DateTime.TryParse(data[2] + " " + data[3], out deleteDate); 

            if (!isCorrectDate || deleteDate < DateTime.Now)
            {
                 _botClient.SendTextMessageAsync(ChatId, $"Актуальные записи не обнаружены");
                return;
            }
            
            try
            {
                ClientDB findClient = ClientRepository.GetClient(deleteDate, firstName, lastName);
                if (findClient == null)
                {                    
                    _botClient.SendTextMessageAsync(ChatId, "Такого клиента не существует");
                    return;
                }
                ClientRepository.RemoveClient(deleteDate, firstName, lastName);
                EntryRepository.AddEntry(deleteDate);
                _botClient.SendTextMessageAsync(ChatId, "Вы успешно удалились");
                Calendar.DeleteEvent(deleteDate);        
            }
            catch (Exception e)
            {
                _botClient.SendTextMessageAsync(ChatId, "Произошла ошибка удаления. Попробуйте еще раз");
                Console.WriteLine(e.Message);
            }          
            
        }
    }
}
