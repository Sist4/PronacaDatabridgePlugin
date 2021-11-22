using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Mime;
using System.Configuration;
using System.Reflection;
using System.Data.SqlClient;
using System.Data;

namespace PronacaApi
{
    public  class GestionVehiculos
    {

        #region Correo
        public string EnvioCorreo(string N_Transaccion,string codigo_transaccion,string placa_seleccionada,string ruta_Imagen)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Correo_Destino = cfg.AppSettings.Settings["Correo_Destino"].Value;
            
            //***********************************************************FIN DEL APP CONFIG

            var pathFromConfig = ConfigurationManager.AppSettings["mensaje"];
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress("precitrol@outlook.com");
                mail.To.Add(Correo_Destino);
                mail.Subject = "Sistema de Pesaje";
                mail.Body = "<h1>Notificacion</h1></br><p>La transaccion nº:"+ N_Transaccion + " con placa seleccionada del operador: "  + placa_seleccionada  + "   no cumple con las condiciones para seguir el proceso de Pesaje.</p></br> <p>Si desea seguir con la transaccion digite el siguiente codigo:"  +  codigo_transaccion + "   </p>";


                mail.IsBodyHtml = true;
                if (ruta_Imagen != (""))
                {
                    mail.Attachments.Add(new Attachment(ruta_Imagen));
                }
                using (SmtpClient smtp = new SmtpClient("smtp-mail.outlook.com", 25))
                {
                    smtp.Credentials = new NetworkCredential("precitrol@outlook.com", "Sistem@s2021");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
                return "";

            }
        }

        #endregion
        #region Camara_FTP
        //code



        public String listarFTP(string placa_enviada)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Usuario_Ftp = cfg.AppSettings.Settings["Usuario_Ftp"].Value;
            string Password_Ftp = cfg.AppSettings.Settings["Password_Ftp"].Value;
            string Direccion_Ftp = cfg.AppSettings.Settings["Direccion_Ftp"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string respuesta = "La placa no coicide:" + placa_enviada;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(Direccion_Ftp);
            ftpRequest.Credentials = new NetworkCredential(Usuario_Ftp, Password_Ftp);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());

            List<string> directories = new List<string>();

            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                //obtenemeos el listado de los archivos ftp
                //directories.Add(line);
                if (line.ToString() !=  "." & line.ToString() !=".."  & line.ToString() != "test")// | line == "..")
                {

                    string[] array = line.Split('_');
                    //FECHA Y HORA EN EL array[0] ;la placa en el array[1]
                     string FEC =   array[0];
                     string PLACA =   array[1].Replace(".jpg","");
                    //obtenemos la fecha y la hora 
                    string años = FEC.Substring(0, 4);
                    string mes = FEC.Substring(4, 2);
                    string dia = FEC.Substring(6, 2);
                    string horas = FEC.Substring(8, 2);
                    string minutos = FEC.Substring(10, 2);
                    string segundos = FEC.Substring(12, 2);
                    DateTime hora_foto = Convert.ToDateTime(años + "-" + mes + "-" + dia + " " + horas + ":" + minutos + ":" + segundos);
                    DateTime Fecha_Actual = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                     DateTime Fecha_restada = (Fecha_Actual.AddMinutes(-30));



                    if (placa_enviada.Equals(PLACA.Replace("-", "")) && (hora_foto.CompareTo(Fecha_restada) >= 0 && hora_foto.CompareTo(Fecha_Actual) <= 0))
                    {
                        respuesta = "";
                        return respuesta;
                        //Hago lo que quiero aquí

                    }
                    
                }

                line = streamReader.ReadLine();

            }

            streamReader.Close();




            return respuesta;

        }


        #endregion

        #region ConexionBdDataBridge
        
     

        SqlConnection ConexionSql = null;
        SqlCommand ComandoSql = null;
        string query = null;
        SqlDataReader LectorDatos = null;
        SqlDataAdapter AdaptadorSql = null;


        //Consulta 
        public string consulta_PinSalida(string N_Transaccion)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "select Veh_PinSalida from tb_vehiculos where veh_ticket='"+ N_Transaccion + "' AND  Veh_Estado='SP' ";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteScalar());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;
        }

        //INSERTAR EL DATO CON EL PIN GENERADO

        public string Insertar_Dato( string Veh_Bascula,  string Veh_Placa,  string Veh_Chofer,  string Veh_Peso_Ingreso,  
                                       string Veh_Ticket, string Veh_Estado)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "INSERT INTO [dbo].[Tb_Vehiculos] ([Veh_Bascula],[Veh_Placa],[Veh_Chofer],[Veh_Peso_Ingreso],[Veh_Ticket],[Veh_Estado],[Veh_Fecha_Ingreso]) VALUES('" +  Veh_Bascula + "','" +  Veh_Placa + "','" +  Veh_Chofer + "','" +  Veh_Peso_Ingreso + "','" +  Veh_Ticket + "','" +  Veh_Estado + "',getdate())";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;
            
        }

        public string Peso_Salida(string Veh_BasculaSalida,string Veh_Peso_Salida, string Veh_Ticket,string Veh_PinSalida, string  Ven_RutaImg)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "update Tb_Vehiculos set Veh_BasculaSalida='"+ Veh_BasculaSalida + "',Veh_Peso_Salida='"+ Veh_Peso_Salida + "',Veh_Fecha_Salida=GETDATE(),Veh_PinSalida='"+ Veh_PinSalida + "',Veh_RutaImg='"+ Ven_RutaImg + "'  where  Veh_Ticket='" + Veh_Ticket + "'";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;

        }


        #endregion


        //final 
        #region Biometrico



        public string consulta_BiometricoChofer(string Cedula_Chofer)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_BDBiometrico"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                //consulta = "select USERINFO.USERID ,USERINFO.Name,CHECKTIME  from USERINFO inner join CHECKINOUT on USERINFO.USERID= CHECKINOUT.USERID where USERINFO.Name='"+ Cedula_Chofer + "' and   CHECKTIME BETWEEN DATEADD(minute, -11, GETDATE() )  and getdate()";
                consulta = "select USERINFO.USERID ,USERINFO.Name,CHECKTIME  from USERINFO inner join CHECKINOUT on USERINFO.USERID= CHECKINOUT.USERID where USERINFO.Name='"+ Cedula_Chofer + "' and   CHECKTIME BETWEEN DATEADD(minute, -11, GETDATE() )  and getdate()";
                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteScalar());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;
        }



        public string consulta_ExisteChofer(string Cedula_Chofer)
        {
                //*************************************************************APP CONFIG
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_BDBiometrico"].Value;

                //***********************************************************FIN DEL APP CONFIG
                string consulta;
                try
                {
                consulta = "select top 1 * from USERINFO where USERINFO.Name='"+  Cedula_Chofer +"'";
                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteScalar());
                    ConexionSql.Close();
                }
                finally
                {

                    if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                    {
                        ConexionSql.Close();
                    }
                }

                return consulta;
            }


        #endregion


        #region Pesaje_Ingreso
        //verificamos si el pesaje de ing. existe un ping generado se guarda en el campo 
        public string consulta_PlacaIngreso(string Placa_Seleccionada)
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG

            //*************VEN_ESTADO**********

//            iP=IngresoParcial
//            IC=IngresoCompleto  

            string consulta;
            try
            {
                consulta = "select Veh_PinEntrada from tb_vehiculos where veh_placa='" + Placa_Seleccionada + "' and veh_estado ='IP'";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteScalar());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;
        }


        public string Gestion_Pesaje(String Veh_Bascula ,String Veh_BasculaSalida,String Veh_Placa 
           ,String Veh_Chofer,String Veh_Peso_Ingreso ,String Veh_Peso_Salida 
           ,String Veh_Ticket ,String Veh_PinEntrada,String Veh_PinSalida 
           ,String Ven_RutaImgIng,String Ven_RutaImgSal,String Veh_Estado )
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "EXECUTE P_TBVehiculos N'" + Veh_Bascula + "',N'" + Veh_BasculaSalida + "',N'" + Veh_Placa  + "',N'" + Veh_Chofer + "',N'" + Veh_Peso_Ingreso + "',N'" + Veh_Peso_Salida + "',N'" + Veh_Ticket + "',N'" + Veh_PinEntrada + "',N'" + Veh_PinSalida + "',N'" + Ven_RutaImgIng + "',N'" + Ven_RutaImgSal + "',N'" + Veh_Estado + "'";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }

            return consulta;

        }



        #endregion

    }


}

