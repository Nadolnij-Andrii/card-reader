using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace card_reader
{
    class Attraction
    {
        public object id { get; set; }
        public object attractionIp { get; set; }
        public object attractionPrice { get; set; }
        public object attractionName { get; set; }
        public object attractionStatus { get; set; }

        public Attraction()
        {

        }
        public Attraction(
            object id,
            object attractionIp,
            object attractionPrice,
            object attractionName,
            object attractionStatus)
        {
            this.id = id;
            this.attractionIp = attractionIp;
            this.attractionPrice = attractionPrice;
            this.attractionName = attractionName;
            this.attractionStatus = attractionStatus;
        }
    }
}
