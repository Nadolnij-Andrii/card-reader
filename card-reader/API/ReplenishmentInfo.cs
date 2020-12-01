using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class ReplenishmentInfo
    {
        public string cardInfo { get; set; }
        public decimal cash { get; set; }
        public decimal cashlessPayment { get; set; }
        public decimal creditCard { get; set; }
        public string loginCard { get; set; }
        public string ip { get; set; }
        public ReplenishmentInfo()
        {

        }
        public ReplenishmentInfo(
             string cardInfo,
             decimal cash,
             decimal cashlessPayment,
             decimal creditCard,
             string loginCard,
             string ip
            )
        {
            this.cardInfo = cardInfo;
            this.cash = cash;
            this.cashlessPayment = cashlessPayment;
            this.creditCard = creditCard;
            this.loginCard = loginCard;
            this.ip = ip;
        }
    }
}
