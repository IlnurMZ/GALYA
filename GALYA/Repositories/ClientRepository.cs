using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GALYA.Model;
using GALYA.Table;
using Npgsql;

namespace GALYA
{
    internal static class ClientRepository
    {
        public static void AddClient(ClientDB client)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "insert into client_list (entry, firstname,lastname,middlename,phone)" +
                    "values (@entry,@firstname,@lastname,@middlename,@phone)";
                connection.Execute(sql, new { entry = client.Entry, firstname = client.FirstName, lastname = client.LastName, middlename = client.MiddleName, phone = client.Phone });
            }
        }
        public static List<ClientDB> GetActualClients()
        {
            List<ClientDB> actualClientsList;
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                var query2 = @" select
                            firstname, lastname, middlename, phone, entry
                            from client_list
                            where entry > @date
                            order by (entry) asc";
                actualClientsList = connection.Query<ClientDB>(query2, new { date = DateTime.Now }).ToList();
            }
            return actualClientsList;
        }     

        public static ClientDB GetClient(DateTime entry, string firstName, string lastName)
        {
            ClientDB client;
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "select * from client_list where entry = (@entry) and lastname = (@lastname) and firstname = (@firstname)";
                client = connection.QueryFirstOrDefault<ClientDB>(sql, new { entry = entry, firstName = firstName, lastname = lastName});              
            }
            return client;            
        }       

        public static List<ClientDB> GetOldClients()
        {
            List<ClientDB> oldClientsList;
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = @" select
                            firstname, lastname, middlename, phone, entry
                            from client_list
                            where entry < @date
                            order by (entry) asc";
                oldClientsList = connection.Query<ClientDB>(sql, new { date = DateTime.Now }).ToList();
            }
            return oldClientsList;
        }

        public static void RemoveClient(DateTime time)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = $"delete from client_list where entry = (@time);";
                connection.Execute(sql, new { time = time});
            }
        }
        public static void RemoveClient(DateTime entry, string firstName, string lastName)
        {
            using (var connection = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "delete from client_list where entry = (@entry) and lastname = (@lastname) and firstname = (@firstname)";
                connection.Execute(sql, new { entry = entry, lastname = lastName, firstName = firstName });
            }
        }
    }
}
