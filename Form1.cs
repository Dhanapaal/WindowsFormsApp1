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
using System.Xml;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            (new ToolTip()).SetToolTip(btnMessage, "Train traffic announcement, such as information about track work, train failures, malfunctions and the like.");
            (new ToolTip()).SetToolTip(btnStation, "Trafficking, both with and without travel.");
            (new ToolTip()).SetToolTip(btnAnnouncement, "Timetable information, ie information on trains at traffic points (stations, stops) each mail corresponds to a particular train at the respective traffic location.");

        }

        private void btnMessage_Click(object sender, EventArgs e)
        {
            InvokeService("TrainMessage");
        }

        private void btnStation_Click(object sender, EventArgs e)
        {
            InvokeService("TrainStation");

        }

        private void btnAnnouncement_Click(object sender, EventArgs e)
        {
          //  InvokeService("TrainAnnouncement");

        }

        void InvokeService(string strCallType)
        {

            WebClient webclient = new WebClient();
            webclient.Headers.Add("Referer", "http://www.example.com"); // Replace with your domain here
            // Registrer a handler that will execute when download is completed.
            webclient.UploadStringCompleted += (obj, arguments) =>
            {
                if (arguments.Cancelled == true)
                {
                    MessageBox.Show("Request cancelled by user");
                }
                else if (arguments.Error != null)
                {
                    MessageBox.Show("Request Failed : " + arguments.Error.Message);
                }
                else
                {
                    UpdateMessagesDataToDB(strCallType, formatXML(arguments.Result));
                }
            };

            try
            {
                // API server url
                Uri address = new Uri("http://api.trafikinfo.trafikverket.se/v1.3/data.xml");
                string requestBody = "<REQUEST>" +
                                        // Use your valid authenticationkey
                                        "<LOGIN authenticationkey='827a4f239b5040d8969c2b5d5e6f733a'/>" +
                                        "<QUERY objecttype='" + strCallType + "' >" +
                                            "<FILTER/>" +
                                            "<EXCLUDE>Deleted</EXCLUDE>" +
                                        "</QUERY>" +
                                    "</REQUEST>";

                webclient.Headers["Content-Type"] = "text/xml";
                webclient.Encoding = System.Text.Encoding.UTF8;
                webclient.UploadStringAsync(address, "POST", requestBody);
            }

            catch (UriFormatException)
            {
                MessageBox.Show("Malformed url, press 'X' to exit.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured, press 'X' to exit. :" + ex.Message);
            }
        }

        string GetMultipleValues(XmlNode rootNode,string strKey)
        {
            string strReturn = "";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(rootNode.OuterXml);
            XmlNodeList parentNode = xmlDoc.GetElementsByTagName(strKey);
            foreach (XmlNode childrenNode in parentNode)
            {
                strReturn = strReturn + childrenNode.InnerText + ";";
            }
                return strReturn;
        }

        void UpdateMessagesDataToDB(string strCallType, string xmlResponse)
        {

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlResponse);
            MySqlConnection conn = new MySqlConnection("Server=localhost;Database=trafikinfo_schema;Uid=root;Pwd=India@123");
            conn.Open();
            {
                string sqlTruncate = "";
                switch (strCallType)
                {
                    case "TrainMessage":
                        sqlTruncate = "DELETE FROM TrainMessages";
                        break;
                    case "TrainStation":
                        sqlTruncate = "DELETE FROM TrainStations";
                        break;
                    case "TrainAnnouncement":
                        sqlTruncate = "DELETE FROM TrainAnnouncements";
                        break;
                }
                MySqlCommand commTruncate = conn.CreateCommand();
                commTruncate.CommandText = sqlTruncate;
                commTruncate.ExecuteNonQuery();

                XmlNodeList parentNode = xmlDoc.GetElementsByTagName(strCallType);
                foreach (XmlNode childrenNode in parentNode)
                {
                    MySqlCommand comm;
                    string sql="";
                    switch (strCallType)
                    {
                        case "TrainMessage":
                            sql = "INSERT INTO TrainMessages(AffectedLocations,    CountyNo,    ExternalDescription,    Geometry,    EventId,    ReasonCodeText,    StartDateTime,    LastUpdateDateTime,    ModifiedTime ) VALUES (";
                            sql = sql + "'" + GetMultipleValues(childrenNode,"AffectedLocation") + "',";
                            sql = sql + childrenNode.SelectSingleNode("CountyNo").InnerText + ",";
                            sql = sql + "'" + childrenNode.SelectSingleNode("ExternalDescription").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("Geometry").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("EventId").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("ReasonCodeText").InnerText + "',";
                            sql = sql + "'" + System.DateTime.Parse(childrenNode.SelectSingleNode("StartDateTime").InnerText).ToString("yyyy-MM-dd HH:mm") + "',";
                            sql = sql + "'" + System.DateTime.Parse(childrenNode.SelectSingleNode("LastUpdateDateTime").InnerText).ToString("yyyy-MM-dd HH:mm") + "',";
                            sql = sql + "'" + System.DateTime.Parse(childrenNode.SelectSingleNode("ModifiedTime").InnerText).ToString("yyyy-MM-dd HH:mm") + "')";
                            break;
                       case "TrainStation":
                            sql = "INSERT INTO TrainStations(Advertised,    AdvertisedLocationName,    AdvertisedShortLocationName,    CountryCode,    CountyNo,    LocationInformationText,    LocationSignature,    ModifiedTime ,PlatformLines,Geometry,Prognosticated) VALUES (";
                            sql = sql + "'" + childrenNode.SelectSingleNode("Advertised").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("AdvertisedLocationName").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("AdvertisedShortLocationName").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("CountryCode").InnerText + "',";
                            //Some response dont have CountyNo
                            try
                            {
                                sql = sql + childrenNode.SelectSingleNode("CountyNo").InnerText + ",";
                            }
                            catch
                            {
                                sql = sql + "null,";
                            }
                            sql = sql + "'',";
                            //sql = sql + "'" + childrenNode.SelectSingleNode("LocationInformationText").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("LocationSignature").InnerText + "',";
                            sql = sql + "'" + System.DateTime.Parse(childrenNode.SelectSingleNode("ModifiedTime").InnerText).ToString("yyyy-MM-dd HH:mm") + "',";
                            sql = sql + "'" + GetMultipleValues(childrenNode, "PlatformLine") + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("Geometry").InnerText + "',";
                            sql = sql + "'" + childrenNode.SelectSingleNode("Prognosticated").InnerText + "')";
                            break;
                        case "TrainAnnouncement":
                            break;
                    }
                    comm = conn.CreateCommand();
                    comm.CommandText = sql;
                    comm.ExecuteNonQuery();
                }
            }
            conn.Close();

        }

        // Format xml so it is readable by humans.
        private static string formatXML(string xml)
        {
            // Format xml.
            XDocument rxml = XDocument.Parse(xml);
            XmlWriterSettings xmlsettings = new XmlWriterSettings();
            xmlsettings.OmitXmlDeclaration = true;
            xmlsettings.Indent = true;
            xmlsettings.IndentChars = "      ";
            var sb = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(sb, xmlsettings))
            {
                rxml.WriteTo(xmlWriter);
            }
            return sb.ToString();
        }
         

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }


        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Enabled = false;
                btnStartStop.Text = "START";

            }
            else
            {
                timer1.Enabled = true;
                btnStartStop.Text = "STOP";

            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Interval = Int32.Parse(txtInterval.Text) *1000;
            btnStartStop.Text = "START";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            InvokeService("TrainMessage");
            System.Threading.Thread.Sleep(30000);
            InvokeService("TrainStation");
            System.Threading.Thread.Sleep(30000);
          //  InvokeService("TrainAnnouncement");
          //  System.Threading.Thread.Sleep(10000);
            timer1.Enabled = true;
            label1.Text = (Int32.Parse(label1.Text) + 1).ToString();
        }
    }

}


