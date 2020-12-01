using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    
    class TransactionAttractions
    {
        public object id { get; set; }
        public object cardId { get; set; }
        public object attractionId { get; set; }
        public object operation { get; set; }
        public object summ { get; set; }
        public object bonus { get; set; }
        public object tickets{ get; set; }
        public DateTime date { get; set; }
        public decimal summBalance { get; set; }
        public decimal bonusesBalance { get; set; }
        public int ticketsBalance { get; set; }
       
        public TransactionAttractions()
        {

        }
        public TransactionAttractions(
            object id,
            object cardId,
            object attractionId,
            object operation,
            object summ,
            object bonus,
            object tikets,
            DateTime date,
            decimal summBalance,
            decimal bonusesBalance,
            int ticketsBalance
            )
        {
            this.id = id;
            this.attractionId = attractionId;
            this.operation = operation;
            this.summ = summ;
            this.bonus = bonus;
            this.tickets = tikets;
            this.date = date;
            this.summBalance = summBalance;
            this.bonusesBalance = bonusesBalance;
            this.ticketsBalance = ticketsBalance;
        }
    }
        
}
