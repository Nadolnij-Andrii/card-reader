using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Configuration;

namespace card_reader
{
    public class Client
    {
        private static string APP_PATH = "http://localhost:9000";
        public int id { get; set; }
        public int cardId { get; set; }
        public string childrenName { get; set; }
        public DateTime childrenDate { get; set; }
        public string parentName { get; set; }
        public bool adultCard { get; set; }
        public Client()
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
        }
        public Client(
            int id,
            int cardId,
            string childrenName,
            DateTime childrenDate,
            string parentName,
            bool adultCard
            )
        {
            this.id = id;
            this.cardId = cardId;
            this.childrenName = childrenName;
            this.childrenDate = childrenDate;
            this.parentName = parentName;
            this.adultCard = adultCard;
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
        }
       
        public static List<Client> getClients(string cardInfoString, string loginCard, string ip)
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetClients/");

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(cardInfo);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            List<Client> clients = new List<Client>();
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (result == "null")
                {
                    return null;
                }
                else
                {
                    string r = result.Remove(result.Length - 1);
                    var k = JsonConvert.DeserializeObject(result);
                    clients = JsonConvert.DeserializeObject<List<Client>>(k.ToString());
                    return clients;
                }
            }
        }

    }
}
