using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using PronacaApi;
namespace PruebasComunicacion
{
    public partial class Form1 : Form
    {
        GestionVehiculos veh = new GestionVehiculos(); 

        public Form1()
        {
            InitializeComponent();
        }

        public HttpWebRequest CreateSOAPWebRequest()
        {
            //Making Web Request    
            //HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(@"https://serverdesaries.pronaca.corp/DatosPesajeDataBridge.asmx");
            ////SOAPAction    
            //Req.Headers.Add(@"https://ln.gesalm.integracion.pronaca.com.ec/validarPeso");

            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(@"https://cdcites.pronaca.com/gestionImportacionPesos/AriesDataBridgeTest");
            //SOAPAction    
            Req.Headers.Add("SOAPAction", "");


            //Content_type    
            Req.ContentType = "text/xml;charset=\"utf-8\"";
            Req.Accept = "text/xml";
            //HTTP method    
            Req.Method = "POST";
            //return HttpWebRequest    
            return Req;
        }


        private void Form1_Load(object sender, EventArgs e)
        {

            HttpWebRequest req = WebRequest.Create("https://cdcites.pronaca.com/gestionImportacionPesos/AriesDataBridgeTest") as HttpWebRequest;
            req.Credentials = new NetworkCredential("data_bridge_test", "UGVzMHNENHQ0QnIxZGczUHIwbmFjYSQ");

            XmlDocument xmlDoc = new XmlDocument();
            using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
            {
                xmlDoc.Load(resp.GetResponseStream());
                xmlDoc.Save(Directory.GetCurrentDirectory() + @"\pi.xml");
                MessageBox.Show(Directory.GetCurrentDirectory());
            }

        }


        public static void Execute()
        {
            const string Comillas = "\"";
            HttpWebRequest request = CreateWebRequest();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=" + Comillas + "1.0" + Comillas + " encoding=" + Comillas + "utf-8" + Comillas + "?> " +
                                 "<soap:Envelope xmlns:xsi=" + Comillas + "http://www.w3.org/2001/XMLSchema-instance" + Comillas + " xmlns:xsd=" + Comillas + "http://www.w3.org/2001/XMLSchema" + Comillas + " xmlns:soap=" + Comillas + "http://schemas.xmlsoap.org/soap/envelope/" + Comillas + ">" +
                                 "<soap:Body>" +
                                 " <validarPeso xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">" +
                                 " <xmlFileBase64>PG5zMTpHZXNJbXBQZXNBciB4bWxuczpuczE9Imh0dHA6Ly9sbi5nZXNhbG0uaW50ZWdyYWNpb24ucHJvbmFjYS5jb20uZWMiPjxDb250cm9sUHJvY2Vzbz48Q29kaWdvQ29tcGFuaWE+MDAyPC9Db2RpZ29Db21wYW5pYT48Q29kaWdvU2lzdGVtYT5EQjwvQ29kaWdvU2lzdGVtYT48Q29kaWdvU2VydmljaW8+VmFsaWRhUGVzb3NEQjwvQ29kaWdvU2VydmljaW8+PFByb2Nlc28+SW5zZXJ0YXIvVmFsaWRhcjwvUHJvY2Vzbz48UmVzdWx0YWRvPjwvUmVzdWx0YWRvPjwvQ29udHJvbFByb2Nlc28+PENhYmVjZXJhPjxUaWNrZXREYXRhQnJpZGdlPjk5OTwvVGlja2V0RGF0YUJyaWRnZT48RmVjaGFUaWNrZXRQcm9jZXNvPjEwLzEwLzIwMjE8L0ZlY2hhVGlja2V0UHJvY2Vzbz48SG9yYVRpY2tldFByb2Nlc28+MTA6MDA6MDA8L0hvcmFUaWNrZXRQcm9jZXNvPjxVc3VhcmlvRGF0YUJyaWRnZT5Db25maWd1cmFkb3I8L1VzdWFyaW9EYXRhQnJpZGdlPjxOdW1lcm9CYXNjdWxhPjE8L051bWVyb0Jhc2N1bGE+PFRpcG9QZXNvPkU8L1RpcG9QZXNvPjxQZXNvVGlja2V0RGF0YUJyaWRnZT41NjAwMDwvUGVzb1RpY2tldERhdGFCcmlkZ2U+PFBsYWNhVmVoaWN1bG8+QUFBOTk5PC9QbGFjYVZlaGljdWxvPjxDZWR1bGFUcmFuc3BvcnRpc3RhPjE3NTE1OTU1NDU8L0NlZHVsYVRyYW5zcG9ydGlzdGE+PE5vbWJyZVRyYW5zcG9ydGlzdGE+QW5nZWwgQXVjYW5jZWxhPC9Ob21icmVUcmFuc3BvcnRpc3RhPjxDb2RDZW50cm9Bcmllcz48L0NvZENlbnRyb0FyaWVzPjxUaWNrZXRBcmllcz4gPC9UaWNrZXRBcmllcz48Q2VkVXN1YXJpb0FyaWVzPiA8L0NlZFVzdWFyaW9Bcmllcz48Tm9tVXN1YXJpb0FyaWVzPiA8L05vbVVzdWFyaW9Bcmllcz48RXN0YXR1c0FyaWVzPjE8L0VzdGF0dXNBcmllcz48TWVuc2FqZUFyaWVzPkVudmlhZG88L01lbnNhamVBcmllcz48L0NhYmVjZXJhPjwvbnMxOkdlc0ltcFBlc0FyPg==</xmlFileBase64>" +
                                 " </validarPeso>" +
                                 " </soap:Body>" +
                                 "</soap:Envelope>");

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();
                    Console.WriteLine(soapResult);
                }
            }
        }
        /// <summary>
        /// Create a soap webrequest to [Url]
        /// </summary>
        /// <returns></returns>
        public static HttpWebRequest CreateWebRequest()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"http://localhost:56405/WebService1.asmx?op=HelloWorld");
            webRequest.Headers.Add(@"SOAP:Action");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

    }
}
