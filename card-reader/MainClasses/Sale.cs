using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    public class Sale
    {
        public int id { get; set; }
        public int saleId { get; set; }
        public decimal saleValue { get; set; }
        public Sale()
        {
        }
        public Sale(
            int id,
            int saleId,
            decimal saleValue)
        {
            this.id = id;
            this.saleId = saleId;
            this.saleValue = saleValue;
        }
    }
}
