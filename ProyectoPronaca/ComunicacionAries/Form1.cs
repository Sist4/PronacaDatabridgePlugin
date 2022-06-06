using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComunicacionAries
{
    public partial class Form1 : Form
    {
        SqlConnection ConexionSql = null;
        SqlCommand ComandoSql = null;
        string query = null;
        SqlDataReader LectorDatos = null;
        SqlDataAdapter AdaptadorSql = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (!arg.Equals("C:\\Program Files (x86)\\METTLER TOLEDO\\DataBridge\\Comunicación Aries\\ComunicacionAries.exe"))
                    G_Msg(arg);
            }
            
            this.Close();
        }
        private void G_Msg(string transaccion)
        {
            try
            {
                //*************************************************************APP CONFIG
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                //string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;


                //***********************************************************FIN DEL APP CONFIG

                string consulta;

                int Codigo = 0;
                int Transaccion;
                string mensaje_V = "";
                string mensaje_R;

                using (SqlConnection connection = new SqlConnection(Conexion_Bd))
                {


                    String sql = "SELECT top 1* FROM ComunicacionAries where ComAries_Estado = 'A' AND ComAries_Transaccion='" + transaccion + "' ORDER BY ComAries_Codigo DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Codigo = Convert.ToInt32(reader.GetInt32(0));
                                Transaccion = Convert.ToInt32(reader.GetInt32(1));
                                mensaje_V = reader.GetString(2);


                            }
                        }
                        connection.Close();
                    }
                }
                if (Codigo.Equals(0))
                {

                }
                else
                {
                    ///PROCESAMOS EL MESNAJE
                    WsPronProduccion.ImportacionPesosAriesPortClient servicioPortClient = new WsPronProduccion.ImportacionPesosAriesPortClient();
                    WsPronProduccion.validarPeso sendBalanceData = new WsPronProduccion.validarPeso();
                    servicioPortClient.ClientCredentials.UserName.UserName = @"data_bridge";
                    servicioPortClient.ClientCredentials.UserName.Password = @"UHIwZFAzczBzRDR0NEJyMWRnM1ByMG40YzQk";
                    sendBalanceData.xmlFileBase64 = mensaje_V; //"PG5zMTpHZXNJbXBQZXNBciB4bWxuczpuczE9Imh0dHA6Ly9sbi5nZXNhbG0uaW50ZWdyYWNpb24ucHJvbmFjYS5jb20uZWMiPjxDb250cm9sUHJvY2Vzbz48Q29kaWdvQ29tcGFuaWE+MDAyPC9Db2RpZ29Db21wYW5pYT48Q29kaWdvU2lzdGVtYT5EQjwvQ29kaWdvU2lzdGVtYT48Q29kaWdvU2VydmljaW8+VmFsaWRhUGVzb3NEQjwvQ29kaWdvU2VydmljaW8+PFByb2Nlc28+SW5zZXJ0YXIvVmFsaWRhcjwvUHJvY2Vzbz48UmVzdWx0YWRvPjwvUmVzdWx0YWRvPjwvQ29udHJvbFByb2Nlc28+PENhYmVjZXJhPjxUaWNrZXREYXRhQnJpZGdlPjk5OTwvVGlja2V0RGF0YUJyaWRnZT48RmVjaGFUaWNrZXRQcm9jZXNvPjEwLzEwLzIwMjE8L0ZlY2hhVGlja2V0UHJvY2Vzbz48SG9yYVRpY2tldFByb2Nlc28+MTA6MDA6MDA8L0hvcmFUaWNrZXRQcm9jZXNvPjxVc3VhcmlvRGF0YUJyaWRnZT5Db25maWd1cmFkb3I8L1VzdWFyaW9EYXRhQnJpZGdlPjxOdW1lcm9CYXNjdWxhPjE8L051bWVyb0Jhc2N1bGE+PFRpcG9QZXNvPkU8L1RpcG9QZXNvPjxQZXNvVGlja2V0RGF0YUJyaWRnZT41NjAwMDwvUGVzb1RpY2tldERhdGFCcmlkZ2U+PFBsYWNhVmVoaWN1bG8+QUFBOTk5PC9QbGFjYVZlaGljdWxvPjxDZWR1bGFUcmFuc3BvcnRpc3RhPjE3NTE1OTU1NDU8L0NlZHVsYVRyYW5zcG9ydGlzdGE+PE5vbWJyZVRyYW5zcG9ydGlzdGE+QW5nZWwgQXVjYW5jZWxhPC9Ob21icmVUcmFuc3BvcnRpc3RhPjxDb2RDZW50cm9Bcmllcz48L0NvZENlbnRyb0FyaWVzPjxUaWNrZXRBcmllcz4gPC9UaWNrZXRBcmllcz48Q2VkVXN1YXJpb0FyaWVzPiA8L0NlZFVzdWFyaW9Bcmllcz48Tm9tVXN1YXJpb0FyaWVzPiA8L05vbVVzdWFyaW9Bcmllcz48RXN0YXR1c0FyaWVzPjE8L0VzdGF0dXNBcmllcz48TWVuc2FqZUFyaWVzPkVudmlhZG88L01lbnNhamVBcmllcz48L0NhYmVjZXJhPjwvbnMxOkdlc0ltcFBlc0FyPg==";

                    WsPronProduccion.validarPesoResponse response = servicioPortClient.ImportacionPesosAries(sendBalanceData);
                    string XmlRespuesta = response.validarPesoResult;
                    ///gUARDAMOS EL MESNAJE PROCESADO
                    ///


                    consulta = "UPDATE COMUNICACIONARIES SET [ComAries_XMLRecibido]='" + XmlRespuesta + "', [ComAries_Estado] ='Procesado' where ComAries_Codigo='" + Codigo + "' ";

                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    ConexionSql.Close();


                    //   MessageBox.Show(XmlRespuesta);

                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Se produjo el siguiente error: "+e.ToString());
            }



        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
