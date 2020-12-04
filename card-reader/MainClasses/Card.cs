using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace card_reader
{
    // информация о карточке
    public class Card
    {

        public int id { get; set; }
        public int cardId { get; set; }
        public decimal cardCount { get; set; }
        public int cardSale { get; set; }
        public int cardRole { get; set; }
        public int cardStatus { get; set; }
        public decimal cardBonus { get; set; }
        public int cardTicket { get; set; }
        public string cardParentName { get; set; }
        public DateTime cardRegDate { get; set; }
        public decimal cardDayBonus { get; set; }
        public DateTime cardDayBonusDateTime { get; set; }
        public decimal TotalAccrued { get; set; }
        public decimal TotalSpend { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public int TotalGames { get; set; }
        private static string APP_PATH = "http://localhost:9000";
        public Card()
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
        }
        public string ToString(StartInfo startInfo)
        {
            Sale sale = startInfo.sales.Find(x => x.saleId == this.cardSale);
            CardStatus status = startInfo.cardStatuses.Find(x => x.status_id == this.cardStatus);
            return "Номер карты: " + this.cardId + "\n" +
                    "Остаток: " + this.cardCount + "\n" +
                    "Тариф: " + sale.saleValue + "%\n" +
                    "Роль: " + this.cardRole + "\n" +
                    "Статус: " + status.status_message + "\n" +
                    "Количество бонусов: " + this.cardBonus + "\n" +
                    "Количество билетов: " + this.cardTicket + "\n" +
                    "ФИО родителя: " + this.cardParentName + "\n" +
                    "Дата регистрации карты:\n " + this.cardRegDate ;
        }
        public string ToString(Sale sale, CardStatus cardStatus)
        {
            return "Номер карты: " + this.cardId + "\n" +
                    "Остаток: " + this.cardCount + "\n" +
                    "Тариф: " + sale.saleValue + "%\n" +
                    "Статус: " + cardStatus.status_message + "\n" +
                    "Количество бонусов: " + this.cardBonus + "\n" +
                    "Количество билетов: " + this.cardTicket + "\n" +
                    "ФИО родителя: " + this.cardParentName + "\n" +
                    "Дата регистрации карты:\n " + this.cardRegDate;
        }
        public static bool isDefined(Card card)
        {
            return (card != null) ? true : false;
        }
        //Регистрация новой карты
        public static Card register(
            string cardInfo, 
            string loginCard,
            string ip, 
            bool swap)
        {

            try
            {
                RegistretedCardInfo registretedCardInfo = new RegistretedCardInfo(cardInfo, loginCard, ip, swap);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/RegisterateNewCard/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(registretedCardInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Card card = new Card();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка регистарции карты");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        card = JsonConvert.DeserializeObject<Card>(k.ToString());
                        return card;
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                return null;
            }
           
        }
        //Пополнение баланса
        public void replenishment(string cardInfo, decimal cash, decimal cashlessPayment, decimal creditCard, string loginCard, string ip)
        {
            try
            {
                ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo( cardInfo, cash, cashlessPayment, creditCard,  loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/Replenishment/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(replenishmentInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Card card = new Card();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка пополнения очков");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        card = JsonConvert.DeserializeObject<Card>(k.ToString());
                        this.cardCount = card.cardCount;
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }       
        public void removeCard(string cardInfoString, string loginCard, string ip)
        {
            try
            {

                if (cardCount > 0)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/RemoveCard/");
                    CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(cardInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка возврата карты");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardCount = card.cardCount;
                            this.cardBonus = card.cardBonus;
                            this.cardTicket = card.cardTicket;
                            this.cardStatus = card.cardStatus;
                            this.cardSale = card.cardSale;
                        }
                    }
                }
                else
                {
                    throw new Exception("На счету 0");
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //Внос бонусов        
        public void addBonuses(string cardInfo, decimal bonuses, string loginCard, string ip)
        {
            try
            {
                ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo( cardInfo, bonuses, 0, 0,loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/AddBonuses/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(replenishmentInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Card card = new Card();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка пополнения бонусов");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        card = JsonConvert.DeserializeObject<Card>(k.ToString());
                        this.cardBonus = card.cardBonus;

                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        public void addDayBonuses(string cardInfo, decimal dayBonuses, string loginCard, string ip)
        {
            try
            {
                ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, dayBonuses, 0, 0, loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/AddDayBonuses/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(replenishmentInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Card card = new Card();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка пополнения бонусов");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        card = JsonConvert.DeserializeObject<Card>(k.ToString());
                        this.cardDayBonus = card.cardDayBonus;
                        this.cardDayBonusDateTime = card.cardDayBonusDateTime;
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        public void activateCard(string cardInfoString,string loginCard, string ip)
        {
            try
            {
                if(this.cardStatus != 1)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ActivateCard/");
                    CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(cardInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка активации карты");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardStatus = card.cardStatus;
                        }
                    }
                }
                else
                {
                    throw new Exception("Карту уже активирована");
                }
               
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }

        }
        public void addTickets(string cardInfo, int tickets, string loginCard, string ip)
        {
            try
            {
                ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo( cardInfo, tickets, 0, 0, loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/AddTickets/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(replenishmentInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                Card card = new Card();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка внесения билетов");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        card = JsonConvert.DeserializeObject<Card>(k.ToString());
                        this.cardTicket = card.cardTicket;
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //
        public void removeTicket(string cardInfo, int tickets, string loginCard, string ip)
        {
            try
            {
                int balance = this.cardTicket;
                if (balance >= tickets)
                {
                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, tickets, 0, 0, loginCard, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/RemoveTickets/");

                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(replenishmentInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка списания билетов");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardTicket = card.cardTicket;
                        }
                    }
                }
                else
                {
                    throw new Exception("Недосточное количесво билетов");
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //Установка пакета
        public void setPacket(string cardInfo, int discount, string loginCard, string ip)
        {
            try
            {
                int balance = this.cardTicket;
                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, discount, 0, 0, loginCard, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/SetPacket/");

                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(replenishmentInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null" || result == "\"null\"")
                        {
                            throw new Exception("Ошибка установки пакета");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardSale = card.cardSale;
                        }
                    }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //Замена
        public Card swap(string swapCard, string firstCardInfo,  string loginCard, string ip)
        {
            SwapCardInfo swapCardInfo = new SwapCardInfo(firstCardInfo, swapCard, loginCard, ip);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/PostSwap/");

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(swapCardInfo);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Card card = new Card();
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (result == "null")
                {
                    throw new Exception("Ошибка переноса");
                }
                else
                {
                    string r = result.Remove(result.Length - 1);
                    var k = JsonConvert.DeserializeObject(result);
                    card = JsonConvert.DeserializeObject<Card>(k.ToString());
                    if(card != null)
                    {
                        return card;
                    }
                    else
                    {
                        throw new Exception("Ошибка переноса");
                    }
                }
            }
        }
        //Перенос
        public static Card transfer( string fromCardInfoString, string toCardInfoString , string logincard, string ip)
        {
            TransferCardInfo transferCardInfo = new TransferCardInfo(fromCardInfoString, toCardInfoString, logincard, ip);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/TransferCard/");

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(transferCardInfo);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Card card = new Card();
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (result == "null")
                {
                    throw new Exception("Ошибка переноса");
                }
                else
                {
                    string r = result.Remove(result.Length - 1);
                    var k = JsonConvert.DeserializeObject(result);
                    card = JsonConvert.DeserializeObject<Card>(k.ToString());
                    if (card != null)
                    {
                        return card;
                    }
                    else
                    {
                        throw new Exception("Ошибка переноса");
                    }
                }
            }
        }       
        //Блокировка карты
        public void block(string cardInfoString, string loginCard, string ip)
        {
            try
            {
                if (this.cardStatus == 1)
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/CardBlock/");
                    CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(cardInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка блокирования карты");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardStatus = card.cardStatus;
                        }
                    }
                }
                else
                {
                    throw new Exception("Карта уже заблокирована");
                }

            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public void returnCashForCard(string cardInfoString, string loginCard, string ip)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ReturnCashForCard/");
                CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(cardInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                CardPrice cardPrice = new CardPrice();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка возврата стоимостик карты");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        cardPrice = JsonConvert.DeserializeObject<CardPrice>(k.ToString());
                        if(cardPrice.cardId == cardId)
                        {
                            if(cardPrice.cardPrice > 0)
                            {
                                FormMessage formMessage = new FormMessage("Стоимость карты уже возвращена", " Касса");
                                formMessage.Show();

                            }
                            else
                            {
                                FormMessage formMessage = new FormMessage("Стоимость карты возвращена", " Касса");
                                formMessage.Show();


                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public void removeTicketsInCount(string cardInfo, string loginCard, string ip, decimal tickets)
        {
            try
            {
                int balance = this.cardTicket;
                if (balance >= tickets)
                {
                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, tickets, 0, 0, loginCard, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/RemoveTickets/");

                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(replenishmentInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка списания билетов");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardTicket = card.cardTicket;
                        }
                    }
                }
                else
                {
                    throw new Exception("Недосточное количесво билетов");
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public void removeBonuses(string cardInfo, string loginCard, string ip, decimal bonuses)
        {
            try
            {
                decimal balance = this.cardBonus;
                if (balance >= bonuses)
                {
                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, bonuses, 0, 0, loginCard, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ReturnBonuses/");

                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(replenishmentInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка списания бонусов");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardBonus = card.cardBonus;
                        }
                    }
                }
                else
                {
                    throw new Exception("Недосточное количесво бонусов");
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public void removeDayBonuses(string cardInfo, string loginCard, string ip, decimal dayBonuses)
        {
            try
            {
                decimal balance = this.cardDayBonus;
                if (balance >= dayBonuses)
                {
                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo(cardInfo, dayBonuses, 0, 0, loginCard, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ReturnDayBonuses/");

                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(replenishmentInfo);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    Card card = new Card();
                    string result = "";
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                        if (result == "null")
                        {
                            throw new Exception("Ошибка списания бонусов");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            card = JsonConvert.DeserializeObject<Card>(k.ToString());
                            this.cardDayBonus = card.cardDayBonus;
                        }
                    }
                }
                else
                {
                    throw new Exception("Недосточное количесво суточных бонусов");
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
    }
}
