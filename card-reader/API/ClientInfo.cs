using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    public class ClientsInfo
    {
        public string cardInfoString { get; set; }
        public string loginInfo { get; set; }
        public string ip { get; set; }
        public string parentName { get; set; }
        public string email { get; set; }
        public string telephone { get; set; }
        public List<Client> clients { get; set; }
        //public ClientContact clientContact { get; set; }
        public int numberOfClients { get; set; }
        public ClientsInfo()
        {

        }
        public ClientsInfo(
            string cardInfoString,
            string loginInfo,
            string ip,
            string parentName,
            int numberOfClients,
            string email,
            string telephone,
            List<Client> clients
            )
        {
            this.cardInfoString = cardInfoString;
            this.loginInfo = loginInfo;
            this.ip = ip;
            this.parentName = parentName;
            this.numberOfClients = numberOfClients;
            this.clients = clients;
            this.email = email;
            this.telephone = telephone;
            //this.clientContact = clientContact;
        }
    }
}
