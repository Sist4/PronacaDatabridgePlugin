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
using Renci.SshNet;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Drawing;
using Caliburn.Micro;
using DataBridge.Attended.ViewModel;
using DataBridge.Core.Services;
using Ninject;
using Application = System.Windows.Application;

namespace PronacaPlugin
{
    public class GestionVehiculos
    {
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        string cam1;
        string cam2;
        string cam3;
        string cam4;
        public string mensaje { get; set; }
        string[] Correo_Destino;
        public GestionVehiculos()
        {
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            cam1 = cfg.AppSettings.Settings["Nom_Camara1"].Value.Substring(0, 8);
            cam2 = cfg.AppSettings.Settings["Nom_Camara2"].Value.Substring(0, 8);
            cam3 = cfg.AppSettings.Settings["Nom_Camara3"].Value.Substring(0, 8);
            cam4 = cfg.AppSettings.Settings["Nom_Camara4"].Value.Substring(0, 8);
        }
        #region Correo
        public bool EnvioCorreoPin(string N_Transaccion, string codigo_transaccion, string placa_seleccionada, string ruta_Imagen1, string ruta_Imagen2)
        {
            //*************************************************************APP CONFIG
            bool envio = false;
            Correo_Destino = new string[11];
            int numero_Correos = Converter.ConvertToInt32(cfg.AppSettings.Settings["Numero_Correos_Destino"].Value);
            Correo_Destino[1] = cfg.AppSettings.Settings["Correo_Destino1"].Value;
            Correo_Destino[2] = cfg.AppSettings.Settings["Correo_Destino2"].Value;
            Correo_Destino[3] = cfg.AppSettings.Settings["Correo_Destino3"].Value;
            Correo_Destino[4] = cfg.AppSettings.Settings["Correo_Destino4"].Value;
            Correo_Destino[5] = cfg.AppSettings.Settings["Correo_Destino5"].Value;
            Correo_Destino[6] = cfg.AppSettings.Settings["Correo_Destino6"].Value;
            Correo_Destino[7] = cfg.AppSettings.Settings["Correo_Destino7"].Value;
            Correo_Destino[8] = cfg.AppSettings.Settings["Correo_Destino8"].Value;
            Correo_Destino[9] = cfg.AppSettings.Settings["Correo_Destino9"].Value;
            Correo_Destino[10] = cfg.AppSettings.Settings["Correo_Destino10"].Value;
            string Correo_Envio = cfg.AppSettings.Settings["Correo_Envio"].Value;
            string Correo_Pasword = cfg.AppSettings.Settings["Correo_Pasword"].Value;
            string Host_Salida = cfg.AppSettings.Settings["Host_Salida"].Value;
            string Host_Puerto = cfg.AppSettings.Settings["Host_Puerto"].Value;
            bool ssl = Boolean.Parse(cfg.AppSettings.Settings["SSL"].Value);
            string directorio = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Camaras\";
            //***********************************************************FIN DEL APP CONFIG*****

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(Correo_Envio);
                for (int i = 1; i <= numero_Correos; i++)
                {
                    mail.To.Add(Correo_Destino[i]);
                }
                //copia oculta

                //for (int i = numero_Correos + 1; i <= 10; i++)
                //{
                //    mail.Bcc.Add(Correo_Destino[i]);
                //}

                mail.Subject = "DataBridge - Sistema de Pesaje - ";
                //mail.Body = "<h1>Notificacion</h1></br><p>La transaccion nº:" + N_Transaccion + " con placa seleccionada del operador: " + placa_seleccionada + "   no cumple con las condiciones para seguir el proceso de Pesaje.</p></br> <p>Si desea seguir con la transaccion digite el siguiente PIN:" + codigo_transaccion + "   </p>";
                mail.Body = "<h1>Notificación</h1><p>Las cámaras no identificaron la placa seleccionada, para proseguir con la transacción digite el PIN en el sistema de pesaje DataBridge.</p><table><tr><td>Fecha y hora:</td><td>" + DateTime.Now.ToString() + "</td></tr><tr><td>N# Transacción:</td><td>" + N_Transaccion + "</td></tr><tr><td>Placa seleccionada:</td><td>" + placa_seleccionada + "</td></tr><tr><td>PIN:</td><td>" + codigo_transaccion + "</td></tr><tr></table>";

                mail.IsBodyHtml = true;
                if (ruta_Imagen1 != (""))
                {
                    mail.Attachments.Add(new Attachment(directorio + ruta_Imagen1 + ".jpg"));
                }
                if (ruta_Imagen2 != (""))
                {
                    mail.Attachments.Add(new Attachment(directorio + ruta_Imagen2 + ".jpg"));
                }
                using (SmtpClient smtp = new SmtpClient(Host_Salida, Convert.ToInt32(Host_Puerto)))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(Correo_Envio, Correo_Pasword);
                    smtp.EnableSsl = ssl; //pronaca en false
                                          // smtp.TargetName = "STARTTLS/smtp-mail.outlook.com"; //solo si el servidor de correo tiene TTLS
                    try
                    {
                        smtp.Send(mail);
                        envio = true;
                    }
                    catch (Exception ex)
                    {
                        envio=false;
                        throw new ExcepcionNegocio("No se pudo enviar el correo, Porfavor revise la conexión a internet");
                        
                    }

                }
                //// fin del proyecto
                return envio;

            }
        }
        public string EnvioCorreoSecuenciaDetenida(string razon, string operador, string ruta_Imagen1, string ruta_Imagen2, string pesoObtenido, string pesoBascula)
        {
            //*************************************************************APP CONFIG
            Correo_Destino = new string[11];
            int numero_Correos = Converter.ConvertToInt32(cfg.AppSettings.Settings["Numero_Correos_Destino"].Value);
            Correo_Destino[1] = cfg.AppSettings.Settings["Correo_Destino1"].Value;
            Correo_Destino[2] = cfg.AppSettings.Settings["Correo_Destino2"].Value;
            Correo_Destino[3] = cfg.AppSettings.Settings["Correo_Destino3"].Value;
            Correo_Destino[4] = cfg.AppSettings.Settings["Correo_Destino4"].Value;
            Correo_Destino[5] = cfg.AppSettings.Settings["Correo_Destino5"].Value;
            Correo_Destino[6] = cfg.AppSettings.Settings["Correo_Destino6"].Value;
            Correo_Destino[7] = cfg.AppSettings.Settings["Correo_Destino7"].Value;
            Correo_Destino[8] = cfg.AppSettings.Settings["Correo_Destino8"].Value;
            Correo_Destino[9] = cfg.AppSettings.Settings["Correo_Destino9"].Value;
            Correo_Destino[10] = cfg.AppSettings.Settings["Correo_Destino10"].Value;
            string Correo_Envio = cfg.AppSettings.Settings["Correo_Envio"].Value;
            string Correo_Pasword = cfg.AppSettings.Settings["Correo_Pasword"].Value;
            string Host_Salida = cfg.AppSettings.Settings["Host_Salida"].Value;
            string Host_Puerto = cfg.AppSettings.Settings["Host_Puerto"].Value;
            bool ssl = Boolean.Parse(cfg.AppSettings.Settings["SSL"].Value);
            string directorio = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Camaras\";
            //***********************************************************FIN DEL APP CONFIG

            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(Correo_Envio);
                mail.From = new MailAddress(Correo_Envio);
                for (int i = 1; i <= numero_Correos; i++)
                {
                    mail.To.Add(Correo_Destino[i]);
                }
                mail.Subject = "DataBridge - Sistema de Pesaje - ";
                //mail.Body = "<h1>Notificacion</h1></br><p>El día " + DateTime.Now.ToString() + " fue detenida la secuencia, por la razón: " + razon + " por el operador: " +operador+".</p>";
                mail.Body = "<h1>Notificación</h1><p> Se ha detenido la secuencia de pesaje en DataBridge.</p><table><tr><td>Fecha y hora:</td><td>" + DateTime.Now.ToString() + "</td></tr><tr><td>Razón:</td><td>" + razon + "</td></tr><tr><td>Operador:</td><td>" + operador + "</td></tr><tr><td>Peso obtenido:</td><td>" + pesoObtenido + "</td></tr><tr><td>Peso en báscula:</td><td>" + pesoBascula + "</td></tr></table>";
                mail.IsBodyHtml = true;
                if (ruta_Imagen1 != (""))
                {
                    mail.Attachments.Add(new Attachment(directorio + ruta_Imagen1 + ".jpg"));
                }
                if (ruta_Imagen2 != (""))
                {
                    mail.Attachments.Add(new Attachment(directorio + ruta_Imagen2 + ".jpg"));
                }
                using (SmtpClient smtp = new SmtpClient(Host_Salida, Convert.ToInt32(Host_Puerto)))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(Correo_Envio, Correo_Pasword);
                    smtp.EnableSsl = ssl; //pronaca en false
                                          // smtp.TargetName = "STARTTLS/smtp-mail.outlook.com"; //solo si el servidor de correo tiene TTLS
                    try
                    {
                        smtp.Send(mail);
                    }
                    catch (Exception ex)
                    {
                        throw new ExcepcionNegocio("Error en el envío del correo");
                    }

                }
                //// fin del proyecto
                return "";

            }
        }

        public void escribirImagen(string ruta, string pesoBascula)
        {
            string directorio = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Camaras\";
            if (ruta != "")
            {
                var filePath = directorio + ruta + ".jpg";
                Bitmap bitmap = null;

                // Create from a stream so we don't keep a lock on the file.
                using (var stream = File.OpenRead(filePath))
                {
                    bitmap = (Bitmap)Bitmap.FromStream(stream);
                }

                using (bitmap)
                using (var graphics = Graphics.FromImage(bitmap))
                using (var font = new Font("Arial", 18, FontStyle.Regular))
                {
                    // Do what you want using the Graphics object here.
                    //graphics.DrawString("Fecha: 08-03-2022", font, Brushes.Red, 0, 650);
                    //graphics.DrawString("Placa: ABCD1234", font, Brushes.Red, 0, 670);
                    graphics.DrawString("Peso en báscula: " + pesoBascula, font, Brushes.Red, 0, 520);

                    // Important part!
                    bitmap.Save(filePath);
                }
            }

        }

        #endregion
        #region Camara_FTP
        //code



        public bool listarFTP(string placa_enviada, int nScaleId)
        {
            bool val = false;
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
                    val=buscarImagenesSFTP(cliente, "/Camara", placa_enviada, nScaleId);
                    cliente.Disconnect();
                }
            } catch (Exception ex)
            {
                //throw new ExcepcionNegocio("Error en la conexión con el servidor SFTP");
                ventanaOK("Error en la conexión con el servidor SFTP", "DataBridge Plugin");
            }


            return val;

        }

        private bool buscarImagenesSFTP(SftpClient cliente, string directorioServidor, string placa_enviada, int nScaleId)
        {

            //*************************************************************APP CONFIG
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string Ubicacion = Uri.UnescapeDataString(uri.Path);
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(Ubicacion);
            string T_Camion = cfg.AppSettings.Settings["T_Camion"].Value;
            StringBuilder placaNumeros = new StringBuilder();
            StringBuilder placaLetras = new StringBuilder();
            StringBuilder placaLeida = new StringBuilder();
            bool digitosPlaca = false;
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
                    string placa = array[2];
                    DateTime Fecha_Leida = new DateTime(Convert.ToInt32(FEC.Substring(0, 4)),
                        Convert.ToInt32(FEC.Substring(4, 2)),
                        Convert.ToInt32(FEC.Substring(6, 2)),
                        Convert.ToInt32(FEC.Substring(8, 2)),
                        Convert.ToInt32(FEC.Substring(10, 2)),
                        Convert.ToInt32(FEC.Substring(12, 2)));
                    //obtenemos la fecha y la hora 
                    //string años = FEC.Substring(0, 4);
                    //string mes = FEC.Substring(4, 2);
                    //string dia = FEC.Substring(6, 2);
                    //string horas = FEC.Substring(8, 2);
                    //string minutos = FEC.Substring(10, 2);
                    //string segundos = FEC.Substring(12, 2);
                    //DateTime hora_foto = Convert.ToDateTime(años + "-" + mes + "-" + dia + " " + horas + ":" + minutos + ":" + segundos);
                    DateTime Fecha_Actual = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Fecha_Actual = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).AddMinutes(5);
                    DateTime Fecha_restada = (Fecha_Actual.AddMinutes(-Convert.ToUInt32(T_Camion)));

                    placaLeida.Append(placa);
                    placaLeida.Replace(".jpg", "");
                    placaLeida.Replace("-", "");
                    //digitosPlaca = placaLeida.Length == 6 ? true : false;
                    //placaLetras.Append(placaLeida);
                    //placaLetras.Remove(3, placaLeida.Length - 3);
                    //placaLetras.Replace('0', 'O');
                    //placaLetras.Replace('1', 'I');
                    //placaLetras.Replace('8', 'B');
                    //placaLetras.Replace('6', 'G');
                    //placaNumeros.Append(placaLeida);
                    //placaNumeros.Remove(0, 3);
                    //if (digitosPlaca == true)
                    //    placaNumeros.Insert(0, '0');

                    //placaNumeros.Replace('O', '0');
                    //placaNumeros.Replace('Q', '0');
                    //placaNumeros.Replace('I', '1');
                    //placaNumeros.Replace('B', '8');
                    //placaNumeros.Replace('G', '6');
                    //placaLeida.Clear();
                    //placaLeida.Append(placaLetras);
                    //placaLeida.Append(placaNumeros);
                    placa = placaLeida.ToString();


                    if (nScaleId == 0 && (sucursalBasculaCamaras.Equals(cam1) || sucursalBasculaCamaras.Equals(cam2)))
                    {
                        if ((placa.Equals(placa_enviada)) && (Fecha_Leida.CompareTo(Fecha_restada) >= 0))
                        {
                            return true ;
                        }
                    }
                    else if (nScaleId == 1 && (sucursalBasculaCamaras.Equals(cam3) || sucursalBasculaCamaras.Equals(cam4)))
                    {
                        if ((placa.Equals(placa_enviada)) && (Fecha_Leida.CompareTo(Fecha_restada) >= 0 ))
                        {
                            return true ;
                        }
                    }
                    placaLeida.Clear();
                    placaLetras.Clear();
                    placaNumeros.Clear();
                }

            }

            return false;

        }


        #endregion
        #region Biometrico
        public string consulta_BiometricoChofer(string Cedula_Chofer)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_BDBiometrico"].Value;
            int T_Chofer = Convert.ToInt32(cfg.AppSettings.Settings["T_Chofer"].Value);
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT personnel_employee.emp_code,iclock_transaction.punch_time FROM personnel_employee INNER JOIN iclock_transaction ON personnel_employee.emp_code=iclock_transaction.emp_code WHERE personnel_employee.first_name=@conductor AND punch_time BETWEEN DATEADD(minute, -@tiempo, GETDATE() )  and getdate()", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@conductor", Cedula_Chofer));
                        command.Parameters.Add(new SqlParameter("@tiempo", T_Chofer));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }



            return consulta;
        }
        public string consulta_ExisteChofer(string Cedula_Chofer)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_BDBiometrico"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT top 1 * FROM personnel_employee where personnel_employee.first_name =@conductor", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@conductor", Cedula_Chofer));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }


            return consulta;
        }

        #endregion
        #region DataBridge
        public string consulta_TransaccionDB()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_DB"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT MAX(CONVERT(int,[TransactionNumber])) + 1 FROM  DataBridge.[Transaction]", Conn))
                    {
                        consulta = Convert.ToString(command.ExecuteScalar());
                        if (consulta.Equals(""))
                        {
                            consulta = "1";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }



            return consulta;
        }
        private void ventanaOK(string texto, String titulo)
        {
            try
            {
                IWindowManager windowManager = ServiceLocator.GetKernel().Get<IWindowManager>();
                CustomOkDialogViewModel viewModel = new CustomOkDialogViewModel(texto);
                viewModel.CustomWindowTitle = titulo;
                viewModel.OkButtonText = "OK";
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    windowManager.ShowDialog(viewModel);

                });
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Error", ex);
            }
        }
        #endregion
        #region Web Service
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
        public void InvokeServiceEntrada(Ticket ticket, string TipoPeso, string centroTransaccion, string nombreConductor, ref string mensajeAries, ref int estatusAries)
        {

            try
            {
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                string Centro = cfg.AppSettings.Settings["Centro_Distribucion"].Value;
                const string Comillas = "\"";

                if (centroTransaccion.Equals(""))
                {
                    centroTransaccion = Centro;
                }

                string XmlEnvio = "<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">" +
                                "<ControlProceso>" +
                                "<CodigoCompania>002</CodigoCompania>" +
                                "<CodigoSistema>DB</CodigoSistema>" +
                                "<CodigoServicio>ValidaPesosDB</CodigoServicio>" +
                                "<Proceso>Insertar/Validar</Proceso>" +
                                "<Resultado></Resultado>" +
                                "</ControlProceso>" +
                                "<Cabecera>" +
                                "<TicketDataBridge>" + ticket.NumeroTicket + "</TicketDataBridge>" +
                                "<FechaTicketProceso>" + ticket.FechaIngreso.ToString("dd/MM/yyyy") + "</FechaTicketProceso>" +
                                "<HoraTicketProceso>" + ticket.FechaIngreso.ToString("HH:MM") + "</HoraTicketProceso>" +
                                "<UsuarioDataBridge>" + ticket.OperadorIngreso + "</UsuarioDataBridge>" +
                                "<NumeroBascula>" + ticket.Bascula + "</NumeroBascula>" +
                                "<TipoPeso>" + TipoPeso + "</TipoPeso>" +
                                "<PesoTicketDataBridge>" + ticket.PesoIngreso + "</PesoTicketDataBridge>" +
                                "<PlacaVehiculo>" + ticket.Placa + "</PlacaVehiculo>" +
                                "<CedulaTransportista>" + ticket.Chofer + "</CedulaTransportista>" +
                                "<NombreTransportista>" + nombreConductor + "</NombreTransportista>" +
                                "<CodCentroAries>" + centroTransaccion + "</CodCentroAries>" +
                                "<TicketAries> </TicketAries>" +
                                "<CedUsuarioAries> </CedUsuarioAries>" +
                                "<NomUsuarioAries> </NomUsuarioAries>" +
                                "<EstatusAries>1</EstatusAries>" +
                                "<MensajeAries>Enviado</MensajeAries>" +
                                "</Cabecera>" +
                                "</ns1:GesImpPesAr>";

                ///*****************************************************esto no va************************************
                string codificiacionMsj = EncodeStrToBase64(XmlEnvio);
                string res = G_Msg(codificiacionMsj, "A", ticket.NumeroTicket);
                //string ejecutable = @"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe " + N_Transaccion;
                //Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe "+N_Transaccion);
                //mensaje=ejecutable;
                //Process.Start(ejecutable);
                // Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\PruebasComunicacion.exe");
                Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Comunicación Aries\ComunicacionAries.exe ", ticket.NumeroTicket);
                System.Threading.Thread.Sleep(3000);
                // G_Msg2();
                //CONSULTA DE DATOS
                int Codigo = 0;
                int Transaccion;
                string mensaje_R = "";
                string Estado = "";

                using (SqlConnection connection = new SqlConnection(Conexion_Bd))
                {
                    String sql = "SELECT top 1* FROM ComunicacionAries where ComAries_Estado = 'Procesado' AND ComAries_Transaccion='" + ticket.NumeroTicket + "' ORDER BY ComAries_Codigo DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Codigo = Convert.ToInt32(reader.GetInt32(0));
                                mensaje_R = reader.GetString(3);
                                Estado = reader.GetString(6);
                            }
                        }
                        connection.Close();
                    }
                }

                //if (Estado != "")
                if (mensaje_R != "")
                {

                    string consulta = "UPDATE ComunicacionAries SET  [ComAries_Estado] ='Fin' where ComAries_Codigo='" + Codigo + "' ";

                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    ConexionSql.Close();
                    mensajeAries = leerMensaje_Xml(DecodeBase64ToString(mensaje_R));
                    estatusAries = leerEstatus_Xml(DecodeBase64ToString(mensaje_R));
                    actualizarEstatusMensajeAries(DecodeBase64ToString(mensaje_R), Codigo, mensajeAries, estatusAries);

                }
                else
                {
                    estatusAries = 0;
                    mensajeAries = "";
                    //string consulta = "DELETE FROM ComunicacionAries  where ComAries_Codigo='" + Codigo + "'  AND [ComAries_Estado] ='A'";

                    //SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    //ConexionSql.Open();
                    //SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    //consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    //ConexionSql.Close();
                }

            }
            catch (Exception e)
            {
                estatusAries = 1;
                mensajeAries = e.Message;
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

        }
        public void InvokeServiceSalida(Ticket ticket, string TipoPeso, string centroTransaccion, string nombreConductor, ref string mensajeAries, ref int estatusAries)
        {

            try
            {
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                string Centro = cfg.AppSettings.Settings["Centro_Distribucion"].Value;
                const string Comillas = "\"";

                if (centroTransaccion.Equals(""))
                {
                    centroTransaccion = Centro;
                }

                string XmlEnvio = "<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">" +
                                "<ControlProceso>" +
                                "<CodigoCompania>002</CodigoCompania>" +
                                "<CodigoSistema>DB</CodigoSistema>" +
                                "<CodigoServicio>ValidaPesosDB</CodigoServicio>" +
                                "<Proceso>Insertar/Validar</Proceso>" +
                                "<Resultado></Resultado>" +
                                "</ControlProceso>" +
                                "<Cabecera>" +
                                "<TicketDataBridge>" + ticket.NumeroTicket + "</TicketDataBridge>" +
                                "<FechaTicketProceso>" + ticket.FechaSalida.ToString("dd/MM/yyyy") + "</FechaTicketProceso>" +
                                "<HoraTicketProceso>" + ticket.FechaSalida.ToString("HH:MM") + "</HoraTicketProceso>" +
                                "<UsuarioDataBridge>" + ticket.OperadorSalida + "</UsuarioDataBridge>" +
                                "<NumeroBascula>" + ticket.BasculaSalida + "</NumeroBascula>" +
                                "<TipoPeso>" + TipoPeso + "</TipoPeso>" +
                                "<PesoTicketDataBridge>" + ticket.PesoSalida + "</PesoTicketDataBridge>" +
                                "<PlacaVehiculo>" + ticket.Placa + "</PlacaVehiculo>" +
                                "<CedulaTransportista>" + ticket.Chofer + "</CedulaTransportista>" +
                                "<NombreTransportista>" + nombreConductor + "</NombreTransportista>" +
                                "<CodCentroAries>" + centroTransaccion + "</CodCentroAries>" +
                                "<TicketAries> </TicketAries>" +
                                "<CedUsuarioAries> </CedUsuarioAries>" +
                                "<NomUsuarioAries> </NomUsuarioAries>" +
                                "<EstatusAries>1</EstatusAries>" +
                                "<MensajeAries>Enviado</MensajeAries>" +
                                "</Cabecera>" +
                                "</ns1:GesImpPesAr>";

                ///*****************************************************esto no va************************************
                string codificiacionMsj = EncodeStrToBase64(XmlEnvio);
                string res = G_Msg(codificiacionMsj, "A", ticket.NumeroTicket);
                //string ejecutable = @"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe " + N_Transaccion;
                //Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe "+N_Transaccion);
                //mensaje=ejecutable;
                //Process.Start(ejecutable);
                // Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\PruebasComunicacion.exe");
                Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Comunicación Aries\ComunicacionAries.exe ", ticket.NumeroTicket);
                System.Threading.Thread.Sleep(3000);
                // G_Msg2();
                //CONSULTA DE DATOS
                int Codigo = 0;
                int Transaccion;
                string mensaje_R = "";
                string Estado = "";

                using (SqlConnection connection = new SqlConnection(Conexion_Bd))
                {
                    String sql = "SELECT top 1* FROM ComunicacionAries where ComAries_Estado = 'Procesado' AND ComAries_Transaccion='" + ticket.NumeroTicket + "' ORDER BY ComAries_Codigo DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Codigo = Convert.ToInt32(reader.GetInt32(0));
                                mensaje_R = reader.GetString(3);
                                Estado = reader.GetString(6);
                            }
                        }
                        connection.Close();
                    }
                }

                //if (Estado != "")
                if (mensaje_R != "")
                {

                    string consulta = "UPDATE ComunicacionAries SET  [ComAries_Estado] ='Fin' where ComAries_Codigo='" + Codigo + "' ";

                    SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    ConexionSql.Open();
                    SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    ConexionSql.Close();
                    mensajeAries = leerMensaje_Xml(DecodeBase64ToString(mensaje_R));
                    estatusAries = leerEstatus_Xml(DecodeBase64ToString(mensaje_R));
                    actualizarEstatusMensajeAries(DecodeBase64ToString(mensaje_R), Codigo, mensajeAries, estatusAries);

                }
                else
                {
                    estatusAries = 0;
                    mensajeAries = "";
                    //string consulta = "DELETE FROM ComunicacionAries  where ComAries_Codigo='" + Codigo + "'  AND [ComAries_Estado] ='A'";

                    //SqlConnection ConexionSql = new SqlConnection(Conexion_Bd);
                    //ConexionSql.Open();
                    //SqlCommand Comando_Sql = new SqlCommand(consulta, ConexionSql);
                    //consulta = Convert.ToString(Comando_Sql.ExecuteNonQuery());
                    //ConexionSql.Close();
                }

            }
            catch (Exception e)
            {
                estatusAries = 1;
                mensajeAries = e.Message;
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

        }
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
                    envioRes = "2/" + " " + Mensaje;
                    break;
                case 3:
                    // EXITO - (termina el proceso)
                    envioRes = "3/ ¡Transacción enviada exitosamente a ARIES!";
                    break;
                case 4:
                    // EXITO SIN TURNO(vehiculo sin carga se envia un pin no se envia al aries salida no se envia)
                    envioRes = "4/" + " " + Mensaje;
                    break;

                case 5:
                    // Error del factor de conversion(aborta el pesaje)
                    envioRes = "5/" + " " + Mensaje;
                    break;

                default:

                    break;

            }


            return envioRes;
        }
        public int leerEstatus_Xml(string Xml)
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
        public string leerMensaje_Xml(string Xml)
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
        public void actualizarEstatusMensajeAries(string Xml, int Codigo, string mensajeAries, int estatusAries)
        {
            const string Comillas = "\"";
            string res_xml = Xml.Replace("<ns1:GesImpPesAr xmlns:ns1=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
            res_xml = res_xml.Replace("ns1:GesImpPesAr", "ns1");

            XmlDocument xmltest = new XmlDocument();
            xmltest.LoadXml(res_xml);
            XmlNodeList nodes = xmltest.SelectNodes("//ns1/Cabecera");
            int Estatus = 0;
            string Mensaje = "";
            string TipoPeso = "F";
            foreach (XmlNode node in nodes)
            {
                Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                Mensaje = node["MensajeAries"].InnerText;
                TipoPeso = node["TipoPeso"].InnerText;
            }
            if (!Mensaje.Equals(""))
            {
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE ComunicacionAries SET ComAries_EstatusRecibido=@estatus, ComAries_MensajeRecibido=@mensaje,ComAries_TipoPeso=@tipoPeso WHERE ComAries_Codigo=@codigo", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@estatus", estatusAries));
                        command.Parameters.Add(new SqlParameter("@mensaje", mensajeAries));
                        command.Parameters.Add(new SqlParameter("@codigo", Codigo));
                        command.Parameters.Add(new SqlParameter("@tipoPeso", TipoPeso));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                res_xml = Xml.Replace("<GesImpPesAr xmlns=" + Comillas + "http://ln.gesalm.integracion.pronaca.com.ec" + Comillas + ">", "<ns1>");
                res_xml = res_xml.Replace("GesImpPesAr", "ns1");
                xmltest = new XmlDocument();
                xmltest.LoadXml(res_xml);
                nodes = xmltest.SelectNodes("//ns1/Cabecera");
                Estatus = 0;
                Mensaje = "";
                TipoPeso = "F";
                foreach (XmlNode node in nodes)
                {
                    Estatus = Convert.ToInt32(node["EstatusAries"].InnerText);
                    Mensaje = node["MensajeAries"].InnerText;
                    TipoPeso = node["TipoPeso"].InnerText;
                }
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE ComunicacionAries SET ComAries_EstatusRecibido=@estatus, ComAries_MensajeRecibido=@mensaje,ComAries_TipoPeso=@tipoPeso WHERE ComAries_Codigo=@codigo", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@estatus", estatusAries));
                        command.Parameters.Add(new SqlParameter("@mensaje", mensajeAries));
                        command.Parameters.Add(new SqlParameter("@codigo", Codigo));
                        command.Parameters.Add(new SqlParameter("@tipoPeso", TipoPeso));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }


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
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("INSERT INTO [dbo].[ComunicacionAries] ([ComAries_XMLEnviado],[ComAries_Estado],ComAries_Transaccion)VALUES(@mensaje,'A',@estado)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@mensaje", Mensaje));
                        command.Parameters.Add(new SqlParameter("@estado", Tem_Transaccion));
                        consulta = Convert.ToString(command.ExecuteNonQuery());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Error en la conexión con la base de datos");
            }


            return consulta;
        }

        #endregion
        #region Transacción
        public void Gestion_Pesaje(Ticket ticket)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("P_TBVehiculos", Conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Veh_Bascula", ticket.Bascula);
                        command.Parameters.AddWithValue("@Veh_BasculaSalida", ticket.BasculaSalida);
                        command.Parameters.AddWithValue("@Veh_Placa", ticket.Placa);
                        command.Parameters.AddWithValue("@Veh_Chofer", ticket.Chofer);
                        command.Parameters.AddWithValue("@Veh_Peso_Ingreso", ticket.PesoIngreso);
                        command.Parameters.AddWithValue("@Veh_Peso_Salida", ticket.PesoSalida);
                        command.Parameters.AddWithValue("@Veh_Ticket", ticket.NumeroTicket);
                        command.Parameters.AddWithValue("@Veh_PinEntrada", ticket.PinEntrada);
                        command.Parameters.AddWithValue("@Veh_PinSalida", ticket.PinSalida);
                        command.Parameters.AddWithValue("@Veh_OperadorEntrada", ticket.OperadorIngreso);
                        command.Parameters.AddWithValue("@Veh_OperadorSalida", ticket.OperadorSalida);
                        command.Parameters.AddWithValue("@Veh_PesosObtenidos", ticket.PesosObtenidos);
                        command.Parameters.AddWithValue("@Veh_Estado", ticket.Estado);
                        int dato = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

        }
        public string consulta_PinSalida(string N_Transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT Veh_PinSalida FROM tb_vehiculos WHERE veh_ticket=@transaccion AND  Veh_Estado='SP' ", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", N_Transaccion));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }


            return consulta;
        }
        public string Peso_Salida(string Veh_BasculaSalida, string Veh_Peso_Salida, string Veh_Ticket, string Veh_PinSalida, string Veh_RutaImg)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos set Veh_BasculaSalida=@basculaSalida,Veh_Peso_Salida=@pesoSalida,Veh_Fecha_Salida=GETDATE(),Veh_PinSalida=@pinSalida,Veh_RutaImg=@rutaSalida WHERE  Veh_Ticket=@transaccion", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@basculaSalida", Veh_BasculaSalida));
                        command.Parameters.Add(new SqlParameter("@pesoSalida", Veh_Peso_Salida));
                        command.Parameters.Add(new SqlParameter("@pinSalida", Veh_PinSalida));
                        command.Parameters.Add(new SqlParameter("@rutaSalida", Veh_RutaImg));
                        command.Parameters.Add(new SqlParameter("@transaccion", Veh_Ticket));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }

            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }


            return consulta;

        }
        public string consulta_PlacaIngreso(string Placa_Seleccionada)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT Veh_PinEntrada from tb_vehiculos WHERE veh_placa=@placa AND veh_estado ='IP'", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@placa", Placa_Seleccionada));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

            return consulta;
        }
        public string consulta_TipoIngreso(string ntransaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT Veh_Val2 from Tb_Vehiculos where Veh_Ticket =@transaccion", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", ntransaccion));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }


            return consulta;
        }
        public void InsertarPesosObtenidos(string pesosObtenidos, string transaccion, int bascula)
        {
            try
            {
                string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos set Veh_PesosObtenidos = Veh_PesosObtenidos + @peso where  Veh_Ticket=@transaccion", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@peso", pesosObtenidos));
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Error en la conexión con la base de datos");
            }



        }
        public int anularTransacción(string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int consulta = 0;
            string pesoSalida;
            //Cambio de estado a TA(Transaccion anulada)


            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Estado='TA' Where Veh_Codigo=(SELECT TOP(1) Veh_Codigo FROM Tb_Vehiculos WHERE  Veh_Ticket=@transaccion ORDER BY Veh_Codigo DESC)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        consulta = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Error en la conexión con la base de datos");
            }

            //consulto si tiene peso de salida
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("SELECT veh_peso_salida FROM Tb_Vehiculos where  Veh_Ticket=@transaccion AND Veh_Estado='TA'", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                    pesoSalida = Convert.ToString(command.ExecuteScalar());
                }
            }

            //si solo fue transaccion de entrada, actualizo a peso de salida 0
            if (pesoSalida.Equals(""))
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Peso_Salida='0' WHERE Veh_Ticket=@transaccion AND Veh_Estado='TA'", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }

            return consulta;
        }
        public void eliminarTransaccionPendiente(int bascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;

            //***********************************************************FIN DEL APP CONFIG
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("DELETE FROM [dbo].[Tb_Vehiculos] WHERE Veh_Estado='IP' AND Veh_BASCULA=@bascula;", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@bascula", bascula));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }
        public string obtenerOperador()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consultaOperador = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 operador FROM login order by Fecha desc", Conn))
                    {
                        consultaOperador = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Error en la conexión con la base de datos");
            }

            return consultaOperador;
        }
        public void detenerSecuencia(string operador, string razon, int bascula, string pesoObtenido, string pesoBascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
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
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Error en la conexión con la base de datos");
            }

        }
        public void actualizarEstadoSalida(string transaccion, string mensaje_recibido, string numeral_recibido, int bascula, string pesoSalida)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos set Veh_BasculaSalida=@bascula,Veh_Estado='SC',Veh_Val1=@msj_recibido,Veh_Val3=@numeral_recibido,Veh_Peso_Salida=@pesoSalida WHERE Veh_Ticket=@transaccion AND Veh_Estado='SP' AND Veh_Val2='3'", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@bascula", bascula));
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        command.Parameters.Add(new SqlParameter("@msj_recibido", mensaje_recibido));
                        command.Parameters.Add(new SqlParameter("@numeral_recibido", numeral_recibido));
                        command.Parameters.Add(new SqlParameter("@pesoSalida", pesoSalida));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

        }
        public void actualizarImagenesPINEntrada(string imgEntrada, string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_RutaImgIng=@imgEntrada WHERE Veh_Ticket=@transaccion", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                    command.Parameters.Add(new SqlParameter("@imgEntrada", imgEntrada));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }
        public void actualizarImagenesPINSalida(string imgSalida, string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_RutaImgSal=@imgSalida WHERE Veh_Ticket=@transaccion", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                    command.Parameters.Add(new SqlParameter("@imgSalida", imgSalida));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }
        public string estatusRecibidoAries(string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string estado = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 ComAries_EstatusRecibido FROM ComunicacionAries WHERE ComAries_Transaccion=@transaccion AND ComAries_TipoPeso='S' AND ComAries_EstatusRecibido='3' ORDER BY ComAries_Codigo DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        estado = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

            return estado;
        }
        public void actualizarEstadoPendienteEntrada(int bascula, Ticket ticket, string PesoBascula, string evento)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int rowsAdded = 0;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Estado='TI' WHERE Veh_Codigo=(SELECT TOP(1) Veh_Codigo FROM Tb_Vehiculos WHERE Veh_Estado='IP' AND Veh_Placa=@vehiculo AND Veh_Bascula=@bascula ORDER BY Veh_Codigo DESC)", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@vehiculo", ticket.Placa));
                    command.Parameters.Add(new SqlParameter("@bascula", bascula));
                    rowsAdded = command.ExecuteNonQuery();
                }
            }
            if(rowsAdded!=0)
                Log.transacción(ticket, PesoBascula, evento, bascula);
        }
        public void actualizarFechaEntrada(int bascula, string transaccion, DateTime fechaIngreso)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Fecha_Ingreso=@fechaIngreso WHERE Veh_Ticket=@transaccion AND Veh_Bascula=@bascula", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@bascula", bascula));
                    command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                    command.Parameters.Add(new SqlParameter("@fechaIngreso", fechaIngreso));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }
        public string estatusRecibidoAriesEntrada(string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string estado = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 ComAries_EstatusRecibido FROM ComunicacionAries WHERE ComAries_Transaccion=@transaccion AND ComAries_Estado='Fin' AND ComAries_TipoPeso='E' AND ComAries_EstatusRecibido='3' ORDER BY ComAries_Codigo DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        estado = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

            return estado;
        }
        public int actualizarEstadoRecibidoEntrada(int bascula, string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int rowsAdded = 0;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Val1='3', Veh_Val2='Entrada Exitosa' WHERE Veh_Codigo=(SELECT TOP(1) Veh_Codigo FROM  Tb_Vehiculos WHERE Veh_Ticket=@transaccion AND Veh_Bascula=@bascula AND Veh_Estado='IC' ORDER BY Veh_Codigo DESC)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@bascula", bascula));
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        rowsAdded = command.ExecuteNonQuery();
                    }
                }
                return rowsAdded;
            }catch(Exception ex)
            {
                return rowsAdded;
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }
            
        }
        public string estatusRecibidoAriesSalida(string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string estado = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 ComAries_EstatusRecibido FROM ComunicacionAries WHERE ComAries_Transaccion=@transaccion AND ComAries_Estado='Fin' AND ComAries_TipoPeso='S' AND ComAries_EstatusRecibido='3' ORDER BY ComAries_Codigo DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        estado = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

            return estado;
        }
        public int actualizarEstadoRecibidoSalida(int bascula, string transaccion)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int rowsAdded = 0;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE Tb_Vehiculos SET Veh_Val3='3', Veh_Val2='Salida Exitosa' WHERE Veh_Codigo=(SELECT TOP(1) Veh_Codigo FROM  Tb_Vehiculos WHERE Veh_Ticket=@transaccion AND Veh_BasculaSalida=@bascula AND Veh_Estado='SC' ORDER BY Veh_Codigo DESC)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@bascula", bascula));
                        command.Parameters.Add(new SqlParameter("@transaccion", transaccion));
                        rowsAdded = command.ExecuteNonQuery();
                    }
                }
                return rowsAdded;
            }catch(Exception ex)
            {
                return rowsAdded;
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }
        }
        public int revisarTransaccionEntradaTerminada()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int rowAdded=0;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("RevisarTransaccionEntradaTerminada", Conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        rowAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("Se perdió la conexión con la base de datos");
            }

            return rowAdded;
        }

        #endregion







    }


}

