using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class TransancionAttractionInfo
    {
        public Card card { get; set; }
        public List<TransactionAttractions> transactionsAttractions { get; set; }
        public List<Attraction> attractions { get; set; }
        public TransancionAttractionInfo()
        {

        }
        public TransancionAttractionInfo(
            Card card,
            List<TransactionAttractions> transactionsAttractions,
            List<Attraction> attractions
            )
        {
            this.card = card;
            this.transactionsAttractions = transactionsAttractions;
            this.attractions = attractions;
        }
    }
}
