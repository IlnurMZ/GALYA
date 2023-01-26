using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GALYA.Repositories;
using GALYA.Table;
using Npgsql;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace GALYA.Keyboard
{
    internal class AdminMenu
    {
        int _year; 
        int _month;
        EntryRepository _entryRepository;
        ClientRepository _clientRepository;
        public AdminMenu()
        {
            _month = DateTime.Now.Month;
            _year = DateTime.Now.Year;
            _entryRepository= new EntryRepository();
            _clientRepository = new ClientRepository();
        }
        internal ReplyKeyboardMarkup StartMenuKeyboard()
        {
            ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                 new[]
                 {
                    new[]
                    {
                        new KeyboardButton("Старые записи"),
                        new KeyboardButton("Новые записи"),
                        new KeyboardButton("Удалить запись")
                    },
                    new []
                    {
                        new KeyboardButton("Добавить время"),
                        new KeyboardButton("Удалить время"),
                        new KeyboardButton("Настройки"),
                    },
                     new []
                    {
                        new KeyboardButton("Выход")
                    },
                 });
            return keyboard;
        }

        internal ReplyKeyboardMarkup AddTimeMenuKeyboard()
        {
            ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(
                 new[]
                 {
                    new[]
                    {
                        new KeyboardButton("Добавить одну запись"),
                    },
                    new[]
                    {
                        new KeyboardButton("Добавить целый день"),
                    },
                    new []
                    {
                        new KeyboardButton("Назад"),
                    }
                 });
            return keyboard;
        }

        internal InlineKeyboardMarkup DaysOfMonthMenuKeyboard(string command = "current")
        {
            var entries_DB = _entryRepository.GetEntries();
            DateTime currentTime = DateTime.Now.AddHours(2); // делаем запась не раньше чем на 2 часа            
            int heigthMenu, widthMenu; // количество строк и столбцов пунктов в меню
            List<DateTime> allActualDays;
            List<DateTime> daysOfMonth;
            bool isNextMonth = false;            
            int dopMenu = 0; // количество дополнительных пунктов меню (след. и пред. месяц)

            if (command == "next")
            {
                _month++; // если содержит запись на следующий месяц
                if (_month == 13)
                {
                    _month = 1;
                    _year++;
                }
            }
            else if (command == "previous")
            {
                _month--; // если содержит запись на предыдущий месяц
                if (_month == 0)
                {
                    _month = 12;
                    _year--;
                }
            }

            allActualDays = entries_DB.Where(d => d.Month == _month && d.Year == _year && d > currentTime).ToList(); // Выбираем все записи нужного месяца 
            daysOfMonth = allActualDays.GroupBy(d => d.Day).Select(g => g.First()).ToList(); // Отбираем только дни               

            if (daysOfMonth.Count % 5 == 0)
                heigthMenu = daysOfMonth.Count / 5;
            else
                heigthMenu = daysOfMonth.Count / 5 + 1;

            int numNextMonth = _month + 1 == 13 ? 1 : _month + 1;
            int numPrevMonth = _month - 1 == 0 ? 12 : _month - 1;
            // Проверка наличия записей на следующий месяц
            if (entries_DB.Any(d => d.Month == numNextMonth && d > DateTime.Now))
            {
                dopMenu++;
                isNextMonth = true;
            }
            // Проверка наличия записей на предыдущий месяц
            if (entries_DB.Any(d => d.Month == numPrevMonth && d > DateTime.Now))
            {
                dopMenu++;
                //isPreviousMonth = true;
            }
            heigthMenu += dopMenu;
            var keyboard = new InlineKeyboardButton[heigthMenu][];

            for (int i = 0; i < heigthMenu - dopMenu; i++)
            {
                // вычисление размерности массива по остатку элементов                
                widthMenu = daysOfMonth.Count - i * 5 >= 5 ? 5 : daysOfMonth.Count - i * 5;
                keyboard[i] = new InlineKeyboardButton[widthMenu];

                for (int j = 0; j < widthMenu; j++)
                {
                    keyboard[i][j] = InlineKeyboardButton.WithCallbackData(
                        "|  " + daysOfMonth[i * 5 + j].ToString("dd.MM") + "  |",
                        "MenuHours " + daysOfMonth[i * 5 + j].ToString("g"));
                }
            }

            if (dopMenu == 1)
            {
                keyboard[heigthMenu - 1] = new InlineKeyboardButton[1];
                if (isNextMonth)
                {
                    keyboard[heigthMenu - 1][0] = InlineKeyboardButton.WithCallbackData(
                                            "|  следующий месяц  |",
                                            "MenuDays " + "next");
                }
                else
                {
                    keyboard[heigthMenu - 1][0] = InlineKeyboardButton.WithCallbackData(
                                        "|  предыдущий месяц  |",
                                        "MenuDays " + "previous");
                }
            }
            else if (dopMenu == 2)
            {
                keyboard[heigthMenu - 2] = new InlineKeyboardButton[1];
                keyboard[heigthMenu - 2][0] = InlineKeyboardButton.WithCallbackData(
                                            "|  следующий месяц  |",
                                            "MenuDays " + "next");

                keyboard[heigthMenu - 1] = new InlineKeyboardButton[1];
                keyboard[heigthMenu - 1][0] = InlineKeyboardButton.WithCallbackData(
                                        "|  предыдущий месяц  |",
                                        "MenuDays " + "previous");
            }
            return new(keyboard);
        }

        internal InlineKeyboardMarkup HoursOfDayKeyboard(string strData)
        {
            var entries_DB = _entryRepository.GetEntries();
            int day = DateTime.Parse(strData).Day;         
            int heigth, width;
            List<DateTime> time = entries_DB.Where(t => t.Month == _month && t.Day == day && t > DateTime.Now.AddHours(2)).ToList();

            if (time.Count % 4 == 0)
                heigth = time.Count / 4;
            else
                heigth = time.Count / 4 + 1;
            var keyboard = new InlineKeyboardButton[heigth + 1][];

            for (int i = 0; i < heigth; i++)
            {
                width = time.Count - i * 4 >= 4 ? 4 : time.Count - i * 4;
                keyboard[i] = new InlineKeyboardButton[width];

                for (int j = 0; j < width; j++)
                {
                    keyboard[i][j] = InlineKeyboardButton.WithCallbackData("| " + time[i * 4 + j].ToString("t") + " |",
                        "SelectedEntry " + time[i * 4 + j]);
                }
            }
            keyboard[heigth] = new InlineKeyboardButton[1];
            keyboard[heigth][0] = InlineKeyboardButton.WithCallbackData("| Вернуться назад |",
                        "MenuDays Back");
            return new(keyboard);
        }

        internal InlineKeyboardMarkup ClientsForDeleteKeyboard()
        {
            var clientList_DB = _clientRepository.GetActualClients();
            if (clientList_DB.Count == 0)
            {
                return null;
            }

            int heigth = clientList_DB.Count;
            var keyboardButtons = new InlineKeyboardButton[heigth][];

            int count = 0;
            foreach (var client in clientList_DB)
            {
                keyboardButtons[count] = new InlineKeyboardButton[1];               
                string shortFIO = $"{client.LastName} {client.FirstName[0]}.{client.MiddleName[0]}.";
                keyboardButtons[count++][0] = InlineKeyboardButton.WithCallbackData($"{shortFIO} - {client.Entry.ToString("dd.MM HH:mm")}",
                    "DeleteClient " + client.Entry.ToString());
            }
            return new(keyboardButtons);
        }

    }
}
