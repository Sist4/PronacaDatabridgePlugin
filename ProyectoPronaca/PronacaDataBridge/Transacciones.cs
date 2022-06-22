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
using DataBridge.Core.TransactionLibrary.WeighingProcessorEvents;

namespace PronacaPlugin
{
   public class Transacciones : TransactionProcessing
    {
        //Variables globales

        string []pesosObtenidos;
        double[] pesoActualBascula;
        double[] pesoObtenido;
        int[] transaccionEnviada;
        bool banderaCamaras;
        string estado;
        string msj_recibido;
        string Numeral_recibido;
        string operador;
        string vehiculo;
        string centro;
        GestionVehiculos VEH;
        //**************Acceso al app config******************//
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        TransactionProcessor myTransactionProcessor;
        Aplicacion aplicacion;
        //Constructor por defecto
        public Transacciones()
        {
            pesoActualBascula = new double[2];
            pesoObtenido = new double[2];
            transaccionEnviada = new int[2];
            pesosObtenidos = new string[2];
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            banderaCamaras = false;
            VEH = new GestionVehiculos();
            estado = "";
            operador = "";
            vehiculo = "";
            centro = "";
            pesoObtenido[1] = 0;
            pesoObtenido[0] = 0;
            transaccionEnviada[0] = 5;
            transaccionEnviada[1] = 5;
            pesosObtenidos[0] = "";
            pesosObtenidos[1] = "";
            aplicacion = new Aplicacion();
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
        }
        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction) 
        {
            try
            {
                return validarTicketEntrada(nScaleId, myTransaction);
            }
            catch (Exception ex)
            {
                return ex.Message;
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
            string operadorEntrada = myTransaction.Loads[0].Pass1Operator;
            //DateTime fechaIngreso = (DateTime)myTransaction.Loads[0].Pass1EnteredDate;
            //guardamos la informacion que envia el databridge 
            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", N_Transaccion, "", "",operadorEntrada,"","", "", "", "IC", msj_recibido, Numeral_recibido);
           // VEH.actualizarFechaEntrada(nScaleId, N_Transaccion, fechaIngreso);
            VEH.InsertarPesosObtenidos(pesosObtenidos[nScaleId], N_Transaccion,nScaleId);
            pesosObtenidos[nScaleId] = "";
            //bandera de transacción enviada
            transaccionEnviada[nScaleId] = 5;

            pesoObtenido[0]=0;
            pesoObtenido[1]=0;

        }
        public override string TransactionCompleting(int nScaleId, TransactionModel myTransaction)
        {
            try
            {
                return validarTicketSalida(nScaleId, myTransaction);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public override void TransactionCompleted(int nScaleId, TransactionModel myTransaction)
        {
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            string pesoSalida = Convert.ToString(myTransaction.Loads[0].Pass2Weight);
            //DateTime fechaSalida = (DateTime)myTransaction.Loads[0].Pass2EnteredDate;
            VEH.InsertarPesosObtenidos(pesosObtenidos[nScaleId], N_Transaccion,nScaleId);
            pesosObtenidos[nScaleId] = "";
            //bendera de transacción enviada
            transaccionEnviada[nScaleId] = 5;

            if (VEH.estatusRecibidoAries(N_Transaccion).Equals("3"))
            {
                VEH.actualizarEstadoSalida(myTransaction.TransactionNumber, "Transacción de Salida exitosa", "3", nScaleId,pesoSalida);
            }


        }
        public override void ScaleAboveThreshold(ScaleAboveThresholdEventArgs myEventArgs)
        {
            //bandera de transacción enviada
            transaccionEnviada[myEventArgs.ScaleId] = 5;

            VEH.actualizarEstadoPendienteEntrada(myEventArgs.ScaleId);
        }
        public override void ScaleBelowNextPassThreshold(ScaleBelowThresholdEventArgs myEventArgs)
        {
            //bandera de transacción enviada
            transaccionEnviada[myEventArgs.ScaleId] = 5;

            VEH.actualizarEstadoPendienteEntrada(myEventArgs.ScaleId);
            pesosObtenidos[myEventArgs.ScaleId] = "";
        }
        public override void ScaleDataReceived(ScaleWeightPacket myScaleWeightData)

        {
            double peso = myScaleWeightData.MainWeightData.GrossWeightValue;
            SetPesoActual(ref peso,myScaleWeightData.ScaleId);  
        }
        public override void WeightSet(int nScaleId, ScaleWeightPacket myScaleWeightData, bool bIsSplitWeight)
        {
            string peso = myScaleWeightData.MainWeightData.GrossWeightValue.ToString();
            pesosObtenidos[nScaleId] = peso + ";";
        }
        public override string SettingWeight(int nScaleId, ScaleWeightPacket myScaleWeightData, bool bIsSplitWeight)
        {
            bool bConnected = ScaleMgr.IsIOConnected(nScaleId);
            if (!bConnected)
            {
                return "No se puede obtener el peso con la secuencia detenida, asegúrese que no haya ningún peso en la balanza y que se inició la secuencia(botón en color verde)";
            }
            else
            {
                return "";
            }
           // return "";
        }
        public override void IOStopped(int nScaleId, IOStoppedEventArgs args)
        {
            double peso = pesoActualBascula[nScaleId];
            string operador = VEH.obtenerOperador();
            //bandera de transacción enviada
            transaccionEnviada[nScaleId] = 5;
            string ruta1 = "";
            string ruta2 = "";
            NotificacionCorreoSecuencia(nScaleId, peso.ToString(),ref ruta1,ref ruta2);
            string razon = "";
            do
            {
                razon = ventanaImput("¡Se detuvo la secuencia!", "DataBridge Plugin", "ingrese la Razón");
            } while (razon == "");
            VEH.detenerSecuencia(VEH.obtenerOperador(), razon, nScaleId,pesoObtenido[nScaleId].ToString(),peso.ToString()) ;
            VEH.EnvioCorreoSecuenciaDetenida(razon, operador, ruta1, ruta2, pesoObtenido[nScaleId].ToString(), peso.ToString());
            VEH.actualizarEstadoPendienteEntrada(nScaleId);
        }

        //public override string SettingOperationalData(int nScaleId, TransactionModel myTransaction, OperationalDataType myOperationalDataType, BaseOperationalDataModel myOperationalData)
        //{
            
        //    return "";
        //}

        #endregion

        #region Métodos privados
        private string ventanaImput(String texto,String titulo,String cajaTexto)
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
        private bool ventanaOKCancel(string texto, String titulo)
        {
            bool? bResult = null;
            bool actualBool = false;
            try
            {
                IWindowManager windowManager = ServiceLocator.GetKernel().Get<IWindowManager>();
                CustomOkCancelDialogViewModel viewModel = new CustomOkCancelDialogViewModel(texto);
                viewModel.CustomWindowTitle = titulo;
                viewModel.OkButtonText = "SI";
                viewModel.CancelButtonText = "NO";
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    bResult = windowManager.ShowDialog(viewModel);
                });
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Error", ex);
            }
            actualBool = bResult.GetValueOrDefault();
            return actualBool;
        }

        private string ObtenerImagen(string Placa,string Nom_Camara, PingReply estado)
        {
            string directorio = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Camaras\";
            string Nombre_Archivo = "";

            if (estado.Status == IPStatus.Success)
            {
                if(Placa.Equals(string.Empty))
                {
                    Nombre_Archivo = Nom_Camara + "_" + DateTime.Now.ToString("yyyyMMddhhmmss")+"_"+"noplaca";
                }
                else
                {
                    Nombre_Archivo = Nom_Camara + "_"+ DateTime.Now.ToString("yyyyMMddhhmmss") + "_"+Placa;
                }
                
                try
                {
                    CameraModel myCamera = CameraModel.GetByName(Nom_Camara);
                    int CameraId = myCamera.Id;
                    int ImageWidth = 800;
                    int ImageHeight = 600;
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
                    if(!Directory.Exists(directorio))
                        {
                            Directory.CreateDirectory(directorio);
                            using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(myImageAsBytes)))
                            {
                                image.Save(directorio + Nombre_Archivo + ".jpg", ImageFormat.Jpeg);  // Or Png
                            }
                        }
                    else
                        {
                            using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(myImageAsBytes)))
                            {
                                image.Save(directorio+ Nombre_Archivo + ".jpg", ImageFormat.Jpeg);  // Or Png
                            }
                        }
                       


                    }
                    else
                    {
                        Nombre_Archivo = "";
                    }
                }
                catch (Exception ex)
                {
                    //throw new ExcepcionNegocio("Error en la captura de la imagen");
                    ventanaOK("Error en la captura de la imagen de la cámara "+Nom_Camara,"DataBridge Plugin");
                    return "";
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
            string HoraTicketProceso = DateTime.Now.ToString("HH:MM"); //Hora de la transacción
            string UsuarioDataBridge = myTransaction.Loads[0].Pass1Operator; //la persona que esté operando la báscula
            string NumeroBascula = nScaleId.ToString(); //identificador de báscula | báscula 0 es la primera báscula | báscula 1 es la segunda báscula |
            string TipoPeso = "E"; //E=Entrada S=Salida
            string Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString(); //Peso de entrada
            string Cedula = myTransaction.Loads[0].Driver.Name.ToString(); //Cédula del chofer
            string Chofer = myTransaction.Loads[0].Driver.Description.ToString(); //cédula del chofer
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name; //placa del vehículo
            string res_Aries = "";
            string mensajeAries = "";
            int estatusAries = 0;
            try
            {
                VEH.InvokeService2(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer,centro,ref mensajeAries,ref estatusAries);
            }
            catch(Exception ex)
            {
                throw;
            }
            //string[] rec_mensaje = res_Aries.Split('/');
           // switch (rec_mensaje[0])
           switch(estatusAries)
            {
                case 0:
                    return "Sin respuesta de Aries en la transacción de entrada. Presione el botón Aceptar e intente denuevo.";
                case 1:
                    return " Error en respuesta de Aries: "+mensajeAries;
                case 2:
                    // ERROR
                    return "ARIES Caso 2: "+ mensajeAries;
                case 3:
                    msj_recibido = "Transacción de Entrada exitosa";
                    Numeral_recibido = "3";
                    transaccionEnviada[nScaleId]=1;
                    ventanaOK("¡Transacción de Entrada exitosa!","DataBridge Plugin");
                    return "";
                //break;
                case 4:
                    msj_recibido = mensajeAries;
                    Numeral_recibido = "4";
                    return "ARIES Caso 4: "+mensajeAries;
                case 5:
                    // Error del factor de conversion(aborta el pesaje)
                    return "ARIES Caso 5: "+mensajeAries;

                default:
                    return "Sin respuesta de Aries en la transacción de entrada. Presione el botón Aceptar e intente denuevo.";
                    //  break;
            }
        }

        private string AriesSalida(TransactionModel myTransaction, int nScaleId,ref string msj_recibido, ref string Numeral_recibido)
        {
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber; //número de transacción
            string FechaTicketProceso = DateTime.Now.ToString("dd/MM/yyyy");// Formato es: MM/dd/YYYY
            string HoraTicketProceso = DateTime.Now.ToString("HH:MM"); //Hora de la transacción
            string UsuarioDataBridge = myTransaction.Loads[0].Pass1Operator; //la persona que esté operando la báscula
            string NumeroBascula = nScaleId.ToString(); //identificador de báscula | báscula 0 es la primera báscula | báscula 1 es la segunda báscula |
            string TipoPeso = "S"; //E=Entrada S=Salida
            string Peso_Ing = myTransaction.Loads[0].Pass2Weight.ToString(); //Peso de salida
            string Cedula = myTransaction.Loads[0].Driver.Name.ToString(); //Cédula del chofer
            string Chofer = myTransaction.Loads[0].Driver.Description.ToString(); //cédula del chofer
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name; //placa del vehículo
            string T_pesaje = VEH.consulta_TipoIngreso(N_Transaccion);
            string mensajeAries = "";
            int estatusAries = 0;
            //string res_Aries="";
            VEH.InvokeService2(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer,centro, ref mensajeAries, ref estatusAries);
            //string[] rec_mensaje = res_Aries.Split('/');
            //switch (rec_mensaje[0])
            switch(estatusAries)
            {
                case 0:
                    return "Sin respuesta de Aries en la transacción de Salida. Presione el botón Completar e intente denuevo.";
                case 1:
                    return " Error en respuesta de Aries: " + mensajeAries;
                case 2:
                    // ERROR
                    return "ARIES Caso 2: " + mensajeAries;
                case 3:
                    msj_recibido = "Transacción de Salida exitosa";
                    Numeral_recibido = "3";
                    transaccionEnviada[nScaleId] = 1;
                    ventanaOK("¡Transacción de Salida exitosa!", "DataBridge Plugin");
                    return "";
                //break;
                case 4:
                    msj_recibido = mensajeAries;
                    Numeral_recibido = "4";
                    return "ARIES Caso 4: " + mensajeAries;
                case 5:
                    // Error del factor de conversion(aborta el pesaje)
                    return "ARIES Caso 5: " + mensajeAries;
                default:
                    return "Sin respuesta de Aries en la transacción de salida. Presione el botón Completar e intente denuevo.";
                    //  break;
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
        private bool ComunicacionCamaras2(TransactionModel myTransaction, int nScaleId,bool IPcam1,string IPcam2,string IPcam3,string IPcam4)
        {
            Ping HacerPing = new Ping();
            int iTiempoEspera = 500;
            PingReply RespuestaPingCamara1;
            PingReply RespuestaPingCamara2;
            string IP_Camara1;
            string IP_Camara2;
            int balanza = nScaleId;
            //****************************** BASCULA 1 ******************************************************
            if (balanza == 0)
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
            string operadorEntrada = myTransaction.Loads[0].Pass1Operator;
            string operadorSalida = myTransaction.Loads[0].Pass2Operator;
            int balanza = nScaleId;
            string pesoBascula = pesoActualBascula[nScaleId].ToString();
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
                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    VEH.escribirImagen(Obtener_ruta1, pesoBascula);
                    VEH.escribirImagen(Obtener_ruta2, pesoBascula);
                    if (estado.Equals("entrada"))
                    {
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", operadorEntrada, operadorSalida, "", "", "", "IP", msj_recibido, Numeral_recibido);
                        //actualizarRutasImagenesEntrada(Obtener_ruta1, Obtener_ruta2, N_Transaccion1);
                    }
                        
                    else
                    {
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), operadorEntrada, operadorSalida, "", "", "", "SP", "", "");
                       //actualizarRutasImagenesSalida(Obtener_ruta1, Obtener_ruta2, N_Transaccion2);
                    }        
                    
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, Obtener_ruta1, Obtener_ruta2);
                    return "Las cámaras no identificaron la placa seleccionada, se envió un PIN al correo para continuar con la transacción";
                }
                else
                {
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(),"",operadorEntrada,operadorSalida,"", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), operadorEntrada,operadorSalida, "", "", "","SP", "", "");
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
                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    VEH.escribirImagen(Obtener_ruta1, pesoBascula);
                    VEH.escribirImagen(Obtener_ruta2, pesoBascula);
                    if (estado.Equals("entrada"))
                    {
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "", operadorEntrada, operadorSalida, "", "", "", "IP", msj_recibido, Numeral_recibido);
                        //actualizarRutasImagenesEntrada(Obtener_ruta1, Obtener_ruta2, N_Transaccion1);
                    }
                    else
                    {
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), operadorEntrada, operadorSalida, "", "", "", "SP", "", "");
                        //actualizarRutasImagenesSalida(Obtener_ruta1, Obtener_ruta2, N_Transaccion2);
                    }
                        
                    
                    
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, Obtener_ruta1, Obtener_ruta2);
                    return "Las cámaras no identificaron la placa seleccionada, se envió un PIN al correo para continuar con la transacción";
                }
                else
                {
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "",operadorEntrada,operadorSalida, "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(),operadorEntrada,operadorSalida, "", "", "","SP", "", "");
                    string envio_correo = VEH.EnvioCorreo(N_Transaccion1, Pin.ToString(), vehiculo, "", "");
                    return "Las camaras: " + IP_Camara1 + " y " + IP_Camara2 + " no se encuentran en la Red, se envió un PIN al correo para continuar con la transacción";
                }

            }
        }

        private void NotificacionCorreoSecuencia(int bascula,string pesoBascula,ref string ruta1,ref string ruta2)
        {
            string nom_Camara1;
            string nom_Camara2;
            string IP_Camara1;
            string IP_Camara2;
            PingReply RespuestaPingCamara1;
            PingReply RespuestaPingCamara2;
            int iTiempoEspera = 500;
            Ping HacerPing = new Ping();
            if (bascula==0)
            {
                nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara1"].Value;
                nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara2"].Value;
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara1"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara2"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
            }
            else
            {
                nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara3"].Value;
                nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara4"].Value;
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara3"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara4"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
            }
            
            ruta1= ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
            ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
            VEH.escribirImagen(ruta1,pesoBascula);
            VEH.escribirImagen(ruta2, pesoBascula);
            
        }

        private void SetPesoActual(ref double peso, int nScaleId)
        {

            pesoActualBascula[nScaleId] = peso;
        }

        private void actualizarRutasImagenesEntrada(string img1,string img2,string transaccion)
        {
            string imgEntrada = img1 + "; " + img2;
            VEH.actualizarImagenesPINEntrada(imgEntrada,transaccion);
        }
        private void actualizarRutasImagenesSalida(string img1, string img2, string transaccion)
        {
            string imgSalida = img1 + "; " + img2;
            VEH.actualizarImagenesPINSalida(imgSalida, transaccion);
        }

        private string validarTicketEntrada(int nScaleId, TransactionModel myTransaction)
        {
            string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value; //tiempo que tiene el chofer para timbrar en el biometrico
            string chofer = myTransaction.Loads[0].Driver.Name; //cédula del chofer que conduce el vehículo 
            string vehiculo = myTransaction.Loads[0].Vehicle.Name; //la placa del vehículo
            operador = myTransaction.Loads[0].Pass1Operator; //operador de databridge
            pesoObtenido[nScaleId] = myTransaction.Loads[0].GrossWeight; //peso obtenido en esa báscula
            estado = "entrada";
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if ((nScaleId == 0 && transaccionEnviada[nScaleId] == 1) || (nScaleId == 1 && transaccionEnviada[nScaleId] == 1))
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
                                string Nota = ventanaImput("Ingrese el PIN ", "DataBridge Plugin", "PIN");
                                if (Ping_Ingreso.Equals(Nota))
                                {
                                    //************************************************COMUNICACION CON EL ARIES *****************************************************
                                    //ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                    return AriesEntrada(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                    //return "";          
                                }
                                else
                                {
                                    return "El PIN ingresado no coincide";
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
                                    if (VEH.listarFTP(vehiculo, nScaleId) == true)
                                    {
                                        //************************************************COMUNICACION CON EL ARIES *****************************************************
                                        //    ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                        string RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, Convert.ToString(myTransaction.Loads[0].Pass1Weight), "", VEH.consulta_TransaccionDB(), "", "", myTransaction.Loads[0].Pass1Operator, "", "", "", "", "IP", msj_recibido, Numeral_recibido);
                                        return AriesEntrada(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                        //return "";
                                    }
                                    else
                                    {
                                        //************************************************NOTIFICACION POR CORREO **************************************************
                                        if (ventanaOKCancel("¿La báscula escogida para la transacción es la correcta?", "DataBridge Plugin"))
                                        {
                                            return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                        }
                                        else
                                        {
                                            return "Porfavor seleccione la báscula correcta y continue la transacción";
                                        }
                                    }



                                }
                                else
                                {
                                    //************************************************NOTIFICACION POR CORREO *****************************************************   
                                    if (ventanaOKCancel("¿La báscula escogida para la transacción es la correcta?", "DataBridge Plugin"))
                                    {
                                        return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                    }
                                    else
                                    {
                                        return "Porfavor seleccione la báscula correcta y continue la transacción";
                                    }
                                }

                            }

                        }
                        else
                        {
                            return "El peso obtenido no es igual al peso actual en la báscula, vuelva a obtener el peso porfavor";
                        }

                    }
                    // si el chofer no Timbro o excedio el tiempo predeterminado(10 minutos) 
                    else
                    {
                        return "El Conductor no ha Timbrado en el Biometrico. El Tiempo de espera son de " + T_Chofer + " Minutos";
                    }
                }
                // FIN DEL FILTRO BIOMETRICO- CHOFER
                else
                {
                    return "El conductor no está registrado en el Biometrico";
                }
            }
        }
        private string validarTicketSalida(int nScaleId, TransactionModel myTransaction)
        {
            vehiculo = myTransaction.Loads[0].Vehicle.Name;
            string Peso_Salida = myTransaction.Loads[0].Pass2Weight.ToString();
            string chofer = myTransaction.Loads[0].Driver.Name.ToString();
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            string T_pesaje = VEH.consulta_TipoIngreso(N_Transaccion);
            string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value;
            operador = myTransaction.Loads[0].Pass2Operator;
            pesoObtenido[nScaleId] = myTransaction.Loads[0].GrossWeight;
            estado = "salida";
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if ((nScaleId == 0 && transaccionEnviada[nScaleId] == 1) || (nScaleId == 1 && transaccionEnviada[nScaleId] == 1))
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
                                string Nota = ventanaImput("Ingrese el PIN:", "DataBridge Plugin", "PIN");
                                if (Ping_Salida.Equals(Nota))
                                {
                                    String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", Ping_Salida, "", operador, "", "", "", "SP", "", "");
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
                                    if (VEH.listarFTP(vehiculo, nScaleId) == true)
                                    {
                                        String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", "", "", operador, "", "", "", "SP", "", "");

                                        //ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                        return AriesSalida(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                        //return "";
                                    }
                                    else
                                    {
                                        if (ventanaOKCancel("¿La báscula escogida para la transacción es la correcta?", "DataBridge Plugin"))
                                        {
                                            try
                                            {
                                                return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                            }
                                            catch (Exception ex)
                                            {
                                                return ex.Message;
                                            }
                                        }
                                        else
                                        {
                                            return "Porfavor seleccione la báscula correcta y continue la transacción";
                                        }

                                    }
                                }
                                else
                                {
                                    if (ventanaOKCancel("¿La báscula escogida para la transacción es la correcta?", "DataBridge Plugin"))
                                    {
                                        try
                                        {
                                            return NotificacionCorreo(myTransaction, nScaleId, banderaCamaras, estado);
                                        }
                                        catch (Exception ex)
                                        {
                                            return ex.Message;
                                        }
                                    }
                                    else
                                    {
                                        return "Porfavor seleccione la báscula correcta y continue la transacción";
                                    }
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
                        return "El Conductor no ha Timbrado en el Biometrico. El Tiempo de espera son de " + T_Chofer + " Minutos";
                    }
                }
                // FIN DEL FILTRO BIOMETRICO- CHOFER
                else
                {
                    return "El Conductor no está registrado en el Biometrico";
                }
            }
        }

        #endregion






    }
}
