﻿using System;
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
using Renci.SshNet;
using System.Xml;
using System.Diagnostics;
using System.Collections;

namespace PronacaPlugin
{
    public class GestionVehiculos
    {
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        public GestionVehiculos()
        {
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
        }
        #region Correo
        public string EnvioCorreo(string N_Transaccion, string codigo_transaccion, string placa_seleccionada, string ruta_Imagen1, string ruta_Imagen2)
        {
            //*************************************************************APP CONFIG
            string Correo_Destino = cfg.AppSettings.Settings["Correo_Destino"].Value;
            string Correo_Destino2 = cfg.AppSettings.Settings["Correo_Destino2"].Value;
            string Correo_Envio = cfg.AppSettings.Settings["Correo_Envio"].Value;
            string Correo_Pasword = cfg.AppSettings.Settings["Correo_Pasword"].Value;
            string Host_Salida = cfg.AppSettings.Settings["Host_Salida"].Value;
            string Host_Puerto = cfg.AppSettings.Settings["Host_Puerto"].Value;

            //***********************************************************FIN DEL APP CONFIG

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(Correo_Envio);
                mail.To.Add(Correo_Destino);
                mail.To.Add(Correo_Destino2);
                mail.Subject = "DataBridge - Sistema de Pesaje - ";
                mail.Body = "<h1>Notificacion</h1></br><p>La transaccion nº:" + N_Transaccion + " con placa seleccionada del operador: " + placa_seleccionada + "   no cumple con las condiciones para seguir el proceso de Pesaje.</p></br> <p>Si desea seguir con la transaccion digite el siguiente PIN:" + codigo_transaccion + "   </p>";


                mail.IsBodyHtml = true;
                if (ruta_Imagen1 != (""))
                {
                    mail.Attachments.Add(new Attachment(@"C:\Camara_DataBridge\" + ruta_Imagen1 + ".jpg"));
                }
                if (ruta_Imagen2 != (""))
                {
                    mail.Attachments.Add(new Attachment(@"C:\Camara_DataBridge\" + ruta_Imagen2 + ".jpg"));
                }
                using (SmtpClient smtp = new SmtpClient(Host_Salida, Convert.ToInt32(Host_Puerto)))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(Correo_Envio, Correo_Pasword);
                    smtp.EnableSsl = false; //pronaca en false
                                           // smtp.TargetName = "STARTTLS/smtp-mail.outlook.com"; //solo si el servidor de correo tiene TTLS
                    try {
                        smtp.Send(mail);
                    }
                    catch(Exception ex)
                    {
                        throw;
                    }
                    
                }
                //// fin del proyecto
                return "";

            }
        }

        #endregion
        #region Camara_FTP
        //code



        public String listarFTP(string placa_enviada, int nScaleId)
        {

            string respuesta = "La placa no coicide:" + placa_enviada;
            try
            {    //*************************************************************APP CONFIG
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                string Usuario_Sftp = cfg.AppSettings.Settings["Usuario_Ftp"].Value;
                string Password_Sftp = cfg.AppSettings.Settings["Password_Ftp"].Value;

                string Ip_Sftp = cfg.AppSettings.Settings["IP_Ftp"].Value;

                //***********************************************************FIN DEL APP CONFIG
                using (SftpClient cliente = new SftpClient(new PasswordConnectionInfo(Ip_Sftp, Usuario_Sftp, Password_Sftp)))
                {
                    cliente.Connect();
                    buscarImagenesSFTP(cliente, "/Camara", placa_enviada, ref respuesta, nScaleId);
                    cliente.Disconnect();
                }
            } catch (Exception ex)
            {

            }


            return respuesta;

        }

        private void buscarImagenesSFTP(SftpClient cliente, string directorioServidor, string placa_enviada, ref string respuesta, int nScaleId)
        {


            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string Ubicacion = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(Ubicacion);
            string T_Camion = cfg.AppSettings.Settings["T_Camion"].Value;
            //***********************************************************FIN DEL APP CONFIG


            var paths = cliente.ListDirectory(directorioServidor).Select(s => s.Name);
            foreach (var path in paths)
            {
                if (path.ToString().Contains(".jpg"))
                {

                    string[] array = path.ToString().Split('_');
                    //FECHA Y HORA EN EL array[0] ;la placa en el array[1]
                    string sucursalBasculaCamaras = array[0];
                    string FEC = array[1];
                    string placa = array[2].Replace(".jpg", "");
                    //obtenemos la fecha y la hora 
                    string años = FEC.Substring(0, 4);
                    string mes = FEC.Substring(4, 2);
                    string dia = FEC.Substring(6, 2);
                    string horas = FEC.Substring(8, 2);
                    string minutos = FEC.Substring(10, 2);
                    string segundos = FEC.Substring(12, 2);
                    DateTime hora_foto = Convert.ToDateTime(años + "-" + mes + "-" + dia + " " + horas + ":" + minutos + ":" + segundos);
                    DateTime Fecha_Actual = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    DateTime Fecha_restada = (Fecha_Actual.AddMinutes(-Convert.ToUInt32(T_Camion)));
                    string placa_con_cero = placa.Substring(0, 3) + "0" + placa.Substring(3, (placa.Length - 3));
                    placa_enviada = placa_enviada.Replace("-", "");
                    placa = placa.Replace("-", "");
                    if (nScaleId == 0 && (sucursalBasculaCamaras.Equals("PBOCAM11") || sucursalBasculaCamaras.Equals("PBOCAM12")))
                    {
                        if ((placa.Equals(placa_enviada) || placa_con_cero.Equals(placa_enviada)) && (hora_foto.CompareTo(Fecha_restada) >= 0 && hora_foto.CompareTo(Fecha_Actual) <= 0))
                        {

                            respuesta = "";
                        }
                    }
                    else if (nScaleId == 1 && (sucursalBasculaCamaras.Equals("PBOCAM21") || sucursalBasculaCamaras.Equals("PBOCAM22")))
                    {
                        if ((placa.Equals(placa_enviada) || placa_con_cero.Equals(placa_enviada)) && (hora_foto.CompareTo(Fecha_restada) >= 0 && hora_foto.CompareTo(Fecha_Actual) <= 0))
                        {

                            respuesta = "";
                        }
                    }
                     

                }


            }

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
                consulta = "select Veh_PinSalida from tb_vehiculos where veh_ticket='" + N_Transaccion + "' AND  Veh_Estado='SP' ";

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

        public string Insertar_Dato(string Veh_Bascula, string Veh_Placa, string Veh_Chofer, string Veh_Peso_Ingreso,
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
                consulta = "INSERT INTO [dbo].[Tb_Vehiculos] ([Veh_Bascula],[Veh_Placa],[Veh_Chofer],[Veh_Peso_Ingreso],[Veh_Ticket],[Veh_Estado],[Veh_Fecha_Ingreso]) VALUES('" + Veh_Bascula + "','" + Veh_Placa + "','" + Veh_Chofer + "','" + Veh_Peso_Ingreso + "','" + Veh_Ticket + "','" + Veh_Estado + "',getdate())";

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

        public string Peso_Salida(string Veh_BasculaSalida, string Veh_Peso_Salida, string Veh_Ticket, string Veh_PinSalida, string Veh_RutaImg)
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
                consulta = "update Tb_Vehiculos set Veh_BasculaSalida='" + Veh_BasculaSalida + "',Veh_Peso_Salida='" + Veh_Peso_Salida + "',Veh_Fecha_Salida=GETDATE(),Veh_PinSalida='" + Veh_PinSalida + "',Veh_RutaImg='" + Veh_RutaImg + "'  where  Veh_Ticket='" + Veh_Ticket + "'";

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
            string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                //consulta = "select USERINFO.USERID ,USERINFO.Name,CHECKTIME  from USERINFO inner join CHECKINOUT on USERINFO.USERID= CHECKINOUT.USERID where USERINFO.Name='"+ Cedula_Chofer + "' and   CHECKTIME BETWEEN DATEADD(minute, -11, GETDATE() )  and getdate()";
                //consulta = "select USERINFO.USERID ,USERINFO.CardNo,CHECKTIME  from USERINFO inner join CHECKINOUT on USERINFO.USERID= CHECKINOUT.USERID where USERINFO.CardNo='" + Cedula_Chofer + "' and   CHECKTIME BETWEEN DATEADD(minute, -" + (Convert.ToInt32(T_Chofer) + 1) + ", GETDATE() )  and getdate()";
                consulta = "SELECT personnel_employee.emp_code,iclock_transaction.punch_time FROM personnel_employee INNER JOIN iclock_transaction ON personnel_employee.emp_code=iclock_transaction.emp_code WHERE personnel_employee.first_name='" + Cedula_Chofer + "' AND punch_time BETWEEN DATEADD(minute, -" + (Convert.ToInt32(T_Chofer) + 1) + ", GETDATE() )  and getdate()";
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
                //consulta = "select top 1 * from USERINFO where USERINFO.CardNo='" + Cedula_Chofer + "'";
                consulta = "SELECT top 1 * FROM personnel_employee where personnel_employee.first_name = '" + Cedula_Chofer + "'";
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


        public string Gestion_Pesaje(String Veh_Bascula, String Veh_BasculaSalida, String Veh_Placa
        , String Veh_Chofer, String Veh_Peso_Ingreso, String Veh_Peso_Salida
        , String Veh_Ticket, String Veh_PinEntrada, String Veh_PinSalida
        , String Veh_RutaImgIng, String Veh_RutaImgSal, string pesosObtenidos,String Veh_Estado, string msj_recibido, string Numeral_recibido)
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
                consulta = "EXECUTE P_TBVehiculos N'" + Veh_Bascula + "',N'" + Veh_BasculaSalida + "',N'" + Veh_Placa + "',N'" + Veh_Chofer + "',N'" + Veh_Peso_Ingreso + "',N'" + Veh_Peso_Salida + "',N'" + Veh_Ticket + "',N'" + Veh_PinEntrada + "',N'" + Veh_PinSalida + "',N'" + Veh_RutaImgIng + "',N'" + Veh_RutaImgSal + "',N'" +pesosObtenidos+"',N'"+ Veh_Estado + "',N'" + msj_recibido + "',N'" + Numeral_recibido + "',N''";

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




        #region ComunicacionDataBridge

        //Codigo de Transaccion del DataBridge
        public string consulta_TransaccionDB()
        {
            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_DB"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "SELECT MAX(CONVERT(int,[TransactionNumber])) + 1 FROM  DataBridge.[Transaction]";

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
        //*************************************Codigo de Transaccion del DataBridge

        //Consultamos el valor si es un pesjae de visitante
        public string consulta_TipoIngreso(string ntransaccion)
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
                consulta = "select Veh_Val2 from Tb_Vehiculos where Veh_Ticket='" + ntransaccion + "'";

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
        //**********fin*********************

        public static string DecodeBase64ToString(string valor)
        {
            byte[] myBase64ret = Convert.FromBase64String(valor);
            string myStr = System.Text.Encoding.UTF8.GetString(myBase64ret);
            return myStr;
        }
        /// <summary>
        /// Convierte texto string en Base64
        /// </summary>
        /// <param name="valor">Valor a reemplazar</param>
        /// <returns></returns>
        //Codificamos el valor de 64 bits estandar de Pronaca 
        public static string EncodeStrToBase64(string valor)
        {
            byte[] myByte = System.Text.Encoding.UTF8.GetBytes(valor);
            string myBase64 = Convert.ToBase64String(myByte);
            return myBase64;
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
        public string InvokeService(string N_Transaccion, string FechaTicketProceso, string HoraTicketProceso, string UsuarioDataBridge, string NumeroBascula, string TipoPeso, string Peso_Ing,
                                  string Vehiculo, string Cedula, string Chofer)
        {

            try
            {
                //*************************************************************APP CONFIG
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                string Centro = cfg.AppSettings.Settings["Centro_Distribucion"].Value;

                //***********************************************************FIN DEL APP CONFIG

                const string Comillas = "\"";


                string XmlEnvio = "<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">" +
                                "<ControlProceso>" +
                                "<CodigoCompania>002</CodigoCompania>" +
                                "<CodigoSistema>DB</CodigoSistema>" +
                                "<CodigoServicio>ValidaPesosDB</CodigoServicio>" +
                                "<Proceso>Insertar/Validar</Proceso>" +
                                "<Resultado></Resultado>" +
                                "</ControlProceso>" +
                                "<Cabecera>" +
                                "<TicketDataBridge>" + N_Transaccion + "</TicketDataBridge>" +
                                "<FechaTicketProceso>" + FechaTicketProceso + "</FechaTicketProceso>" +
                                "<HoraTicketProceso>" + HoraTicketProceso + "</HoraTicketProceso>" +
                                "<UsuarioDataBridge>" + UsuarioDataBridge + "</UsuarioDataBridge>" +
                                "<NumeroBascula>" + NumeroBascula + "</NumeroBascula>" +
                                "<TipoPeso>" + TipoPeso + "</TipoPeso>" +
                                "<PesoTicketDataBridge>" + Peso_Ing + "</PesoTicketDataBridge>" +
                                "<PlacaVehiculo>" + Vehiculo + "</PlacaVehiculo>" +
                                "<CedulaTransportista>" + Cedula + "</CedulaTransportista>" +
                                "<NombreTransportista>" + Chofer + "</NombreTransportista>" +
                                "<CodCentroAries>" + Centro + "</CodCentroAries>" +
                                "<TicketAries> </TicketAries>" +
                                "<CedUsuarioAries> </CedUsuarioAries>" +
                                "<NomUsuarioAries> </NomUsuarioAries>" +
                                "<EstatusAries>1</EstatusAries>" +
                                "<MensajeAries>Enviado</MensajeAries>" +
                                "</Cabecera>" +
                                "</ns1:GesImpPesAr>";

                ///*****************************************************esto no va************************************
                string codificiacionMsj = EncodeStrToBase64(XmlEnvio);
                string res = G_Msg(codificiacionMsj, "A", N_Transaccion);

                Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\PruebasComunicacion.exe");
                System.Threading.Thread.Sleep(3000);
                //CONSULTA DE DATOS
                int Codigo = 0;
                int Transaccion;
                string mensaje_R = "";
                string Estado = "";

                using (SqlConnection connection = new SqlConnection(Conexion_Bd))
                {


                    String sql = "SELECT top 10 * FROM Temporal where Tem_Estado = 'Procesado' AND Tem_Transaccion='" + N_Transaccion + "'";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Codigo = Convert.ToInt32(reader.GetInt32(0));
                                mensaje_R = reader.GetString(3);
                                Estado = reader.GetString(4);
                            }
                        }
                        connection.Close();
                    }
                }


                if (Estado != "")
                {

                    string consulta = "UPDATE TEMPORAL SET  [Tem_Estado] ='Fin' where Tem_Codigo='" + Codigo + "' ";

                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    ConexionSql.Close();
                    return leer_Xml(DecodeBase64ToString(mensaje_R));

                }
                else
                {

                    string consulta = "DELETE FROM TEMPORAL  where Tem_Codigo='" + Codigo + "'  AND [Tem_Estado] ='A'";

                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    ConexionSql.Close();
                
                    return "El mensaje No fue procesado por Aries";





                }


                //FIN


                //////////////////Calling CreateSOAPWebRequest method    
                ////////////////HttpWebRequest request = CreateSOAPWebRequest();
                //////////////////request.Credentials = new NetworkCredential("Aries_DataBridge", "Pronaca2021$");
                ////////////////request.Credentials = new NetworkCredential("data_bridge_test", "UGVzMHNENHQ0QnIxZGczUHIwbmFjYSQ");
                ////////////////XmlDocument SOAPReqBody = new XmlDocument();
                //////////////////SOAP Body Request    
                ////////////////SOAPReqBody.LoadXml(@"<?xml version=" + Comillas + "1.0" + Comillas + " encoding=" + Comillas + "utf-8" + Comillas + "?> " +
                ////////////////                     "<soap:Envelope xmlns:xsi=" + Comillas + "http://www.w3.org/2001/XMLSchema-instance" + Comillas + " xmlns:xsd=" + Comillas + "http://www.w3.org/2001/XMLSchema" + Comillas + " xmlns:soap=" + Comillas + "http://schemas.xmlsoap.org/soap/envelope/" + Comillas + ">" +
                ////////////////                     "<soap:Body>" +
                ////////////////                     " <validarPeso xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">" +
                ////////////////                     " <xmlFileBase64>" + codificiacionMsj + "</xmlFileBase64>" +
                ////////////////                     " </validarPeso>" +
                ////////////////                     " </soap:Body>" +
                ////////////////                     "</soap:Envelope>");



                ////////////////using (Stream stream = request.GetRequestStream())
                ////////////////{
                ////////////////    SOAPReqBody.Save(stream);
                ////////////////}
                ////////////////System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ////////////////string ServiceResult;
                //////////////////Geting response from request    
                ////////////////using (WebResponse Serviceres = request.GetResponse())
                ////////////////{
                ////////////////    using (StreamReader rd = new StreamReader(Serviceres.GetResponseStream()))
                ////////////////    {
                ////////////////        //reading stream    
                ////////////////        ServiceResult = rd.ReadToEnd();
                ////////////////    }
                ////////////////}
                //////////////////de codificamos el xml recibido

                ////////////////string xmlResultado = ServiceResult.Replace("</validarPesoResult></validarPesoResponse></soap:Body></soap:Envelope>", "");
                ////////////////xmlResultado = xmlResultado.Replace("<?xml version=" + Comillas + "1.0" + Comillas + " encoding=" + Comillas + "utf-8" + Comillas + "?><soap:Envelope xmlns:soap=" + Comillas + "http://schemas.xmlsoap.org/soap/envelope/" + Comillas + " xmlns:xsi=" + Comillas + "http://www.w3.org/2001/XMLSchema-instance" + Comillas + " xmlns:xsd=" + Comillas + "http://www.w3.org/2001/XMLSchema" + Comillas + "><soap:Body><validarPesoResponse xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + "><validarPesoResult>", "");


                //////////////////Procesamos el msj
                ////////////////return leer_Xml(DecodeBase64ToString(xmlResultado));

            }
            catch (Exception e)
            {
                return e.ToString();
                //MessageBox.Show("problemas de comunicacion" + e);
            }

        }



        //*************Lectura del XML*****************************
        public string leer_Xml(string Xml)
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
            /*
                        ESTATUS DEL ARIES
            1 - SIEMPRE SE ENVIA
            RESPUESTA
            -------------- posibles fallos del xml(problema de comunicacion)
            2 - ERROR
            -------- -
            3 - EXITO - (termina el proceso)
            ---------------------------------------------------------------------------------------------
            4 - EXITO SIN TURNO(vehiculo sin carga se envia un pin no se envia al aries salida no se envia)
            ya no se envia al aries el pesaje de salida
            ---------------------------------------------------------------------------------------------
            5 - Error del factor de conversion(aborta el pesaje)
            */
            String envioRes = "";
            switch (Estatus)
            {

                case 2:
                    // ERROR
                    envioRes = "2/" + Estatus.ToString() + " " + Mensaje;
                    break;
                case 3:
                    // EXITO - (termina el proceso)
                    envioRes = "3/ EL PROCESOS SE COMPLETO CON EXITO";
                    break;
                case 4:
                    // EXITO SIN TURNO(vehiculo sin carga se envia un pin no se envia al aries salida no se envia)
                    envioRes = "4/" + Estatus.ToString() + " " + Mensaje;
                    break;

                case 5:
                    // Error del factor de conversion(aborta el pesaje)
                    envioRes = "5/" + Estatus.ToString() + " " + Mensaje;
                    break;

                default:

                    break;

            }


            return envioRes;
        }

        //*************FIN Lectura del XML*****************************

        private string G_Msg(string Mensaje, string Estado, string Tem_Transaccion)
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
                consulta = "INSERT INTO [dbo].[Temporal]  ([Tem_Mensaje],[Tem_Estado],Tem_Transaccion)VALUES('" + Mensaje + "','A'," + Tem_Transaccion + ")";

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
        public void InsertarPesosObtenidos(ArrayList pesosObtenidos,string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            foreach(String peso in pesosObtenidos)
            {
                try
                {
                    consulta = "update Tb_Vehiculos set Veh_PesosObtenidos= Veh_PesosObtenidos+'"+ peso+ "'+';' where  Veh_Ticket='" + transaccion + "'";
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
            }
            
           
        }

        public void anularTransacción(string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta;
            try
            {
                consulta = "update Tb_Vehiculos set Veh_Estado='TA' where  Veh_Ticket='" + transaccion + "'";

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
            
        }
        public void eliminarTransaccionPendiente()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            string consulta2;
            try
            {
                consulta2 = "DELETE FROM [dbo].[Tb_Vehiculos] WHERE Veh_Estado='IP';";

                SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                ConexionSql.Open();
                SqlCommand Comando_Sql = new SqlCommand(consulta2, ConexionSql);
                consulta2 = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                ConexionSql.Close();
            }
            finally
            {

                if (ConexionSql != null && ConexionSql.State != ConnectionState.Closed)
                {
                    ConexionSql.Close();
                }
            }
        }

        public void detenerSecuencia(string operador,string razon,int bascula,string pesoObtenido,string pesoBascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("INSERT INTO[dbo].[Secuencia] VALUES(GETDATE(),@operador,@razon,@bascula,@pesoObtenido,@pesoBascula)", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@operador", operador));
                    command.Parameters.Add(new SqlParameter("@razon", razon));
                    command.Parameters.Add(new SqlParameter("@bascula", bascula));
                    command.Parameters.Add(new SqlParameter("@pesoObtenido", pesoObtenido));
                    command.Parameters.Add(new SqlParameter("@pesoBascula", pesoBascula));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }

        #endregion


    }


}

