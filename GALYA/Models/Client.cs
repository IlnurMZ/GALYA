using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Calendar = GALYA.Service.Calendar;

namespace GALYA
{
    public class Client
    {      
        public long ChatId { get; set; }
        ITelegramBotClient _botClient;
        ClientMenu _clientMenu;        
        DateTime _entryDate;
        Stack<Action<Message>> taskStack;
        EntryRepository _entryRepository;
        ClientRepository _clientRepository;
        public Client(ITelegramBotClient botClient, Chat chat)
        {            
            _botClient = botClient;
            ChatId = chat.Id;
            _clientMenu = new ClientMenu();
            taskStack = new Stack<Action<Message>>();
            _entryRepository = new EntryRepository();
            _clientRepository = new ClientRepository();
        }

        public async Task OnAnswerCallbackQueryAsync(CallbackQuery callbackQuery)
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

                    _entryDate = DateTime.Parse(strInfo[1] + " " + strInfo[2]);
                    await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, $"Вы выбрали {_entryDate.ToString("g")}. \n" +
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

        public async Task OnAnswerMessageAsync(Message message)
        {            
            string str = message.Text;

            if (taskStack.Count > 0)
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

            ClientDB _client = new ClientDB() { Entry = _entryDate, LastName = str[0], FirstName = str[1], MiddleName = str[2], Phone = str[3] };
            try
            {
                _clientRepository.AddClient(_client);
                _entryRepository.RemoveEntry(_entryDate);
                _botClient.SendTextMessageAsync(ChatId, $"{_client.LastName}, Вы записались на {_entryDate.ToString("g")}. Будем ждать! Спасибо!");
            }
            catch (Exception e)
            {
                _botClient.SendTextMessageAsync(ChatId, $"Произошла ошибка записи!");
                Console.WriteLine(e.Message);
            }

            DateTime start = _entryDate;
            DateTime end = _entryDate.AddMinutes(30);
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
            bool isCorrectDate = DateTime.TryParse(data[2] + " " + data[3], out DateTime deleteDate);

            if (!isCorrectDate || deleteDate < DateTime.Now)
            {
                _botClient.SendTextMessageAsync(ChatId, $"Актуальные записи не обнаружены");
                return;
            }

            try
            {
                ClientDB findClient = _clientRepository.GetClient(deleteDate, firstName, lastName);
                if (findClient == null)
                {
                    _botClient.SendTextMessageAsync(ChatId, "Такого клиента не существует");
                    return;
                }
                _clientRepository.RemoveClient(deleteDate, firstName, lastName);
                _entryRepository.AddEntry(deleteDate);
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
