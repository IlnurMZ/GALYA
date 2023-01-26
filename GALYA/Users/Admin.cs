using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Calendar = GALYA.Service.Calendar;

namespace GALYA
{
    internal class Admin
    {
        ITelegramBotClient _botClient;
        AdminMenu _adminMenu;        
        Stack<Action<Message>> _tasks; // определяет способ обработки входных данных
        ReplyKeyboardMarkup keyboard;
        EntryRepository _entryRepository;
        ClientRepository _clientRepository;
        Calendar _calendar;
        internal readonly string Password = "123";
        internal bool IsFinished { get; set; } = false;        
        public long ChatId { get; set; }

        public Admin(ITelegramBotClient botClient, Chat chat)
        {
            _botClient = botClient;
            ChatId = chat.Id;
            _adminMenu = new AdminMenu();
            _tasks = new Stack<Action<Message>>();
            _tasks.Push(CheckPassword); // забиваем первоначальное состояние на проверку пароля
            _entryRepository = new EntryRepository();
            _clientRepository = new ClientRepository();
            _calendar = new Calendar();
        }        

        void CheckPassword(Message message)
        {
            if (message.Text == Password)
            {
                _botClient.SendTextMessageAsync(ChatId, "Пароль введен успешно =)");
                IsFinished = false;
                keyboard = _adminMenu.StartMenuKeyboard();
                _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: keyboard);
            }
            else
            {
                _botClient.SendTextMessageAsync(ChatId, "Пароль введен неверно =(. Попробуйте еще раз");
                _tasks.Push(CheckPassword);
            }
        }

        public async Task OnAnswerCallbackQueryAsync(CallbackQuery callbackQuery)
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
                            _entryRepository.RemoveEntry(delTime);
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
                            _clientRepository.RemoveClient(delTime);
                            _entryRepository.AddEntry(delTime);
                            await _botClient.SendTextMessageAsync(ChatId, "Клиент успешно удален");
                            _calendar.DeleteEvent(delTime);
                            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                            await _botClient.SendTextMessageAsync(ChatId, "Добавлено событие в календарь");
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task OnAnswerMessageAsync(Message message)
        {

            string command = message.Text;
            InlineKeyboardMarkup InKeyboard;            

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
                        IsFinished = true;
                        _tasks.Push(CheckPassword);
                        break;

                    case "Добавить время":

                        keyboard = _adminMenu.AddTimeMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Какой время вы хотите добавить? ", replyMarkup: keyboard);
                        break;

                    case "Добавить целый день":

                        await _botClient.SendTextMessageAsync(ChatId, "Какой день вы хотите добавить? \nПример: 1.1.2024");
                        _tasks.Push(_entryRepository.AddFullDayEntries);
                        break;

                    case "Добавить одну запись":

                        await _botClient.SendTextMessageAsync(ChatId, "Какое время вы хотите добавить? \nПример: 12:00 1.1.2024");
                        _tasks.Push(_entryRepository.AddEntry);
                        break;

                    case "Назад":

                        keyboard = _adminMenu.StartMenuKeyboard();
                        await _botClient.SendTextMessageAsync(ChatId, "Вы можете:", replyMarkup: keyboard);
                        break;

                    case "Настройки":

                        await _botClient.SendTextMessageAsync(ChatId, "Этот пункт меню находится в разработке");
                        break;

                    case "Новые записи":

                        StringBuilder allEntries2 = new StringBuilder();
                        List<ClientDB> actualClientsList = _clientRepository.GetActualClients();
                        if (actualClientsList.Count > 0)
                        {
                            foreach (var client in actualClientsList)
                            {
                                allEntries2.AppendLine($"ФИО: {client.LastName} {client.FirstName} {client.MiddleName} - {client.Entry} \nтел.{client.Phone} \n");
                            }
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(ChatId, "К вам пока еще никто не записался");
                        }               

                        await _botClient.SendTextMessageAsync(ChatId, allEntries2.ToString());
                        break;

                    case "Старые записи":

                        StringBuilder allEntries = new StringBuilder();
                        List<ClientDB> oldClientsList = _clientRepository.GetOldClients();                   

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
