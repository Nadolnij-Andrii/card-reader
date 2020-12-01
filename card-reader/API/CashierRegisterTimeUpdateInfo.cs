using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class CashierRegisterTimeUpdateInfo
    {
        public DateTime timeLastPing { get; set; }
        public string loginCard { get; set; }
        public string ip { get; set; }
        public CashierRegisterTimeUpdateInfo(
             DateTime timeLastPing,
             string loginCard,
             string ip 
            )
        {
            this.timeLastPing = timeLastPing;
            this.loginCard = loginCard;
            this.ip = ip;
        }
    }
}
