using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using GALYA.Model;
using GALYA.Keyboard;
using Npgsql;
using Dapper;
using GALYA.Table;

namespace GALYA
{
    internal class AdminQuery
    {
        ITelegramBotClient _botClient;
        AdminMenu _adminMenu;
        Admin _admin;               
        Stack<Action<Message>> _tasks; // определяет способ обработки входных данных
        public long ChatId { get; set; }
        public AdminQuery(Admin admin, ITelegramBotClient botClient, Chat chat)
        {
            _admin = admin;
            _botClient = botClient;
            ChatId = chat.Id;
            _adminMenu = new AdminMenu();                    
            _tasks = new Stack<Action<Message>>();
            _tasks.Push(CheckPassword); // забиваем первоначальное состояние на проверку пароля
        }

        void AddFullDayAsync(Message message)
        {            
            if (!DateTime.TryParse(message.Text, out DateTime newTime))
            {                
                throw new ArgumentException("Ошибка добавления времени");
            }           

            List<DateTime> entries = AdminMenu.GetEntries();

            if (!entries.Any(d => d.Day == newTime.Day))
            {
                StringBuilder strValues = new StringBuilder();               
                newTime = newTime.AddMinutes(480);
                // начинаем день с 8 часов                
                for (int i = 0; i < 8; i++)
                {
                    strValues.Append("(\'" + newTime.ToString() + "\'),");                    
                    newTime = newTime.AddMinutes(30);
                }

                newTime = newTime.AddMinutes(60); // 60 мин. отдыха
                for (int i = 0; i < 7; i++)
                {
                    strValues.Append("(\'" + newTime.ToString() + "\'),");                    
                    newTime = newTime.AddMinutes(30);
                }
                strValues.Append("(\'" + newTime.ToString() + "\')");

                try
                {
                    using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                    {
                        string sql = $"insert into free_entries (entry) values {strValues}";
                        connection.Execute(sql);
                    }                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }                
                _botClient.SendTextMessageAsync(ChatId, "Добавлено");
            }
            else
            {
                _botClient.SendTextMessageAsync(ChatId, "Такое время уже существует");
            }
        }
      
        //public void Add1(DateTime entry)
        //{
        //    using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
        //    {
        //        string sql = $"insert into free_entries (entry) values (@time)";
        //        connection.Execute(sql, new { time = entry });                
        //    }
        //}

        void AddEntryAsync(Message message)
        {
            DateTime newTime = DateTime.Now;
            try
            {
                newTime = DateTime.Parse(message.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _botClient.SendTextMessageAsync(ChatId, "Ошибка добавления времени");
                return;
            }

            try
            {
                using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                {
                    string sql = $"insert into free_entries (entry) values (@time)";
                    connection.Execute(sql, new { time = newTime });
                }                
                _botClient.SendTextMessageAsync(ChatId, "Добавлено");
            }
            catch (Exception e)
            {
                _botClient.SendTextMessageAsync(ChatId, "Ошибка добавления записи. Возможно запись уже существует");
                Console.WriteLine(e.Message);
            }           
        }
        void CheckPassword(Message message)
        {
            if (message.Text == _admin.Password)
            {
                _botClient.SendTextMessageAsync(ChatId, "Пароль введен успешно =)");
                _admin.IsFinished = false;                
                ReplyKeyboardMarkup keyboard = _adminMenu.StartMenuKeyboard();
                _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: keyboard);
            }
            else
            {
                _botClient.SendTextMessageAsync(ChatId, "Пароль введен неверно =(. Попробуйте еще раз");
                _tasks.Push(CheckPassword);
            }
        }       

        internal async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {            
            string str = callbackQuery.Data;
            string[] strInfo = str.Split(" ");
            InlineKeyboardMarkup keyboard;
            try
            {
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

                            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                            {
                                string sql = $"delete from free_entries where entry = (@time);";
                                connection.Execute(sql, new { time = delTime });
                            }
                            keyboard = _adminMenu.HoursOfDayKeyboard(strInfo[1]);
                            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                            await _botClient.EditMessageTextAsync(ChatId, callbackQuery.Message.MessageId, $"Вы удалили время: {delTime.ToString("g")}", replyMarkup: keyboard);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;

                    case "DeleteClient":

                        try
                        {
                            DateTime delTime = DateTime.Parse($"{strInfo[1]} {strInfo[2]}");

                            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                            {
                                string sql1 = $"delete from client_list where entry = (@time);";
                                connection.Execute(sql1, new { time = delTime });

                                string sql2 = $"insert into free_entries (entry) values (@time)";
                                connection.Execute(sql2, new { time = delTime });
                            }
                            
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
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
        }

        internal async Task HandleMessage(Message message)
        {            
            string command = message.Text;            
            InlineKeyboardMarkup InKeyboard;
            ReplyKeyboardMarkup RepKeyboard;

            if (_tasks.Count > 0 && command != "Назад")
            {
                _tasks.Pop().Invoke(message);
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
                        try
                        {
                            _tasks.Push(AddFullDayAsync);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }                        
                        break;

                    case "Добавить одну запись":

                        await _botClient.SendTextMessageAsync(ChatId, "Какое время вы хотите добавить? \nПример: 12:00 1.1.2024");
                        try
                        {
                            _tasks.Push(AddEntryAsync);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                        }
                        
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
                        List<ClientDB> actualClientsList;

                        var query2 = @" select
                            firstname, lastname, middlename, phone, entry
                            from client_list
                            where entry > @date
                            order by (entry) asc";

                        using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                        {
                            actualClientsList = connection.Query<ClientDB>(query2, new { date = DateTime.Now }).ToList();
                        }
                        
                        foreach (var client in actualClientsList)
                        {
                            allEntries2.AppendLine($"ФИО: {client.LastName} {client.FirstName} {client.MiddleName} - {client.Entry} \nтел.{client.Phone} \n");
                        }
                        await _botClient.SendTextMessageAsync(ChatId, allEntries2.ToString());
                        break;

                    case "Старые записи":

                        StringBuilder allEntries = new StringBuilder();                        
                        List<ClientDB> oldClientsList;

                        var query = @" select
                            firstname, lastname, middlename, phone, entry
                            from client_list
                            where entry < @date
                            order by (entry) asc";

                        using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
                        {
                            oldClientsList = connection.Query<ClientDB>(query, new { date = DateTime.Now }).ToList();                            
                        }
                       
                        if (oldClientsList.Count > 0)
                        {
                            foreach (var client in oldClientsList)
                            {
                                allEntries.AppendLine($"ФИО: {client.LastName} {client.FirstName} {client.MiddleName} - {client.Entry} \nтел.{client.Phone} \n");
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
