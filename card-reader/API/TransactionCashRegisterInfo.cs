using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class TransactionCashRegisterInfo
    {
        public Card card { get; set; }
        public List<TransactionCashRegister> transactionsCashRegister { get; set; }
        public TransactionCashRegisterInfo()
        {

        }
        public TransactionCashRegisterInfo(
            Card card,
            List<TransactionCashRegister> transactionsCashRegister
            )
        {
            this.card = card;
            this.transactionsCashRegister = transactionsCashRegister;
        }
    }
}
