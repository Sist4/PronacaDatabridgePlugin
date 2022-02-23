using System;
using DataBridge.Core.Business;
using DataBridge.Core.TransactionManager;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Reflection;
using DataBridge.Core.Types;
using DataBridge.VideoServerLibrary.CameraData;
using DataBridge.VideoServerManager;
using System.Drawing.Imaging;
using DataBridge.Core.Services;
using DataBridge.DeviceManager;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using System.IO;
using System.Collections.Generic;
using DataBridge.ScaleLibrary.ScaleData;
using DataBridge.ScaleLibrary.Events;
using Caliburn.Micro;
using DataBridge.Attended.ViewModel;
using Ninject;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using System.Diagnostics;
using System.Threading;
using PronacaPlugin;
using System.Collections;
using DataBridge.IOManager;
using DataBridge.Core.Services;
using DataBridge.Core.TransactionManager;
using DataBridge.ScaleManager;

namespace PronacaPlugin
{
   public class Transacciones : TransactionProcessing
    {
        //Variables globales
        ArrayList pesosObtenidos;
        double[] pesoActualBascula;
        bool banderaCamaras;
        string estado;
        string msj_recibido;
        string Numeral_recibido;
        GestionVehiculos VEH;
        bool banderaTransaccionEnviada;
        //**************Acceso al app config******************//
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        //Constructor por defecto
        public Transacciones()
        {
            pesoActualBascula = new double[2];
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            banderaCamaras = false;
            VEH = new GestionVehiculos();
            estado = "";
            pesosObtenidos = new ArrayList();
            banderaTransaccionEnviada = false;
        }

        #region Propiedades

       
        private IScaleMgr _myScaleMgr = null;
        private IScaleMgr ScaleMgr
        {
            get
            {
                if (_myScaleMgr == null)
                {
                    _myScaleMgr = ServiceManager.GetService<IScaleMgr>();
                }
                return _myScaleMgr;
            }
        }

        #endregion

        #region Propiedades de video para la camara

        private ConcurrentDictionary<int, bool> _automaticallyComplete = null;
        private ConcurrentDictionary<int, bool> CompleteTransactionAutomatically
        {
            get
            {
                if (_automaticallyComplete == null)
                {
                    _automaticallyComplete = new ConcurrentDictionary<int, bool>();
                }

                return _automaticallyComplete;
            }
        }

        private IVideoServerMgr _myVideoServerMgr = null;
        public IVideoServerMgr VideoServerMgr
        {
            get
            {
                if (_myVideoServerMgr == null)
                {
                    _myVideoServerMgr = ServiceManager.GetService<IVideoServerMgr>();
                }
                return _myVideoServerMgr;
            }
        }
        #endregion

        #region Métodos públicos
        public override void TransactionVoided(int nScaleId, TransactionModel myTransaction)
        {
            VEH.anularTransacción(myTransaction.TransactionNumber);
            VEH.eliminarTransaccionPendiente();
        }
        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction) 
        {
            string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value; //tiempo que tiene el chofer para timbrar en el biometrico
            string chofer = myTransaction.Loads[0].Driver.Name; //cédula del chofer que conduce el vehículo 
            string vehiculo = myTransaction.Loads[0].Vehicle.Name; //la placa del vehículo
            estado = "entrada";
            if(banderaTransaccionEnviada==true)
            {
                return "";
            }
            else
            {
                //*****************************FILTRO DEL CHOFER*********************************************************
                //Tomar en cuenta que debe estar abierto el progrma ZKAccess3.5 Security System del biometrico  
                //Consultamos si el chofer existe en el biometrico 
                if (VEH.consulta_ExisteChofer(chofer) != "")
                {
                    //ventanaOK("Si se identifico al chofer", "DataBridge Plugin");
                    //*****************************FILTRO DEL BIOMETRICO*********************************************************
                    // si existe procedemosa verificar si se tomo la lectura en el trascuroso de los 10 minutos
                    if (VEH.consulta_BiometricoChofer(chofer) != "")
                    {
                        //ventanaOK("peso obtenido: " + myTransaction.Loads[0].Pass1Weight + " peso actual: " + pesoActualEntrada + " peso actual -10: " + (pesoActualEntrada - 10) +
                        //  "peso actual +10: " + (pesoActualEntrada + 10), "ventana pesos");
                        if (myTransaction.Loads[0].Pass1Weight >= (pesoActualBascula[nScaleId] - 10) && myTransaction.Loads[0].Pass1Weight <= (pesoActualBascula[nScaleId] + 10))
                        {
                            //pasa el primer filtro del chofer 
                            //********************** FILTRO DE LA PLACA AL INGRESO*******************************************************************   
                            string Ping_Ingreso = VEH.consulta_PlacaIngreso(vehiculo);
                            //ventanaOK("Si timbro el chofer", "DataBridge Plugin");
                            if (Ping_Ingreso != "")
                            {
                                //si el vehiculo anteriormente se registro un pin el operador ya debio haber registrado 
                                ventanaOK("Se envió un email al coordinador con un PIN para terminar la transacción de entrada ", "DataBridge Plugin");
                                string Nota = ventanaIngresoDato("Ingrese el PIN ", "DataBridge Plugin", "PIN");
                                if (Ping_Ingreso.Equals(Nota))
                                {
                                    //************************************************COMUNICACION CON EL ARIES *****************************************************
                                    //ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                    return AriesEntrada(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                    //return "";          
                                }
                                else
                                {
                                    return "El PIN Ingresado No Coincide";
                                }
                            }
                            else
                            {
                                //************************************************COMUNICACION CAMARAS *****************************************************
                                if (ComunicacionCamaras(myTransaction, nScaleId) == true)
                                {
                                    banderaCamaras = true;
                                    //si la respuesta es positiva de cualquiera de las dos camaras
                                    ///BUSCAMOS EN LA RUTA DEL FTP SI LOS DEVUELVE EN BLANCO ES Q SI ENCONTRO LA FOTO
                                    if (VEH.listarFTP(vehiculo).Equals(""))
                                    {
                                        //************************************************COMUNICACION CON EL ARIES *****************************************************
                                        //    ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                        return AriesEntrada(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                        //return "";
                                    }
                                    else
                                    {
                                        //************************************************NOTIFICACION POR CORREO *****************************************************
                                        try
                                        {
                                            return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                        }
                                        catch (Exception ex)
                                        {
                                            VEH.eliminarTransaccionPendiente();
                                            return "Error en el envío del correo: " + ex.Message;
                                        }


                                    }
                                }
                                else
                                {
                                    //************************************************NOTIFICACION POR CORREO *****************************************************
                                    banderaCamaras = false;
                                    return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                }

                            }

                        }
                        else
                        {
                            return "El peso obtenido no es igual al peso actual, vuelva a obtener el peso porfavor";
                        }

                    }
                    // si el chofer no Timbro o excedio el tiempo predeterminado(10 minutos) 
                    else
                    {
                        return "El Chofer no ha Timbrado en el Biometrico. Recuerde tener abierto el software del Biometrico y que el Tiempo de espera son de " + T_Chofer + " Minutos";
                    }
                }
                // FIN DEL FILTRO BIOMETRICO- CHOFER
                else
                {
                    return "El chofer debe estar Creado en el Biometrico";
                }
            }
            
        }
        public override void TransactionAccepted(int nScaleId, TransactionModel myTransaction)
        {

            GestionVehiculos VEH = new GestionVehiculos();
            //DATOS
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name;
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            string Chofer = myTransaction.Loads[0].Driver.Name.ToString();
            string Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString();
            //gurdamos la informacion que envia el databridge 
            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", N_Transaccion, "", "", "", "", "", "IC", msj_recibido, Numeral_recibido);
            VEH.InsertarPesosObtenidos(pesosObtenidos, N_Transaccion);
            pesosObtenidos.Clear();
            banderaTransaccionEnviada = false;

        }
        public override string TransactionCompleting(int nScaleId, TransactionModel myTransaction)
        {
           string  vehiculo = myTransaction.Loads[0].Vehicle.Name;
           string Peso_Salida = myTransaction.Loads[0].Pass2Weight.ToString();
           string chofer = myTransaction.Loads[0].Driver.Name.ToString();
           string N_Transaccion = myTransaction.Loads[0].TransactionNumber; 
           string T_pesaje = VEH.consulta_TipoIngreso(N_Transaccion);
           string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value;
            estado = "salida";
            if(banderaTransaccionEnviada==true)
            {
                return "";
            }
            else
            {
                if (VEH.consulta_ExisteChofer(chofer) != "")
                {
                    if (VEH.consulta_BiometricoChofer(chofer) != "")
                    {
                        if (myTransaction.Loads[0].Pass2Weight >= (pesoActualBascula[nScaleId] - 10) && myTransaction.Loads[0].Pass2Weight <= (pesoActualBascula[nScaleId] + 10))
                        {
                            string Ping_Salida = VEH.consulta_PinSalida(N_Transaccion);
                            if (Ping_Salida != "")
                            {
                                string Nota = ventanaIngresoDato("Ingrese el PIN:", "DataBridge Plugin", "PIN");
                                if (Ping_Salida.Equals(Nota))
                                {
                                    String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", "", "", "", "", "SC", "", "");
                                    //ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                    return AriesSalida(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                    //return "";
                                }
                                else
                                {
                                    return "El PIN Ingresado No Coincide";
                                }
                            }
                            else
                            {
                                if (ComunicacionCamaras(myTransaction, nScaleId) == true)
                                {
                                    banderaCamaras = true;
                                    if (VEH.listarFTP(vehiculo).Equals(""))
                                    {
                                        String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", "", "", "", "", "SC", "", "");

                                        //ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                        return AriesSalida(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                        //return "";
                                    }
                                    else
                                    {
                                        return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                    }
                                }
                                else
                                {
                                    banderaCamaras = false;
                                    return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                }

                            }
                        }
                        else
                        {
                            return "El peso obtenido no es igual al peso actual, vuelva a obtener el peso porfavor";
                        }
                    }
                    // si el chofer no Timbro o excedio el tiempo predeterminado(10 minutos) 
                    else
                    {
                        return "El Chofer no ha Timbrado en el Biometrico, Recuerde tener abierto el software del Biometrico y que el Tiempo de espera son de " + T_Chofer + " Minutos";
                    }
                }
                // FIN DEL FILTRO BIOMETRICO- CHOFER
                else
                {
                    return "El chofer debe estar Creado en el Biometrico";
                }
            }
            
        }
        public override void TransactionCompleted(int nScaleId, TransactionModel myTransaction)
        {
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            VEH.InsertarPesosObtenidos(pesosObtenidos, N_Transaccion);
            pesosObtenidos.Clear();
            banderaTransaccionEnviada = false;
        }
        public override void ScaleAboveThreshold(ScaleAboveThresholdEventArgs myEventArgs)
        {
            pesosObtenidos.Clear();
        }
        public override void ScaleDataReceived(ScaleWeightPacket myScaleWeightData)
        {
            double peso = myScaleWeightData.MainWeightData.GrossWeightValue;
            SetPesoActual(ref peso,myScaleWeightData.ScaleId);  
        }
        public override void WeightSet(int nScaleId, ScaleWeightPacket myScaleWeightData, bool bIsSplitWeight)
        {
            //ventanaOK("se tomo el peso: " + myScaleWeightData.MainWeightData.GrossWeightValue,"ventana peso tomado");
            pesosObtenidos.Add(myScaleWeightData.MainWeightData.GrossWeightValue.ToString());
        }
        #endregion

        #region Métodos privados
        private string ventanaIngresoDato(String texto,String titulo,String cajaTexto)
        {
            try
            {
                // Get singleton
                IWindowManager windowManager = ServiceLocator.GetKernel().Get<IWindowManager>();

                CustomInputDialogViewModel viewModel = new CustomInputDialogViewModel();
                viewModel.CustomWindowTitle = titulo;
                viewModel.DisplayText = texto;
                viewModel.WatermarkValue = cajaTexto;
                viewModel.YesButtonText = "OK";
                viewModel.NoButtonText = "Cancel";
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    windowManager.ShowDialog(viewModel);

                });

                if (viewModel.DialogResult)
                {
                    // Do something with textbox result
                    string myTextboxResult = viewModel.InputValue;
                    return myTextboxResult;
                }
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Error", ex);
            }
            return string.Empty;
            
        }

        private void ventanaOK(string texto,String titulo)
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
            catch(Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Error", ex);
            }
        }

        private string ObtenerImagen(string Placa,string Nom_Camara, PingReply estado)
        {
            if (estado.Status == IPStatus.Success)
            {
                string Nombre_Archivo = Placa + "_" + Nom_Camara + DateTime.Now.ToString("yyyyMMddhhmmss");
                try
                {
                    CameraModel myCamera = CameraModel.GetByName(Nom_Camara);
                    int CameraId = myCamera.Id;
                    int ImageWidth = 600;
                    int ImageHeight = 800;
                    int Quality = myCamera.ImageQuality;
                    ImageFormat myImageFormat = ImageFormat.Jpeg;

                    CameraImageData cameraImageData = VideoServerMgr.CaptureCameraImage(CameraId, ImageWidth, ImageHeight, Quality, myImageFormat);

                    if (cameraImageData != null && cameraImageData.ImageBase64 != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (string imageBase in cameraImageData.ImageBase64)
                        {
                            sb.Append(imageBase);
                        }

                        byte[] myImageAsBytes = Convert.FromBase64String(sb.ToString());

                        using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(myImageAsBytes)))
                        {
                            image.Save(@"C:\Camara_DataBridge\" + Nombre_Archivo + ".jpg", ImageFormat.Jpeg);  // Or Png
                        }


                    }
                    else
                    {
                        Nombre_Archivo = "";
                    }
                }
                catch (Exception ex)
                {
                    ServiceManager.LogMgr.WriteError("Failed to create transaction", ex);
                    Nombre_Archivo = "";

                }

                return Nombre_Archivo;
            }
            else
            {
                return "";
            }

        }

        private string AriesEntrada(TransactionModel myTransaction, int nScaleId,ref string msj_recibido,ref string Numeral_recibido)
        {
            string N_Transaccion = VEH.consulta_TransaccionDB(); //myTransaction.Loads[0].TransactionNumber; //número de transacción
            string FechaTicketProceso = DateTime.Now.ToString("dd/MM/yyyy");// Formato es: MM/dd/YYYY
            string HoraTicketProceso = DateTime.Now.ToString("hh:mm"); //Hora de la transacción
            string UsuarioDataBridge = myTransaction.Loads[0].Pass1Operator; //la persona que esté operando la báscula
            string NumeroBascula = nScaleId.ToString(); //identificador de báscula | báscula 0 es la primera báscula | báscula 1 es la segunda báscula |
            string TipoPeso = "E"; //E=Entrada S=Salida
            string Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString(); //Peso de entrada
            string Cedula = myTransaction.Loads[0].Driver.Name.ToString(); //Cédula del chofer
            string Chofer = myTransaction.Loads[0].Driver.Description.ToString(); //cédula del chofer
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name; //placa del vehículo
            string res_Aries = "";
            try
            {
                res_Aries = VEH.InvokeService(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer);
            }
            catch(Exception ex)
            {
                throw;
            }
            
            string[] rec_mensaje = res_Aries.Split('/');
            switch (rec_mensaje[0])
            {

                case "":
                    return "Error en la comunicación con Aries";

                case "2":
                    // ERROR
                    return rec_mensaje[1];

                case "3":
                    msj_recibido = "Transacción de Entrada exitosa";
                    Numeral_recibido = "3";
                    banderaTransaccionEnviada = true;
                    ventanaOK("¡Transacción de Entrada exitosa!", "DataBridge Plugin");
                    return "";
                //break;
                case "4":
                    msj_recibido = rec_mensaje[1];
                    Numeral_recibido = "4";
                    banderaTransaccionEnviada = true;
                    ventanaOK("¡Pesaje Entrada Visitante exitoso!", "DataBridge Plugin");
                    return "";
                case "5":
                    // Error del factor de conversion(aborta el pesaje)
                    return rec_mensaje[1];

                default:
                    return "Sin respuesta de Aries en la transacción de entrada. Presione el botón Aceptar e intente denuevo";
                    //  break;
            }
        }

        private string AriesSalida(TransactionModel myTransaction, int nScaleId,ref string msj_recibido, ref string Numeral_recibido)
        {
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber; //número de transacción
            string FechaTicketProceso = DateTime.Now.ToString("dd/MM/yyyy");// Formato es: MM/dd/YYYY
            string HoraTicketProceso = DateTime.Now.ToString("hh:mm"); //Hora de la transacción
            string UsuarioDataBridge = myTransaction.Loads[0].Pass1Operator; //la persona que esté operando la báscula
            string NumeroBascula = nScaleId.ToString(); //identificador de báscula | báscula 0 es la primera báscula | báscula 1 es la segunda báscula |
            string TipoPeso = "S"; //E=Entrada S=Salida
            string Peso_Ing = myTransaction.Loads[0].Pass2Weight.ToString(); //Peso de salida
            string Cedula = myTransaction.Loads[0].Driver.Name.ToString(); //Cédula del chofer
            string Chofer = myTransaction.Loads[0].Driver.Description.ToString(); //cédula del chofer
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name; //placa del vehículo
            string T_pesaje = VEH.consulta_TipoIngreso(N_Transaccion);
            if (T_pesaje != ("4")) 
            { 
                string res_Aries = VEH.InvokeService(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer);
                string[] rec_mensaje = res_Aries.Split('/');
                switch (rec_mensaje[0])
                {

                    case "2":
                        // ERROR
                        return rec_mensaje[1];

                    // break;
                    case "3":
                        msj_recibido = "Transacción de Salida exitosa";
                        Numeral_recibido = "3";
                        banderaTransaccionEnviada = true;
                        ventanaOK("¡Transacción de Salida exitosa!", "DataBridge Plugin");
                        return "";
                    //break;
                    case "4":
                        
                        msj_recibido = rec_mensaje[1];
                        Numeral_recibido = "4";
                        banderaTransaccionEnviada = true;
                        ventanaOK("¡Pesaje Salida Visitante exitoso!", "DataBridge Plugin");
                        return "";
                    case "5":
                        // Error del factor de conversion(aborta el pesaje)
                        return rec_mensaje[1];

                    default:
                        return "Sin respuesta de Aries en la transacción de salida. Presione el botón Aceptar e intente denuevo"; 
                        //  break;
                }
            }
            else
            {
                ventanaOK("¡Pesaje Salida Visitante exitoso!", "DataBridge Plugin");
                return "";
            }
        }

        private bool ComunicacionCamaras(TransactionModel myTransaction,int nScaleId)
        {
            Ping HacerPing = new Ping();
            int iTiempoEspera = 500;
            PingReply RespuestaPingCamara1;
            PingReply RespuestaPingCamara2;
            string IP_Camara1;
            string IP_Camara2;
            int balanza = nScaleId;
            //****************************** BASCULA 1 ******************************************************
            if (balanza==0)
            {
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara1"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara2"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
                if (RespuestaPingCamara1.Status == IPStatus.Success || RespuestaPingCamara2.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }
            //****************************** BASCULA 2 ******************************************************
            else
            {
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara3"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara4"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
                if (RespuestaPingCamara1.Status == IPStatus.Success || RespuestaPingCamara2.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }

        }

        private string NotificacionCorreo(TransactionModel myTransaction, int nScaleId, bool banderaCamaras,string estado)
        {
            var seed = Environment.TickCount;
            var random = new Random(seed);
            var Pin = random.Next(999, 9999);
            Ping HacerPing = new Ping();
            int iTiempoEspera = 500;
            PingReply RespuestaPingCamara1;
            PingReply RespuestaPingCamara2;
            string IP_Camara1;
            string IP_Camara2;
            string nom_Camara1;
            string nom_Camara2;
            string RES;
            string vehiculo = myTransaction.Loads[0].Vehicle.Name; //placa del vehículo
            string chofer = myTransaction.Loads[0].Driver.Name.ToString(); //cédula del chofer
            string peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString(); //Peso de entrada
            string peso_Salida = myTransaction.Loads[0].Pass2Weight.ToString();//Peso salida
            string N_Transaccion2 = myTransaction.Loads[0].TransactionNumber; //número de transacción
            string N_Transaccion1 = VEH.consulta_TransaccionDB();
            int balanza = nScaleId;
            //****************************** BASCULA 1 ******************************************************
            if (balanza == 0)
            {
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara1"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara2"].Value;
                if (banderaCamaras == true)
                {
                    nom_Camara1= cfg.AppSettings.Settings["Nom_Camara1"].Value;
                    nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara2"].Value;
                    RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                    RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
                    if(estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), "", "", "","SP", "", "");


                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, Obtener_ruta1, Obtener_ruta2);
                    return "Las cámaras no identificaron la placa seleccionada, se envió un PIN al correo para continuar con la transacción";
                }
                else
                {
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), "", "", "","SP", "", "");
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, "", "");
                    return "Las camaras: " + IP_Camara1 + " y " + IP_Camara2 + " no se encuentran en la Red, se envió un PIN al correo para continuar con la transacción";
                }


            }
            //****************************** BASCULA 2 ******************************************************
            else
            {
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara3"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara4"].Value;
                if (banderaCamaras == true)
                {
                    nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara3"].Value;
                    nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara4"].Value;
                    RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                    RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), "", "", "","SP", "", "");
                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, Obtener_ruta1, Obtener_ruta2);
                    return "Las cámaras no identificaron la placa seleccionada, se envió un PIN al correo para continuar con la transacción";
                }
                else
                {
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), "", "", "","SP", "", "");
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, "", "");
                    return "Las camaras: " + IP_Camara1 + " y " + IP_Camara2 + " no se encuentran en la Red, se envió un PIN al correo para continuar con la transacción";
                }

            }
        }

        private void SetPesoActual(ref double peso, int nScaleId)
        {

            pesoActualBascula[nScaleId] = peso;
        }

        #endregion






    }
}
