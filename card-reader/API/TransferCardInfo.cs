using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class TransferCardInfo
    {
        public string fromCardInfoString { get; set; }
        public string toCardInfoString { get; set; }
        public string loginCard { get; set; }
        public string ip { get; set; }
        public TransferCardInfo()
        {

        }
        public TransferCardInfo(
            string fromCardInfoString,
            string toCardInfoString,
            string loginCard,
            string ip
        )
        {
            this.fromCardInfoString = fromCardInfoString;
            this.toCardInfoString = toCardInfoString;
            this.loginCard = loginCard;
            this.ip = ip;
        }

    }
}
