using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;

namespace card_reader
{
    public partial class form : System.Windows.Forms.Form
    {
        bool isWorkShiftOpen = false;
        Card currentCard;
        DateTime date;
        stack stk = new stack(30);
        FlowLayoutPanel[] panels;
        bool swap = false;
        Card swapCard;
        bool transfer = false;
        Card transferCard;
        public string clientName;

        private Cashier loginedCashier { get; set; }
        private CashierRegister currentCashierRegister { get; set; }
        private string loginCardInfo { get; set; }
        private string ip { get; set; }
        private StartInfo startInfo { get; set; }
        private string cardInfo;
        private string firstCardInfo;
        private bool excMess = false;
        public form()
        {

            timer = new System.Windows.Forms.Timer();
            InitializeComponent();

            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            panels = new FlowLayoutPanel[] { packetsPanel, prizePanel, elsePanel, shiftPanel };
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosing);

        }
        static System.Windows.Forms.Timer timer;
        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                timer.Interval = 100000;
                CashierRegisterTimeUpdateInfo cashierRegisterTimeUpdateInfo = new CashierRegisterTimeUpdateInfo(DateTime.Now, loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/UpdateTimeLastPing/");

                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(cashierRegisterTimeUpdateInfo);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (!(httpResponse.StatusCode == HttpStatusCode.Accepted))
                {
                    throw new Exception("Ошибка обновления статуса кассы");
                }
                else
                {
                    excMess = false;
                }
            }
            catch (Exception exc)
            {
                if (excMess == false)
                {
                    timer.Stop();
                    FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                    formMessage.Show();
                    excMess = true;
                }
            }
        }
        public form(Cashier loginedCashier, CashierRegister currentCashierRagister, string loginCardInfo, string ip, StartInfo startInfo)
        {
            timer = new System.Windows.Forms.Timer();
            InitializeComponent();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            this.loginedCashier = loginedCashier;
            this.currentCashierRegister = currentCashierRagister;
            this.loginCardInfo = loginCardInfo;
            this.ip = ip;
            this.startInfo = startInfo;
            panels = new FlowLayoutPanel[] { packetsPanel, prizePanel, elsePanel };
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosing);
            cashierInfoLabel.Text = "Кассир: " + loginedCashier.cashierName + "\nВремя входа: " + DateTime.Now;
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
        }
        private bool isMouse(EventArgs e)
        {
            bool mouse = (e is MouseEventArgs);
            return mouse;
        }
        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (isWorkShiftOpen)
            {
                date = DateTime.Now;
            }
        }
        private void form_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (isWorkShiftOpen)
            {
                try
                {
                    if ((DateTime.Now - date) < TimeSpan.FromMilliseconds(30)) e.Handled = true;

                }
                catch (Exception exc)
                {
                    FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                    formMessage.Show();

                }
            }
        }
        private void form_KeyUp(object sender, KeyEventArgs e)
        {
            if (isWorkShiftOpen)
            {
                try
                {
                    if ((DateTime.Now - date) < TimeSpan.FromMilliseconds(30))
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            cardInfo = Encoding.ASCII.GetString(stk.get());
                            currentCard = selectCard(cardInfo, loginCardInfo, ip);
                            //Если карта не зарегистрирована
                            if (currentCard != null && swap == true)
                            {
                                swap = false;
                                swapCard = null;
                                firstCardInfo = "";
                                FormMessage formMessage = new FormMessage("Используйте новую карту", "Касса");
                                formMessage.Show();
                            }
                            if (currentCard == null)
                            {
                                if (swap == true)
                                {
                                    currentCard = swapCard.swap(cardInfo, firstCardInfo, loginCardInfo, ip);
                                    swap = false;
                                    swapCard = null;

                                    FormMessage formMessage = new FormMessage("Данные перенесены на новую карту", "Касса");
                                    formMessage.Show();

                                }
                                else
                                {
                                    if (licenseCheck(cardInfo, loginCardInfo, ip))
                                    {
                                        string message = "Карта не зарегистрирована. Зарегистрировать?";
                                        string caption = "Новая карта - Касса";
                                        //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                                        DialogResult result = new DialogResult();

                                        // Displays the MessageBox.

                                        if (transfer == false)
                                        {
                                            FormMessage formMessage = new FormMessage(message, caption, result);
                                            formMessage.ShowDialog();
                                            if (formMessage.result == DialogResult.Yes)
                                            {
                                                //Регистрация карты
                                                MatchCollection matches = Regex.Matches(cardInfo, @"([0-9])+");
                                                var cardId = matches[1].ToString();
                                                currentCard = Card.register(cardInfo, loginCardInfo, ip, swap);
                                                if (currentCard != null)
                                                {
                                                    FormMessage formMessage1 = new FormMessage("Данные успешно добавленны", "Касса");
                                                    formMessage1.Show();
                                                }
                                            }
                                            else
                                            {
                                                currentCard = null;
                                            }
                                        }
                                        else
                                        {
                                            firstCardInfo = "";
                                            transfer = false;
                                            transferCard = null;
                                            throw new Exception("Карта не зарегистрирована.\nДля переноса требуется зарегестрированая карта");
                                        }
                                    }

                                }
                            }
                            if (currentCard != null && transferCard != null)
                            {
                                if (((int)transferCard.cardId != (int)currentCard.cardId))
                                {
                                    if (transfer == true)
                                    {
                                        if ((int)currentCard.cardStatus == 1)
                                        {
                                            Card toCard = currentCard;
                                            currentCard = Card.transfer(firstCardInfo, cardInfo, loginCardInfo, ip);
                                            transfer = false;
                                            transferCard = null;
                                            FormMessage formMessage2 = new FormMessage("Данные перенесены на другую карту", "Касса");
                                            formMessage2.Show();

                                        }
                                        else
                                        {
                                            firstCardInfo = "";
                                            transfer = false;
                                            cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                                            transferCard = null;
                                            throw new Exception("Карта заблокирована");
                                        }
                                    }
                                }
                                else
                                {
                                    firstCardInfo = "";
                                    transfer = false;
                                    transferCard = null;
                                    FormMessage formMessage3 = new FormMessage("Используйте другую карту", "Касса");
                                    formMessage3.Show();

                                }
                            }
                            if (currentCard != null)
                            {
                                cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                            }
                            stk.clear();
                            return;
                        }
                        stk.push((byte)e.KeyValue);
                    }
                }
                catch (Exception exc)
                {
                    transferCard = null;
                    swapCard = null;
                    transfer = false;
                    swap = false;
                    firstCardInfo = "";
                    FormMessage formMessage4 = new FormMessage(exc.Message, "Касса");
                    formMessage4.Show();

                }
            }
        }
        public void delegateMetod(string parentName)
        {
            clientName = parentName;
        }
        private static string APP_PATH = "http://localhost:9000";
        public static Card selectCard(string inputInfo, string loginCard, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(inputInfo, loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetCard/");

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
                        return null;
                    }
                    else if (result == "\"null\"")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorbissness")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorcashier")
                    {
                        throw new Exception("Ошибка данных кассы/кассира.");
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
        public bool licenseCheck(string inputInfo, string loginCard, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(inputInfo, loginCard, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/CheckLicense/");

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
                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    return true;
                }
                else if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return false;
                }
                else
                {
                    throw new Exception(httpResponse.StatusDescription);
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                return false;
            }
        }
        //Событие кнопки "Пакеты"
        private void packetsButton_Click(object sender, EventArgs e)
        {
            if (isMouse(e))
            {
                foreach (var panel in panels)
                {
                    panel.Visible = false;
                }
                packetsPanel.Visible = true;
            }
        }
        //Событие кнопки "Призы"
        private void prizesButton_Click(object sender, EventArgs e)
        {
            if (isMouse(e))
            {
                foreach (var panel in panels)
                {
                    panel.Visible = false;
                }
                prizePanel.Visible = true;
            }
        }
        //Событие кнопки "Другое"
        private void elseButton_Click(object sender, EventArgs e)
        {
            if (isMouse(e))
            {
                foreach (var panel in panels)
                {
                    panel.Visible = false;
                }
                elsePanel.Visible = true;
            }
        }
        //Внесение средств
        private void replenishmentButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (Card.isDefined(currentCard))
                    {
                        decimal cash = 0;
                        if (Decimal.TryParse(calcTextBox.Text, out cash))
                        {
                            Form2 form2 = new Form2(cash, cardInfo, ip, currentCard, loginCardInfo);
                            form2.ShowDialog();
                            if (form2.dialogResult == DialogResult.Yes)
                            {
                                decimal money = form2.money;
                                decimal cardPrice = form2.moneyForCard;
                                decimal change = form2.change;
                                calcTextBox.Clear();
                                currentCard = form2.currentCard;
                                printCheck(cash, money, cardPrice, change, currentCard);
                                cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                                FormMessage formMessage = new FormMessage( "Номер карты: " + currentCard.cardId +
                                    "\nПополнено очков :" + money +
                                    "\nСдача :" + change, "Касса - Пополнение" );
                                formMessage.Show();
                            }
                            else if (form2.dialogResult == DialogResult.No)
                            {
                                FormMessage formMessage = new FormMessage("Ошибка пополнения карты", "Касса");
                                formMessage.Show();

                            }

                        }
                        else
                        {
                            FormMessage formMessage = new FormMessage("Введите количество полученных средств", "Касса");
                            formMessage.Show();

                        }
                    }
                    else
                    {
                        FormMessage formMessage = new FormMessage("Карта не определенна. Проведите картой для пополения", "Касса");
                        formMessage.Show();

                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //Возврат карты
        private void returnButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    WorkShiftReport workShiftReport = GetXreport(new LoginInfo(loginCardInfo, ip));
                    CardPrice cardPrice = new CardPrice();

                    if (workShiftReport.workShift.cashOnHand >= currentCard.cardCount)
                    {
                        currentCard.removeCard(cardInfo, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                       
                    }
                    else
                    {
                        FormMessage formMessage = new FormMessage("В кассе недостаточно средств", "Касса");
                        formMessage.Show();

                    }

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        //Обработчики кнопок калькулятора
        private void calcBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    var btn = sender as Button;
                    calcTextBox.Text += btn.Text;
                }
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
                if (isMouse(e)) calcTextBox.Text = "";
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
                if (isMouse(e))
                {
                    var text = calcTextBox.Text;
                    if (text.Length > 0)
                    {
                        calcTextBox.Text = text.Substring(0, text.Length - 1);
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void enterCalcButton_Click(object sender, EventArgs e)
        {

        }
        //-
        //Выбор тарифного плана
        private void packets2Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 1, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void packets7Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 3, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void packets5Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 2, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void packets10Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 4, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void packets15Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 5, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void packets20Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 6, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void packets25Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 7, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void packetStandartButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 0, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                   

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        //-
        //Меню "Другое"
        //Блокировка карты
        private void blockButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        currentCard.block(cardInfo, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                       

                    }
                    else
                    {
                        throw new Exception("Карта не определена");
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
        private void swapButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        swapCard = currentCard;
                        if (swapCard.cardStatus != 0)
                        {
                            swap = true;
                            firstCardInfo = cardInfo;
                            FormMessage formMessage = new FormMessage("Проведите новой картой", "Касса");
                            formMessage.Show();

                        }
                        else
                        {
                            throw new Exception("Карта заблокирована");
                        }
                       
                    }
                    else
                    {
                        throw new Exception("Проведите картой которую необходимо заменить");
                    }
                }

            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }

        }
        //Добавление бонусов
        private void addBonusButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        var bonuses = decimal.Parse(calcTextBox.Text);
                        currentCard.addBonuses(cardInfo, bonuses, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                        printCheckBonuses(bonuses,  currentCard);
                        calcTextBox.Clear();
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        //Добавление тикетов
        private void addTicketButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        int tickets = 0;
                        if (Int32.TryParse(calcTextBox.Text, out tickets))
                        {
                            currentCard.addTickets(cardInfo, tickets, loginCardInfo, ip);
                            cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                            printCheckTicket(tickets, currentCard);
                            calcTextBox.Clear();
                        }
                        else
                        {
                            throw new Exception("Введите количество билетов");
                        }
                    }
                    else
                    {
                        throw new Exception("Проведите картой на которую необходимо добавить билеты");
                    }

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        //-
        //Списание тикетов
        private void prize5Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 5, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(5, currentCard);
                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize10Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 10, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(10, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize20Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 20, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(20, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize50Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 50, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(50, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize100Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 100, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(100, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize200Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 200, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(200, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void prize500Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicket(cardInfo, 500, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(500, currentCard);

                    calcTextBox.Clear();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void elseButton_MouseClick(object sender, MouseEventArgs e)
        {

        }
        //-

        private void printCheck(decimal cash, decimal money, decimal cardPrice, decimal change, Card printCard)
        {
            PrintDocument p = new PrintDocument();
            Sale sale = startInfo.sales.Find(x => x.saleId == printCard.cardSale);
            string sale_name = "начальный";
            string stars = "***********************************\n";
            if (sale.saleValue != 0)
            {
                sale_name = sale.saleValue + "% скидка";
            }
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.Alignment = StringAlignment.Center;
            string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
            string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + date + "\nКассир: "
                + loginedCashier.cashierName + "\n" + stars + "Карта № "
                + printCard.cardId;
            check += " \nПолучено средств: " + cash;
            if(change > 0)
            {
                check += " \nСдача:  " + change;
            }            
            if (cardPrice > 0)
            {
                check += " \nЦена карты:   " + cardPrice;
            }
            check += " \nПополнение баланса:  " + money + " * 1.00"
                + "\n" + stars + "Карта № "
                + printCard.cardId + ".\nБаланс очков:  " + printCard.cardCount
                + "\nСкидка: " + sale_name
                + "\n---------------------------------------------------"
                + "\nОчки:  " + money;
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 200), format1);
                e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 150, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                p.Print();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception Occured While Printing", ex);
            }
        }
        private void printCheckBonuses(decimal bonus,  Card printCard)
        {
            PrintDocument p = new PrintDocument();
            Sale sale = startInfo.sales.Find(x => x.saleId == printCard.cardSale);
            string sale_name = "начальный";
            string stars = "***********************************\n";
            if (sale.saleValue != 0)
            {
                sale_name = sale.saleValue + "% скидка";
            }
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.Alignment = StringAlignment.Center;
            string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
            string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + DateTime.Now + "\nКассир: "
                + loginedCashier.cashierName + "\n" + stars + "Карта № "
                + printCard.cardId;            
            check += "\nПополнение бонусов: " + bonus + " * 1.00"
                + "\n" + stars + "Баланс бонусов:  " + printCard.cardBonus
                ;
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 80), format1);
                e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 80, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                p.Print();
                FormMessage formMessage2 = new FormMessage(check, "Касса - Пополнение бонусов");
                formMessage2.Show();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception Occured While Printing", ex);
            }
        }
        private void printCheckTicket(decimal tickets, Card printCard)
        {
            PrintDocument p = new PrintDocument();
            Sale sale = startInfo.sales.Find(x => x.saleId == printCard.cardSale);
            string sale_name = "начальный";
            string stars = "***********************************\n";
            if (sale.saleValue != 0)
            {
                sale_name = sale.saleValue + "% скидка";
            }
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.Alignment = StringAlignment.Center;
            string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
            string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + DateTime.Now + "\nКассир: "
                + loginedCashier.cashierName + "\n" + stars + "Карта № "
                + printCard.cardId;
                check += "\nПополнение билетов: " + tickets + " * 1.00"
                + "\n" + stars
                + "Баланс билетов:  " + printCard.cardTicket;
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 80), format1);
                e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 80, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                p.Print();
                FormMessage formMessage2 = new FormMessage(check, "Касса - Пополение билетов");
                formMessage2.Show();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception Occured While Printing", ex);
            }
        }
        private void printCheckRemoveTickets(decimal tickets, Card printCard)
        {
            PrintDocument p = new PrintDocument();
            Sale sale = startInfo.sales.Find(x => x.saleId == printCard.cardSale);
            string sale_name = "начальный";
            string stars = "***********************************\n";
            if (sale.saleValue != 0)
            {
                sale_name = sale.saleValue + "% скидка";
            }
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.Alignment = StringAlignment.Center;
            string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
            string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + DateTime.Now + "\nКассир: "
                + loginedCashier.cashierName + "\n" + stars + "Карта № "
                + printCard.cardId;
            check += " \nСписание билетов: " + tickets + " * 1.00"
                + "\n" + stars + "Баланс билетов :  " + printCard.cardTicket;
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 80), format1);
                e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 80, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                p.Print();
                FormMessage formMessage2 = new FormMessage(check, "Касса - Списание билетов");
                formMessage2.Show();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception Occured While Printing", ex);
            }
        }
        private void printCheckRemoveBonuses(decimal bonus, Card printCard)
        {
            PrintDocument p = new PrintDocument();
            Sale sale = startInfo.sales.Find(x => x.saleId == printCard.cardSale);
            string sale_name = "начальный";
            string stars = "***********************************\n";
            if (sale.saleValue != 0)
            {
                sale_name = sale.saleValue + "% скидка";
            }
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.Alignment = StringAlignment.Center;
            string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
            string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + DateTime.Now + "\nКассир: "
                + loginedCashier.cashierName + "\n" + stars + "Карта № "
                + printCard.cardId;
            check += "\nСписание бонусов: " + bonus + " * 1.00"
                + "\n" + stars  + "Баланс бонусов:  " + printCard.cardBonus;
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 80), format1);
                e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 80, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
            };
            try
            {
                p.Print();
                FormMessage formMessage2 = new FormMessage(check, "Касса - Списание бонусов");
                formMessage2.Show();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception Occured While Printing", ex);
            }
        }
        private void printTransactionsSpanding(string cardInfoString, string loginCard, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                PrintDocument p = new PrintDocument();
                string stars = "***********************************\n";
                string trans = "\n*********** ТРАНЗАКЦИИ ************\n";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetTransancionAttractionInfo/");
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
                TransancionAttractionInfo transancionAttractionInfo = new TransancionAttractionInfo();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    string r = result.Remove(result.Length - 1);
                    var k = JsonConvert.DeserializeObject(result);
                    transancionAttractionInfo = JsonConvert.DeserializeObject<TransancionAttractionInfo>(k.ToString());
                }
                if (transancionAttractionInfo.transactionsAttractions != null && transancionAttractionInfo.transactionsAttractions.Count > 0)
                {
                    string transactionInfo = "";
                    for (int i = 0; i < transancionAttractionInfo.transactionsAttractions.Count; i++)
                    {
                        TimeSpan day = new TimeSpan(24, 0, 0);
                        if (transancionAttractionInfo.card != null)
                        {
                            if (transancionAttractionInfo.attractions != null && transancionAttractionInfo.attractions.Count > 0)
                            {
                                if ((transancionAttractionInfo.transactionsAttractions[i].date.DayOfYear == DateTime.Now.DayOfYear) && (transancionAttractionInfo.transactionsAttractions[i].date.Year == DateTime.Now.Year))
                                {
                                    Attraction attraction = new Attraction();
                                    attraction = transancionAttractionInfo.attractions.Find(x => Int32.Parse(x.id.ToString()) == Int32.Parse(transancionAttractionInfo.transactionsAttractions[i].attractionId.ToString()));
                                    transactionInfo += transancionAttractionInfo.transactionsAttractions[i].date + "\n" + attraction.attractionName + "\nИгра\nОчков\n             -" + transancionAttractionInfo.transactionsAttractions[i].summ + "\nБонусов\n            -" + transancionAttractionInfo.transactionsAttractions[i].bonus + "\n" + stars ;


                                }

                            }
                            else
                            {
                                throw new Exception("Ошибка данных об аттракционах");
                            }
                        }
                        else
                        {
                            throw new Exception("Карта не определена");
                        }

                    }
                    string check = DateTime.Now +  trans  + DateTime.Now + "\n" + stars + " Карта № " + transancionAttractionInfo.card.cardId + "\n" + stars + transactionInfo + "ТЕКУЩИЙ БАЛАНС: " + transancionAttractionInfo.card.cardBonus + " очков\nБАЛАНС БОНУСОВ: " + transancionAttractionInfo.card.cardBonus + "\n" + stars;

                    p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                    {
                        e1.Graphics.Clear(Color.White);
                        e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                    };
                    try
                    {
                        p.Print();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Возникла ошибка во время печати", ex);
                    }

                }
                else
                {
                    FormMessage formMessage = new FormMessage("Нет транзакций", "Касса");
                    formMessage.Show();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void printTransactionRepanishment(string cardInfoString, string loginCard, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(cardInfoString, loginCard, ip);
                PrintDocument p = new PrintDocument();
                string stars = "***********************************\n";
                string trans = "\n*********** ТРАНЗАКЦИИ ************\n";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetTransancionCashierRegister/");
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
                TransactionCashRegisterInfo transactionCashRegisterInfo = new TransactionCashRegisterInfo();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    string r = result.Remove(result.Length - 1);
                    var k = JsonConvert.DeserializeObject(result);
                    transactionCashRegisterInfo = JsonConvert.DeserializeObject<TransactionCashRegisterInfo>(k.ToString());
                }
                if (transactionCashRegisterInfo.transactionsCashRegister != null &&
                    transactionCashRegisterInfo.transactionsCashRegister.Count > 0
                    )
                {

                    string transactionInfo = "";
                    for (int i = 0; i < transactionCashRegisterInfo.transactionsCashRegister.Count; i++)
                    {
                        TimeSpan day = new TimeSpan(24, 0, 0);
                        if (transactionCashRegisterInfo.card != null)
                        {
                            if ((transactionCashRegisterInfo.transactionsCashRegister[i].date.DayOfYear == DateTime.Now.DayOfYear) && (transactionCashRegisterInfo.transactionsCashRegister[i].date.Year == DateTime.Now.Year))
                            {
                                string operation = "";
                                if ((Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) >= 8
                                    && Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) <= 11)
                                    || Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) == 13
                                    || Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) == 17)
                                {
                                    operation = "-";
                                }
                                if ((Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) >= 5
                                    && Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) <= 7)
                                    || Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) == 12
                                    || Int32.Parse(transactionCashRegisterInfo.transactionsCashRegister[i].operation.ToString()) == 14)
                                {
                                    operation = "+";
                                }

                                    transactionInfo += 
                                    transactionCashRegisterInfo.transactionsCashRegister[i].date + "\n"
                                    + "Изменение счета\nОчков            " + operation.Trim()
                                    + transactionCashRegisterInfo.transactionsCashRegister[i].summ + "\nБонусов            "
                                    + operation.Trim()
                                    + transactionCashRegisterInfo.transactionsCashRegister[i].bonus + "\n"
                                    + "Билетов            "
                                    + operation.Trim()
                                    + transactionCashRegisterInfo.transactionsCashRegister[i].tickets + "\n"
                                    + stars
                                    ;


                                
                            }
                        }
                        else
                        {
                            throw new Exception("Карта не определена");
                        }

                    }

                        string check = DateTime.Now + trans + "Кассир: " + loginedCashier.cashierName + "\n" + DateTime.Now +
                                       "\n" + stars + " Карта № " + transactionCashRegisterInfo.card.cardId +
                                       "\n"+
                                       stars +
                                       transactionInfo
                                       + "ТЕКУЩИЙ БАЛАНС: " + transactionCashRegisterInfo.card.cardCount
                                       + " очков\nБАЛАНС БОНУСОВ: " + transactionCashRegisterInfo.card.cardBonus + "\n"
                                       + "БАЛАНС БИЛЕТОВ: " + transactionCashRegisterInfo.card.cardTicket + "\n"
                                       + stars
                                       ;
                        p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                        {
                            e1.Graphics.Clear(Color.White);
                            e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                        };
                        try
                        {
                            p.Print();
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Возникла ошибка во время печати", ex);
                        }
                }
                else
                {
                    FormMessage formMessage = new FormMessage("Нет транзакций", "Касса");
                    formMessage.Show();
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void printTransactionsButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        printTransactionsSpanding(cardInfo, loginCardInfo, ip);
                       
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }

        }
        //Перенос
        private void transferButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        transferCard = currentCard;
                        if ((int)transferCard.cardStatus == 1)
                        {
                            firstCardInfo = cardInfo;
                            transfer = true;

                            FormMessage formMessage = new FormMessage("Проведите картой на которую необходимо перенести средства", "Касса");
                            formMessage.Show();
                        }
                        else
                        {
                            firstCardInfo = "";
                            transfer = false;
                            throw new Exception("Карта заблокирована");
                        }
                       
                    }
                    else
                    {
                        throw new Exception("Проведите картой с которой необходимо перенсети средства");
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void changeInfoClientButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {

                    if (currentCard != null)
                    {
                        FormChangeInfo formChangeInfo = new FormChangeInfo(cardInfo, loginCardInfo, ip);
                        formChangeInfo.ShowDialog();
                        currentCard = form.selectCard(cardInfo, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + 
                            "\n" + "Всего внесенно:" + currentCard.TotalAccrued +
                            "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }

        }
        private void buttonActivatingCard_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {

                    if (currentCard != null)
                    {
                        currentCard.activateCard(cardInfo, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                       
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void Form1_FormClosing(object sender, FormClosedEventArgs e)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/FormClose/");

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
                if (httpResponse.StatusCode == HttpStatusCode.Accepted)
                {

                }
                else if (httpResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    throw new Exception("Ошибка закрытия сеанса");
                }
                else
                {
                    throw new Exception(httpResponse.StatusDescription);
                }

            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private void printTransactionRepanishmantButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        printTransactionRepanishment(cardInfo, loginCardInfo, ip);
                        
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        public void timerCheck()
        {
            if (!timer.Enabled)
            {
                timer.Start();
            }
        }
        DateTime loginDateTime;
        private void form_Load(object sender, EventArgs e)
        {
            loginDateTime = DateTime.Now;
            LoginInfo loginInfo = new LoginInfo();
            loginInfo = CheckShistInfo(loginCardInfo, ip);
            foreach (Control control in tableLayoutPanel2.Controls)
            {
                control.Enabled = false;
            }
            if (loginInfo.loginInfoResponce.cashier != null && loginInfo.loginInfoResponce.workShift == null && loginInfo.loginInfoResponce.isWokrShiftStarts == false)
            {
                cashierInfoLabel.Text = "Кассир: " + loginedCashier.cashierName + "\nВремя входа: " + loginDateTime;

                button5.Enabled = true;


            }
            else if (loginInfo.loginInfoResponce.cashier != null && loginInfo.loginInfoResponce.workShift != null && loginInfo.loginInfoResponce.isWokrShiftStarts == true)
            {
                cashierInfoLabel.Text = "Кассир: " + loginedCashier.cashierName + "\nВремя входа: " + loginDateTime;
                button6.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
            }
            else
            {
                FormMessage formMessage = new FormMessage("Ошибка получения данных о смене", "Касса");
                formMessage.Show();
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private LoginInfo CheckShistInfo(string loginCardInfo, string ip)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/CheckWorkShift/");

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
                    if (result == "null")
                    {
                        throw new Exception("Ошибка получения данных смены");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        loginInfo.loginInfoResponce = JsonConvert.DeserializeObject<LoginInfo.LoginInfoResponce>(k.ToString());
                    }
                }
                return loginInfo;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                return null;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/OpenWorkShift/");

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
                            throw new Exception("Ошибка открытия смены");
                        }
                        else
                        {
                            isWorkShiftOpen = true;
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            LoginInfo loginResponseInfo = new LoginInfo();

                            loginResponseInfo = JsonConvert.DeserializeObject<LoginInfo>(k.ToString());
                            if (loginResponseInfo.loginInfoResponce.workShift != null)
                            {
                                cashierInfoLabel.Text = "Кассир: " + loginResponseInfo.loginInfoResponce.cashier.cashierName + "\nВремя входа: " + loginDateTime + "\nВремя открытия смены:\n" + loginResponseInfo.loginInfoResponce.workShift.startTime;
                                FormMessage formMessage = new FormMessage("Смена открыта", "Касса");
                                formMessage.Show();

                                button5.Enabled = false;
                                button6.Enabled = false;
                                button5.Visible = false;
                                button6.Visible = false;



                                foreach (Control control in tableLayoutPanel2.Controls)
                                {
                                    control.Enabled = true;
                                }
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
                }
                //return loginInfo;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                //return null;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ContinueWorkShift/");

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
                            throw new Exception("Ошибка продолжения смены");
                        }
                        else
                        {
                            isWorkShiftOpen = true;
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);
                            LoginInfo loginResponseInfo = new LoginInfo();

                            loginResponseInfo = JsonConvert.DeserializeObject<LoginInfo>(k.ToString());
                            if (loginResponseInfo.loginInfoResponce.workShift != null)
                            {
                                cashierInfoLabel.Text = "Кассир: " + loginResponseInfo.loginInfoResponce.cashier.cashierName + "\nВремя входа: " + loginDateTime + "\nВремя открытия смены:\n" + loginResponseInfo.loginInfoResponce.workShift.startTime;
                                FormMessage formMessage = new FormMessage("Смена продолжена", "Касса");
                                formMessage.Show();
                                button5.Enabled = false;
                                button6.Enabled = false;
                                button5.Visible = false;
                                button6.Visible = false;
                                foreach (Control control in tableLayoutPanel2.Controls)
                                {
                                    control.Enabled = true;
                                }
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
                }
                //return loginInfo;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
                //return null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetXReport/");

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
                            throw new Exception("Ошибка вывода X-отчета");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);

                            WorkShiftReport workShiftReport = new WorkShiftReport();

                            workShiftReport = JsonConvert.DeserializeObject<WorkShiftReport>(k.ToString());
                            if (workShiftReport.workShift != null && workShiftReport.workShift.id > 0 && workShiftReport.workShiftInfos.Count > 0)
                            {
                                if (workShiftReport.workShiftInfos.FindAll(x => x.workShiftId == workShiftReport.workShift.id).Count > 0)
                                {
                                    Decimal totalInsert = workShiftReport.workShift.cashlessPaymentCount + workShiftReport.workShift.cashCount + workShiftReport.workShift.creditCardCount;
                                    PrintDocument p = new PrintDocument();
                                    string stars = "--------------------------------\n";
                                    StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
                                    format1.Alignment = StringAlignment.Center;
                                    string header = "X-отчет\n" + DateTime.Now + "\n" + ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                                    string check = "Закрыто смен: " + workShiftReport.workShift.closedShifts + "\n" + ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                                        + loginedCashier.cashierName + "\n\n" + stars + "Продаж :" + totalInsert + "\n\n"
                                        + "\t Наличные :" + workShiftReport.workShift.cash + "\n"
                                        + "\t Безналичная оплата :" + workShiftReport.workShift.cashlessPayment + "\n"
                                        + "\t Кредитная карта :" + workShiftReport.workShift.creditCard + "\n" +
                                        stars
                                        + "Возвратов :" + workShiftReport.workShift.refundCount + "\n\n"
                                        + "\t Наличные :" + workShiftReport.workShift.refund + "\n" +
                                        stars
                                        + "Внесений :" + workShiftReport.workShift.contributionsCount + "\n"
                                        + "\t Наличные :" + workShiftReport.workShift.contributions + "\n" +
                                        stars
                                        + "Изьятий :" + workShiftReport.workShift.withdrawalCount + "\n"
                                        + "\t Наличные :" + workShiftReport.workShift.withdrawal + "\n" +
                                        stars
                                        + "\t Выручка :" + workShiftReport.workShift.revenue + "\n"
                                        + "\t Наличных в кассе :" + workShiftReport.workShift.cashOnHand + "\n"
                                        + "\t Необнуляемая сумма :" + workShiftReport.workShift.nonNullableAmount;

                                    p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                                    {
                                        e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 200), format1);
                                        e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 150, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                                    };
                                    try
                                    {
                                        p.Print();
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Exception Occured While Printing", ex);
                                    }
                                    FormMessage formMessage = new FormMessage(check, "Касса - X-отчет");
                                    formMessage.Show();
                                }
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
                }
                //return loginInfo;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                //return null;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                string message = "Закрыть смену и получить Z-отчет?";
                string caption = "Z-отчет - Касса";
                //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult dialogResult = new DialogResult(); ;

                FormMessage formMessage = new FormMessage(message, caption, dialogResult);
                formMessage.ShowDialog();
                if (formMessage.result == DialogResult.Yes)
                {

                    LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetZReport/");

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
                                throw new Exception("Ошибка вывода Z-отчета");
                            }
                            else
                            {
                                string r = result.Remove(result.Length - 1);
                                var k = JsonConvert.DeserializeObject(result);

                                WorkShiftReport workShiftReport = new WorkShiftReport();

                                workShiftReport = JsonConvert.DeserializeObject<WorkShiftReport>(k.ToString());
                                if (workShiftReport.workShift != null && workShiftReport.workShift.id > 0 && workShiftReport.workShiftInfos.Count > 0)
                                {
                                    if (workShiftReport.workShiftInfos.FindAll(x => x.workShiftId == workShiftReport.workShift.id).Count > 0)
                                    {

                                        Decimal totalInsert = workShiftReport.workShift.cashlessPaymentCount + workShiftReport.workShift.cashCount + workShiftReport.workShift.creditCardCount;
                                        PrintDocument p = new PrintDocument();
                                        string stars = "--------------------------------\n";
                                        StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
                                        format1.Alignment = StringAlignment.Center;
                                        string header = "Z-отчет\n" + DateTime.Now + "\n" + ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                                        string check = "Закрыто смен: " + workShiftReport.workShift.closedShifts + "\n" + ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                                            + loginedCashier.cashierName + "\n\n" + stars + "Продаж :" + totalInsert + "\n\n"
                                            + "\t Наличные :" + workShiftReport.workShift.cash + "\n"
                                            + "\t Безналичная оплата :" + workShiftReport.workShift.cashlessPayment + "\n"
                                            + "\t Кредитная карта :" + workShiftReport.workShift.creditCard + "\n" +
                                            stars
                                            + "Возвратов :" + workShiftReport.workShift.refundCount + "\n\n"
                                            + "\t Наличные :" + workShiftReport.workShift.refund + "\n" +
                                            stars
                                            + "Внесений :" + workShiftReport.workShift.contributionsCount + "\n"
                                            + "\t Наличные :" + workShiftReport.workShift.contributions + "\n" +
                                            stars
                                            + "Изьятий :" + workShiftReport.workShift.withdrawalCount + "\n"
                                            + "\t Наличные :" + workShiftReport.workShift.withdrawal + "\n" +
                                            stars
                                            + "\t Выручка :" + workShiftReport.workShift.revenue + "\n"
                                            + "\t Наличных в кассе :" + workShiftReport.workShift.cashOnHand + "\n"
                                            + "\t Необнуляемая сумма :" + workShiftReport.workShift.nonNullableAmount;

                                        p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                                        {
                                            e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 200), format1);
                                            e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 150, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                                        };
                                        try
                                        {
                                            p.Print();
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception("Exception Occured While Printing", ex);
                                        }
                                        FormMessage formMessage2 = new FormMessage(check, "Касса - Z-отчет");
                                        formMessage2.Show();
                                        Close();
                                    }
                                }
                            }
                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage3 = new FormMessage(result, "Касса");
                            formMessage3.Show();


                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage4 = new FormMessage(result, "Касса");
                            formMessage4.Show();

                        }
                    }
                    //return loginInfo;
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                Close();
                //return null;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                string message = "Изьять средства из кассы";
                string caption = "Изьятие средств - Касса";
                //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult dialogResult = new DialogResult();

                FormMessage formMessage = new FormMessage(message, caption, dialogResult);
                formMessage.ShowDialog();

                if (formMessage.result == DialogResult.Yes)
                {

                    LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/WithdrawaCash/");

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
                                throw new Exception("Ошибка изьятия средств");
                            }
                            else
                            {
                                string r = result.Remove(result.Length - 1);
                                var k = JsonConvert.DeserializeObject(result);
                                string stars = "--------------------------------\n";
                                WorkShift workShift = new WorkShift();

                                workShift = JsonConvert.DeserializeObject<WorkShift>(k.ToString());
                                if (workShift != null && workShift.id > 0)
                                {
                                    //string header = "X-отчет\n" + DateTime.Now + "\n" + ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                                    string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                                        + loginedCashier.cashierName + "\n\n" + stars
                                        + "Изьятий :" + workShift.withdrawalCount + "\n"
                                        + "\t Наличные :" + workShift.withdrawal + "\n" +
                                        stars
                                        + "\t Выручка :" + workShift.revenue + "\n"
                                        + "\t Наличных в кассе :" + workShift.cashOnHand + "\n"
                                        + "\t Необнуляемая сумма :" + workShift.nonNullableAmount;
                                    FormMessage formMessage2 = new FormMessage(check," Изьятие средств - Касса");
                                    formMessage2.Show();
                                }
                            }
                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage3 = new FormMessage(result, "Касса");
                            formMessage3.Show();


                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage3 = new FormMessage(result, "Касса");
                            formMessage3.Show();

                        }
                    }
                }
                calcTextBox.Clear();
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
                calcTextBox.Clear();
                //Close();
            }
        }

        private void ReturnCashForCardbutton_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    WorkShiftReport workShiftReport = GetXreport(new LoginInfo(loginCardInfo, ip));
                    CardPrice cardPrice = new CardPrice();

                    if (workShiftReport.workShift.cashOnHand >= Form2.GetCardPrice(loginCardInfo, ip))
                    {
                        currentCard.returnCashForCard(cardInfo, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                       
                    }
                    else
                    {
                        FormMessage formMessage = new FormMessage("В кассе недостаточно средств", "Касса");
                        formMessage.Show();

                    }
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }

        private void elsePanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeTicketsInCount(cardInfo, loginCardInfo, ip, Decimal.Parse(calcTextBox.Text));
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveTickets(Decimal.Parse(calcTextBox.Text), currentCard);
                    calcTextBox.Text = "";
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeBonuses(cardInfo, loginCardInfo, ip, Decimal.Parse(calcTextBox.Text));
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveBonuses(Decimal.Parse(calcTextBox.Text), currentCard);
                    calcTextBox.Text = "";
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                decimal contribution = Decimal.Parse(calcTextBox.Text);
                string message = "Внести " + contribution + " на кассу?";
                string caption = "Внесение средств - Касса";
                //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult dialogResult = new DialogResult();

                FormMessage formMessage = new FormMessage(message, caption, dialogResult);
                formMessage.ShowDialog();

                if (formMessage.result == DialogResult.Yes)
                {

                    ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo();
                    replenishmentInfo.loginCard = loginCardInfo;
                    replenishmentInfo.ip = ip;
                    replenishmentInfo.cash = Decimal.Parse(calcTextBox.Text);
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/ContributeCash/");

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
                                throw new Exception("Ошибка внесения средств");
                            }
                            else
                            {
                                string r = result.Remove(result.Length - 1);
                                var k = JsonConvert.DeserializeObject(result);
                                string stars = "--------------------------------\n";
                                WorkShift workShift = new WorkShift();

                                workShift = JsonConvert.DeserializeObject<WorkShift>(k.ToString());
                                if (workShift != null && workShift.id > 0)
                                {
                                    //string header = "X-отчет\n" + DateTime.Now + "\n" + ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                                    string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                                        + loginedCashier.cashierName + "\n\n" + stars
                                        + "Внесений :" + workShift.contributionsCount + "\n"
                                        + "\t Наличные :" + workShift.contributions + "\n" +
                                        stars
                                        + "\t Выручка :" + workShift.revenue + "\n"
                                        + "\t Наличных в кассе :" + workShift.cashOnHand + "\n"
                                        + "\t Необнуляемая сумма :" + workShift.nonNullableAmount;
                                    FormMessage formMessage2 = new FormMessage(check, "Внесение средств на кассу - Касса" );
                                    formMessage2.Show();
                                }
                            }
                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage3 = new FormMessage(result, "Касса");
                            formMessage3.Show();


                        }
                        else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                        {
                            result = streamReader.ReadToEnd();
                            FormMessage formMessage4 = new FormMessage(result, "Касса");
                            formMessage4.Show();

                        }
                    }
                }
                calcTextBox.Clear();
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса - Внос средств на кассу");
                formMessage.Show();

                calcTextBox.Clear();
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                WorkShiftReport workShiftReport = GetXreport(new LoginInfo(loginCardInfo, ip));
                if (workShiftReport.workShift.cashOnHand >= Decimal.Parse(calcTextBox.Text))
                {
                    string caption = "Изьять средства из кассы";
                    string message = "Изьять " + calcTextBox.Text + " из кассы? ";
                    //MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult dialogResult = new DialogResult();

                    FormMessage formMessage = new FormMessage(message, caption, dialogResult);
                    formMessage.ShowDialog();

                    if (formMessage.result == DialogResult.Yes)
                    {
                        
                        ReplenishmentInfo replenishmentInfo = new ReplenishmentInfo();

                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/WithdrawaSomeCash/");
                        replenishmentInfo.cash = Decimal.Parse(calcTextBox.Text);
                        replenishmentInfo.loginCard = loginCardInfo;
                        replenishmentInfo.ip = ip;

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
                                    throw new Exception("Ошибка изьятия средств");
                                }
                                else
                                {
                                    string r = result.Remove(result.Length - 1);
                                    var k = JsonConvert.DeserializeObject(result);
                                    string stars = "--------------------------------\n";
                                    WorkShift workShift = new WorkShift();

                                    workShift = JsonConvert.DeserializeObject<WorkShift>(k.ToString());
                                    if (workShift != null && workShift.id > 0)
                                    {
                                        //string header = "X-отчет\n" + DateTime.Now + "\n" + ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                                        string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                                            + loginedCashier.cashierName + "\n\n" + stars
                                            + "Изьятий :" + workShift.withdrawalCount + "\n"
                                            + "\t Наличные :" + workShift.withdrawal + "\n" +
                                            stars
                                            + "\t Выручка :" + workShift.revenue + "\n"
                                            + "\t Наличных в кассе :" + workShift.cashOnHand + "\n"
                                            + "\t Необнуляемая сумма :" + workShift.nonNullableAmount;
                                        FormMessage formMessage2 = new FormMessage(check,"Изьятие средств - Касса" );
                                        formMessage2.Show();
                                    }
                                }
                            }
                            else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                            {
                                result = streamReader.ReadToEnd();
                                FormMessage formMessage3 = new FormMessage(result, "Касса");
                                formMessage3.Show();


                            }
                            else if (httpResponse.StatusCode == HttpStatusCode.NotAcceptable)
                            {
                                result = streamReader.ReadToEnd();
                                FormMessage formMessage4 = new FormMessage(result, "Касса");
                                formMessage4.Show();

                            }
                        }
                    }
                }
                else
                {
                    FormMessage formMessage = new FormMessage("В кассе недостаточно средств", "Касса");
                    formMessage.Show();

                }
                calcTextBox.Clear();
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
                calcTextBox.Clear();
                //Close();
            }
        }
        public WorkShiftReport GetXreport(LoginInfo loginInfo)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetXReport/");

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
                            throw new Exception("Ошибка вывода X-отчета");
                        }
                        else
                        {
                            string r = result.Remove(result.Length - 1);
                            var k = JsonConvert.DeserializeObject(result);

                            WorkShiftReport workShiftReport = new WorkShiftReport();

                            workShiftReport = JsonConvert.DeserializeObject<WorkShiftReport>(k.ToString());
                            if (workShiftReport.workShift != null && workShiftReport.workShift.id > 0 && workShiftReport.workShiftInfos.Count > 0)
                            {
                                if (workShiftReport.workShiftInfos.FindAll(x => x.workShiftId == workShiftReport.workShift.id).Count > 0)
                                {
                                    return workShiftReport;
                                }
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
                }
                return null;
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

                return null;
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {


                Card card = selectCard(cardInfo, loginCardInfo, ip);
                string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                PrintDocument p = new PrintDocument();
                string stars = "--------------------------------\n";
                StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
                format1.Alignment = StringAlignment.Center;
                string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "Кассир: "
                    + loginedCashier.cashierName + "\n" + stars +
                    card.ToString(startInfo.sales.Find(x => x.saleId == card.cardSale), startInfo.cardStatuses.Find(x => x.status_id == card.cardStatus));
                p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                {
                    e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 80), format1);
                    e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 80, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                };
                try
                {
                    p.Print();
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception Occured While Printing", ex);
                }
                FormMessage formMessage = new FormMessage(check, "Касса - X-отчет");
                formMessage.Show();
            }
            catch(Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                FormInputNumber formInputNumber = new FormInputNumber();
                formInputNumber.ShowDialog();
                if (formInputNumber.number > 0)
                {
                    int number = formInputNumber.number;

                    Card card = selectCardByNymber(number, loginCardInfo, ip);
                    string header = ConfigurationManager.AppSettings.Get("checkHeader").Replace("\\n", "\n");
                    PrintDocument p = new PrintDocument();
                    string stars = "--------------------------------\n";
                    StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
                    format1.Alignment = StringAlignment.Center;
                    string check = ConfigurationManager.AppSettings.Get("checkCompanyName").Replace("\\n", "\n") + stars + "\nКассир: "
                        + loginedCashier.cashierName + "\n\n" + stars +
                        card.ToString(startInfo.sales.Find(x => x.saleId == card.cardSale), startInfo.cardStatuses.Find(x => x.status_id == card.cardStatus));
                    p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
                    {
                        e1.Graphics.DrawString(header, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, 200), format1);
                        e1.Graphics.DrawString(check, new Font("Tahoma", 10), new SolidBrush(Color.Black), new RectangleF(0, 150, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));
                    };
                    try
                    {
                        p.Print();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Exception Occured While Printing", ex);
                    }
                    FormMessage formMessage = new FormMessage(check, "Касса - X-отчет");
                    formMessage.Show();
                }
            }

            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        public static Card selectCardByNymber(int number, string loginCardInfo, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(number.ToString(), loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetCardByNumber/");

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
                        return null;
                    }
                    else if (result == "\"null\"")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorbissness")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorcashier")
                    {
                        throw new Exception("Ошибка данных кассы/кассира.");
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

        private void button14_Click(object sender, EventArgs e)
        {
            if (isMouse(e))
            {
                foreach (var panel in panels)
                {
                    panel.Visible = false;
                }
                packetsPanel.Visible = true;
            }
        }

        private void packetsPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void buttonBDay_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.setPacket(cardInfo, 7, loginCardInfo, ip);
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button9_Click_1(object sender, EventArgs e)
        {

        }
        public static string selectCardAndCardInfoByNymber(int number, string loginCardInfo, string ip)
        {
            try
            {
                CardInfo cardInfo = new CardInfo(number.ToString(), loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetCardInfoByNumber/");

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
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        return null;
                    }
                    else if (result == "\"null\"")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorbissness")
                    {
                        throw new Exception("Ошибка данных карты.");
                    }
                    else if (JsonConvert.DeserializeObject<string>(result) == "errorcashier")
                    {
                        throw new Exception("Ошибка данных кассы/кассира.");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        return JsonConvert.DeserializeObject<string>(k.ToString());
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
        private void button15_Click(object sender, EventArgs e)
        {
            cardInfo = "";
            currentCard = null;
            swap = false;
            swapCard = null;
            transfer = false;
            transferCard = null;
            firstCardInfo = "";
            cardInfoLabel.Text = "";
        }
        private void button16_Click(object sender, EventArgs e)
        {
            
            FormInputNumber formInputNumber = new FormInputNumber();
            formInputNumber.ShowDialog();
            if (formInputNumber.number > 0)
            {
                int number = formInputNumber.number;
                cardInfo = selectCardAndCardInfoByNymber(number, loginCardInfo, ip);
                currentCard = selectCard(cardInfo, loginCardInfo, ip);
                if(currentCard != null && currentCard.id > 0)
                {
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                }                
            }
        }

        private void buttonAddDayBonus_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    if (currentCard != null)
                    {
                        var dayBonuses = decimal.Parse(calcTextBox.Text);
                        currentCard.addDayBonuses(cardInfo, dayBonuses, loginCardInfo, ip);
                        cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus +"\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                        printCheckBonuses(dayBonuses, currentCard);
                        calcTextBox.Clear();
                    }
                    else
                    {
                        throw new Exception("Карта не определена");
                    }

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }

        private void buttonRemoveDayBonus_Click(object sender, EventArgs e)
        {
            try
            {
                if (isMouse(e))
                {
                    currentCard.removeDayBonuses(cardInfo, loginCardInfo, ip, Decimal.Parse(calcTextBox.Text));
                    cardInfoLabel.Text = currentCard.ToString(this.startInfo) + "\n" + "Суточный бонус:" + currentCard.cardDayBonus + "\n" + "Дата внесения сут. бонуса:" + currentCard.cardDayBonusDateTime + "\n" + "Всего внесенно:" + currentCard.TotalAccrued + "\n" + "Всего потраченно:" + currentCard.TotalSpend +
                            "\n" + "Всего игр: " + currentCard.TotalGames +
                            "\n" + "Телефон: " + currentCard.Telephone +
                            "\n" + "Email: " + currentCard.Email;
                    printCheckRemoveBonuses(Decimal.Parse(calcTextBox.Text), currentCard);
                    calcTextBox.Text = "";
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
