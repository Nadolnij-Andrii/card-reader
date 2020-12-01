using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace card_reader
{
    public partial class Form2 : Form
    {
        private static string APP_PATH = "http://localhost:9000";
        public decimal cash { get; set; }
        public string cardInfo { get; set; }
        public string ip { get; set; }
        public Card currentCard { get; set; }
        public string loginCardInfo { get; set; }
        public decimal money = 0;
        public decimal cardPrice = 0;
        public decimal moneyForCard = 0;
        public decimal change = 0;
        public decimal moneyLeft = 0;
        public bool isCardPaid = false;
        public DialogResult dialogResult = DialogResult.OK;
        public Form2()
        {
            InitializeComponent();
        }
        public Form2(decimal cash, string cardInfo, string ip, Card currentCard, string loginCardInfo)
        {

            this.cash = cash;
            this.cardInfo = cardInfo;
            this.ip = ip;
            this.currentCard = currentCard;
            this.loginCardInfo = loginCardInfo;
            InitializeComponent();
            this.cardPrice = GetCardPrice(loginCardInfo, ip);
            isCardPaid = IsCardPaid();
            if (isCardPaid)
            {
                label3.Text = "Оставшаяся сумма: " + cash;
                moneyLeft = cash;
            }
            else
            {
                label3.Text = "Цена карты: " + cardPrice +
                "\n Оставшаяся сумма: " + (cash - cardPrice);
                moneyLeft = (cash - cardPrice);

            }
            label1.Text = "Номер карты: " + currentCard.cardId + "\n" +
                "Остаток: " + currentCard.cardCount + "\n" +
                "ФИО родителя: " + currentCard.cardParentName + "\n" +
                "Дата регистрации карты:\n " + currentCard.cardRegDate;
            label4.Text = "Сдача : " + moneyLeft;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Card.isDefined(currentCard)) throw new Exception("Карта не определена");
                if (calcTextBox.Text.Length <= 0) throw new Exception("Введите значение в поле ввода");
                money = decimal.Parse(calcTextBox.Text);
                if (money <= moneyLeft)
                {
                    if (!isCardPaid) moneyForCard = PaidForCard().cardPrice;
                    if (radioButton1.Checked)
                    {
                        currentCard.replenishment(cardInfo, money, 0, 0, loginCardInfo, ip);
                        dialogResult = DialogResult.Yes;
                    }
                    else if (radioButton2.Checked)
                    {
                        currentCard.replenishment(cardInfo, 0, money, 0, loginCardInfo, ip);
                        dialogResult = DialogResult.Yes;

                    }
                    else if (radioButton3.Checked)
                    {
                        currentCard.replenishment(cardInfo, 0, 0, money, loginCardInfo, ip);
                        dialogResult = DialogResult.Yes;

                    }
                    
                }
                else
                {
                    throw new Exception("Введена сумма больше полученной");
                }
                Close();
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
                dialogResult = DialogResult.No;
            }

        }

        private void calcBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var btn = sender as Button;
                calcTextBox.Text += btn.Text;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void clearCalcButton_Click(object sender, EventArgs e)
        {
            try
            {
                calcTextBox.Text = "";
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void backspaceCalcButton_Click(object sender, EventArgs e)
        {
            try
            {

                var text = calcTextBox.Text;
                if (text.Length > 0)
                {
                    calcTextBox.Text = text.Substring(0, text.Length - 1);
                }

            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public static decimal GetCardPrice(string loginCardInfo, string ip)
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/getcardprice/");
            LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(loginInfo);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (result == "null")
                    {
                        throw new Exception("Ошибка получения стоимости карты");
                    }
                    else
                    {
                        //string r = result.Remove(result.Length - 1);
                        //var k = JsonConvert.DeserializeObject(result);
                        //cardPrice = JsonConvert.DeserializeObject<decimal>(k.ToString());
                        string r = result.Trim('\\');
                        result = r.Trim('"');
                        return Decimal.Parse(result);
                    }
                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                return 0;
            }
        }
        public CardPrice PaidForCard()
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/PaidForCard/");
            ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo();

            if (radioButton1.Checked)
            {
                replenishmentInfo = new ReplenishmentInfo(cardInfo, cash,0,0, loginCardInfo, ip);
            }
            else if (radioButton2.Checked)
            {
                replenishmentInfo = new ReplenishmentInfo(cardInfo, 0, cash, 0, loginCardInfo, ip);
            }
            else if (radioButton3.Checked)
            {
                replenishmentInfo = new ReplenishmentInfo(cardInfo, 0, 0, cash, loginCardInfo, ip);
            }
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
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (result == "null")
                    {
                        throw new Exception("Ошибка оплаты карты");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        return JsonConvert.DeserializeObject<CardPrice>(k.ToString());
                    }
                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                return null;
            }
        }
        public bool IsCardPaid()
        {
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/IsCardPaid/");
            CardInfo cardInfoForGetPrice = new CardInfo(cardInfo, loginCardInfo, ip);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(cardInfoForGetPrice);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            string result = "";
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (result == "null")
                    {
                        throw new Exception("Ошибка получения данных о оплате карты");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        CardPrice cardPrice = JsonConvert.DeserializeObject<CardPrice>(k.ToString());
                        if (cardPrice != null && cardPrice.cardPrice > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                {
                    result = streamReader.ReadToEnd();
                    FormMessage formMessage = new FormMessage(result, "Касса");
                    formMessage.Show();

                }
                return false;
            }
        }
        private void calcTextBox_TextChanged(object sender, EventArgs e)
        {
            
            if(calcTextBox.Text != null && calcTextBox.Text != "")
            {
                change = (moneyLeft - Decimal.Parse(calcTextBox.Text));
                label4.Text = "Сдача : " + change;
            }
            else
            {
                label4.Text = "Сдача : " + moneyLeft;
            }
        }
    }
}
