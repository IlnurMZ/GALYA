using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace GALYA
{
    internal interface IEntry
    {
        public void AddEntry(Message date);        
        public void AddFullDayEntries(Message date);
        public List<DateTime> GetEntries();        
        public void RemoveEntry(DateTime entry);
    }
}
