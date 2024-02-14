using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFPolupanov.Models
{
    class Client
    {
        public ulong client_id;
        public string client_name;

        public Client(ulong client_id, string client_name)
        { 
            this.client_id = client_id;
            this.client_name = client_name;
        }
    }
}
