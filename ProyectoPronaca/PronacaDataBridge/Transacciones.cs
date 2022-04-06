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
        string operador;
        string vehiculo;
        string pesoObtenido;
        string centro;
        GestionVehiculos VEH;
        bool banderaTransaccionEnviada;
        int contadorCamarasBascula;
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
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            banderaCamaras = false;
            VEH = new GestionVehiculos();
            estado = "";
            pesosObtenidos = new ArrayList();
            banderaTransaccionEnviada = false;
            contadorCamarasBascula = 0;
            operador = "";
            vehiculo = "";
            centro = "";
            pesoObtenido = "N/A";
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
            VEH.eliminarTransaccionPendiente();
        }
        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction) 
        {
            string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value; //tiempo que tiene el chofer para timbrar en el biometrico
            string chofer = myTransaction.Loads[0].Driver.Name; //cédula del chofer que conduce el vehículo 
            string vehiculo = myTransaction.Loads[0].Vehicle.Name; //la placa del vehículo
            operador = myTransaction.Loads[0].Pass1Operator;
            pesoObtenido = myTransaction.Loads[0].GrossWeight.ToString();
            estado = "entrada";
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if (banderaTransaccionEnviada==true)
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
                                        if (VEH.listarFTP(vehiculo, nScaleId).Equals(""))
                                        {
                                            //************************************************COMUNICACION CON EL ARIES *****************************************************
                                            //    ventanaOK("¡Transacción Exitosa!", "DataBridge Plugin");
                                            return AriesEntrada(myTransaction, nScaleId, ref msj_recibido, ref Numeral_recibido);
                                            //return "";
                                        }
                                        else
                                        {
                                        //************************************************NOTIFICACION POR CORREO **************************************************
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
                                    //************************************************NOTIFICACION POR CORREO *****************************************************   
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
        public override void TransactionAccepted(int nScaleId, TransactionModel myTransaction)
        {

            GestionVehiculos VEH = new GestionVehiculos();
            //DATOS
            string Vehiculo = myTransaction.Loads[0].Vehicle.Name;
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            string Chofer = myTransaction.Loads[0].Driver.Name.ToString();
            string Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString();
            string operadorEntrada = myTransaction.Loads[0].Pass1Operator;
            //gurdamos la informacion que envia el databridge 
            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", N_Transaccion, "", "",operadorEntrada,"","", "", "", "IC", msj_recibido, Numeral_recibido);
            VEH.InsertarPesosObtenidos(pesosObtenidos, N_Transaccion);
            pesosObtenidos.Clear();
            banderaTransaccionEnviada = false;
            contadorCamarasBascula = 0;
            pesoObtenido = "N/A";

        }
        public override string TransactionCompleting(int nScaleId, TransactionModel myTransaction)
        {
           vehiculo = myTransaction.Loads[0].Vehicle.Name;
           string Peso_Salida = myTransaction.Loads[0].Pass2Weight.ToString();
           string chofer = myTransaction.Loads[0].Driver.Name.ToString();
           string N_Transaccion = myTransaction.Loads[0].TransactionNumber; 
           string T_pesaje = VEH.consulta_TipoIngreso(N_Transaccion);
           string T_Chofer = cfg.AppSettings.Settings["T_Chofer"].Value;
            operador = myTransaction.Loads[0].Pass2Operator;
            pesoObtenido = myTransaction.Loads[0].GrossWeight.ToString();
            estado = "salida";
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if (banderaTransaccionEnviada==true)
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
                                    String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", "","",operador, "", "", "", "SC", "", "");
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
                                    
                                    if (VEH.listarFTP(vehiculo, nScaleId).Equals(""))
                                    {
                                        String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), vehiculo, chofer, "", Peso_Salida, N_Transaccion, "", "","",operador, "", "", "", "SC", "", "");

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
        public override void TransactionCompleted(int nScaleId, TransactionModel myTransaction)
        {
            string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
            VEH.InsertarPesosObtenidos(pesosObtenidos, N_Transaccion);
            pesosObtenidos.Clear();
            banderaTransaccionEnviada = false;
            VEH.actualizarEstadoSalida(myTransaction.TransactionNumber);

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
        public override void IOStopped(int nScaleId, IOStoppedEventArgs args)
        {
            double peso = pesoActualBascula[nScaleId];
            string razon = "";
            do
            {
                razon = ventanaImput("¡Se detuvo la secuencia!", "DataBridge Plugin", "ingrese la Razón");
            } while (razon == "");
            VEH.detenerSecuencia(VEH.obtenerOperador(), razon, nScaleId,pesoObtenido,peso.ToString()) ;
            NotificacionCorreoSecuencia(razon,VEH.obtenerOperador(), nScaleId,pesoObtenido,peso.ToString());
        }
        public override void InputsSignaled(int nScaleId, InputsSignaledEventArgs args)
        {

        }
        public override void TransactionCleared(int nScaleId, TransactionModel myTransaction)
        {
            VEH.eliminarTransaccionPendiente();
        }
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
                res_Aries = VEH.InvokeService(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer,centro);
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
                    int num2 = rec_mensaje[1].IndexOf('.');
                    string mensaje2 = rec_mensaje[1].Substring(num2 + 1, rec_mensaje[1].Length - (num2 + 1));
                    return mensaje2;

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
                    int num4 = rec_mensaje[1].IndexOf('.');
                    string mensaje4 = rec_mensaje[1].Substring(num4 + 1, rec_mensaje[1].Length - (num4 + 1));
                    return mensaje4;
                case "5":
                    // Error del factor de conversion(aborta el pesaje)
                    int num5 = rec_mensaje[1].IndexOf('.');
                    string mensaje5 = rec_mensaje[1].Substring(num5 + 1, rec_mensaje[1].Length - (num5 + 1));
                    return mensaje5;

                default:
                    return "Sin respuesta de Aries en la transacción de entrada. Presione el botón Aceptar e intente denuevo.";
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
                string res_Aries = VEH.InvokeService(N_Transaccion, FechaTicketProceso, HoraTicketProceso, UsuarioDataBridge, NumeroBascula, TipoPeso, Peso_Ing, Vehiculo, Cedula, Chofer,centro);
                string[] rec_mensaje = res_Aries.Split('/');
                switch (rec_mensaje[0])
                {

                    case "2":
                        // ERROR
                        int num2 = rec_mensaje[1].IndexOf('.');
                        string mensaje2 = rec_mensaje[1].Substring(num2 + 1, rec_mensaje[1].Length - (num2 + 1));
                        return mensaje2;

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
                        int num4 = rec_mensaje[1].IndexOf('.');
                        string mensaje4 = rec_mensaje[1].Substring(num4 + 1, rec_mensaje[1].Length - (num4 + 1));
                        return mensaje4;
                    case "5":
                        // Error del factor de conversion(aborta el pesaje)
                        int num5 = rec_mensaje[1].IndexOf('.');
                        string mensaje5 = rec_mensaje[1].Substring(num5 + 1, rec_mensaje[1].Length - (num5 + 1));
                        return mensaje5;

                    default:
                        return "Sin respuesta de Aries en la transacción de salida. Presione el botón Completar e intente denuevo"; 
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
                    if(estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "",operadorEntrada,operadorSalida,"", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(), operadorEntrada,operadorSalida,"", "", "","SP", "", "");


                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    VEH.escribirImagen(Obtener_ruta1, pesoBascula);
                    VEH.escribirImagen(Obtener_ruta2, pesoBascula);
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
                    if (estado.Equals("entrada"))
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, peso_Ing, "", "0", Pin.ToString(), "",operadorEntrada,operadorSalida, "", "", "","IP", msj_recibido, Numeral_recibido);
                    else
                        RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", vehiculo, chofer, "", peso_Salida, N_Transaccion2, "", Pin.ToString(),operadorEntrada,operadorSalida, "", "", "","SP", "", "");
                    string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
                    string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
                    VEH.escribirImagen(Obtener_ruta1, pesoBascula);
                    VEH.escribirImagen(Obtener_ruta2, pesoBascula);
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

        private void NotificacionCorreoSecuencia(string razon, string operador,int bascula,string pesoObtenido,string pesoBascula)
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
            
            string Obtener_ruta1 = ObtenerImagen(vehiculo, nom_Camara1, RespuestaPingCamara1);
            string Obtener_ruta2 = ObtenerImagen(vehiculo, nom_Camara2, RespuestaPingCamara2);
            VEH.escribirImagen(Obtener_ruta1,pesoBascula);
            VEH.escribirImagen(Obtener_ruta2, pesoBascula);
            VEH.EnvioCorreoSecuenciaDetenida(razon, operador, Obtener_ruta1, Obtener_ruta2,pesoObtenido,pesoBascula);
        }

        private void SetPesoActual(ref double peso, int nScaleId)
        {

            pesoActualBascula[nScaleId] = peso;
        }

        #endregion






    }
}
