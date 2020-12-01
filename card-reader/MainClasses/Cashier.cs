using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    public class Cashier
    {
        public int id { get; set; }
        public int cashierId { get; set; }
        public object cashierName { get; set; }
        public object cashierCardId { get; set; }

        public  Cashier()
        {

        }

        public Cashier(
            int id,
            int cashierId,
            object cashierName,
            object cashierCardId
            )
        {
            this.id = id;
            this.cashierId = cashierId;
            this.cashierName = cashierName;
            this.cashierCardId = cashierCardId;
        }
    }
}
