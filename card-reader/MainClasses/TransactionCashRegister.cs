using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class TransactionCashRegister
    {
        public object id { get; set; }
        public object cardId { get; set; }
        public object toCardId { get; set; }
        public object operation { get; set; }
        public object summ { get; set; }
        public object bonus { get; set; }
        public object tickets { get; set; }
        public object cashier_register_id { get; set; }
        public object cashier_id { get; set; }
        public object cashier_name { get; set; }
        public DateTime date { get; set; }
        public decimal summBalance { get; set; }
        public decimal bonusesBalance { get; set; }
        public int ticketsBalance { get; set; }

        public TransactionCashRegister()
        {

        }
        public TransactionCashRegister(
            object id,
            object cardId,
            object toCardId,
            object operation,
            object summ,
            object bonus,
            DateTime date,
            object tickets,
            int cashier_id,
            string cashier_name,
            int cashier_register_id,
            decimal summBalance,
            decimal bonusesBalance,
            int ticketsBalance)
        {
            this.id = id;
            this.cardId = cardId;
            this.toCardId = toCardId;
            this.operation = operation;
            this.summ = summ;
            this.bonus = bonus;
            this.date = date;
            this.tickets = tickets;
            this.cashier_id = cashier_id;
            this.cashier_name = cashier_name;
            this.cashier_register_id = cashier_register_id;
            this.summBalance = summBalance;
            this.bonusesBalance = bonusesBalance;
            this.ticketsBalance = ticketsBalance;
        }
    }
}
