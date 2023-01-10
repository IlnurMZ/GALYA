using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace GALYA
{
    public class ClientMenu
    {
        int _year = DateTime.Now.Year;
        int _month = DateTime.Now.Month;     

        internal InlineKeyboardMarkup StartMenuKeyboard()
        {           
            InlineKeyboardMarkup keyboard = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Записаться", "MenuDays"),
                        InlineKeyboardButton.WithCallbackData("Отписаться", "Unsubcribe")
                    }
                }
                );
            return keyboard;            
        }

        internal InlineKeyboardMarkup DaysOfMonthMenuKeyboard(string command = "current")
        {
            DateTime currentTime = DateTime.Now.AddHours(2); // делаем запась не раньше чем на 2 часа
            var myDataBase = DataBaseInfo.FreeEntry;
            int heigthMenu, widthMenu; // количество строк и столбцов пунктов в меню
            List<DateTime> allActualDays;
            List<DateTime> daysOfMonth;
            bool isNextMonth = false;
            //bool isPreviousMonth = false;
            int dopMenu = 1; // количество дополнительных пунктов меню (след. и пред. месяц), + возврат в главное меню

            if (command == "next")
            {
                _month++; 
                if (_month == 13)
                {
                    _month = 1;
                    _year++;
                }
            }
            else if (command == "previous")
            {
                _month--; 
                if (_month == 0)
                {
                    _month = 12;
                    _year--;
                }
            }

            allActualDays = myDataBase.Where(d => d.Month == _month && d.Year == _year && d > currentTime).ToList(); // Выбираем все записи нужного месяца 
            daysOfMonth = allActualDays.GroupBy(d => d.Day).Select(g => g.First()).ToList(); // Отбираем только дни               

            if (daysOfMonth.Count % 5 == 0)
                heigthMenu = daysOfMonth.Count / 5; 
            else
                heigthMenu = daysOfMonth.Count / 5 + 1;

            int numNextMonth = _month + 1 == 13 ? 1 : _month + 1; 
            int numPrevMonth = _month - 1 == 0 ? 12 : _month - 1;
            // Проверка наличия записей на следующий месяц
            if (myDataBase.Any(d => d.Month == numNextMonth && d > DateTime.Now))
            {
                dopMenu++;                
                isNextMonth = true;
            }
            // Проверка наличия записей на предыдущий месяц
            if (myDataBase.Any(d => d.Month == numPrevMonth && d > DateTime.Now))
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

            if (dopMenu == 2)
            {
                keyboard[heigthMenu - 2] = new InlineKeyboardButton[1];
                if (isNextMonth)
                {
                    keyboard[heigthMenu - 2][0] = InlineKeyboardButton.WithCallbackData(
                                            "|  следующий месяц  |",
                                            "MenuDays " + "next");
                }
                else
                {
                    keyboard[heigthMenu - 2][0] = InlineKeyboardButton.WithCallbackData(
                                        "|  предыдущий месяц  |",
                                        "MenuDays " + "previous");
                }
            }
            else if (dopMenu == 3)
            {
                keyboard[heigthMenu - 3] = new InlineKeyboardButton[1];
                keyboard[heigthMenu - 3][0] = InlineKeyboardButton.WithCallbackData(
                                            "|  следующий месяц  |",
                                            "MenuDays " + "next");

                keyboard[heigthMenu - 2] = new InlineKeyboardButton[1];
                keyboard[heigthMenu - 2][0] = InlineKeyboardButton.WithCallbackData(
                                        "|  предыдущий месяц  |",
                                        "MenuDays " + "previous");
            }

            keyboard[heigthMenu - 1] = new InlineKeyboardButton[1];
            keyboard[heigthMenu - 1][0] = InlineKeyboardButton.WithCallbackData(
                                       "|  Возврат в главное меню  |",
                                       "BackMainMenu");

            return new (keyboard);
        }

        internal InlineKeyboardMarkup HoursOfDayKeyboard(string strData)
        {
            int day = DateTime.Parse(strData).Day;
            var myDataBase = DataBaseInfo.FreeEntry; 
            int heigth, width;            
            List<DateTime> time = myDataBase.Where(t => t.Month == _month && t.Day == day && t > DateTime.Now.AddHours(2)).ToList(); //поиск записей по выбранному дню 

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
            return new (keyboard);
        }

    }
}
