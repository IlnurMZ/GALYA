using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GALYA
{
    internal static class Config
    {
        public static readonly string SqlConnectionString;
        public static readonly string Token;

        static Config()
        {
            List<string> param = new();
            string path = "Config.cfg";
            try
            {
                using (var reader = new StreamReader(path))
                {
                    while (!reader.EndOfStream)
                    {
                        var row = reader.ReadLine();
                        if (!string.IsNullOrEmpty(row))
                        {
                            param.Add(row);
                        }                        
                    }

                    if (param.Count == 2)
                    {
                        SqlConnectionString = param[0];
                        Token = param[1];
                    }
                    else
                    {
                        throw new Exception("Ошибка чтения конфигурационного файла");
                    }
                    Console.WriteLine("Данные из конфигурационного файла успешно считаны");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
    
}
