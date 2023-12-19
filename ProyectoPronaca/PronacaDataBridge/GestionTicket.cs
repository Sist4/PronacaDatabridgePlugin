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
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Drawing;
using Caliburn.Micro;
using DataBridge.Attended.ViewModel;
using DataBridge.Core.Services;
using Ninject;
using Application = System.Windows.Application;
using Ninject.Planning;
using System.Net.Sockets;
using System.Windows;
using System.CodeDom;
using DataBridge.Core.Types;
using DataBridge.Core.Entities;
using Attachment = System.Net.Mail.Attachment;
using DataBridge.Core.Business;

namespace PronacaPlugin
{
    public class GestionTicket
    {
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        string cam11;
        string cam12;
        string cam13;
        string cam14;
        string cam21;
        string cam22;
        string cam23;
        string cam24;
        public string mensaje { get; set; }
        string[] Correo_Destino;
        public GestionTicket()
        {
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            cam11 = cfg.AppSettings.Settings["Nom_Camara11"].Value.Substring(0, 8);
            cam12 = cfg.AppSettings.Settings["Nom_Camara12"].Value.Substring(0, 8);
            cam13 = cfg.AppSettings.Settings["Nom_Camara13"].Value.Substring(0, 8);
            cam14 = cfg.AppSettings.Settings["Nom_Camara14"].Value.Substring(0, 8);
            cam21 = cfg.AppSettings.Settings["Nom_Camara21"].Value.Substring(0, 8);
            cam22 = cfg.AppSettings.Settings["Nom_Camara22"].Value.Substring(0, 8);
            cam23 = cfg.AppSettings.Settings["Nom_Camara23"].Value.Substring(0, 8);
            cam24 = cfg.AppSettings.Settings["Nom_Camara24"].Value.Substring(0, 8);
        }
        #region Correo
        /// <summary>
        /// Se envía un correo por dos razones:
        /// 1. Las cámaras no detectaron la placa, por lo tanto no se pudo validar que la placa seleccionada por DataBridge es la placa del camión
        /// en la báscula, se envía un correo con la información de fecha y hora, N# de transacción, placa seleccionada, 
        /// PIN(Código numérico randómico de 4 digitos) e imagenes capturadas en ese momento del vehículo por delante y detrás.
        /// 2. Se presionó el botón verde para detener la secuencia, se levantan las barreras y se permite entrar o salir algún vehículo sin ser
        /// registrado, se envía un correo con la información de fecha y hora, razón de la detención, operador, peso obtenido, peso en báscula e imagenes
        /// capturadas en ese momento del vehículo por delante y por detrás.
        /// Se cargan los correos de destino, correo de envío, contraseña, host, puerto, ssl del archivo de configuración, 
        /// se añaden ls imagenes como archivo adjunto si esque las hay y se envía el email a los correos de destino.
        /// </summary>
        /// <param name="correo">Objeto donde se enceuntran los datos del correo que se va a enviar</param>
        /// <param name="nombreImagen1">ruta de la imagen  capturada por la cámara de entrada que se va a adjuntar en el correo</param>
        /// <param name="nombreImagen2">ruta de la imagen  capturada por la cámara de salida que se va a adjuntar en el correo</param>
        /// <param name="objeto"> Objeto Ticket si esque el correo es envio de PIN, o Secuencia si es por detención de secuencia</param>
        /// <returns></returns>
        /// <exception cref="ExcepcionNegocio"></exception>
        public bool EnviarCorreo(Correo correo, string nombreImagen1, string nombreImagen2,Object objeto)
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
                    correo.Destinatarios += Correo_Destino[i] + ";";
                }
                //copia oculta

                //for (int i = numero_Correos + 1; i <= 10; i++)
                //{
                //    mail.Bcc.Add(Correo_Destino[i]);
                //}
                
                mail.Subject = "DataBridge - Sistema de Pesaje - ";
                //mail.Body = "<h1>Notificacion</h1></br><p>La transaccion nº:" + N_Transaccion + " con placa seleccionada del operador: " + placa_seleccionada + "   no cumple con las condiciones para seguir el proceso de Pesaje.</p></br> <p>Si desea seguir con la transaccion digite el siguiente PIN:" + codigo_transaccion + "   </p>";
                // mail.Body = "<h1>Notificación</h1><p>Las cámaras no identificaron la placa seleccionada, para proseguir con la transacción digite el PIN en el sistema de pesaje DataBridge.</p><table><tr><td>Fecha y hora:</td><td>" + DateTime.Now.ToString() + "</td></tr><tr><td>N# Transacción:</td><td>" + N_Transaccion + "</td></tr><tr><td>Placa seleccionada:</td><td>" + placa_seleccionada + "</td></tr><tr><td>PIN:</td><td>" + codigo_transaccion + "</td></tr><tr></table>";
                if (correo.Tipo.Equals("PIN"))
                {
                    Ticket ticket = (Ticket)objeto;
                    mail.Body = "<h1>Notificación</h1><p>" + correo.Asunto + "</p><table><tr><td>Fecha y hora:</td><td>" + DateTime.Now.ToString() + "</td></tr><tr><td>N# Transacción:</td><td>" + ticket.Numero + "</td></tr><tr><td>Placa seleccionada:</td><td>" + ticket.PlacaVehiculo + "</td></tr><tr><td>PIN:</td><td>" + correo.Pin + "</td></tr><tr></table>";
                }
                    
                if (correo.Tipo.Equals("SECUENCIA"))
                {
                    Secuencia secuencia = (Secuencia)objeto;
                    mail.Body = "<h1>Notificación</h1><p>" + correo.Asunto + "</p><table><tr><td>Fecha y hora:</td><td>" + DateTime.Now.ToString() + "</td></tr><tr><td>Razón:</td><td>" + secuencia.Razon + "</td></tr><tr><td>Operador:</td><td>" + secuencia.Operador + "</td></tr><tr><td>Peso obtenido:</td><td>" + secuencia.PesoObtenido + "</td></tr><tr><td>Peso en báscula:</td><td>" + secuencia.PesoBascula + "</td></tr></table>";
                }
                   

                correo.Remitente = Correo_Envio;
                //mail.Body = correo.Asunto;
                mail.IsBodyHtml = true;
                if (!nombreImagen1.Equals(string.Empty))
                {
                    mail.Attachments.Add(new Attachment(directorio + nombreImagen1 + ".jpg"));
                    correo.RutaImagen1 = directorio + nombreImagen1 + ".jpg";
                }
                if (!nombreImagen2.Equals(string.Empty))
                {
                    mail.Attachments.Add(new Attachment(directorio + nombreImagen2 + ".jpg"));
                    correo.RutaImagen2 = directorio + nombreImagen2 + ".jpg";
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
                        Log.Mensajes(ex.Message, "Envío Correo");
                        MensajesBD(ex.Message, "Envío Correo");
                        throw new ExcepcionNegocio("No se pudo enviar el correo, Porfavor revisar que se encuentre en red y que el servidor de correos está habilitado");  
                    }

                }
                //// fin del proyecto
                return envio;

            }
        }
        /// <summary>
        /// Se escribe en la imagen capturada por las cámaras  que se enviará por correo el peso actual en báscula en color rojo
        /// </summary>
        /// <param name="ruta">ruta de la imagen capturada por la cámara</param>
        /// <param name="pesoBascula">peso actual que se registra en el terminal de peso</param>
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

        /// <summary>
        /// Se guardan las imagenes que se enviarán como un arreglos de bytes y se insertan en la base de datos
        /// en el campo id  se inserta un autonomérico con identificador CORR_, en el campo tipo  se inserta si es un correo por PIN o por Secuencia, 
        /// en el campo PIN se inserta el codigo randomico numérico si esque lo hubo, se insertan los campos asunto, remitente, destinatarios
        /// y las imagenes si esque las hubo
        /// </summary>
        /// <param name="correo">Objeto que contiene toda los datos del correo</param>
        public void GuardarCorreo(Correo correo)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            byte[] imageData1 = null;
            byte[] imageData2 = null;
            bool banderaCam1 = false;
            bool banderaCam2 = false;
            if (!correo.RutaImagen1.Equals(string.Empty))
            {
                imageData1 = ReadFile(correo.RutaImagen1);
                banderaCam1 = true;
            }   
            if(!correo.RutaImagen2.Equals(string.Empty))
            {
                imageData2 = ReadFile(correo.RutaImagen2);
                banderaCam2 = true;
            }
                
            if(banderaCam1 && banderaCam2)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("INSERT INTO CORREO(CORR_ID,CORR_FECHA,CORR_TIPO,CORR_PIN,CORR_ASUNTO,CORR_REMITENTE,CORR_DESTINATARIOS,CORR_IMAGENCAMARADELANTERA,CORR_IMAGENCAMARATRASERA) VALUES(@id,GETDATE(),@tipo,@pin,@asunto,@remitente,@destinatarios,@camara1,@camara2)", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@id", correo.Id));
                            command.Parameters.Add(new SqlParameter("@tipo", correo.Tipo));
                            command.Parameters.Add(new SqlParameter("@pin", correo.Pin));
                            command.Parameters.Add(new SqlParameter("@asunto", correo.Asunto));
                            command.Parameters.Add(new SqlParameter("@remitente", correo.Remitente));
                            command.Parameters.Add(new SqlParameter("@destinatarios", correo.Destinatarios));
                            command.Parameters.Add(new SqlParameter("@camara1", imageData1));
                            command.Parameters.Add(new SqlParameter("@camara2", imageData2));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            if(banderaCam1 && banderaCam2==false)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("INSERT INTO CORREO(CORR_ID,CORR_FECHA,CORR_TIPO,CORR_PIN,CORR_ASUNTO,CORR_REMITENTE,CORR_DESTINATARIOS,CORR_IMAGENCAMARADELANTERA) VALUES(@id,GETDATE(),@tipo,@pin,@asunto,@remitente,@destinatarios,@camara1)", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@id", correo.Id));
                            command.Parameters.Add(new SqlParameter("@tipo", correo.Tipo));
                            command.Parameters.Add(new SqlParameter("@pin", correo.Pin));
                            command.Parameters.Add(new SqlParameter("@asunto", correo.Asunto));
                            command.Parameters.Add(new SqlParameter("@remitente", correo.Remitente));
                            command.Parameters.Add(new SqlParameter("@destinatarios", correo.Destinatarios));
                            command.Parameters.Add(new SqlParameter("@camara1", imageData1));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            if(banderaCam1==false && banderaCam2)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("INSERT INTO CORREO(CORR_ID,CORR_FECHA,CORR_TIPO,CORR_PIN,CORR_ASUNTO,CORR_REMITENTE,CORR_DESTINATARIOS,CORR_IMAGENCAMARATRASERA) VALUES(@id,GETDATE(),@tipo,@pin,@asunto,@remitente,@destinatarios,@camara2)", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@id", correo.Id));
                            command.Parameters.Add(new SqlParameter("@tipo", correo.Tipo));
                            command.Parameters.Add(new SqlParameter("@pin", correo.Pin));
                            command.Parameters.Add(new SqlParameter("@asunto", correo.Asunto));
                            command.Parameters.Add(new SqlParameter("@remitente", correo.Remitente));
                            command.Parameters.Add(new SqlParameter("@destinatarios", correo.Destinatarios));
                            command.Parameters.Add(new SqlParameter("@camara2", imageData2));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            if(banderaCam1==false && banderaCam2==false)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("INSERT INTO CORREO(CORR_ID,CORR_FECHA,CORR_TIPO,CORR_PIN,CORR_ASUNTO,CORR_REMITENTE,CORR_DESTINATARIOS) VALUES(@id,GETDATE(),@tipo,@pin,@asunto,@remitente,@destinatarios)", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@id", correo.Id));
                            command.Parameters.Add(new SqlParameter("@tipo", correo.Tipo));
                            command.Parameters.Add(new SqlParameter("@pin", correo.Pin));
                            command.Parameters.Add(new SqlParameter("@asunto", correo.Asunto));
                            command.Parameters.Add(new SqlParameter("@remitente", correo.Remitente));
                            command.Parameters.Add(new SqlParameter("@destinatarios", correo.Destinatarios));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Mensajes(ex.Message, "GuardarCorreo");
                    MensajesBD(ex.Message, "GuardarCorreo");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            
          

        }
        byte[] ReadFile(string sPath)
        {
            //Initialize byte array with a null value initially.
            byte[] data = null;

            //Use FileInfo object to get file size.
            FileInfo fInfo = new FileInfo(sPath);
            long numBytes = fInfo.Length;

            //Open FileStream to read file
            FileStream fStream = new FileStream(sPath, FileMode.Open, FileAccess.Read);

            //Use BinaryReader to read file stream into byte array.
            BinaryReader br = new BinaryReader(fStream);

            //When you use BinaryReader, you need to supply number of bytes to read from file.
            //In this case we want to read entire file. So supplying total number of bytes.
            data = br.ReadBytes((int)numBytes);
            return data;
        }

        #endregion
        #region Camara_FTP

        #endregion
        #region Biometrico

        /// <summary>
        /// Obtengo el último registro del conductor por fecha más reciente, que se encuentre en estado P (Pendiente) y en el dispositivo
        /// biométrico que tenga en el archivo de configuración estado true.
        /// </summary>
        /// <param name="biometricoEntrada">nombre deldispositivo  biometrico de la entrada</param>
        /// <param name="biometricoSalida">nombre del dispositivo biométrico de la salida</param>
        /// <param name="entrada">estado del dispositivo biométrico de entrada</param>
        /// <param name="salida">estado del dispositivo biométrico de salida</param>
        /// <returns>Objeto Conductor que contiene toda la información del conductor</returns>
        
        public Conductor ObtenerUltimoConductorPendiente(string biometricoEntrada,string biometricoSalida,bool entrada,bool salida)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            Conductor conductor = new Conductor();
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1* FROM CONDUCTOR WHERE COND_ESTADO='P' AND (COND_DISPOSITIVO=@entrada OR COND_DISPOSITIVO=@salida) ORDER BY COND_FECHA DESC", Conn))
                    {
                       if(entrada==true && salida==false)
                        {
                            command.Parameters.Add(new SqlParameter("@entrada", biometricoEntrada));
                            command.Parameters.Add(new SqlParameter("@salida", ""));
                        }
                        if(entrada== false && salida==true)
                        {
                            command.Parameters.Add(new SqlParameter("@entrada", ""));
                            command.Parameters.Add(new SqlParameter("@salida", biometricoSalida));
                        }
                        if(entrada == true && salida==true)
                        {
                            command.Parameters.Add(new SqlParameter("@entrada", biometricoEntrada));
                            command.Parameters.Add(new SqlParameter("@salida", biometricoSalida));
                        }
                        if(entrada==false && salida==false)
                        {
                            command.Parameters.Add(new SqlParameter("@entrada", ""));
                            command.Parameters.Add(new SqlParameter("@salida", ""));
                        }
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                conductor.Id = reader.GetInt32(0);
                                conductor.Fecha = reader.GetDateTime(1);
                                conductor.Dispositivo = reader.GetString(2);
                                conductor.Planta = reader.GetString(3);
                                conductor.Cedula = reader.GetString(4);
                                conductor.Estado = reader.GetString(4);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Mensajes(ex.Message, "ObtenerUltimoConductorPendiente");
                MensajesBD(ex.Message, "ObtenerUltimoConductorPendiente");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

            return conductor;
        }

        /// <summary>
        /// Comparo si el ultimo conductor obtenido es el mismo que el conductor seleccionado en el software DataBridge
        /// comparanando las cédulas.
        /// </summary>
        /// <param name="cedulaSeleccionada">Cédula del conductor seleccionada en el software DataBridge</param>
        /// <returns>Si las cédulas son las mismas se devuelve true, caso contrario false</returns>
        public bool ExisteTimbrado(string cedulaSeleccionada)
        {
            bool estadoBiometricoEntrada = Convert.ToBoolean(cfg.AppSettings.Settings["Biometrico_Entrada"].Value);
            bool estadoBiometricoSalida = Convert.ToBoolean(cfg.AppSettings.Settings["Biometrico_Salida"].Value);
            string nombreBiometricoEntrada = cfg.AppSettings.Settings["Nom_Biometrico_Entrada"].Value;
            string nombreBiometricoSalida = cfg.AppSettings.Settings["Nom_Biometrico_Salida"].Value;
            bool bandera = false;
            Conductor conductor = ObtenerUltimoConductorPendiente(nombreBiometricoEntrada,nombreBiometricoSalida, estadoBiometricoEntrada,estadoBiometricoSalida);
            if (cedulaSeleccionada.Equals(conductor.Cedula))
            {
                bandera = true;
            }
            else
            {
                bandera = false;
            }
            return bandera;
        }

        /// <summary>
        /// Busco la cédula seleccionada en la base de datos del dispositivo biométrico
        /// </summary>
        /// <param name="cedulaSeleccionada">Cédula del conductor seleccionada en el software DataBridge</param>
        /// <returns> si se encuentra la cédula se devuelve true, caso contrario false</returns>
        public bool ExisteConductor(string cedulaSeleccionada)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_BDBiometrico"].Value;
            string consulta = "";
            bool bandera = false;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 * FROM personnel_employee where personnel_employee.first_name =@conductor", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@conductor", cedulaSeleccionada));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }

                if (consulta.Equals(string.Empty))
                    bandera = false;
                else
                    bandera = true;
            }
            catch (Exception ex)
            {
                Log.Mensajes(ex.Message, "ExisteConductor");
                MensajesBD(ex.Message, "ExisteConductor");
                throw new ExcepcionSQL("BDD Biométrico: " + ex.Message);
            }


            return bandera;
        }

        #endregion
        #region DataBridge
        /// <summary>
        /// Obtengo el número de transacción en la base de datos de DataBridge, convierto en tipo de datos numérico y sumo uno
        /// </summary>
        /// <returns>devuelvo el siguiente número de transacción de DataBridge</returns>
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
                Log.Mensajes(ex.Message, "consulta_TransaccionDB");
                MensajesBD(ex.Message, "consulta_TransaccionDB");
                throw new ExcepcionSQL("BDD DataBridge: " + ex.Message);
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

        /// <summary>
        /// Se crea el XML con los datos que se enviarán a ARIES mediante Web Service, se llama al ejecutable de comunicación y se le pasa como
        /// parametro el número de ticket, que será un identificador único por transacción, se selecciona el registro que tiene como id de ticket
        /// enviado y con estado E (Enviado), se guarda el mensaje y estatus devueltos por el Web Service, si existe el mensaje se cambia de 
        /// estado a T (Terminado).
        /// </summary>
        /// <param name="ticket">Objeto que contiene toda datos relacionados a la transacción</param>
        /// <param name="centroTransaccion">Centro seleccionado en el software DataBridge</param>
        /// <param name="nombreConductor">Nombre del conductor seleccionado en el software DataBridge</param>
        /// <param name="mensajeAries">Mensaje que devuelve ARIES al Web Service</param>
        /// <param name="estatusAries">Estatis que devuekve ARIES al Web Service</param>
        public void InvocarServicio(Ticket ticket, string centroTransaccion, string nombreConductor, ref string mensajeAries, ref int estatusAries)
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
                                "<TicketDataBridge>" + ticket.Numero + "</TicketDataBridge>" +
                                "<FechaTicketProceso>" + ticket.Fecha.ToString("dd/MM/yyyy") + "</FechaTicketProceso>" +
                                "<HoraTicketProceso>" + ticket.Fecha.ToString("HH:MM") + "</HoraTicketProceso>" +
                                "<UsuarioDataBridge>" + ticket.Operador + "</UsuarioDataBridge>" +
                                "<NumeroBascula>" + ticket.Bascula + "</NumeroBascula>" +
                                "<TipoPeso>" + ticket.Tipo + "</TipoPeso>" +
                                "<PesoTicketDataBridge>" + ticket.PesoEnviado + "</PesoTicketDataBridge>" +
                                "<PlacaVehiculo>" + ticket.PlacaVehiculo + "</PlacaVehiculo>" +
                                "<CedulaTransportista>" + ticket.CedulaConductor + "</CedulaTransportista>" +
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
                //string res = G_Msg(codificiacionMsj, "A", ticket.Numero);
                GuardarMensajeEnviado(codificiacionMsj, ticket.Id);
                //string ejecutable = @"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe " + N_Transaccion;
                //Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\ComunicacionAries.exe "+N_Transaccion);
                //mensaje=ejecutable;
                //Process.Start(ejecutable);
                // Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\Comunicación Aries\PruebasComunicacion.exe");
                Process.Start(@"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Comunicación Aries\ComunicacionAries.exe ", ticket.Id);
                System.Threading.Thread.Sleep(10000);
                // G_Msg2();
                //CONSULTA DE DATOS
                Aries aries = new Aries();

                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("SELECT TOP 1* FROM ARIES WHERE ARIES_ESTADO='E' AND TIC_ID=@id ORDER BY ARIES_FECHA DESC", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@id", ticket.Id));

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    aries.Id = reader.GetString(0);
                                    aries.EstatusRecibido = reader.GetInt32(3);
                                    aries.MensajeRecibido = reader.GetString(4);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Mensajes(ex.Message, "InvocarServicio");
                    MensajesBD(ex.Message, "InvocarServicio");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }

                if (aries.MensajeRecibido != "")
                {
                    try
                    {
                        using (var Conn = new SqlConnection(Conexion_Bd))
                        {
                            Conn.Open();
                            using (var command = new SqlCommand("UPDATE ARIES SET ARIES_ESTADO='T' WHERE ARIES_ID=@id", Conn))
                            {
                                command.Parameters.Add(new SqlParameter("@id", aries.Id));
                                int rowsAdded = command.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                    }

                    estatusAries = aries.EstatusRecibido;
                    mensajeAries = aries.MensajeRecibido;

                }
                else
                {
                    estatusAries = 0;
                    mensajeAries = "";
                }

            }
            catch (Exception ex)
            {
                estatusAries = 1;
                mensajeAries = ex.Message;
                Log.Mensajes(ex.Message, "InvocarServicio");
                MensajesBD(ex.Message, "InvocarServicio");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

        }
        /// <summary>
        /// Se crear un registro con los datos de id, número de ticket, fecha, xml enviado
        /// </summary>
        /// <param name="Mensaje">xml encriotado con los datos del ticket enviado</param>
        /// <param name="IdTicket"> número de ticket al que está asociado el mensaje xml enviado</param>w
        public void GuardarMensajeEnviado(string Mensaje,string IdTicket)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("INSERT INTO ARIES(ARIES_ID,TIC_ID,ARIES_FECHA,ARIES_XMLENVIADO,ARIES_ESTADO) VALUES((SELECT 'ARI_'+CAST(COUNT(ARIES_ID)+1 AS NVARCHAR) FROM ARIES),@IdTicket,GETDATE(),@mensaje,'P')", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@IdTicket",IdTicket));
                        command.Parameters.Add(new SqlParameter("@mensaje", Mensaje));
                        consulta = Convert.ToString(command.ExecuteNonQuery());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "GuardarMensajeEnviado");
                Log.Mensajes(ex.Message, "GuardarMensajeEnviado");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        /// <summary>
        /// Se desencripta el texto recibido en algormitrmo de cifrado por bloque de 64 bits
        /// </summary>
        /// <param name="valor">mensaje xml encriotado</param>
        /// <returns>mensaje en xml desenciptado</returns>
        public static string DecodeBase64ToString(string valor)
        {
            byte[] myBase64ret = Convert.FromBase64String(valor);
            string myStr = System.Text.Encoding.UTF8.GetString(myBase64ret);
            return myStr;
        }
        /// <summary>
        /// Se encripta el texto recibido con el algoritmo de cifrado por bloques de 64 bits
        /// </summary>
        /// <param name="valor">mensaje en xml que se va a encriptar</param>
        /// <returns> mensaje en xml encriptado</returns>
        public static string EncodeStrToBase64(string valor)
        {
            byte[] myByte = System.Text.Encoding.UTF8.GetBytes(valor);
            string myBase64 = Convert.ToBase64String(myByte);
            return myBase64;
        }
        #endregion

        #region Transacción

        /// <summary>
        /// Obtengo Código PIN asociado a la transacción en la que no se leyeron las placas al vehículo 
        /// y que la transaccion se encuentra ne estado P (Pendiente).
        /// </summary>
        /// <param name="placaSeleccionada">placa seleccionada en el software DataBridge</param>
        /// <returns>devuelvo código PIN que concuerde con la placa seleccionada</returns>
        public string ObtenerPIN(string placaSeleccionada)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 CORR_PIN FROM CORREO INNER JOIN TICKET ON TICKET.CORR_ID=CORREO.CORR_ID WHERE TICKET.TIC_PLACAVEHICULO=@placa AND TICKET.TIC_ESTADO='P' ORDER BY CORR_FECHA DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("placa",placaSeleccionada));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerPIN");
                Log.Mensajes(ex.Message, "ObtenerPIN");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

            return consulta;
        }
        /// <summary>
        /// Obtengo Id del ticket que concuerde con la placa seleccionada y se encuentre en estado P(Pendiente).
        /// </summary>
        /// <param name="placaSeleccionada">placa seleccionada en el software DataBridge</param>
        /// <returns>devuelvo id de ticket</returns>
        public string ObtenerIdTicketPendientePIN(string placaSeleccionada)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 TIC_ID FROM TICKET INNER JOIN CORREO ON CORREO.CORR_ID=TICKET.CORR_ID WHERE TICKET.TIC_PLACAVEHICULO=@placa AND TICKET.TIC_ESTADO='P' ORDER BY TIC_FECHA,TIC_HORA,TIC_NUMERO DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@placa", placaSeleccionada));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerIdTicketPendientePIN");
                Log.Mensajes(ex.Message, "ObtenerIdTicketPendientePIN");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

            return consulta;
        }
        /// <summary>
        /// Obtengo un listado de vehículos en estado P (Pendiente) capturados por la cámara delantera
        /// </summary>
        /// <param name="bascula">número de báscula</param>
        /// <returns>devuelve el listado de vehículos en estado pendiente</returns>
        public List<Vehiculo> ObtenerVehiculosPendientesCamDelantera(int bascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            List<Vehiculo> vehiculos = new List<Vehiculo>();
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT * FROM VEHICULO WHERE VEH_CAMARA=@camara AND VEH_ESTADO='P' ORDER BY VEH_FECHA DESC", Conn))
                    {
                        if (bascula == 0)
                            command.Parameters.Add(new SqlParameter("@camara",cam11));
                        else
                            command.Parameters.Add(new SqlParameter("@camara",cam21));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Vehiculo vehiculo = new Vehiculo(
                                    reader.GetInt32(0),
                                    reader.GetString(1),
                                    reader.GetDateTime(2),
                                    reader.GetString(3),
                                    reader.GetString(4));
                                vehiculos.Add(vehiculo);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerVehiculosPendientesCamDelantera");
                Log.Mensajes(ex.Message, "ObtenerVehiculosPendientesCamDelantera");
                Console.WriteLine("BDD Plugin: " + ex.Message);
            }

            return vehiculos;
        }
        public  List<Vehiculo> ListaVehiculosValidos(int bascula,string direccionCamaras,string direccionLoop)
        {
            List<Vehiculo> vehiculosDelanteros = ObtenerVehiculosPendientesCamDelantera(bascula);
            List<Vehiculo> vehiculosTraseros = ObtenerVehiculosPendientesCamTrasera(bascula);
            if (direccionCamaras == "dentro")
            {
                List<Vehiculo> vehiculos = ObtenerVehiculosPendientesCamDelantera(bascula);
                vehiculos.AddRange(ObtenerVehiculosPendientesCamTrasera(bascula));
                return vehiculos;
            }
            else
            {
                if (direccionCamaras == "fuera" && direccionLoop == "Exit")
                    return vehiculosTraseros;
                else
                {
                    return vehiculosDelanteros;
                }
                
            }  

        }
        public string listarPlacasLeidas(int bascula, string direccionCamaras, string direccionLoop)
        {
            HashSet<string> listaPlacas = new HashSet<string>();
            string lista = "";
            
            foreach (Vehiculo vehiculo in ListaVehiculosValidos(bascula,direccionCamaras,direccionLoop))
            {
                listaPlacas.Add(vehiculo.Placa);
            }
            foreach(string placa in listaPlacas)
            {
                lista += placa + ";";
            }
            
            return lista;
        }
        public List<Vehiculo> ObtenerVehiculosPendientesCamTrasera(int bascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            List<Vehiculo> vehiculos = new List<Vehiculo>();
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT * FROM VEHICULO WHERE VEH_CAMARA=@camara AND VEH_ESTADO='P' ORDER BY VEH_FECHA DESC", Conn))
                    {
                        if (bascula == 0)
                            command.Parameters.Add(new SqlParameter("@camara", cam12));
                        else
                            command.Parameters.Add(new SqlParameter("@camara", cam22));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Vehiculo vehiculo = new Vehiculo(
                                    reader.GetInt32(0),
                                    reader.GetString(1),
                                    reader.GetDateTime(2),
                                    reader.GetString(3),
                                    reader.GetString(4));
                                vehiculos.Add(vehiculo);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Mensajes(ex.Message, "ObtenerVehiculosPendientesCamTrasera");
                Console.WriteLine("BDD Plugin: " + ex.Message);
            }

            return vehiculos;
        }
        public string LimpiarPlaca(string placa)
        {
            
            bool digitosPlaca = false;
            StringBuilder placaNumeros = new StringBuilder();
            StringBuilder placaLetras = new StringBuilder();
            StringBuilder placaLeida = new StringBuilder();

            placa.Replace(".jpg", "");
            placa.Replace("-", "");

            if (placa.Length >= 7)
                placaLeida.Append(placa.Substring(0, 7));
            else
                placaLeida.Append(placa);


            digitosPlaca = placaLeida.Length == 6 ? true : false;
            placaLetras.Append(placaLeida);
            placaLetras.Remove(3, placaLeida.Length - 3);
            placaLetras.Replace('0', 'O');
            placaLetras.Replace('1', 'I');
            placaLetras.Replace('8', 'B');
            placaLetras.Replace('6', 'G');
            placaNumeros.Append(placaLeida);
            placaNumeros.Remove(0, 3);
            if (digitosPlaca == true)
                placaNumeros.Insert(0, '0');

            placaNumeros.Replace('O', '0');
            placaNumeros.Replace('Q', '0');
            placaNumeros.Replace('I', '1');
            placaNumeros.Replace('B', '8');
            placaNumeros.Replace('G', '6');
            placaLeida.Clear();
            placaLeida.Append(placaLetras);
            placaLeida.Append(placaNumeros);
            placa = placaLeida.ToString();
            placaLeida.Clear();
            placaLetras.Clear();
            placaNumeros.Clear();
            return placa;
        }
        public bool ValidarVehiculo(string placaSeleccionada,int bascula, string direccionCamaras, string direccionLoop)
        {
            List<Vehiculo> vehiculos = ListaVehiculosValidos(bascula, direccionCamaras, direccionCamaras);
            bool bandera = false;
            String placa = "";
            foreach (Vehiculo vehiculo in vehiculos)
            {
                if(vehiculo.Placa.Equals(placaSeleccionada))
                {
                    bandera = true;
                    break;
                }
                else
                { 
                    if(LimpiarPlaca(vehiculo.Placa).Equals(placaSeleccionada))
                    {
                        bandera = true;
                        break;
                    }
                    
                }
                
            }
            return bandera;
        }
        public string ObtenerSiguienteIdTicket()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT 'TIC_'+CAST(COUNT(TIC_ID)+1 AS NVARCHAR) FROM TICKET", Conn))
                    {
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerSiguienteIdTicket");
                Log.Mensajes(ex.Message, "ObtenerSiguienteIdTicket");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }


            return consulta;
        }
        public string ObtenerSiguienteIdCorreo()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT 'CORR_'+CAST(COUNT(CORR_ID)+1 AS NVARCHAR) FROM CORREO", Conn))
                    {
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerSiguienteIdCorreo");
                Log.Mensajes(ex.Message, "ObtenerSiguienteIdCorreo");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }


            return consulta;
        }
        public void GuardarTicket(Ticket ticket)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SP_TerminarTransaccion", Conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Id", ticket.Id);
                        command.Parameters.AddWithValue("@IdCorreo", ticket.IdCorreo);
                        command.Parameters.AddWithValue("@Bascula", ticket.Bascula);
                        command.Parameters.AddWithValue("@Numero", ticket.Numero);
                        command.Parameters.AddWithValue("@Tipo", ticket.Tipo);                   
                        command.Parameters.AddWithValue("@Cedula", ticket.CedulaConductor);
                        command.Parameters.AddWithValue("@Placa", ticket.PlacaVehiculo);
                        command.Parameters.AddWithValue("@Operador", ticket.Operador);
                        command.Parameters.AddWithValue("@PesoEnviado", ticket.PesoEnviado);
                        command.Parameters.AddWithValue("@PesosObtenidos", ticket.PesosObtenidos);
                        command.Parameters.AddWithValue("@Estado", ticket.Estado);
                        int dato = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "GuardarTicket");
                Log.Mensajes(ex.Message, "GuardarTicket");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        public void ComprobarTicketEntrada(TransactionModel myTransaction,int status)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SP_TerminarTransaccionEntrada", Conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@transaccion", myTransaction.TransactionNumber);
                        command.Parameters.AddWithValue("@placa", myTransaction.CurrentLoad.Vehicle.Name);
                        command.Parameters.AddWithValue("@conductor", myTransaction.CurrentLoad.Driver.Name);
                        command.Parameters.AddWithValue("@operador",myTransaction.CurrentLoad.Pass1Operator);
                        command.Parameters.AddWithValue("@peso", myTransaction.CurrentLoad.Pass1Weight);
                        command.Parameters.AddWithValue("@estatus",status );
                        int dato = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ComprobarTicketEntrada");
                Log.Mensajes(ex.Message, "ComprobarTicketEntrada");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        public void ActualizarEstadoVehiculosTerminados(int bascula, string direccionCamaras,string direccionLoop)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            int rowsAdded = 0;

            if(direccionCamaras.Equals("dentro"))
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("UPDATE VEHICULO  SET VEH_ESTADO='T' WHERE VEH_ESTADO='P' AND VEH_CAMARA=@camDelantera OR VEH_CAMARA=@camTrasera", Conn))
                        {
                            if (bascula == 0)
                            {
                                command.Parameters.Add(new SqlParameter("@camDelantera", cam11));
                                command.Parameters.Add(new SqlParameter("@camTrasera", cam12));
                            }
                            if (bascula == 1)
                            {
                                command.Parameters.Add(new SqlParameter("@camDelantera", cam21));
                                command.Parameters.Add(new SqlParameter("@camTrasera", cam22));
                            }

                            rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MensajesBD(ex.Message, "ActualizarEstadoVehiculosTerminados");
                    Log.Mensajes(ex.Message, "ActualizarEstadoVehiculosTerminados");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("UPDATE VEHICULO  SET VEH_ESTADO='T' WHERE VEH_ESTADO='P' AND VEH_CAMARA=@camara", Conn))
                        {
                            if (bascula == 0 && direccionLoop.Equals("Entrance"))
                            {
                                command.Parameters.Add(new SqlParameter("@camara", cam11));
                            }
                            if (bascula == 0 && direccionLoop.Equals("Exit"))
                            {
                                command.Parameters.Add(new SqlParameter("@camara", cam12));
                            }
                            if (bascula == 1 && direccionLoop.Equals("Entrance"))
                            {
                                command.Parameters.Add(new SqlParameter("@camara", cam21));
                            }
                            if (bascula == 1 && direccionLoop.Equals("Exit"))
                            {
                                command.Parameters.Add(new SqlParameter("@camara", cam22));
                            }

                            rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MensajesBD(ex.Message, "ActualizarEstadoVehiculosTerminados");
                    Log.Mensajes(ex.Message, "ActualizarEstadoVehiculosTerminados");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            
        }
        public void ActualizarEstadoTicketTerminado(string idTicket)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE TICKET SET TIC_ESTADO='T' WHERE TIC_ID=@idTicket", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@idTicket",idTicket));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ActualizarEstadoTicketTerminado");
                Log.Mensajes(ex.Message, "ActualizarEstadoTicketTerminado");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        public void ActualizarEstadoTicketEnviado(string idTicket)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE TICKET SET TIC_ESTADO='E' WHERE TIC_ID=@idTicket", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@idTicket", idTicket));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ActualizarEstadoTicketEnviado");
                Log.Mensajes(ex.Message, "ActualizarEstadoTicketEnviado");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        public void ActualizarEstadoTicketInvalidos(Ticket ticket,string pesoBascula,string evento,int bascula)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("UPDATE TICKET SET TIC_ESTADO='I' WHERE TIC_BASCULA=@bascula AND TIC_ESTADO='P'", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@bascula",bascula));
                        int rowsAdded = command.ExecuteNonQuery();

                        if(rowsAdded!=0)
                            Log.Estado(ticket, pesoBascula, evento, bascula);
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ActualizarEstadoTicketInvalidos");
                Log.Mensajes(ex.Message, "ActualizarEstadoTicketInvalidos");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }
        public void ActualizarEstadoConductoresTerminados()
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            bool estadoBiometricoEntrada = Boolean.Parse(cfg.AppSettings.Settings["Biometrico_Entrada"].Value);
            bool estadoBiometricoSalida = Boolean.Parse(cfg.AppSettings.Settings["Biometrico_Salida"].Value);
            string BiometricoEntrada = cfg.AppSettings.Settings["Nom_Biometrico_Entrada"].Value;
            string BiometricoSalida = cfg.AppSettings.Settings["Nom_Biometrico_Salida"].Value;
            if (estadoBiometricoEntrada)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("UPDATE CONDUCTOR SET COND_ESTADO='T' WHERE COND_ESTADO='P' AND COND_DISPOSITIVO=@biometrico", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@biometrico", BiometricoEntrada));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MensajesBD(ex.Message, "ActualizarEstadoConductoresTerminados");
                    Log.Mensajes(ex.Message, "ActualizarEstadoConductoresTerminados");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
            if(estadoBiometricoSalida)
            {
                try
                {
                    using (var Conn = new SqlConnection(Conexion_Bd))
                    {
                        Conn.Open();
                        using (var command = new SqlCommand("UPDATE CONDUCTOR SET COND_ESTADO='T' WHERE COND_ESTADO='P' AND COND_DISPOSITIVO=@biometrico", Conn))
                        {
                            command.Parameters.Add(new SqlParameter("@biometrico", BiometricoSalida));
                            int rowsAdded = command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Mensajes(ex.Message, "ActualizarEstadoConductoresTerminados");
                    throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
                }
            }
           
        }
        public bool ExisteTransaccionDataBridge(Ticket ticket)
        {
            return false;
        }
        public bool ExisteCorreoPendiente(string placaSeleccionada)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            bool bandera = false;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 CORR_PLACA FROM CORREO WHERE CORR_PLACA=@placa AND CORR_ESTADO='P' ORDER BY CORR_FECHA", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@placa", placaSeleccionada));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }

                if (consulta.Equals(string.Empty))
                    bandera = false;
                else
                    bandera = true;
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ExisteCorreoPendiente");
                Log.Mensajes(ex.Message, "ExisteCorreoPendiente");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }


            return bandera;
        }
        public void GuardarSecuencia(Secuencia secuencia)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("INSERT INTO SECUENCIA VALUES(@idCorreo,GETDATE(),@operador,@razon,@bascula,@pesoObtenido,@pesoBascula)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@idCorreo",secuencia.IdCorreo));
                        command.Parameters.Add(new SqlParameter("@operador", secuencia.Operador));
                        command.Parameters.Add(new SqlParameter("@razon", secuencia.Razon));
                        command.Parameters.Add(new SqlParameter("@bascula", secuencia.Bascula));
                        command.Parameters.Add(new SqlParameter("@pesoObtenido",secuencia.PesoObtenido));
                        command.Parameters.Add(new SqlParameter("@pesoBascula", secuencia.PesoBascula));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "GuardarSecuencia");
                Log.Mensajes(ex.Message, "GuardarSecuencia");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

        }
        public int AnularTransacción(string transaccion, string placa)
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
                    using (var command = new SqlCommand("UPDATE TICKET SET TIC_ESTADO='A' WHERE TIC_PLACAVEHICULO=@placa AND TIC_ESTADO='T' AND TIC_NUMERO=@numero", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@numero", transaccion));
                        command.Parameters.Add(new SqlParameter("@placa", placa));
                        consulta = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "AnularTransacción");
                Log.Mensajes(ex.Message, "AnularTransacción");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
            return consulta;
        }
        public string ObtenerIdTicketPendiente(string placaSeleccionada, string tipo)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            string consulta = "";
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("SELECT TOP 1 TIC_ID FROM TICKET WHERE TIC_PLACAVEHICULO=@placa AND TIC_ESTADO='P' AND TIC_TIPO=@tipo ORDER BY TIC_FECHA,TIC_HORA DESC", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@placa", placaSeleccionada));
                        command.Parameters.Add(new SqlParameter("@tipo", tipo));
                        consulta = Convert.ToString(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MensajesBD(ex.Message, "ObtenerIdTicketPendiente");
                Log.Mensajes(ex.Message, "ObtenerIdTicketPendiente");
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }

            return consulta;
        }

        public void MensajesBD(string mensaje, string metodo)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            try
            {
                using (var Conn = new SqlConnection(Conexion_Bd))
                {
                    Conn.Open();
                    using (var command = new SqlCommand("INSERT INTO LOG VALUES(GETDATE(),@mensaje,@evento)", Conn))
                    {
                        command.Parameters.Add(new SqlParameter("@mensaje",mensaje));
                        command.Parameters.Add(new SqlParameter("@evento",metodo));
                        int rowsAdded = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ExcepcionSQL("BDD Plugin: " + ex.Message);
            }
        }

        #endregion







    }


}

