using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ComunicacionAries
{
    public partial class Form1 : Form
    {
        SqlConnection ConexionSql = null;
        SqlCommand ComandoSql = null;
        string query = null;
        SqlDataReader LectorDatos = null;
        SqlDataAdapter AdaptadorSql = null;
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        public Form1()
        {
            InitializeComponent();
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            Aries aries = new Aries();
            foreach (string arg in args)
            {
                if (!arg.Equals("C:\\Program Files (x86)\\METTLER TOLEDO\\DataBridge\\Comunicación Aries\\ComunicacionAries.exe"))
                {
                    ObtenerAriesPendiente(arg, aries);
                    if (EnviarXML(aries))
                        ActualizarRegistro(aries);
                }

            }

            this.Close();
        }
        private bool EnviarXML(Aries aries)
        {
            if (aries.Id.Equals(""))
            {
                return false;
            }
            else
            {
                ///PROCESAMOS EL MENSAJE
                try
                {
                    WsPronProduccion.ImportacionPesosAriesPortClient servicioPortClient = new WsPronProduccion.ImportacionPesosAriesPortClient();
                    WsPronProduccion.validarPeso sendBalanceData = new WsPronProduccion.validarPeso();
                    servicioPortClient.ClientCredentials.UserName.UserName = @"data_bridge";
                    servicioPortClient.ClientCredentials.UserName.Password = @"UHIwZFAzczBzRDR0NEJyMWRnM1ByMG40YzQk";
                    sendBalanceData.xmlFileBase64 = aries.XmlEnviado; //"PG5zMTpHZXNJbXBQZXNBciB4bWxuczpuczE9Imh0dHA6Ly9sbi5nZXNhbG0uaW50ZWdyYWNpb24ucHJvbmFjYS5jb20uZWMiPjxDb250cm9sUHJvY2Vzbz48Q29kaWdvQ29tcGFuaWE+MDAyPC9Db2RpZ29Db21wYW5pYT48Q29kaWdvU2lzdGVtYT5EQjwvQ29kaWdvU2lzdGVtYT48Q29kaWdvU2VydmljaW8+VmFsaWRhUGVzb3NEQjwvQ29kaWdvU2VydmljaW8+PFByb2Nlc28+SW5zZXJ0YXIvVmFsaWRhcjwvUHJvY2Vzbz48UmVzdWx0YWRvPjwvUmVzdWx0YWRvPjwvQ29udHJvbFByb2Nlc28+PENhYmVjZXJhPjxUaWNrZXREYXRhQnJpZGdlPjk5OTwvVGlja2V0RGF0YUJyaWRnZT48RmVjaGFUaWNrZXRQcm9jZXNvPjEwLzEwLzIwMjE8L0ZlY2hhVGlja2V0UHJvY2Vzbz48SG9yYVRpY2tldFByb2Nlc28+MTA6MDA6MDA8L0hvcmFUaWNrZXRQcm9jZXNvPjxVc3VhcmlvRGF0YUJyaWRnZT5Db25maWd1cmFkb3I8L1VzdWFyaW9EYXRhQnJpZGdlPjxOdW1lcm9CYXNjdWxhPjE8L051bWVyb0Jhc2N1bGE+PFRpcG9QZXNvPkU8L1RpcG9QZXNvPjxQZXNvVGlja2V0RGF0YUJyaWRnZT41NjAwMDwvUGVzb1RpY2tldERhdGFCcmlkZ2U+PFBsYWNhVmVoaWN1bG8+QUFBOTk5PC9QbGFjYVZlaGljdWxvPjxDZWR1bGFUcmFuc3BvcnRpc3RhPjE3NTE1OTU1NDU8L0NlZHVsYVRyYW5zcG9ydGlzdGE+PE5vbWJyZVRyYW5zcG9ydGlzdGE+QW5nZWwgQXVjYW5jZWxhPC9Ob21icmVUcmFuc3BvcnRpc3RhPjxDb2RDZW50cm9Bcmllcz48L0NvZENlbnRyb0FyaWVzPjxUaWNrZXRBcmllcz4gPC9UaWNrZXRBcmllcz48Q2VkVXN1YXJpb0FyaWVzPiA8L0NlZFVzdWFyaW9Bcmllcz48Tm9tVXN1YXJpb0FyaWVzPiA8L05vbVVzdWFyaW9Bcmllcz48RXN0YXR1c0FyaWVzPjE8L0VzdGF0dXNBcmllcz48TWVuc2FqZUFyaWVzPkVudmlhZG88L01lbnNhamVBcmllcz48L0NhYmVjZXJhPjwvbnMxOkdlc0ltcFBlc0FyPg==";

                    WsPronProduccion.validarPesoResponse response = servicioPortClient.ImportacionPesosAries(sendBalanceData);
                    aries.XmlRecibido = response.validarPesoResult;
                    aries.MensajeRecibido = LeerMensaje_Xml(DecodeBase64ToString(aries.XmlRecibido));
                    aries.EstatusRecibido = LeerEstatus_Xml(DecodeBase64ToString(aries.XmlRecibido));
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Se produjo el siguiente error: " + ex.Message);
                    return false;
                }
            }
        }
        private void ActualizarRegistro(Aries aries)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE ARIES SET ARIES_ESTATUSRECIBIDO=@estatus,ARIES_MENSAJERECIBIDO=@mensaje,ARIES_XMLRECIBIDO=@xml,ARIES_ESTADO='E' WHERE ARIES_ID=@id", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@estatus", aries.EstatusRecibido));
                        command.Parameters.Add(new SqlParameter("@mensaje", aries.MensajeRecibido));
                        command.Parameters.Add(new SqlParameter("@xml", aries.XmlRecibido));
                        command.Parameters.Add(new SqlParameter("@id", aries.Id));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Se produjo el siguiente error: " + ex.Message);
            }
        }
        private void ObtenerAriesPendiente(string idTicket, Aries aries)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG

            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1* FROM ARIES WHERE ARIES_ESTADO='P' AND TIC_ID=@idTicket ORDER BY ARIES_FECHA DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@idTicket", idTicket));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                aries.Id = reader.GetString(0);
                                aries.XmlEnviado = reader.GetString(5);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Se produjo el siguiente error: " + ex.Message);
            }






            

        }

    


        private int LeerEstatus_Xml(string Xml)
        {
            const string Comillas = "\"";
            string res_xml = Xml.Replace("<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
            res_xml = res_xml.Replace("ns1:GesImpPesAr", "ns1");

            XmlDocument xmltest = new XmlDocument();
            xmltest.LoadXml(res_xml);
            XmlNodeList nodes = xmltest.SelectNodes("//ns1/Cabecera");
            int Estatus = 0;
            string Mensaje = "";
            foreach (XmlNode node in nodes)
            {
                Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                Mensaje = node["MensajeAries"].InnerText;
            }
            if (Estatus != 0)
                return Estatus;
            else
            {
                res_xml = Xml.Replace("<GesImpPesAr xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
                res_xml = res_xml.Replace("GesImpPesAr", "ns1");
                xmltest = new XmlDocument();
                xmltest.LoadXml(res_xml);
                nodes = xmltest.SelectNodes("//ns1/Cabecera");
                Estatus = 0;
                Mensaje = "";
                foreach (XmlNode node in nodes)
                {
                    Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                    Mensaje = node["MensajeAries"].InnerText;
                }
                if (Estatus != 0)
                    return Estatus;
                else
                    return 1;
            }
        }
        private string LeerMensaje_Xml(string Xml)
        {
            const string Comillas = "\"";
            string res_xml = Xml.Replace("<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
            res_xml = res_xml.Replace("ns1:GesImpPesAr", "ns1");

            XmlDocument xmltest = new XmlDocument();
            xmltest.LoadXml(res_xml);
            XmlNodeList nodes = xmltest.SelectNodes("//ns1/Cabecera");
            int Estatus = 0;
            string Mensaje = "";
            foreach (XmlNode node in nodes)
            {
                Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                Mensaje = node["MensajeAries"].InnerText;
            }

            if (Mensaje != "")
                return Mensaje;
            else
            {
                res_xml = Xml.Replace("<GesImpPesAr xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
                res_xml = res_xml.Replace("GesImpPesAr", "ns1");
                xmltest = new XmlDocument();
                xmltest.LoadXml(res_xml);
                nodes = xmltest.SelectNodes("//ns1/Cabecera");
                Estatus = 0;
                Mensaje = "";
                foreach (XmlNode node in nodes)
                {
                    Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                    Mensaje = node["MensajeAries"].InnerText;
                }

                if (Mensaje != "")
                    return Mensaje;
                else
                    return Xml;
            }
        }

        public static string DecodeBase64ToString(string valor)
        {
            byte[] myBase64ret = Convert.FromBase64String(valor);
            string myStr = System.Text.Encoding.UTF8.GetString(myBase64ret);
            return myStr;
        }
        public static string EncodeStrToBase64(string valor)
        {
            byte[] myByte = System.Text.Encoding.UTF8.GetBytes(valor);
            string myBase64 = Convert.ToBase64String(myByte);
            return myBase64;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
