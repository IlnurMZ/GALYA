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
    internal class Admin
    {
        internal string Password { get; } = "123"; // временное решение                  
        internal bool IsFinished { get; set; } = false;      
        AdminQuery _query;
        public long ChatId { get; set; }

        public Admin(ITelegramBotClient botClient, Chat chat)
        {            
            ChatId = chat.Id;
            _query = new AdminQuery(this, botClient, chat);
        }

        public async Task OnAnswerCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            await _query.HandleCallbackQuery(callbackQuery);
        }

        public async Task OnAnswerMessageAsync(Message message)
        {
            await _query.HandleMessage(message);
        }     

    }
}
