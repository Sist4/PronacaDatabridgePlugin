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
namespace PronacaApi
{
   public class Transacciones : TransactionProcessing
    {
        
        double[] pesoMaximo;
        double peso;
        // Default Constructor
        public Transacciones()
        {
            for (int nScale = 0; nScale < 6; nScale++)
            {
                TransactionNeedsProcessed.Add(nScale, false);
            }
            pesoMaximo = new double[6];
        }

        
        
        #region Propiedades para el peso maximo

        private Dictionary<int, bool> _myTransactionNeedsProcessed = null;
        private Dictionary<int, bool> TransactionNeedsProcessed
        {
            get
            {
                if (_myTransactionNeedsProcessed == null)
                {
                    _myTransactionNeedsProcessed = new Dictionary<int, bool>();
                }

                return _myTransactionNeedsProcessed;
            }
        }

        private Dictionary<int, ScaleWeightPacket> _myMaxScaleWeightData = null;
        private Dictionary<int, ScaleWeightPacket> MaxScaleWeightData
        {
            get
            {
                if (_myMaxScaleWeightData == null)
                {
                    _myMaxScaleWeightData = new Dictionary<int, ScaleWeightPacket>();
                }

                return _myMaxScaleWeightData;
            }
        }
        #endregion
        
        //Antes de completar la transacción(BOTON COMPLETAR)
        public override string TransactionCompleting(int nScaleId, TransactionModel myTransaction)
        {
            GestionVehiculos VEH = new GestionVehiculos();
            string Vehiculo = "";
            string Chofer = "";
                Vehiculo = myTransaction.Loads[0].Vehicle.DisplayDescription;
                //string Material = myTransaction.Loads[0].Material.Description;
                //string Empresa = myTransaction.Loads[0].Account.Description;
                string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
               string Nota = myTransaction.Loads[0].Note;
                string Peso_Salida = myTransaction.Loads[0].Pass2Weight.ToString();
                ////gurdamos la informacion que envia el databridge 
                Chofer = myTransaction.Loads[0].Driver.Name.ToString();
            //1-*****************************Filtro del chofer y biometrico*********************************************************
            //******Tomar encunta que debe estar abierto el progrma ZKAccess3.5 Security System del biometrico  
            //***Consultamos si el chofer existe en el biometrico 
            if (VEH.consulta_ExisteChofer(Chofer) != "")
            {
                // si existe procedemosa verificar si se tomo la lectura en el trascuroso de los 10 minutos
                if (VEH.consulta_BiometricoChofer(Chofer) != "")
                {
                    //pasa el primer filtro del chofer 
                    //2-********************** FILTRO DE LA PLACA AL salir el Vehiculo*******************************************************************   
                    string Ping_Salida = VEH.consulta_PinSalida(N_Transaccion);

                    if (Ping_Salida != "")
                    {
                        //si el vehiculo anteriormente se registro un pin el operador ya debio haber registrado 

                        if (Ping_Salida.Equals(Nota))
                        {
                            // si cumple con el ping se cambia el estado  IC=Ingreso Completado mandamos el update 
                            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), nScaleId.ToString(), Vehiculo, Chofer, "", Peso_Salida, N_Transaccion,"", "", "", "", "SC");
                            return "";
                        }
                        else
                        {
                            return "El Ping Ingresado No Coincide";
                        }



                    }
                    else
                    {

                        //hhacemos ping a la carpeta ftp 

                        Ping HacerPing = new Ping();
                        int iTiempoEspera = 500;
                        PingReply RespuestaPingCamara;
                        PingReply RespuestaPingFTP;
                        string sDireccion;
                        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                        UriBuilder uri = new UriBuilder(codeBase);
                        string path = Uri.UnescapeDataString(uri.Path);
                        Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                        sDireccion = cfg.AppSettings.Settings["IP_Camara"].Value;
                        RespuestaPingCamara = HacerPing.Send(sDireccion, iTiempoEspera);

                        if (RespuestaPingCamara.Status == IPStatus.Success)
                        {

                            //HACEMOS PING A LA CARPETA FTP

                            sDireccion = cfg.AppSettings.Settings["IP_Ftp"].Value;
                            RespuestaPingFTP = HacerPing.Send(sDireccion, iTiempoEspera);

                            if (RespuestaPingFTP.Status == IPStatus.Success)
                            {

                                //si la trasaccion va ser la primera vez que se verifica 
                                //realizamos la conexion FTP Y Revisamos que exista un archivo en el trascurso de entre la hora actual menos 10 minutos con la comparacion de la placa 
                                if (VEH.listarFTP(Vehiculo).Equals(""))
                                {
                                    ventanaNueva("se empezó con la lectura del sftp");
                                    return "";
                                }
                                else
                                {
                                    
                                                    //la informacion no coincide o no se tomo la foto se genera un pin y se lo envia 

                                    var seed = Environment.TickCount;
                                    var random = new Random(seed);
                                    var Pin = random.Next(999, 9999);
                                    String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, "", Peso_Salida, N_Transaccion, "", Pin.ToString(), "", "", "SP");
                                    string Obtener_ruta = CreateTransaction(1, 1, Vehiculo);
                                    if (Obtener_ruta.Equals(""))
                                    {
                                        //si la imagen no se creo
                                        string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                                    }
                                    else
                                    {
                                        //si la imagen se creo se adjunta en el correo electronico 
                                        string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, @"C:\CAMARA\" + Obtener_ruta + ".jpg");

                                    }

                                    return "La Placa Seleccionada no se encuentra en los registros de la Camara se enviará un Pin para que siga con la Transaccion";

                                }
                            }
                            else
                            {
                                var seed = Environment.TickCount;
                                var random = new Random(seed);
                                var Pin = random.Next(999, 9999);
                                String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, "", Peso_Salida, N_Transaccion, "", Pin.ToString(), "", "", "SP");
                                string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                                return "La carpeta ftp no esta en red";

                            }
                        }
                        //caso que no haya respuesta de la camara 
                        else
                        {
                            var seed = Environment.TickCount;
                            var random = new Random(seed);
                            var Pin = random.Next(999, 9999);
                            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, "", Peso_Salida, N_Transaccion, "", Pin.ToString(), "", "", "SP");
                                string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                            return "La camara no se encuentra en la Red";

                        }
                    }

                }
                // si el chofer no Timbro o excedio el tiempo predeterminado(10 minutos) 
                else
                {
                    return "El Chofer no a Timbrado en el Biometrico, Recuerde tener abierto el software del Biometrico y que el Tiempo de espera son 2 Minutos";
                }
            }
            // FIN DEL FILTRO BIOMETRICO- CHOFER
            else
            {
                return "El chofer debe estar Creado en el Biometrico";
            }


        }

        //Después de que continúe la transacción.
        public override void TransactionContinued(int nScaleId, TransactionModel myTransaction)
        {

            //base.TransactionContinued(nScaleId, myTransaction);
        }
        // Antes de continuar con la transacción
        public override string TransactionContinuing(int nScaleId, TransactionModel myTransaction)
        {
            return base.TransactionContinuing(nScaleId, myTransaction);
        }
        //BOTON ACEPTAR
        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction)
        {
            GestionVehiculos VEH = new GestionVehiculos();
            string Vehiculo = "";

            string Nota;
            string Chofer = "";
            string Peso_Ing;
            try
            {

                string Material = myTransaction.Loads[0].Material.Description;
                string Empresa = myTransaction.Loads[0].Account.Description;
                string N_Transaccion = myTransaction.Loads[0].TransactionNumber;

                Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString();
                ////gurdamos la informacion que envia el databridge 
                Chofer = myTransaction.Loads[0].Driver.Name.ToString();
                Vehiculo = myTransaction.Loads[0].Vehicle.DisplayDescription;
                Nota = myTransaction.Loads[0].Note;
            }
            catch (Exception ex)
            {
                return "Debe Seleccionar un Chofer";
            }
            //1-*****************************Filtro del chofer y biometrico*********************************************************
            //******Tomar enc euncta que debe estar abierto el progrma ZKAccess3.5 Security System del biometrico  
            //***Consultamos si el chofer existe en el biometrico 
            if (VEH.consulta_ExisteChofer(Chofer) != "")
            {
                // si existe procedemosa verificar si se tomo la lectura en el trascuroso de los 10 minutos
                if (VEH.consulta_BiometricoChofer(Chofer) != "")
                {
                    //pasa el primer filtro del chofer 
                    //2-********************** FILTRO DE LA PLACA AL INGRESO*******************************************************************   
                    string Ping_Ingreso = VEH.consulta_PlacaIngreso(Vehiculo);

                    if (Ping_Ingreso != "")
                    {
                        //si el vehiculo anteriormente se registro un pin el operador ya debio haber registrado 

                        if (Ping_Ingreso.Equals(Nota))
                        {
                            //  String RES = VEH.Gestion_Pesaje(nScaleId.ToString(),"",Vehiculo,Chofer, Peso_Ing,"","","","","","","IC");
                            // si cumple con el ping se cambia el estado  IC=Ingreso Completado mandamos el update 
                            return "";
                        }
                        else
                        {
                            return "El Ping Ingresado No Coincide";
                        }
                    }
                    else
                    {

                        //hhacemos ping a la carpeta ftp 

                        Ping HacerPing = new Ping();
                        int iTiempoEspera = 500;
                        PingReply RespuestaPingCamara;
                        PingReply RespuestaPingFTP;
                        string sDireccion;
                        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                        UriBuilder uri = new UriBuilder(codeBase);
                        string path = Uri.UnescapeDataString(uri.Path);
                        Configuration cfg = ConfigurationManager.OpenExeConfiguration(path);
                        sDireccion = cfg.AppSettings.Settings["IP_Camara"].Value;
                        RespuestaPingCamara = HacerPing.Send(sDireccion, iTiempoEspera);



                        if (RespuestaPingCamara.Status == IPStatus.Success)
                        {
                            //HACEMOS PING A LA CARPETA FTP

                            sDireccion = cfg.AppSettings.Settings["IP_Ftp"].Value;
                            RespuestaPingFTP = HacerPing.Send(sDireccion, iTiempoEspera);


                            if (RespuestaPingFTP.Status == IPStatus.Success)
                            {
                                //si la trasaccion va ser la primera vez que se verifica 
                                //realizamos la conexion FTP Y Revisamos que exista un archivo en el trascurso de entre la hora actual menos 10 minutos con la comparacion de la placa 
                                if (VEH.listarFTP(Vehiculo).Equals(""))
                                {
                                    ventanaNueva("se empezó con la lectura del sftp");
                                    return "";
                                }
                                else
                                {
                                    //                //la informacion no coincide o no se tomo la foto se genera un pin y se lo envia 
                                    var seed = Environment.TickCount;
                                    var random = new Random(seed);
                                    var Pin = random.Next(999, 9999);
                                    String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", "0", Pin.ToString(), "", "", "", "IP");

                                    string Obtener_ruta = CreateTransaction(1, 1, Vehiculo);
                                    if (Obtener_ruta.Equals(""))
                                    {
                                        //si la imagen no se creo
                                        string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                                    }
                                    else
                                    {
                                        //si la imagen se creo se adjunta en el correo electronico 
                                        string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, @"C:\CAMARA\" + Obtener_ruta + ".jpg");

                                    }

                                    //string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                                    return "La Placa Seleccionada no se encuentra en los registros de la Camara, se enviará un Pin para que siga con la Transacción";

                                }

                            }
                            else
                            {
                                //                //la informacion no coincide o no se tomo la foto se genera un pin y se lo envia 
                                var seed = Environment.TickCount;
                                var random = new Random(seed);
                                var Pin = random.Next(999, 9999);
                                String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", "0", Pin.ToString(), "", "", "", "IP");
                                string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                                return "La Carpeta FTP no esta en Red";

                            }



                        }
                        //caso que no haya respuesta de la camra 
                        else
                        {
                            //                //la informacion no coincide o no se tomo la foto se genera un pin y se lo envia 
                            var seed = Environment.TickCount;
                            var random = new Random(seed);
                            var Pin = random.Next(999, 9999);
                            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(), "", Vehiculo, Chofer, Peso_Ing, "", "0", Pin.ToString(), "", "", "", "IP");
                            string envio_correo = VEH.EnvioCorreo("", Pin.ToString(), Vehiculo, "");
                            return "La camara no se encuentra en la Red";

                        }






                    }
                    //para crear el  filtro de la placa consultamos primero si no existe un vehiculo registrado
                    //2-********************** FIN FILTRO DE LA PLACA AL INGRESO*******************************************************************   
                }
                // si el chofer no Timbro o excedio el tiempo predeterminado(10 minutos) 
                else
                {
                    return "El Chofer no ha Timbrado en el Biometrico, Recuerde tener abierto el software del Biometrico y que el Tiempo de espera son 10 Minutos";
                }
            }
            // FIN DEL FILTRO BIOMETRICO- CHOFER
            else
            {
                return "El chofer debe estar Creado en el Biometrico";
            }



        }


        public override void ScaleAboveThreshold(ScaleAboveThresholdEventArgs myEventArgs)
        {
            lock (TransactionNeedsProcessed)
            {
                TransactionNeedsProcessed[myEventArgs.ScaleId] = true;
            }
        }

        public override void ScaleDataReceived(ScaleWeightPacket myScaleWeightData)
        {
            int nScaleId = myScaleWeightData.ScaleId;
            peso = myScaleWeightData.MainWeightData.GrossWeightValue;

            if (!myScaleWeightData.MainWeightData.NoDataError)
            {
                lock (TransactionNeedsProcessed)
                {
                    if (TransactionNeedsProcessed[nScaleId])
                    {
                        if (pesoMaximo[nScaleId] == 0.00 || myScaleWeightData.MainWeightData.GrossWeightValue > pesoMaximo[nScaleId])
                            pesoMaximo[nScaleId] = myScaleWeightData.MainWeightData.GrossWeightValue;

                    }
                }
            }
        }
        public override void TransactionAccepted(int nScaleId, TransactionModel myTransaction)
        {

            lock (TransactionNeedsProcessed)
            {
                TransactionNeedsProcessed[nScaleId] = false;
                pesoMaximo[nScaleId] = 0.00;

            }
            GestionVehiculos VEH = new GestionVehiculos();
                //DATOS


                string Vehiculo = myTransaction.Loads[0].Vehicle.DisplayDescription;
                string Material = myTransaction.Loads[0].Material.Description;
                string Empresa = myTransaction.Loads[0].Account.Description;
                string N_Transaccion = myTransaction.Loads[0].TransactionNumber;
                string Chofer = myTransaction.Loads[0].Driver.Name.ToString();
                string Nota = myTransaction.Loads[0].Note;
                string Peso_Ing = myTransaction.Loads[0].Pass1Weight.ToString();
                //gurdamos la informacion que envia el databridge 
                            String RES = VEH.Gestion_Pesaje(nScaleId.ToString(),"",Vehiculo,Chofer, Peso_Ing,"",N_Transaccion,"","","","","IC");
                //string respuesta =  //VEH.Insertar_Dato(nScaleId.ToString(), Vehiculo, Chofer, Peso_Ing, N_Transaccion, "A");


                if (RES.Equals("1"))
                {
                    // return "";

                }
                else
                {
                    //   return "No se pudo Guardar la transaccion";
                }

            //   base.TransactionAccepted(nScaleId, myTransaction);
        }
        public override string SettingWeight(int nScaleId, ScaleWeightPacket myScaleWeightData, bool bIsSplitWeight)
        {
            ventanaNueva("peso obteniendose");
            if (myScaleWeightData.MainWeightData.GrossWeightValue < (pesoMaximo[nScaleId]))
            {
                if (myScaleWeightData.MainWeightData.GrossWeightValue >= (pesoMaximo[nScaleId] - 100))
                {
                    return "";
                }
                else
                {
                    return "El peso ha bajado considerablemente, porfavor obtenga denuevo el peso" + "  * peso obtenido: " + myScaleWeightData.MainWeightData.GrossWeightValue +
                       " peso máximo: " + pesoMaximo[nScaleId] + " *";
                }
            }
            else
                return "El conductor no se ha bajado aún, porfavor obtenga denuevo el peso" + "  * peso obtenido: " + myScaleWeightData.MainWeightData.GrossWeightValue +
                       " peso máximo: " + pesoMaximo[nScaleId] + " *";
        }




        //*********************Proceso Para crear la imagen desde el data bridge********************************

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

       // private string  CreateTransaction(int nVehicleId, int nInboundLane,string Placa)
        private string  CreateTransaction(int nVehicleId, int nInboundLane,string Placa)
        {

            string Nombre_Archivo =  Placa  +   DateTime.Now.ToString("yyyyMMddhhmmss");
            try
            {
                TransactionModel myTransaction = TransactionModel.Create();
                myTransaction.SetOperationalData(OperationalDataType.Vehicle, nVehicleId);
                string myCameraName = String.Empty;
                if (nInboundLane == 1)
                {
                    myCameraName = "Cam1";
                }
                else if (nInboundLane == 2)
                {
                    myCameraName = "Cam2";
                }

                CameraModel myCamera = CameraModel.GetByName(myCameraName);
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
                        image.Save(@"C:\CAMARA\"+ Nombre_Archivo + ".jpg", ImageFormat.Jpeg);  // Or Png
                    }


                }
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Failed to create transaction", ex);
                Nombre_Archivo = ""; 
            
            }

            return Nombre_Archivo;

        }

        private void ventanaNueva(String texto)
        {
            try
            {
                // Get singleton
                IWindowManager windowManager = ServiceLocator.GetKernel().Get<IWindowManager>();

                CustomInputDialogViewModel viewModel = new CustomInputDialogViewModel();
                viewModel.CustomWindowTitle = "Custom Data Entry Plugin";
                viewModel.DisplayText = texto;
                viewModel.WatermarkValue = "Enter PO Number";
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
                }
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Error", ex);
            }
        }









    }
}
