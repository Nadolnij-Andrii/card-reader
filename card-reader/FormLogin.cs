using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace card_reader
{
    public partial class FormLogin : Form
    {
        private string APP_PATH = "http://localhost:9000";
        public FormLogin()
        {
            if (Process.GetProcesses().Count(x => x.ProcessName == "card-reader") > 1)
                Process.GetCurrentProcess().Kill();
            InitializeComponent();
            APP_PATH = ConfigurationManager.AppSettings.Get("serverURI");
        }
        DateTime date;
        stack stk = new stack(30);
        LoginInfo loginInfo = new LoginInfo();
        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            date = DateTime.Now;
        }
        private void form_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((DateTime.Now - date) < TimeSpan.FromMilliseconds(30)) e.Handled = true;
        }
        private void form_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if ((DateTime.Now - date) < TimeSpan.FromMilliseconds(30))
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        //string inputInfo = ";111=125001=3=63115235?";
                        //MatchCollection matches = Regex.Matches(inputInfo, @"([0-9])+");
                        MatchCollection matches = Regex.Matches(Encoding.ASCII.GetString(stk.get()), @"([0-9])+");
                        string inputInfo = Encoding.ASCII.GetString(stk.get());
                    if (matches.Count >= 3)
                    {
                        var ip = GetLocalIPAddress();
                            loginInfo = new LoginInfo(inputInfo, ip.MapToIPv4().ToString());

                            LoginInfo.LoginInfoResponce loginInfoResponce = new LoginInfo.LoginInfoResponce();
                            GetCashierInfo();
                            stk.clear();
                            return;
                        }
                        stk.clear();
                    }
                    stk.push((byte)e.KeyValue);
                }
            }
            catch (Exception exc)
            {
                stk.clear();
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();
            }
        }
        private StartInfo getStartInfo(string loginCardInfo, string ip)
        {
            try
            {
                LoginInfo loginInfo = new LoginInfo(loginCardInfo, ip);
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetStartInfo/");

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
                StartInfo startInfo = new StartInfo();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result == "null")
                    {
                        throw new Exception("Ошибка получения данных для запуска");
                    }
                    else
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);
                        startInfo = JsonConvert.DeserializeObject<StartInfo>(k.ToString());
                        return startInfo;
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
        private void GetCashierInfo()
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(APP_PATH + "/api/cashiermashine/GetCashierInfo/");
                var ip = GetLocalIPAddress();
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                LoginInfo newLoginInfo = new LoginInfo(loginInfo.cardInfo, ip.MapToIPv4().ToString());
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(newLoginInfo, Formatting.Indented, new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                LoginInfo.LoginInfoResponce loginInfoResponce = new LoginInfo.LoginInfoResponce();
                string result = "";
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                    if (result != "null")
                    {
                        string r = result.Remove(result.Length - 1);
                        var k = JsonConvert.DeserializeObject(result);

                        loginInfoResponce = JsonConvert.DeserializeObject<LoginInfo.LoginInfoResponce>(k.ToString());
                    }
                    else
                    {
                        throw new Exception("Ошибка данных кассира/кассы");
                    }
                }
                if (loginInfoResponce != null)
                {
                    StartInfo startInfo = getStartInfo(newLoginInfo.cardInfo, ip.MapToIPv4().ToString());
                    if (startInfo != null)
                    {
                        form formCashRegister = new form(
                            loginInfoResponce.cashier,
                            loginInfoResponce.cashierRegister,
                            newLoginInfo.cardInfo,
                            ip.MapToIPv4().ToString(),
                            startInfo
                        );
                        this.Hide();
                        formCashRegister.ShowDialog();
                        this.Show();
                    }
                    else
                    {
                        FormMessage formMessage = new FormMessage("Ошибка получения начальных данных.", "Касса");
                        formMessage.Show();

                    }
                }
                else if (loginInfoResponce == null)
                {
                    FormMessage formMessage = new FormMessage("Ошибка входа.\nПроверьте карту.", "Касса");
                    formMessage.Show();

                }
                else
                {
                    FormMessage formMessage = new FormMessage(httpResponse.StatusDescription, "Касса");
                    formMessage.Show();

                }
            }
            catch (Exception exc)
            {
                FormMessage formMessage = new FormMessage(exc.Message, "Касса");
                formMessage.Show();

            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void FormLogin_Load(object sender, EventArgs e)
        {

        }
    }
   
}
