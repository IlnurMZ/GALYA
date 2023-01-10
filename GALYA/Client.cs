using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace GALYA
{
    public class Client
    {       
        public string FIO { get; set; }
        public string PhoneNumber { get; set; }        
        //public bool IsFinished { get; set; }

        public long ChatId { get; set; }
        ClientQuery _query;

        public Client(ITelegramBotClient botClient, Chat chat)
        {
            ChatId = chat.Id;
            _query = new ClientQuery(this, botClient, chat);
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
