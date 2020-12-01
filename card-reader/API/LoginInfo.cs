using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace card_reader
{
    public class LoginInfo
    {
        public string cardInfo { get; set; }
        public string IP { get; set; }
        public LoginInfoResponce loginInfoResponce { get; set; }
        public LoginInfo()
        {

        }
        public LoginInfo(
            string cardInfo,
            string IP
            )
        {
            this.cardInfo = cardInfo;
            this.IP = IP;
        }

        public class LoginInfoResponce
        {
            public Cashier cashier { get; set; }
            public CashierRegister cashierRegister { get; set; }
            public WorkShift workShift { get; set; }
            public bool isWokrShiftStarts { get; set; }
            public LoginInfoResponce()
            {

            }
            public LoginInfoResponce(
                Cashier cashier,
                CashierRegister cashierRegister,
                WorkShift workShift,
                bool isWokrShiftStarts
                )
            {
                this.cashier = cashier;
                this.cashierRegister = cashierRegister;
                this.workShift = workShift;
                this.isWokrShiftStarts = isWokrShiftStarts;
            }

        }
    }
}
