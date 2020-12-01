
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace card_reader
{
    public class ClientContact
    {
        public int id { get; set; }
        public int cardId { get; set; }
        public string clientName { get; set; }
        public DateTime bDay { get; set; }
        public string telephone { get; set; }
        public string email { get; set; }
        public int gender { get; set; }
        public ClientContact(
                int id,
             int cardId,
             string clientName,
             DateTime bDay,
             string telephone,
             string email,
             int gender
            )
        {
            this.id = id;
            this.cardId = cardId;
            this.clientName = clientName;
            this.bDay = bDay;
            this.telephone = telephone;
            this.email = email;
            this.gender = gender;
        }
        public ClientContact()
        {

        }
    }
}