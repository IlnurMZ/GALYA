using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GALYA
{
    internal interface IClient
    {
        void AddClient(ClientDB client);
        public List<ClientDB> GetActualClients();
        public ClientDB GetClient(DateTime entry, string firstName, string lastName);
        public List<ClientDB> GetOldClients();
        public void RemoveClient(DateTime delTime);
        public void RemoveClient(DateTime entry, string firstName, string lastName);
    }
}
