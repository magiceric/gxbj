using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using MySql.Data.MySqlClient;
using System.Threading;

namespace MyCrawler
{
    public partial class FormMain : Form
    {

        private string sUrlHomepage = "";
        private string sHref = "";
        private int iPageIdx = 0;
        private string sOffseted = "offset=";
        private string sCount20 = "count=20";
        private string sMsgList = "?t=message/list";
        private string sStarred = "&action=star";
        private int iStep = 30;     //注意，这里取值能不能等于20，会不会出问题？时间充裕的时候研究研究……
        private int iOffset = 0;

        string sTodo = "";
        HtmlElementCollection hecAs;

        private MySqlConnection connRaw;
        private MySqlConnection connUsed;
        private MySqlConnection connAbandoned;
        private MySqlConnection connEmpl;
        private MySqlConnection connUpdateTime;

        private int iNewMsgCount;

        private TextBox tbProgress;

        private bool bCrawling = false;

        //public FormMain(TextBox tb)
        //{
        //    this.tbProgress = tb;
        //}

        private string ReplaceSubStr(string strOrg, string strOld, string strNew)
        {
            return strOrg.Substring(0, strOrg.IndexOf(strOld)) + strNew
                + strOrg.Substring(strOrg.IndexOf(strOld) + strOld.Length, strOrg.Length - strOrg.IndexOf(strOld) - strOld.Length);
        }
        private string InsertAfterSubStr(string strOrg, string strKeyword, string strToAdd)
        {
            return strOrg.Substring(0, strOrg.IndexOf(strKeyword) + strKeyword.Length) + strToAdd
                + strOrg.Substring(strOrg.IndexOf(strKeyword) + strKeyword.Length, strOrg.Length - strOrg.IndexOf(strKeyword) - strKeyword.Length);
        }

        public FormMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "fakeid";
            dataGridView1.Columns[0].Width = 100;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[1].Name = "remark_name";
            dataGridView1.Columns[1].Width = 600;

            timer1.Start();
        }

        private static string GetLocalIp()  
        { 
            string hostname = Dns.GetHostName();//得到本机名 
            //IPHostEntry localhost = Dns.GetHostByName(hostname);//方法已过期，只得到IPv4的地址  
            IPHostEntry localhost = Dns.GetHostEntry(hostname);

            foreach (IPAddress addr in localhost.AddressList)
            {
                string sIP = addr.ToString();
                if (sIP.IndexOf("192.168.1.") >= 0
                    || sIP.IndexOf("192.168.41.") >= 0)
                    return sIP;
            }

            IPAddress localaddr = localhost.AddressList[0];  
            return "unfound";
        }

        private void InitialDBConnection()
        {
            string strSource = "";

            string sIP = GetLocalIp();
            //家里内网
            if (sIP.IndexOf("192.168.1.") >= 0)
                strSource = "192.168.1.118";
            //本机
            if (sIP.IndexOf("192.168.1.118") >= 0)
                strSource = "localhost";
            //外面
            if (strSource == "")
                strSource = "magiceric.vicp.net";


            string strConnection = "Database=gxbj;Data Source="
                + strSource
                +";Port=3306;User Id=eric;Password=bingoo;Charset=gbk;TreatTinyAsBoolean=false;";

            connRaw = new MySqlConnection(strConnection);
            connUsed = new MySqlConnection(strConnection);
            connAbandoned = new MySqlConnection(strConnection);
            connEmpl = new MySqlConnection(strConnection);
            connUpdateTime = new MySqlConnection(strConnection);

            try
            {
                connRaw.Open();
                connUsed.Open();
                connAbandoned.Open();
                connEmpl.Open();
                connUpdateTime.Open();

                toolStripStatusLabel1.Text = "Database connected sucessfully.";
            }
            catch (Exception)
            {
                MessageBox.Show("警告：数据库连接失败。");
                throw;
            }
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            sTodo = "获取用户信息";
            textBox1.Text = "0";
            iPageIdx = 0;

            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "fakeid";
            dataGridView1.Columns[0].Width = 100;
            dataGridView1.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[1].Name = "remark_name";
            dataGridView1.Columns[1].Width = 600;
            dataGridView1.Rows.Clear();

            sUrlHomepage = "https://mp.weixin.qq.com";
            webBrowser1.Navigate(sUrlHomepage, null, null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (bCrawling) return;

            bCrawling = true;

            sTodo = "消息管理数据抓取";
            textBox1.Text = "0";
            iOffset = 0;
            iNewMsgCount = 0;

            dataGridView1.ColumnCount = 6;
            dataGridView1.Columns[0].Name = "type";
            dataGridView1.Columns[0].Width = 10;
            dataGridView1.Columns[1].Name = "msgid";
            dataGridView1.Columns[1].Width = 100;
            dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[2].Name = "fakeid";
            dataGridView1.Columns[2].Width = 100;
            dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[3].Name = "remark_name";
            dataGridView1.Columns[3].Width = 100;
            dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns[4].Name = "message_time";
            dataGridView1.Columns[4].Width = 100;
            dataGridView1.Columns[5].Name = "message_content";
            dataGridView1.Columns[5].Width = 600;
            dataGridView1.Rows.Clear();

            InitialDBConnection();

            sUrlHomepage = "https://mp.weixin.qq.com";
            webBrowser1.Navigate(sUrlHomepage, null, null, null);
        }

        ////根据Url地址得到网页的html源码
        //private string GetWebContent(string Url)
        //{
        //    string strResult = "";
        //    try
        //    {
        //        //创建访问目标
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
        //        //声明一个HttpWebRequest请求
        //        request.Timeout = 30000;
        //        //设置连接超时时间
        //        request.Headers.Set("Pragma", "no-cache");
        //        //得到回应
        //        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //        //得到数据流
        //        Stream streamReceive = response.GetResponseStream();
        //        //对获取到的数据流进行编码解析，让我们可以进行正常读取
        //        Encoding encoding = Encoding.GetEncoding("GB2312");
        //        StreamReader streamReader = new StreamReader(streamReceive, encoding);
        //        //读取出数据流中的信息
        //        strResult = streamReader.ReadToEnd();
        //        //关闭流
        //        streamReader.Close();
        //        //关闭网络响应流
        //        response.Close();
        //    }
        //    catch
        //    {
        //    }
        //    return strResult;
        //}

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlDocument doc;
            
            doc = webBrowser1.Document;

            //string strWebContent = GetWebContent(sUrlHomepage);

            richTextBox1.Text = doc.Url.LocalPath;

            //读取网页失败，尝试刷新
            if (doc.Url.LocalPath.IndexOf("dnserror")>=0)
            {
                webBrowser1.Navigate(sHref, null, null, null);
            }

            //登录处理
            if (doc.Title == "公众平台登录")
            {
                doc.All["account"].SetAttribute("value", "2547370594@qq.com");
                doc.All["password"].SetAttribute("value", "asd+123");
                doc.All["login_button"].InvokeMember("click");
                richTextBox1.Text = doc.Url.LocalPath;
            }
            
            //登陆后处理
            if (doc.Url.LocalPath=="/cgi-bin/home")
            {
                switch (sTodo)
                {
                    case "获取用户信息":
                        hecAs = doc.GetElementsByTagName("a");
                        foreach (HtmlElement he in hecAs)
                        {
                            if (he.OuterText == "用户管理")
                            {
                                sHref = he.GetAttribute("HREF");
                                sHref = ReplaceSubStr(sHref, "pagesize=10", "pagesize=100");
                                webBrowser1.Navigate(sHref, null, null, null);
                            }
                        }
                        richTextBox1.Text = doc.Url.LocalPath;
                        break;
                    case "消息管理数据抓取":
                        hecAs = doc.GetElementsByTagName("a");
                        foreach (HtmlElement he in hecAs)
                        {
                            if (he.OuterText == "消息管理")
                            {
                                sHref = he.GetAttribute("HREF");
                                webBrowser1.Navigate(sHref, null, null, null);
                            }
                        }
                        richTextBox1.Text = doc.Url.LocalPath;
                        break;
                    default:
                        break;
                }

            }

            //“用户管理”页面处理
            if (doc.Url.LocalPath == "/cgi-bin/contactmanage")
            {
                HtmlElementCollection hecAs = doc.GetElementsByTagName("a");
                bool bEmpty = true;
                foreach (HtmlElement he in hecAs)
                {
                    if (he.GetAttribute("className") == "remark_name")
                    {
                        int iRow = dataGridView1.Rows.Add();
                        dataGridView1.Rows[iRow].Cells[0].Value = he.GetAttribute("data-fakeid");
                        dataGridView1.Rows[iRow].Cells[1].Value = he.InnerHtml;
                        //if (he.OuterText != null)
                        //    listBox1.Items.Add(he.OuterText);
                        //else
                        //    listBox1.Items.Add(he.InnerHtml);
                        textBox1.Text = (Int16.Parse(textBox1.Text) + 1).ToString();
                        bEmpty = false;
                    }
                }
                if (bEmpty) MessageBox.Show("Done!");
                else
                {
                    string sPageIdx = "pageidx=" + iPageIdx.ToString();
                    iPageIdx += 1;
                    sHref = ReplaceSubStr(sHref, sPageIdx, "pageidx=" + iPageIdx.ToString());
                    webBrowser1.Navigate(sHref, null, null, null);
                }

                richTextBox1.Text = doc.Body.InnerHtml;
            }

            //“消息管理”页面处理
            if (doc.Url.LocalPath == "/cgi-bin/message")
            {
                string sQuery = doc.Url.Query;
                string sCountPerferred = "count=" + iStep.ToString();
                sHref = doc.Url.OriginalString;

                //尚未进行分页处理
                if (sQuery.IndexOf(sOffseted) == -1)
                {
                    sHref = ReplaceSubStr(sHref, sCount20, sCountPerferred + "&offset=" + iOffset.ToString());
                    webBrowser1.Navigate(sHref, null, null, null);
                }
                    //已作分页处理
                else
                {
                    bool bEmpty = true;
                    bool bNoMoreNew = false;
                    MessageRawData rawMsg = new MessageRawData();
                    HtmlElementCollection hecAs = doc.GetElementsByTagName("li");
                    foreach (HtmlElement he in hecAs)
                    {
                        if (he.GetAttribute("className").IndexOf("message_item") != -1)
                        {
                            int iRow = dataGridView1.Rows.Add();
                            if (sHref.IndexOf(sStarred) == -1)
                                rawMsg.starred = "--";
                            else
                                rawMsg.starred = "★";
                            dataGridView1.Rows[iRow].Cells[0].Value = rawMsg.starred;

                            rawMsg.msgid = UInt64.Parse(he.GetAttribute("data-id"));
                            dataGridView1.Rows[iRow].Cells[1].Value = rawMsg.msgid;

                            HtmlElementCollection hecAsChild = he.GetElementsByTagName("a");
                            foreach (HtmlElement heChild in hecAsChild)
                            {
                                if (heChild.GetAttribute("className") == "remark_name")
                                {
                                    rawMsg.fakeid = UInt64.Parse(heChild.GetAttribute("data-fakeid"));
                                    dataGridView1.Rows[iRow].Cells[2].Value = rawMsg.fakeid;

                                    rawMsg.remark_name = heChild.InnerHtml;
                                    dataGridView1.Rows[iRow].Cells[3].Value = rawMsg.remark_name;
                                }
                            }

                            HtmlElementCollection hecDivs = he.GetElementsByTagName("div");
                            foreach (HtmlElement heChild in hecDivs)
                            {
                                if (heChild.GetAttribute("className") == "message_time")
                                {
                                    rawMsg.message_time = heChild.InnerHtml;
                                    dataGridView1.Rows[iRow].Cells[4].Value = rawMsg.message_time;
                                }
                                if (heChild.GetAttribute("className") == "wxMsg")
                                {
                                    rawMsg.message_content = heChild.InnerHtml;
                                    dataGridView1.Rows[iRow].Cells[5].Value = rawMsg.message_content;
                                }
                            }

                            try
                            {
                                MySqlCommand msqlCmd;
                                MySqlDataReader msqlRdr;
                                string strSQL = ""
                                        + "SELECT * \n"
                                        + "FROM message_rawdata\n"
                                        + "WHERE msgid='" + rawMsg.msgid.ToString() + "'\n";
                                
                                msqlCmd = new MySqlCommand(strSQL,connRaw);
                                msqlRdr = msqlCmd.ExecuteReader();

                                if (!msqlRdr.HasRows)
                                {
                                    msqlRdr.Close();

                                    strSQL = ""
                                        + "INSERT INTO message_rawdata\n"
                                        + "\t(msgid, fakeid, remark_name, message_time, message_content, starred, checktime)\n"
                                        + "\tVALUES (@msgid, @fakeid, @remark_name, @message_time, @message_content, @starred, @checktime);";
                                    MySqlParameter msqlParamMsgid = new MySqlParameter("@msgid", MySqlDbType.UInt64);
                                    MySqlParameter msqlParamFakeid = new MySqlParameter("@fakeid", MySqlDbType.UInt64);
                                    MySqlParameter msqlParamName = new MySqlParameter("@remark_name", MySqlDbType.VarChar, 500);
                                    MySqlParameter msqlParamTime = new MySqlParameter("@message_time", MySqlDbType.VarChar, 30);
                                    MySqlParameter msqlParamContent = new MySqlParameter("@message_content", MySqlDbType.VarChar, 3000);
                                    MySqlParameter msqlParamStar = new MySqlParameter("@starred", MySqlDbType.VarChar, 2);
                                    MySqlParameter msqlParamChecktime = new MySqlParameter("@checktime", MySqlDbType.DateTime);

                                    msqlParamMsgid.Value = rawMsg.msgid;
                                    msqlParamFakeid.Value = rawMsg.fakeid;
                                    msqlParamName.Value = rawMsg.remark_name;
                                    msqlParamTime.Value = rawMsg.message_time;
                                    msqlParamContent.Value = rawMsg.message_content;
                                    msqlParamStar.Value = rawMsg.starred;
                                    msqlParamChecktime.Value = DateTime.Now;

                                    msqlCmd = new MySqlCommand(strSQL, connRaw);
                                    msqlCmd.Parameters.Add(msqlParamMsgid);
                                    msqlCmd.Parameters.Add(msqlParamFakeid);
                                    msqlCmd.Parameters.Add(msqlParamName);
                                    msqlCmd.Parameters.Add(msqlParamTime);
                                    msqlCmd.Parameters.Add(msqlParamContent);
                                    msqlCmd.Parameters.Add(msqlParamStar);
                                    msqlCmd.Parameters.Add(msqlParamChecktime);

                                    msqlCmd.ExecuteNonQuery();

                                    iNewMsgCount++;

                                    textBox1.Text = (Int16.Parse(textBox1.Text) + 1).ToString();
                                    bEmpty = false;
                                }
                                else
                                {
                                    bNoMoreNew = true;
                                }

                                msqlRdr.Close();

                            }
                            catch (Exception)
                            {                                
                                throw;
                            }

                        }
                    }

                    if (bEmpty || bNoMoreNew)
                    {
                        //尚未读取星标消息
                        if (sHref.IndexOf(sStarred)==-1)
                        {
                            sHref = InsertAfterSubStr(sHref, sMsgList, sStarred);
                            string sOffsetIdx = "offset=" + iOffset.ToString();
                            iOffset = 0;
                            sHref = ReplaceSubStr(sHref, sOffsetIdx, "offset=" + iOffset.ToString());                                
                            webBrowser1.Navigate(sHref, null, null, null);
                        }
                            //消息数据抓取完毕
                        else
                        {
                            //MessageBox.Show("Done!");
                            toolStripStatusLabel1.Text = iNewMsgCount.ToString() + " new messages added.";

                            this.tbProgress = textBox2;
                            if (this.tbProgress != null)
                            {
                                Thread thread1 = new Thread(new ThreadStart(this.ProposerMatching));
                                thread1.Start();
                            }                 

                            //ProposerMatching();
                            
                        }
                            
                    }
                    else
                    {
                        string sOffsetIdx = "offset=" + iOffset.ToString();
                        iOffset += iStep;
                        sHref = ReplaceSubStr(sHref, sOffsetIdx, "offset=" + iOffset.ToString());
                        webBrowser1.Navigate(sHref, null, null, null);
                    }
                }

                richTextBox1.Text = doc.Body.InnerHtml;

            }

        }

        private void ProposerMatching()
        {
            try
            {
                MySqlCommand msqlCmd;
                MySqlDataReader msqlRdr;
                string strSQL = ""
                        + "SELECT * \n"
                        + "FROM message_rawdata\n"
                        + "ORDER BY msgid";

                msqlCmd = new MySqlCommand(strSQL, connRaw);
                msqlRdr = msqlCmd.ExecuteReader();

                if (msqlRdr.HasRows)
                {
                    int iRowsCount = 0;
                    int iRowsUsed = 0;
                    int iRowAbandoned = 0;
                    int iSkipped = 0;
                    do
                    {
                        while (msqlRdr.Read())
                        {
                            iRowsCount++;

                            string sReason = "";

                            MessageRawData rawMsg = new MessageRawData();
                            rawMsg.msgid = (UInt64)msqlRdr.GetUInt64("msgid");
                            rawMsg.fakeid = (UInt64)msqlRdr.GetUInt64("fakeid");
                            rawMsg.remark_name = (string)msqlRdr.GetString("remark_name");
                            rawMsg.message_time = (string)msqlRdr.GetString("message_time");
                            rawMsg.message_content = (string)msqlRdr.GetString("message_content");
                            rawMsg.starred = (string)msqlRdr.GetString("starred");

                            string sPper = rawMsg.message_content;

                            sPper = sPper.Trim();   //去头尾空格
                            sPper = sPper.Replace(" ", "");     //去中间空间（半角）
                            sPper = sPper.Replace("　", "");    //去中间空格（全角） 
                            sPper = sPper.ToUpper();    //转大写

                            MySqlCommand cmdEmpl;
                            MySqlDataReader drEmpl;
                            strSQL = ""
                                + "SELECT * \n"
                                + "FROM employeebase\n"
                                + "WHERE notesid='" + sPper + "'\n";
                            cmdEmpl = new MySqlCommand(strSQL, connEmpl);
                            drEmpl = cmdEmpl.ExecuteReader();

                            if (drEmpl.HasRows) //国信员工
                            {
                                MySqlCommand cmdUsed;
                                MySqlDataReader drUsed;
                                strSQL = ""
                                    + "SELECT * \n"
                                    + "FROM message_used\n"
                                    + "WHERE fakeid='" + rawMsg.fakeid + "'\n";
                                cmdUsed = new MySqlCommand(strSQL, connUsed);
                                drUsed = cmdUsed.ExecuteReader();

                                if (!drUsed.HasRows) //该用户还没有推荐人
                                {
                                    drUsed.Close();

                                    UseOneRow(
                                        rawMsg.msgid
                                        , rawMsg.fakeid
                                        , rawMsg.remark_name
                                        , rawMsg.message_time
                                        , rawMsg.message_content
                                        , rawMsg.starred
                                        , sPper);

                                    iRowsUsed++;
                                }
                                else
                                {
                                    //放入弃用表
                                    drUsed.Read();
                                    sReason = "该用户已有推荐人" + drUsed["message_content"];
                                }

                                drUsed.Close();

                            }
                            else  //匹配不到国信员工数据
                            {   
                                //放入弃用表
                                //加入“客户”类推荐人时还要判断是否已知的微信号
                                sReason = "回复信息未能成功匹配国信员工Notes号";
                            }

                            drEmpl.Close();

                            if (sReason!="")
                            {

                                MySqlCommand cmdAbandoned;
                                MySqlDataReader drAbandoned;
                                strSQL = ""
                                    + "SELECT * \n"
                                    + "FROM message_abandoned\n"
                                    + "WHERE msgid='" + rawMsg.msgid + "'\n";
                                cmdAbandoned = new MySqlCommand(strSQL, connAbandoned);
                                drAbandoned = cmdAbandoned.ExecuteReader();

                                if (!drAbandoned.HasRows) //该记录未被处理过
                                {
                                    drAbandoned.Close();

                                    AbandondOneRow(
                                        rawMsg.msgid
                                        , rawMsg.fakeid
                                        , rawMsg.remark_name
                                        , rawMsg.message_time
                                        , rawMsg.message_content
                                        , rawMsg.starred
                                        , sReason);

                                    iRowAbandoned++;
                                }
                                else
                                {
                                    iSkipped++;
                                }

                                drAbandoned.Close();
                            }

                        }

                        Invoke((Action)delegate
                        {
                            this.tbProgress.Text = ""
                                + "Total : " + iRowsCount.ToString()
                                + ", Used : " + iRowsUsed.ToString()
                                + ", Abandoned : " + iRowAbandoned.ToString()
                                + ", Skipped : " + iSkipped.ToString();
                            this.tbProgress.Refresh();

                            //Application.DoEvents();
                            //System.Threading.Thread.Sleep(100);//为了有利于对效果进行观察，每隔100ms输出一次
                        });

                    } while (msqlRdr.NextResult());

                }

                msqlRdr.Close();

                Invoke((Action)delegate
                {
                    toolStripStatusLabel1.Text = "Proposers matching done.";
                });

                strSQL = "UPDATE updatetime SET lastupdatetime=NOW();";
                MySqlCommand msqlUpdateTime = new MySqlCommand(strSQL, connUpdateTime);
                msqlUpdateTime.ExecuteNonQuery();

                bCrawling = false;

            }
            catch (Exception)
            {

                throw;
            }
        }

        private void AbandondOneRow(
            UInt64 msgid
            , UInt64 fakeid
            , string remark_name
            , string message_time
            , string message_content
            , string starred
            , string reason
           )
        {
            try
            {
                string strSQL = ""
                    + "INSERT INTO message_abandoned\n"
                    + "\t(msgid, fakeid, remark_name, message_time, message_content, starred, reason, checktime)\n"
                    + "\tVALUES (@msgid, @fakeid, @remark_name, @message_time, @message_content, @starred, @reason, @checktime);";
                
                MySqlParameter msqlParamMsgid = new MySqlParameter("@msgid", MySqlDbType.UInt64);
                MySqlParameter msqlParamFakeid = new MySqlParameter("@fakeid", MySqlDbType.UInt64);
                MySqlParameter msqlParamName = new MySqlParameter("@remark_name", MySqlDbType.VarChar, 500);
                MySqlParameter msqlParamTime = new MySqlParameter("@message_time", MySqlDbType.VarChar, 30);
                MySqlParameter msqlParamContent = new MySqlParameter("@message_content", MySqlDbType.VarChar, 3000);
                MySqlParameter msqlParamStar = new MySqlParameter("@starred", MySqlDbType.VarChar, 2);
                MySqlParameter msqlParamReason = new MySqlParameter("@reason", MySqlDbType.VarChar, 50);
                MySqlParameter msqlParamChecktime = new MySqlParameter("@checktime", MySqlDbType.DateTime);

                msqlParamMsgid.Value = msgid;
                msqlParamFakeid.Value = fakeid;
                msqlParamName.Value = remark_name;
                msqlParamTime.Value = message_time;
                msqlParamContent.Value = message_content;
                msqlParamStar.Value = starred;
                msqlParamReason.Value = reason;
                msqlParamChecktime.Value = DateTime.Now;

                MySqlCommand msqlCmd = new MySqlCommand(strSQL, connAbandoned);
                msqlCmd.Parameters.Add(msqlParamMsgid);
                msqlCmd.Parameters.Add(msqlParamFakeid);
                msqlCmd.Parameters.Add(msqlParamName);
                msqlCmd.Parameters.Add(msqlParamTime);
                msqlCmd.Parameters.Add(msqlParamContent);
                msqlCmd.Parameters.Add(msqlParamStar);
                msqlCmd.Parameters.Add(msqlParamReason);
                msqlCmd.Parameters.Add(msqlParamChecktime);

                msqlCmd.ExecuteNonQuery();

            }
            catch (Exception)
            {                
                throw;
            }
        }

        private void UseOneRow(
            UInt64 msgid
            , UInt64 fakeid
            , string remark_name
            , string message_time
            , string message_content
            , string starred            
            , string proposer
            )
        {
            try
            {
                string strSQL = ""
                    + "INSERT INTO message_used\n"
                    + "\t(msgid, fakeid, remark_name, message_time, message_content, starred, proposer, checktime)\n"
                    + "\tVALUES (@msgid, @fakeid, @remark_name, @message_time, @message_content, @starred, @proposer, @checktime);";

                MySqlParameter msqlParamMsgid = new MySqlParameter("@msgid", MySqlDbType.UInt64);
                MySqlParameter msqlParamFakeid = new MySqlParameter("@fakeid", MySqlDbType.UInt64);
                MySqlParameter msqlParamName = new MySqlParameter("@remark_name", MySqlDbType.VarChar, 500);
                MySqlParameter msqlParamTime = new MySqlParameter("@message_time", MySqlDbType.VarChar, 30);
                MySqlParameter msqlParamContent = new MySqlParameter("@message_content", MySqlDbType.VarChar, 3000);
                MySqlParameter msqlParamStar = new MySqlParameter("@starred", MySqlDbType.VarChar, 2);
                MySqlParameter msqlParamProposer = new MySqlParameter("@proposer", MySqlDbType.VarChar, 500);
                MySqlParameter msqlParamChecktime = new MySqlParameter("@checktime", MySqlDbType.DateTime);

                msqlParamMsgid.Value = msgid;
                msqlParamFakeid.Value = fakeid;
                msqlParamName.Value = remark_name;
                msqlParamTime.Value = message_time;
                msqlParamContent.Value = message_content;
                msqlParamStar.Value = starred;
                msqlParamProposer.Value = proposer;
                msqlParamChecktime.Value = DateTime.Now;

                MySqlCommand msqlCmd = new MySqlCommand(strSQL, connUsed);
                msqlCmd.Parameters.Add(msqlParamMsgid);
                msqlCmd.Parameters.Add(msqlParamFakeid);
                msqlCmd.Parameters.Add(msqlParamName);
                msqlCmd.Parameters.Add(msqlParamTime);
                msqlCmd.Parameters.Add(msqlParamContent);
                msqlCmd.Parameters.Add(msqlParamStar);
                msqlCmd.Parameters.Add(msqlParamProposer);
                msqlCmd.Parameters.Add(msqlParamChecktime);

                msqlCmd.ExecuteNonQuery();

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            splitContainerMain.Top = 0;
            splitContainerMain.Left = 0;
            splitContainerMain.Height = this.ClientRectangle.Height - statusStrip1.Height;
            splitContainerMain.Width = this.ClientRectangle.Width;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!bCrawling)
                button1_Click(sender, e);
        }

    }
}
