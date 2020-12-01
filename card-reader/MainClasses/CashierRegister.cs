using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    public class CashierRegister
    {
        public int id { get; set; }
        public int cashierRegisterId { get; set; }
        public object cashierRegisterIP { get; set; }
        public DateTime timeLastPing { get; set; }
        public CashierRegister()
        {

        }
        public CashierRegister(
            int id,
            int cashierRegisterId,
            object cashierRegisterIP,
            DateTime timeLastPing
            )
        {
            this.id = id;
            this.cashierRegisterId = cashierRegisterId;
            this.cashierRegisterIP = cashierRegisterIP;
            this.timeLastPing = timeLastPing;
        }
    }
}
