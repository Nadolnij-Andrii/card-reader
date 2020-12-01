using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class CardInfo
    {
        public string inputInfo { get; set; }
        public string loginCard { get; set; }
        public string ip { get; set; }
        public CardInfo(
            string inputInfo,
            string loginCard,
            string ip
            )
        {
            this.inputInfo = inputInfo;
            this.loginCard = loginCard;
            this.ip = ip;
        }
    }
}
