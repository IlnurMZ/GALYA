using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace GALYA
{
    internal class AdminQuery
    {
        ITelegramBotClient _botClient;
        AdminMenu _adminMenu;
        Admin _admin;               
        Stack<Func<Message, Task>> _tasks; // определяет способ обработки входных данных
        public long ChatId { get; set; }
        public AdminQuery(Admin admin, ITelegramBotClient botClient, Chat chat)
        {
            _admin = admin;
            _botClient = botClient;
            ChatId = chat.Id;
            _adminMenu = new AdminMenu();                    
            _tasks = new Stack<Func<Message, Task>>();
            _tasks.Push(CheckPassword); // забиваем первоначальное состояние на проверку пароля
        }

        async Task AddFullDayAsync(Message message)
        {
            DateTime newTime = DateTime.Now;
            try
            {
                newTime = DateTime.Parse(message.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _botClient.SendTextMessageAsync(ChatId, "Ошибка добавления времени");
                return;
            }

            if (!DataBaseInfo.FreeEntry.Any(d => d.Day == newTime.Day))
            {
                newTime = newTime.AddMinutes(480); // начинаем день с 8 часов                
                for (int i = 0; i < 8; i++)
                {
                    DataBaseInfo.FreeEntry.Add(newTime);
                    newTime = newTime.AddMinutes(30);
                }

                newTime = newTime.AddMinutes(60); // 60 мин. отдыха
                for (int i = 0; i < 8; i++)
                {
                    DataBaseInfo.FreeEntry.Add(newTime);
                    newTime = newTime.AddMinutes(30);
                }
                DataBaseInfo.FreeEntry.Sort();
                await _botClient.SendTextMessageAsync(ChatId, "Добавлено");
            }
            else
            {
                await _botClient.SendTextMessageAsync(ChatId, "Такое время уже существует");
            }
        }

        async Task AddEntryAsync(Message message)
        {
            DateTime newTime = DateTime.Now;
            try
            {
                newTime = DateTime.Parse(message.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await _botClient.SendTextMessageAsync(ChatId, "Ошибка добавления времени");
                return;
            }

            if (!DataBaseInfo.FreeEntry.Any(d => d == newTime))
            {
                DataBaseInfo.FreeEntry.Add(newTime);
                await _botClient.SendTextMessageAsync(ChatId, "Добавлено");
                DataBaseInfo.FreeEntry.Sort();
            }
            else
            {
                await _botClient.SendTextMessageAsync(ChatId, "Такое время уже существует");
            }
        }
        async Task CheckPassword(Message message)
        {
            if (message.Text == _admin.Password)
            {
                await _botClient.SendTextMessageAsync(ChatId, "Пароль введен успешно =)");
                _admin.IsFinished = false;
                //_admin.IsChecked = true;
                ReplyKeyboardMarkup keyboard = _adminMenu.StartMenuKeyboard();
                await _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: keyboard);
            }
            else
            {
                await _botClient.SendTextMessageAsync(ChatId, "Пароль введен неверно =(. Попробуйте еще раз");
                _tasks.Push(CheckPassword);
            }
        }       

        internal async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {            
            string str = callbackQuery.Data;
            string[] strInfo = str.Split(" ");
            InlineKeyboardMarkup keyboard;
            switch (strInfo[0])
            {
                case "MenuDays":

                    keyboard = strInfo.Length == 1 ? _adminMenu.DaysOfMonthMenuKeyboard() : _adminMenu.DaysOfMonthMenuKeyboard(strInfo[1]); // исправил
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);           
                    await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, "Выберете запись", replyMarkup: keyboard);
                    break;

                case "MenuHours":
                    
                    keyboard = _adminMenu.HoursOfDayKeyboard(strInfo[1]);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы выбрали {strInfo[1]}.");
                    await _botClient.EditMessageReplyMarkupAsync(chatId: ChatId, callbackQuery.Message.MessageId, keyboard);
                    break;

                case "SelectedEntry":

                    try
                    {
                        DateTime delTime = DateTime.Parse(strInfo[1] + " " + strInfo[2]);                        
                        DataBaseInfo.FreeEntry.Remove(delTime);                       
                        keyboard = _adminMenu.HoursOfDayKeyboard(strInfo[1]);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, $"Вы удалили время: {delTime.ToString("g")}", replyMarkup: keyboard);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }                   
                    break;

                case "DeleteTime":
                    
                    try
                    {
                        DateTime delTime = DateTime.Parse($"{strInfo[1]} {strInfo[2]}");
                        DataBaseInfo.ClientList.Remove(delTime);
                        DataBaseInfo.FreeEntry.Add(delTime);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.SendTextMessageAsync(ChatId, "Клиент удален");
                    }
                    catch (Exception e)
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.SendTextMessageAsync(ChatId, "Ошибка удаления клиента");
                        Console.WriteLine(e.Message);
                    }                   
                    break;
            }
        }

        internal async Task HandleMessage(Message message)
        {            
            string command = message.Text;            
            InlineKeyboardMarkup InKeyboard;
            ReplyKeyboardMarkup RepKeyboard;

            if (_tasks.Count > 0 && command != "Назад")
            {
                await _tasks.Pop().Invoke(message);
            }
            else
            {
                switch (command)
                {
                    case "Выход":

                        await _botClient.SendTextMessageAsync(ChatId, "Вы вышли!", replyMarkup: new ReplyKeyboardRemove());
                        _admin.IsFinished = true;
                        //_admin.IsChecked = false;
                        _tasks.Push(CheckPassword);
                        break;

                    case "Добавить время":

                        RepKeyboard = _adminMenu.AddTimeMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Какой время вы хотите добавить? ", replyMarkup: RepKeyboard);
                        break;

                    case "Добавить целый день":

                        await _botClient.SendTextMessageAsync(ChatId, "Какой день вы хотите добавить? \nПример: 1.1.2024");
                        _tasks.Push(AddFullDayAsync);
                        break;

                    case "Добавить одну запись":

                        await _botClient.SendTextMessageAsync(ChatId, "Какое время вы хотите добавить? \nПример: 12:00 1.1.2024");
                        _tasks.Push(AddEntryAsync);
                        break;

                    case "Назад":

                        RepKeyboard = _adminMenu.StartMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: RepKeyboard);
                        break;

                    case "Настройки":
                        
                        await _botClient.SendTextMessageAsync(ChatId, "Этот пункт меню находится в разработке");
                        break;

                    case "Новые записи":

                        StringBuilder allEntries2 = new StringBuilder();
                        var newClientsList = DataBaseInfo.ClientList.Where(x => x.Key > DateTime.Now).ToList();
                        foreach (var entrie in newClientsList)
                        {
                            allEntries2.AppendLine($"ФИО: {entrie.Value[0]} - {entrie.Key} \nтел.{entrie.Value[1]} \n");
                        }
                        await _botClient.SendTextMessageAsync(ChatId, allEntries2.ToString());
                        break;

                    case "Старые записи":

                        StringBuilder allEntries = new StringBuilder();
                        var oldClientsList = DataBaseInfo.ClientList.Where(x => x.Key < DateTime.Now).ToList();
                        if (oldClientsList.Count > 0)
                        {
                            foreach (var entrie in oldClientsList)
                            {
                                allEntries.AppendLine($"ФИО: {entrie.Value[0]} - {entrie.Key} \nтел.{entrie.Value[1]} \n");
                            }
                        } 
                        else
                        {
                            allEntries.AppendLine($"Записи отсутствуют");
                        }                        
                        await _botClient.SendTextMessageAsync(ChatId, allEntries.ToString());                        
                        break;            

                    case "Удалить запись":

                        InKeyboard = _adminMenu.ClientsForDeleteKeyboard();
                        if (InKeyboard == null)
                        {
                            await _botClient.SendTextMessageAsync(ChatId, "Нет клиентов на удаление");
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(ChatId, "Выберите клиента", replyMarkup: InKeyboard);
                        }                        
                        break;

                    case "Удалить время":
                       
                        InKeyboard = _adminMenu.DaysOfMonthMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Выберете время ", replyMarkup: InKeyboard);                       
                        break;

                    default:                        
                        await _botClient.SendTextMessageAsync(ChatId, "Галина не знает такой команды =(");
                        break;
                }
            }
        }
    }
}
