using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GALYA.Model;
using Npgsql;

namespace GALYA.Repositories
{
    internal static class EntryRepository
    {
        public static void AddEntry(DateTime entry)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = $"insert into free_entries (entry) values (@time)";
                connection.Execute(sql, new { time = entry });
            }
        }

        public static void AddFullDayEntries(string strValues)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = $"insert into free_entries (entry) values {strValues}";
                connection.Execute(sql);
            }
        }        

        public static void RemoveEntry(DateTime entry)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = $"delete from free_entries where entry = (@time);";
                connection.Execute(sql, new { time = entry });
            }
        }
    }
}
