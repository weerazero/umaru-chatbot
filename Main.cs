using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Matrix;
using Matrix.Xml;
using Matrix.Xmpp;
using Matrix.Xmpp.Client;
using Umaru_AI.Settings;
using RosterItem = Matrix.Xmpp.Roster.RosterItem;
using Subscription = Matrix.Xmpp.Roster.Subscription;
using System.Runtime;


namespace Umaru_AI
{
    public partial class Main : Form
    {

        const string SettingP = "Settings";
        int move, mx, my; // ย้ายฟอรม
        string  checkL="";bool checkBot = true; //แปลภาษา

        Double resultValue = 0;//เครื่องคิดเลข
        String operationPerformed = "";//เครื่องคิดเลข
        bool isOperationPerformed = false;//เครื่องคิดเลข
        
        //ประกาศ API message
        [DllImport("user32.dll")] //ยืม referense .dll  ของ OS
        
        public static extern IntPtr SendMessage(IntPtr window, int message, int wparam, int lparam); //ส่งค่าข้อความลงล่าสุด
        const int WM_VSCROLL = 0x115; //เกี่ยวกับการเลื่อน listview เป็นล่าสุด
        const int SB_BOTTOM = 7; //เกี่ยวกับการเลื่อน listview เป็นล่าสุด
        private readonly Dictionary<string, ListViewGroup> _dictContactGroups = new Dictionary<string, ListViewGroup>(); //เกี่ยวกับ listviewGroup
        private readonly Dictionary<Jid, RosterItem> _dictContats = new Dictionary<Jid, RosterItem>();
        private Settings.Settings _settings; //เกี่ยวกับการตั้งค่า
        private Login _login;

        public Main()
        {
            InitializeComponent();

            SetLicense();
            RegisterCustomElements();
            InitSettings();
            InitContactList();
        }

        private static void RegisterCustomElements()
        {
            Factory.RegisterElement<Settings.Settings>("ag-software:settings", "Settings");
            Factory.RegisterElement<Login>("ag-software:settings", "Login");
        }
        private void InitSettings()
        {
            _settings = Util.LoadSettings();
            if (_settings.Login == null)
                _settings.Login = new Login();

            _login = _settings.Login;

            if (_login != null)
            {
                if (!string.IsNullOrEmpty(_login.User))
                    txtUsername.Text = _login.User;

                if (!string.IsNullOrEmpty(_login.Server))
                    // = _login.Server;

                if (!string.IsNullOrEmpty(_login.Password))
                    txtPassword.Text = _login.Password;
            }
        }
        private static void SetLicense() //ลิกขสิทธ์ ใลอนุญาติในการใช้ api XMPP Matrix
        {
            // request demo license here:
            // http://www.ag-software.net/matrix-xmpp-sdk/request-demo-license/
            const string LIC = @"eJxkkFtTwjAQhf+K46ujoaJcnDWjDUXQolBUhLeUpiXYNDWXFvj1oty8vOzs
7rdn98yCz6cs0+xoIdJMXx/T5FTL2JRUsat0g44x9JWM7NR0Izw0NuIS0KED
A0szw80SO4D2ORCrjRRMYXikgmGvoKmlRipA3zUQKXKaLXeAy+xoawXQjoEn
KE+xpinTNz+cnUXroQ1bD+8PveQRNcxb5Fyx1jrD5xWnVmk6VUD/EHR1iwmJ
jbLrXdsCvuJv/aVT+dL/ATDkSUaNVQzbjl6VuRP1X/vtMeGoES/Hes7avY8H
gaZE9dnl4N0JTwztBKuQiNnCLKukSJLBSU6c+uQ2JkS79eYYpYYGppIHb2/q
2QuLc78xHs09139yJwx5sxWaN4Jk5N4+XaC85Pe+lnmtbNNQsJek4Q/Lak+i
WX0StGum+KA2jNMAlUFxN1AjN+5Nmu/XgA6+AW3fjT8FEA==";
            Matrix.License.LicenseManager.SetLicense(LIC);
            // when something is wrong with your license you can find the error here
            Console.WriteLine(Matrix.License.LicenseManager.LicenseError);
        }     
        private void button2_Click(object sender, System.EventArgs e) //Login Hangout
        {
            

            ShowMsgbot("กำลังล็อกอิน Hangout");
            string[] tmp = txtUsername.Text .Split ('@');
            xmppClient.SetUsername(tmp[0]);
            xmppClient.SetXmppDomain("gmail.com");
            xmppClient.Password = txtPassword.Text;

            // BOSH exmaple
            //xmppClient.Transport = Matrix.Net.Transport.BOSH;
            //xmppClient.Uri = new System.Uri("http://matrix.ag-software.de/http-bind/");

            xmppClient.Status = "ready for chat";
            xmppClient.Show = Matrix.Xmpp.Show.chat;

            // set settings
            _login.User = txtUsername.Text;
            _login.Server = "gmail.com";
            _login.Password = txtPassword.Text;

            xmppClient.Open();
        }
        private void xmppClient_OnError(object sender, ExceptionEventArgs e) //แสดงerror ไม่ทราบสาเหตุ
        {
            DisplayEvent("พบผิดพลาดไม่ทราบสาเหตุ");
            ShowMsgbot("พบผิดพลาดไม่ทราบสาเหตุ");
        }
        private void xmppClient_OnLogin(object sender, Matrix.EventArgs e)// แสดงสถานะล้อกอิน
        {
            DisplayEvent("ทำการล็อกอิน");
            ShowMsgbot("ทำการล็อกอิน Hangout");
        }
        private void DisplayEvent(string ev)
        {
            listEvents.Items.Add(ev);
            listEvents.SelectedIndex = listEvents.Items.Count - 1;
        }
        private void xmppClient_OnBind(object sender, JidEventArgs e)
        {
            DisplayEvent("ทำการเชื่อมโยง");//เกาะ โยง
        }
        private void xmppClient_OnClose(object sender, Matrix.EventArgs e)//แสดง ปิดการเชื่อมต่อ
        {
            DisplayEvent("ทำการปิดการเชื่อมต่อ");
            listContacts.Items.Clear();
        }
        private void xmppClient_OnRosterEnd(object sender, Matrix.EventArgs e)//แสดงสถานะปจุบัน
        {
            DisplayEvent("สถานะปัจจุบัน");
        }
        private void xmppClient_OnRosterItem(object sender, Matrix.Xmpp.Roster.RosterEventArgs e)//จัดการผู้ที่ติดต่อทั้งหมด goupbox
        {
            DisplayEvent(string.Format("รายการชื่อผู้ติดต่อ\t{0}\t{1}", e.RosterItem.Jid, e.RosterItem.Name));

            if (e.RosterItem.Subscription != Subscription.remove)
            {
                // set a default group name
                string groupname = "รายชื่อกลุ่มผู้ติดต่อ";

                // id the contact has groups get the 1st group. In this example we don't support multiple or nested groups
                // for contacts, but XMPP has support for this.
                if (e.RosterItem.HasGroups)
                    groupname = e.RosterItem.GetGroups()[0];

                if (!_dictContactGroups.ContainsKey(groupname))
                {
                    var newGroup = new ListViewGroup(groupname);
                    _dictContactGroups.Add(groupname, newGroup);
                    listContacts.Groups.Add(newGroup);
                }

                var listGroup = _dictContactGroups[groupname];

                // contact already exists, this is a contact update
                if (_dictContats.ContainsKey(e.RosterItem.Jid))
                {
                    listContacts.Items.RemoveByKey(e.RosterItem.Jid);
                }

                //var newItem = new ListViewItem(e.RosterItem.Jid, listGroup) {Name = e.RosterItem.Jid};
                var newItem = new RosterListViewItem(e.RosterItem.Name ?? e.RosterItem.Jid, 0, listGroup)
                { Name = e.RosterItem.Jid.Bare };
                newItem.SubItems.AddRange(new[] { "", "" });


                listContacts.Items.Add(newItem);
            }
        }
        private void xmppClient_OnPresence(object sender, PresenceEventArgs e)//แสดงไฟโชวสถานะออนไลน
        {
            DisplayEvent(string.Format("การระบุตัวตน\t{0}", e.Presence.From));

            var item = listContacts.Items[e.Presence.From.Bare] as RosterListViewItem;
            if (item != null)
            {
                item.ImageIndex = Util.GetRosterImageIndex(e.Presence);
                string resource = e.Presence.From.Resource;
                if (e.Presence.Type != PresenceType.unavailable)
                {
                    if (!item.Resources.Contains(resource))
                        item.Resources.Add(resource);
                }
                else
                {
                    if (item.Resources.Contains(resource))
                        item.Resources.Remove(resource);
                }
            }
        }
        private void xmppClient_OnMessage(object sender, MessageEventArgs e)//จัดการข้อความเข้า
        {
            DisplayEvent("ข้อความเข้า"); // แสดงผลการทำงาน ข้อความเข้า

            if (e.Message.Body != null)
            {
                if (!Util.ChatForms.ContainsKey(e.Message.From.Bare))
                {
                    //get nickname from the roster listview
                    string nick = e.Message.From.Bare;
                    var item = listContacts.Items[e.Message.From.Bare];
                    if (item != null)
                        nick = item.Text;

                    var f = new HangoutChat(e.Message.From, xmppClient, nick);
                    f.Show();
                    f.IncomingMessage(e.Message);
                }
            }
        }
        private void button3_Click(object sender, System.EventArgs e)//ปิดการเชื่อมต่อ
        {
            xmppClient.Close();
            ShowMsgbot("ยกเลิกการเชื่อมต่อ Hangout");
        }
        private void xmppClient_OnAuthError(object sender, Matrix.Xmpp.Sasl.SaslEventArgs e)//id or pass ผิด
        {
            DisplayEvent("รหัสหรือไอดีผิดพลาด");
            ShowMsgbot("รหัสหรือไอดีผิดพลาดหรือเปล่า?");
            xmppClient.Close();
        }
        private void xmppClient_OnReceiveXml(object sender, TextEventArgs e)//รับค่า xml พร้อมกำหนดคุณลักษณะแสดงผล
        {
            rtfDebug.SelectionStart = rtfDebug.Text.Length;
            rtfDebug.SelectionLength = 0;
            rtfDebug.SelectionColor = Color.Red;
            rtfDebug.AppendText("ส่ง: ");
            rtfDebug.SelectionColor = Color.Black;
            rtfDebug.AppendText(e.Text);
            rtfDebug.AppendText("\r\n");
            ScrollRtfToEnd(rtfDebug);
        }
        private void xmppClient_OnSendXml(object sender, TextEventArgs e)//ส่ง xml พร้อมกำหนดคุณลักษณะแสดงผล
        {
            rtfDebug.SelectionStart = rtfDebug.Text.Length;
            rtfDebug.SelectionLength = 0;
            rtfDebug.SelectionColor = Color.Blue;
            rtfDebug.AppendText("รับ: ");
            rtfDebug.SelectionColor = Color.Black;
            rtfDebug.AppendText(e.Text);
            rtfDebug.AppendText("\r\n");
            ScrollRtfToEnd(rtfDebug);
        }
        private void ScrollRtfToEnd(RichTextBox rtf) //เลื่อนสถานะแสดงข้อความล่าสุด
        {
            SendMessage(rtf.Handle, WM_VSCROLL, SB_BOTTOM, 0);
        }
        private void InitContactList() //จัดรูปแบบสถานะผู้ใช้ติดต่อ
        {
            listContacts.Clear();

            listContacts.Columns.Add("Name", 100, HorizontalAlignment.Left);
            listContacts.Columns.Add("Status", 75, HorizontalAlignment.Left);
            listContacts.Columns.Add("Resource", 75, HorizontalAlignment.Left);

            listContacts.LargeImageList = ilstStatus;
        }
        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            Util.SaveSettings(_settings);//บันทึกการตั้งค่า ไอ พาส คุณลักษณะ
        }
        private void chatToolStripMenuItem_Click(object sender, System.EventArgs e)//เรียกใช้ฟรอมจากชื่อของผู้ที่แชท
        {
            if (listContacts.SelectedItems.Count > 0)
            {
                var item = listContacts.SelectedItems[0];
                if (!Util.ChatForms.ContainsKey(item.Name))
                {
                    var f = new HangoutChat(item.Name, xmppClient, item.Text);
                    f.Show();
                }
            }
        }
        private void listContacts_MouseUp(object sender, MouseEventArgs e) //การคลิกขวาที่ผู้ติดต่อ
        {
            if (listContacts.SelectedItems.Count != 0) //คลิ้กที่ผู้ติดต่อแล้วค่าจะไม่เท่ากับ 0
            {
                ctxMenuRoster.Show(Cursor.Position);//โชวเมนูผู้ติดต่อ
            }
        }


        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            move = 0;
        }//ย้ายฟรอม

        private void button1_Click(object sender, System.EventArgs e)//แปลภาษา
        {
            if (langin.Text == "" || langin.Text == null)
            {
                langin.Focus();
                ShowMsgbot("อย่าปล่อยให้ว่างไว้สิ");
            }
            else
            {
                //by google
                //id = source (input lang)
                //id = result_box(output lang)
                HtmlDocument doc = webBrowser1.Document;
                HtmlElement input = doc.GetElementById("source");
                input.SetAttribute("value", langin.Text);
                switchlangurl();
            }
        }
        private void switchlangurl() //สลับภาษาและเปลี่ยนที่อยู่ url
        {
            if (comboBox1.SelectedItem.ToString()== "ภาษาอังกฤษ" && comboBox2.SelectedItem.ToString() == "ภาษาไทย")
            {
                if(checkL != "enth")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#en/th/");
                    checkL = "enth";
                }
                Translation.Interval = 3000;
                Translation.Start();
                
            }
            else if (comboBox1.SelectedItem.ToString() == "ภาษาอังกฤษ" && comboBox2.SelectedItem.ToString() == "ภาษาญี่ปุ่น")
            {
                if (checkL != "enjp")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#en/ja/");
                    checkL = "enjp";
                }
                Translation.Interval = 3000;
                Translation.Start();
                
            }
            else if (comboBox1.SelectedItem.ToString() == "ภาษาไทย" && comboBox2.SelectedItem.ToString() == "ภาษาอังกฤษ")
            {
                if (checkL != "then")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#th/en/");
                    checkL = "then";
                }
                Translation.Interval = 3000;
                Translation.Start();
                
            }
            else if (comboBox1.SelectedItem.ToString() == "ภาษาไทย" && comboBox2.SelectedItem.ToString() == "ภาษาญี่ปุ่น")
            {
                if (checkL != "thjp")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#th/ja/");
                    checkL = "thjp";
                }
                Translation.Interval = 3000;
                Translation.Start();
                
            }
            else if (comboBox1.SelectedItem.ToString() == "ภาษาญี่ปุ่น" && comboBox2.SelectedItem.ToString() == "ภาษาอังกฤษ")
            {
                if (checkL != "jpen")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#ja/en/");
                    checkL = "jpen";
                }
                Translation.Interval = 3000;
                Translation.Start();
            }
            else if (comboBox1.SelectedItem.ToString() == "ภาษาญี่ปุ่น" && comboBox2.SelectedItem.ToString() == "ภาษาไทย")
            {
                if (checkL != "jpth")
                {
                    webBrowser1.Navigate("https://translate.google.co.th/?hl=th&tab=wT#ja/th/");
                    checkL = "jpth";
                }
                Translation.Interval = 3000;
                Translation.Start();
                
            }
            else
            {
                ShowMsgbot("ผิดพลาดกรุณาเลือกภาษาให้ไม่ซ้ำกัน");
            }
        }
        private void ShowMsgbot(string msg)//แสดงการพูดของบอท
        {
            pictureBox3.Show();
            textBox2.Enabled = true;
            textBox2.Show();
            textBox2.Text = msg;
            HideMsgbot.Interval = 6000;
            HideMsgbot.Start();
            
        }

        private void Exit_Click(object sender, System.EventArgs e)//ปิดโปรแกรม
        {
            Random R = new Random();
            int msg = R.Next(1, 3);
            switch (msg)
            {
                case 1: ShowMsgbot("บะบายน้า"); break;
                case 2: ShowMsgbot("ไว้เจอกันใหม่นะ"); break;
                case 3: ShowMsgbot("ลาก่อน...ไว้เจอกัน"); break;
            }
            msgexit.Interval = 2000;
            msgexit.Start();
        }

        private void Minitray_Click(object sender, System.EventArgs e) //หุบโปรแกรม
        {
            Random R = new Random();
            int msg = R.Next(1, 3);
            switch (msg)
            {
                case 1: ShowMsgbot("ชะแว้บบบ"); break;
                case 2: ShowMsgbot("พักสักหน่อยก็ดี"); break;
                case 3: ShowMsgbot("จะหุบทำไม.."); break;
            }
            msgminize.Interval = 1000;
            msgminize.Start();
        }

        private void Translation_Tick(object sender, System.EventArgs e)//แปลออกมาทาง richtextbox
        {
            HtmlDocument doc = webBrowser1.Document;
            HtmlElement output = doc.GetElementById("result_box");
            langout.Text = output.InnerText;
            string talk = "";
            if(langout .Text.Length  < 10&& checkBot ==true)//ถ้าข้อความที่แปลน้อยกว่า10ตัว
            {
                talk = langin.Text  +" แปลว่า "+ langout.Text +"นะจ๊ะ";
                ShowMsgbot(talk);
            }

            Translation.Stop();
        }

        private void Main_Load(object sender, System.EventArgs e) //เรียกใช้ครั้งแรกทำอะไรดีน้า
        {
            TimeOnline.Interval = 500;
            TimeOnline.Start();
            //-----------------ตั้งค่า
            string[] CheckConfig = new string[4]; //เพิ่มตั้งค่าเพิ่มขนาดด้วย
            
            CheckConfig[0] = ConfigFile.SettingFileRead("hangoutshow");
            CheckConfig[1] = ConfigFile.SettingFileRead("tauto");
            CheckConfig[2] = ConfigFile.SettingFileRead("botTalk");
            CheckConfig[3] = ConfigFile.SettingFileRead("Cal");

            if (CheckConfig[0] == "0")// แสดงการทำงาน hangout
            {
                listEvents.Hide();
                rtfDebug.Hide();
                on.Show();
                off.Hide();
                panel1.BackColor = Color.Red;
            }
            else
            {
                listEvents.Show();
                rtfDebug.Show();
                on.Hide();
                off.Show();
                panel1.BackColor = Color.GreenYellow;
            }
            //------------------------------------------
            if(CheckConfig[1] == "0")//การแปลภาษาอัติโนมัติ
            {
                checkBot = true;
                panel2.BackColor = Color.Red;
                button5.Hide();
                button6.Show();
                TAuto.Stop();
            }
            else
            {
                button6.Hide();
                button5.Show();
                panel2.BackColor = Color.GreenYellow;
                checkBot = false;
                TAuto.Interval = 2000;
                TAuto.Start();
            }
            //---------------------------------------
            if(CheckConfig[2] == "0")//อนุญาติบอทคุยแฮงเอ้า
            {
                button4.Hide();
                button7.Show();
                panel3.BackColor = Color.Red;             
            }
            else
            {
                button7.Hide();
                button4.Show();
                panel3.BackColor = Color.GreenYellow;
            }
            //----------------------------+
            if(CheckConfig[3]=="0")//เครื่องคิดเลข
            {
                DetectCalPAD.Stop();
                button8.Hide();
                button9.Show();
                panel4.BackColor = Color.Red;
            }
            else
            {
                DetectCalPAD.Interval = 50;
                DetectCalPAD.Start();
                button9.Hide();
                button8.Show();
                panel4.BackColor = Color.GreenYellow;
            }
            //----------------------------------


            //------------ETC LOADING Set----------------
            SwitchFace.Interval = 1200;
            SwitchFace.Start();
            SwtichFace2.Interval = 5000;
            SwtichFace2.Start();
            //--------message bot 
            textBox2.Hide();
            pictureBox3.Hide();
            textBox2.Enabled = false;
            //--------------------------------

            //-------talk bot
            groupBox2.Hide();
            //-------------------------------

        }
   
        private void TAuto_Tick(object sender, System.EventArgs e)//การแปลอัติโนมัติ
        {
            HtmlDocument doc = webBrowser1.Document;
            HtmlElement input = doc.GetElementById("source");
            input.SetAttribute("value", langin.Text);
            switchlangurl();                    
        }

        private void on_Click(object sender, System.EventArgs e)//ตั้งค่า เปิด แสดงการทำงาน hangout
        {
            listEvents.Show();
            rtfDebug.Show();
            on.Hide();
            off.Show();
            panel1.BackColor = Color.GreenYellow;
            ConfigFile.SettingFileWrite("hangoutshow", "1");
        }

        private void off_Click(object sender, System.EventArgs e)//ตั้งค่า ปิด แสดงการทำงาน hangout
        {
            listEvents.Hide ();
            rtfDebug.Hide ();
            on.Show ();
            off.Hide();
            panel1.BackColor = Color.Red;
            ConfigFile.SettingFileWrite("hangoutshow", "0");
        }

        private void button6_Click(object sender, System.EventArgs e)//ตั้งค่า เปิด แปลภาษาอัตโนมัติ
        {
            button6.Hide();
            button5.Show();
            panel2.BackColor = Color.GreenYellow;
            checkBot = false;
            TAuto.Interval= 2000;
            TAuto.Start();
            ConfigFile.SettingFileWrite("tauto", "1");

        }

        private void button5_Click(object sender, System.EventArgs e)//ตั้งค่า ปิด แปลภาษาอัตโนมัติ
        {
            checkBot = true;
            panel2.BackColor = Color.Red;
            button5.Hide();
            button6.Show();
            TAuto.Stop();
            ConfigFile.SettingFileWrite("tauto","0");
        }

        private void HideMsgbot_Tick(object sender, System.EventArgs e)
        {
            pictureBox3.Hide();           
            textBox2.Hide();
            textBox2.Text  = "";
            textBox2.Enabled = false;
            HideMsgbot.Stop();
        }//ซ่อนการพูดของบอท

        private void TalkBot_Click(object sender, System.EventArgs e)//สนทนากับบอท
        {
            
            label12.Text = textBox1.Text;
            if (textBox1.Text == "")
            {
                textBox1.Focus();
                ShowMsgbot("กรุณาใส่ข้อความที่ต้องการจะสนทนาด้วยจ้า");
            }
            else
            {
                string outmsg = "";
                outmsg = ConversationRW.read(textBox1.Text);
                if (outmsg == "?")
                {
                    ShowMsgbot("ช่วยเพิ่มคำถามกับคำตอบให้หน่อย");
                    groupBox1.Hide();
                    groupBox2.Show();
                    textBox4.Text = "";
                }
                else
                {
                    label12.Text = textBox1.Text;
                    ShowMsgbot(outmsg);
                }
            }
        }

        private void ADDTalk_Click(object sender, System.EventArgs e)//เพิ่มคำถามกับคำตอบให้บอท
        {
            ConversationRW.write(textBox1.Text,textBox4.Text);
            groupBox2.Hide();
            groupBox1.Show();
        }

        private void button7_Click(object sender, System.EventArgs e)//ตั้งค่าอนุญาติบอทคุยแทน เปิด
        {
            button7.Hide();
            button4.Show();
            panel3.BackColor = Color.GreenYellow;
            ConfigFile.SettingFileWrite("botTalk", "1");
        }

        private void button4_Click(object sender, System.EventArgs e)//ตั้งค่าอนุญาติบอทคุยแทน ปิด
        {
            button4.Hide();
            button7.Show();
            panel3.BackColor = Color.Red;
            ConfigFile.SettingFileWrite("botTalk", "0");
        }

        private void SwtichFace1_Tick(object sender, System.EventArgs e)//
        {
            pictureBox1.Image = Umaru_AI.Properties.Resources._2;
            SwtichFace1.Stop();
        }
       
        private void SwtichFace2_Tick(object sender, System.EventArgs e)//กระพริบตา
        {
            Random r = new Random();
           int R= r.Next(1,3);
            if (R == 1)
            {
                pictureBox1.Image = Umaru_AI.Properties.Resources._5;
                SwitchFace3.Interval = 500;
                SwitchFace3.Start();
            }else if (R == 3)
            {
                SwtichFace1.Interval = 500;
                SwtichFace1.Start();
            }
            else
            {
                pictureBox1.Image = Umaru_AI.Properties.Resources._6;
                //แสดงอารมหน้าอื่นๆ
            }   
                      
        }

        private void SwitchFace3_Tick(object sender, System.EventArgs e)//
        {
            pictureBox1.Image = Umaru_AI.Properties.Resources._2;
            SwitchFace4.Interval = 200;
            SwitchFace4.Start();
            SwitchFace3.Stop();
        }

        private void SwitchFace4_Tick(object sender, System.EventArgs e)
        {
            pictureBox1.Image = Umaru_AI.Properties.Resources._5;
            SwtichFace1.Interval = 200;
            SwtichFace1.Start();
            SwitchFace4.Stop();
           
        }

        private void SwitchFace_Tick(object sender, System.EventArgs e)//
        {
            if (textBox2.Enabled  == true)
            {
                pictureBox1.Image = Umaru_AI.Properties.Resources._3;
                SwtichFace1.Interval = 500;
                SwitchFace.Interval = 1200;
                SwtichFace1.Start();
            }

        }

        private void button_click(object sender, System.EventArgs e)
        {
            if((textBox3 .Text =="0")||(isOperationPerformed))
            {
                textBox3.Clear();
            }
            isOperationPerformed = false;
            Button button = (Button)sender;
            if (button.Text == ".")
            {
                if (!textBox3.Text.Contains("."))
                    textBox3.Text = textBox3.Text + button.Text;
            }
            else
                textBox3.Text = textBox3.Text + button.Text;
        }

        private void operator_click(object sender, System.EventArgs e)
        {
            Button button = (Button)sender;

            if(resultValue != 0)
            {
                button22.PerformClick();
                operationPerformed = button.Text;
                resultValue = Double.Parse(textBox3.Text);
                label16.Text = resultValue + " " + operationPerformed;
                isOperationPerformed = true;
            }
            else
            {
                operationPerformed = button.Text;
                resultValue = Double.Parse(textBox3.Text);
                label16.Text = resultValue + " " + operationPerformed;
                isOperationPerformed = true;
            }
        }

        private void button24_Click(object sender, System.EventArgs e)
        {
            textBox3.Text = "0";
            resultValue = 0;
        }

        private void button22_Click(object sender, System.EventArgs e)//คำนวน
        {
            switch (operationPerformed)
            {
                case "+":
                    textBox3.Text = (resultValue + Double.Parse(textBox3.Text)).ToString();
                    break;
                case "-":
                    textBox3.Text = (resultValue - Double.Parse(textBox3.Text)).ToString();
                    break;
                case "*":
                    textBox3.Text = (resultValue * Double.Parse(textBox3.Text)).ToString();
                    break;
                case "/":
                    textBox3.Text = (resultValue / Double.Parse(textBox3.Text)).ToString();
                    break;
                default:
                    break;
            }
            resultValue = Double.Parse(textBox3.Text);
            label16.Text = "";
        }

        private void button9_Click(object sender, System.EventArgs e)//ตั้งค่า เปิดดักจับคียแพท คิดเลข
        {
            DetectCalPAD.Interval = 50;
            DetectCalPAD.Start();
            button9.Hide();
            button8.Show();
            panel4.BackColor = Color.GreenYellow;
            ConfigFile.SettingFileWrite("Cal", "1");
        }

        private void button8_Click(object sender, System.EventArgs e)//ตั้งค่า ปิดดักจับคียแพท คิดเลข
        {
            DetectCalPAD.Stop();
            button8.Hide();
            button9.Show();
            panel4.BackColor = Color.Red;
            ConfigFile.SettingFileWrite("Cal", "0");

        }

        private void msgminize_Tick(object sender, System.EventArgs e)//แสดงข้อความแจ้งหุบ
        {                   
            msgminize.Stop();
            this.WindowState = FormWindowState.Minimized;
        }

        private void msgexit_Tick(object sender, System.EventArgs e)
        {            
            this.Close();
        }

        private void DetectCalPAD_Tick(object sender, System.EventArgs e)//ดักจับคียแพท
        {

                int T = textBox3.Text.Length;
                string S = Reverse(textBox3.Text);
            if (S != "")
            {
                char s = S[0];
                switch (s)
                {
                    case '0': zero.BackColor = Color.White ;                
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '1': one.BackColor = Color.White ;
                        zero.BackColor = Color.Gray;                      
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '2': two.BackColor = Color.White;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;                                         
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '3': three.BackColor = Color.White;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;                        
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '4': four.BackColor = Color.White;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;                       
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '5': five.BackColor = Color.White;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;                       
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '6': six.BackColor = Color.White ;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;                       
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '7': seven.BackColor = Color.White ;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;                        
                        eight.BackColor = Color.Gray;
                        nine.BackColor = Color.Gray; ; break;
                    case '8': eight.BackColor = Color.White;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;                      
                        nine.BackColor = Color.Gray; ; break;
                    case '9': nine.BackColor = Color.White ;
                        zero.BackColor = Color.Gray;
                        one.BackColor = Color.Gray;
                        two.BackColor = Color.Gray;
                        three.BackColor = Color.Gray;
                        four.BackColor = Color.Gray;
                        five.BackColor = Color.Gray;
                        six.BackColor = Color.Gray;
                        seven.BackColor = Color.Gray;
                        eight.BackColor = Color.Gray; break;  
                }

            }
        }
        
        public string Reverse(string text)//ย้อนกลับ string
        {
            char[] cArray = text.ToCharArray();
            string reverse = String.Empty;
            for (int i = cArray.Length - 1; i > -1; i--)
            {
                reverse += cArray[i];
            }
            return reverse;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e) //กรณี enter
        {
            if (e.KeyCode == Keys.Enter)
            {
                label12.Text = textBox1.Text;
                if (textBox1.Text == "")
                {
                    textBox1.Focus();
                    ShowMsgbot("กรุณาใส่ข้อความที่ต้องการจะสนทนาด้วยจ้า");
                }
                else
                {
                    string outmsg = "";
                    outmsg = ConversationRW.read(textBox1.Text);
                    if (outmsg == "?")
                    {
                        ShowMsgbot("ช่วยเพิ่มคำถามกับคำตอบให้หน่อย");
                        groupBox1.Hide();
                        groupBox2.Show();
                        textBox4.Text = "";
                    }
                    else
                    {
                        label12.Text = textBox1.Text;
                        ShowMsgbot(outmsg);
                    }
                }
            }           
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)//กรณี enter
        {
            if (e.KeyCode == Keys.Enter)
            {
                ConversationRW.write(textBox1.Text, textBox4.Text);
                groupBox2.Hide();
                groupBox1.Show();
            }
            textBox4.Text = "";
        }

        private void button12_Click(object sender, System.EventArgs e)//ยกเลิกเพิ่มคำ
        {
            groupBox2.Hide();
            groupBox1.Show();
            textBox1.Text = "";
        }

        private void TimeOnline_Tick(object sender, System.EventArgs e)
        {
            timeonly.Text  = DateTime.Now.ToString();
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            move = 1;
            mx = e.X;
            my = e.Y;
        }//ย้ายฟรอม
        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (move == 1)
            {
                this.SetDesktopLocation(MousePosition.X - mx, MousePosition.Y - my);
            }

        } //ย้ายฟรอม

    }
}
