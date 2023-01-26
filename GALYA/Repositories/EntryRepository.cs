using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GALYA.Keyboard;
using GALYA.Model;
using Npgsql;
using Telegram.Bot.Types;

namespace GALYA.Repositories
{
    internal class EntryRepository
    {
        public readonly string SqlConnectionString;
        public EntryRepository()
        {
            // Надо доработать
            SqlConnectionString = "User ID=postgres;Password=270103;Host=localhost;Port=5432;Database=galyabase;";
        }
        public void AddEntry(Message date)
        {
            if (DateTime.TryParse(date.Text, out DateTime newTime))
            {
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    string sql = $"insert into free_entries (entry) values (@time)";
                    connection.Execute(sql, new { time = newTime });
                }
            }
            else
            {
                throw new Exception("Ошибка ввода даты");
            }         
        }

        public void AddEntry(DateTime newTime)
        {            
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    string sql = $"insert into free_entries (entry) values (@time)";
                    connection.Execute(sql, new { time = newTime });
                }            
        }

        public void AddFullDayEntries(Message date)
        {
            if (!DateTime.TryParse(date.Text, out DateTime newTime))
            {
                throw new ArgumentException("Ошибка добавления времени");
            }

            List<DateTime> entries = GetEntries();

            if (!entries.Any(d => d.Day == newTime.Day))
            {
                StringBuilder strValues = new StringBuilder();
                newTime = newTime.AddMinutes(480);
                // рабочий день идет с 8 до 17 часов                
                for (int i = 0; i < 8; i++)
                {
                    strValues.Append("(\'" + newTime.ToString() + "\'),");
                    newTime = newTime.AddMinutes(30);
                }
                // с 12 до 13 часов - перерыв 
                newTime = newTime.AddMinutes(60); 
                for (int i = 0; i < 7; i++)
                {
                    strValues.Append("(\'" + newTime.ToString() + "\'),");
                    newTime = newTime.AddMinutes(30);
                }
                strValues.Append("(\'" + newTime.ToString() + "\')");        

                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    string sql = $"insert into free_entries (entry) values {strValues}";
                    connection.Execute(sql);
                }
            }
            else
            {                
                throw new Exception("Такой день уже есть в списке");
            }
        }

        public List<DateTime> GetEntries()
        {
            var query = @" select
                *
                from free_entries
                order by (entry) asc";

            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Query<DateTime>(query);
                return list.ToList();
            }
        }

        public void RemoveEntry(DateTime entry)
        {
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                string sql = $"delete from free_entries where entry = (@time);";
                connection.Execute(sql, new { time = entry });
            }
        }
    }
}
