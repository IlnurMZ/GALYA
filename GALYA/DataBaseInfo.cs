using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GALYA
{
    internal static class DataBaseInfo
    {
        public static List<DateTime> FreeEntry = new List<DateTime>()
        {
            new DateTime(2023,1,15,10,30,0),
            new DateTime(2023,1,15,13,30,0),
            new DateTime(2023,1,17,16,00,0),
            new DateTime(2023,1,17,16,30,0),
            new DateTime(2023,1,20,9,00,0),
            new DateTime(2023,1,20,10,30,0),
            new DateTime(2023,2,1,9,00,0),
            new DateTime(2023,2,1,10,30,0)
        };
        
        public static SortedDictionary<DateTime, string[]> ClientList = new SortedDictionary<DateTime, string[]>()
        {
            { new DateTime(2022,11,20,12,30,0), new string[] {"Бубин Крест Пикович","89057729450"} },
            { new DateTime(2023,2,20,12,30,0), new string[] {"Иванов Иван Иванович","89059929450"} },
            { new DateTime(2023,11,30,12,30,0), new string[] {"Джеков Потрошитель Петрович","6666666" } },
            { new DateTime(2023,12,30,12,30,0), new string[] {"Бараков Дональд Бушевич","77777" } },
        };
    }
}
