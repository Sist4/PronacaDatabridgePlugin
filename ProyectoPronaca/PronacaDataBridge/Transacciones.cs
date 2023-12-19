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
using DataBridge.Core.Entities;
using Ninject.Planning;
using System.ComponentModel;

namespace PronacaPlugin
{
   public class Transacciones : TransactionProcessing
    {
        //Variables globales
        double[] pesoActualBascula;
        int[] transaccionEnviada;
        string[] conductorEnviado;
        string[] vehiculoEnviado;
        int[] pesoObtenido;
        string[] pesosObtenidos;
        Ticket[] ticket;
        bool banderaCamaras;
        string estado;
        string centro;
        GestionTicket gest;
        //**************Acceso al app config******************//
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        string Dir_Camaras;
        string Dir_Loop;
        //Constructor por defecto
        public Transacciones()
        {
            pesoActualBascula = new double[2];
            transaccionEnviada = new int[2];
            conductorEnviado = new string[2];
            vehiculoEnviado = new string[2];
            pesoObtenido = new int[2];
            pesosObtenidos = new string[2];
            ticket = new Ticket[2];
            ticket[0] = Ticket.GetInstance();
            ticket[1] = Ticket.GetInstance();
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
            banderaCamaras = false;
            gest = new GestionTicket();
            estado = "";
            centro = "";
            transaccionEnviada[0] = 5;
            transaccionEnviada[1] = 5;
            pesoObtenido[0] = 0;
            pesoObtenido[1] = 0;
            Dir_Camaras = cfg.AppSettings.Settings["Dir_Camaras"].Value;
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

        public override void OKToWeighStateChanged(int nScaleId, OKToWeighStateChangedEventArgs args)
        {
            if(args.IsOKToWeigh.Equals(true) && Dir_Camaras.Equals("afuera"))
                gest.ActualizarEstadoVehiculosTerminados(nScaleId, Dir_Camaras, Dir_Loop);
        }
        public override void LoopStateChanged(int nScaleId, LoopStateChangedEventArgs args)
        {
            Dir_Loop = args.LoopType.ToString();
        }
        public override void TransactionVoided(int nScaleId, TransactionModel myTransaction)
        {

            int rowAdded = 0;
            try
            {
                rowAdded = gest.AnularTransacción(myTransaction.TransactionNumber, myTransaction.Loads[0].Vehicle.Name);
                
            }
            catch (Exception ex)
            {
                ventanaOK(ex.Message, "DataBridge Plugin");
            }

        }
        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction) 
        {
            try
            {
                return ValidarTicketEntrada(nScaleId, myTransaction,"E");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public override void TransactionAccepted(int nScaleId, TransactionModel myTransaction)
        {
            bool estadoBasculaEntrada = Boolean.Parse(cfg.AppSettings.Settings["Bascula_Entrada"].Value);
            bool estadoBasculaSalida = Boolean.Parse(cfg.AppSettings.Settings["Bascula_Salida"].Value);
            try
            {
                gest.ComprobarTicketEntrada(myTransaction, 2);
            }
            catch (Exception ex)
            {
                ventanaOK(ex.Message, "DataBridge Plugin");
            }


            //bandera de transacción enviada
            transaccionEnviada[nScaleId] = 5;
            pesoObtenido[nScaleId] = 0;
            pesosObtenidos[nScaleId] = "";

            gest.ActualizarEstadoConductoresTerminados();
            gest.ActualizarEstadoTicketInvalidos(ticket[nScaleId], pesoActualBascula[nScaleId].ToString(), "TransactionAccepted", nScaleId);
            gest.ActualizarEstadoVehiculosTerminados(nScaleId, "dentro", Dir_Loop);
            ticket[nScaleId].Nuevo();


        }
        public override string TransactionCompleting(int nScaleId, TransactionModel myTransaction)
        {
            try
            {
                return ValidarTicketSalida(nScaleId, myTransaction, "S");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public override void TransactionCompleted(int nScaleId, TransactionModel myTransaction)
        {
            bool estadoBasculaEntrada = Boolean.Parse(cfg.AppSettings.Settings["Bascula_Entrada"].Value);
            bool estadoBasculaSalida = Boolean.Parse(cfg.AppSettings.Settings["Bascula_Salida"].Value);
            int rowsAdded = 0;
            try
            {
                gest.ComprobarTicketEntrada(myTransaction, 1);
            }
            catch (Exception ex)
            {
                ventanaOK(ex.Message, "DataBridge Plugin");
            }


            //bandera de transacción enviada
            transaccionEnviada[nScaleId] = 5;
            pesoObtenido[nScaleId] = 0;
            pesosObtenidos[nScaleId] = "";

            gest.ActualizarEstadoConductoresTerminados();
            gest.ActualizarEstadoTicketInvalidos(ticket[nScaleId], pesoActualBascula[nScaleId].ToString(), "TransactionCompleted", nScaleId);
            gest.ActualizarEstadoVehiculosTerminados(nScaleId, "dentro", Dir_Loop);
            ticket[nScaleId].Nuevo();
        }

        public override void ScaleAboveThreshold(ScaleAboveThresholdEventArgs myEventArgs)
        {
            //bandera de transacción enviada
            //transaccionEnviada[myEventArgs.ScaleId] = 5;

            //VEH.actualizarEstadoPendienteEntrada(myEventArgs.ScaleId,ticket[myEventArgs.ScaleId],
            //   pesoActualBascula[myEventArgs.ScaleId].ToString(),"Scale Above Threshold");
        }
        public override void ScaleBelowNextPassThreshold(ScaleBelowThresholdEventArgs myEventArgs)
        {
            //bandera de transacción enviada
            transaccionEnviada[myEventArgs.ScaleId] = 5;
            gest.ActualizarEstadoVehiculosTerminados(myEventArgs.ScaleId,"dentro",Dir_Loop);
            gest.ActualizarEstadoTicketInvalidos(Ticket.GetInstance(), pesoActualBascula[myEventArgs.ScaleId].ToString(), "ScaleBelowNextPassThreshold", myEventArgs.ScaleId);
            /*
            transaccionEnviada[myEventArgs.ScaleId] = 5;
            gest.actualizarEstadoPendienteEntrada(myEventArgs.ScaleId, ticket[myEventArgs.ScaleId],
                pesoActualBascula[myEventArgs.ScaleId].ToString(), "Scale Below Next Pass Threshold");

            pesosObtenidos[myEventArgs.ScaleId] = "";

            */
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
            //return "";
        }
        public override void IOStopped(int nScaleId, IOStoppedEventArgs args)
        {
            Secuencia secuencia = new Secuencia();
            Correo correo = new Correo();
            secuencia.Bascula = nScaleId;
            secuencia.PesoBascula=pesoActualBascula[nScaleId].ToString();
            secuencia.Operador=Log.operadorLog;
            secuencia.PesoObtenido = ticket[nScaleId].PesoEnviado;
            //bandera de transacción enviada
            transaccionEnviada[nScaleId] = 5;
            pesosObtenidos[nScaleId] = "0";
            string nombreImagen1 = "";
            string nombreImagen2 = "";
            NotificacionCorreoSecuencia(nScaleId,secuencia,ref nombreImagen1,ref nombreImagen2);
            string razon = "";
            
            do
            {
                razon = ventanaImput("¡Se detuvo la secuencia!", "DataBridge Plugin", "ingrese la Razón");
                secuencia.Razon = razon;
            } while (razon == "");

            correo.Id = gest.ObtenerSiguienteIdCorreo();
            correo.Fecha = DateTime.Now;
            correo.Tipo = "SECUENCIA";
            correo.Asunto = " Se ha detenido la secuencia de pesaje en DataBridge. ";
            if (gest.EnviarCorreo(correo, nombreImagen1, nombreImagen2,secuencia))
            {
                gest.GuardarCorreo(correo);
                secuencia.IdCorreo = correo.Id;
                gest.GuardarSecuencia(secuencia);
                ventanaOK("Se envió un correo con la información de la detención de la secuencia de pesaje", "DataBridge Plugin");
                //return "Se envió un email al coordinador con un PIN para continuar con la transacción";
            }

            gest.ActualizarEstadoConductoresTerminados();
            gest.ActualizarEstadoTicketInvalidos(ticket[nScaleId], pesoActualBascula[nScaleId].ToString(), "IOStopped", nScaleId);
            gest.ActualizarEstadoVehiculosTerminados(nScaleId,Dir_Camaras,Dir_Loop);

        }
        public override void PhotoeyeStateChanged(int nScaleId, PhotoeyeStateChangedEventArgs args)
        {
            //ventanaOK(args.CurrentState.ToString(), "ventana");
            //if (args.CurrentState.ToString() == "Broken" && transaccionEnviada[nScaleId] == 1 )
            //    ventanaOK("Se interrumpió un Fotosensor. Asegurese de volver a presionar el botón Aceptar o Completar y terminar la transacción", "DataBridge Plugin");
        }
        public override void TransactionStateChanged(int nScaleId, TransactionStateChangedEventArgs args)
        {
            //double peso = pesoActualBascula[nScaleId];
            //string razon = "";
            //transaccionEnviada[nScaleId] = 5;
            //pesosObtenidos[nScaleId] = "";
            //string ruta1 = "";
            //string ruta2 = "";
            //string texto = " Se ha abortado la transacción en DataBridge ";
            //NotificacionCorreoSecuencia(nScaleId, peso.ToString(), ref ruta1, ref ruta2);
            //if (args.CurrentState.ToString().Equals("Aborted"))
            //{
            //    do
            //    {
            //        razon = ventanaImput("¡Transacción abortada!", "DataBridge Plugin", "ingrese la Razón");
            //    } while (razon == "");
            //    VEH.detenerSecuencia(VEH.obtenerOperador(), "Abortado - "+razon, nScaleId, pesoObtenido[nScaleId].ToString(), peso.ToString());
            //    VEH.EnvioCorreoSecuenciaDetenida("Abortado:"+razon,VEH.obtenerOperador(), ruta1, ruta2, pesoObtenido[nScaleId].ToString(), peso.ToString(),texto);
            //    VEH.actualizarEstadoPendienteEntrada(nScaleId, ticket[nScaleId],
            //    pesoActualBascula[nScaleId].ToString(), "IO Aborted");
            //}
                
        }

        public override string SettingOperationalData(int nScaleId, TransactionModel myTransaction, OperationalDataType myOperationalDataType, BaseOperationalDataModel myOperationalData)
        {
            try
            {
                switch (myOperationalDataType)
                {
                    case OperationalDataType.Vehicle:
                        ventanaOK("Las placas leídas por las cámaras son: " + gest.listarPlacasLeidas(nScaleId,Dir_Camaras,Dir_Loop), "DataBridge Plugin");
                        return "";

                    default:
                        return "";

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
              
        }
        public override void TransactionLoaded(int nScaleId, TransactionModel myTransaction)
        {
            if(myTransaction.Status.ToString().Equals("Incomplete"))
                ventanaOK("Las placas leídas por las cámaras son: " + gest.listarPlacasLeidas(nScaleId,Dir_Camaras,Dir_Loop), "DataBridge Plugin");
        }

        public override string OutputsSignaling(int nScaleId, OutputsSignaledEventArgs args)
        {
            //args.
            return "";
        }
        #endregion

        #region Métodos privados
       
        private string EnviarAries(TransactionModel myTransaction, int nScaleId, Ticket ticket)
        {
            ticket.Fecha = DateTime.Now;
            string NombreConductor = myTransaction.Loads[0].Driver.Description.ToString(); //nombre del chofer
            string mensajeAries = "";
            int estatusAries = 0;
            ticket.PesosObtenidos = pesosObtenidos[nScaleId];
            gest.GuardarTicket(ticket);
            try
            {
                //gest.InvocarServicio(ticket, centro, NombreConductor, ref mensajeAries, ref estatusAries);
                estatusAries = 3;
                mensajeAries = "EXITO";
            }
            catch (Exception ex)
            {
                throw;
            }
            //string[] rec_mensaje = res_Aries.Split('/');
            // switch (rec_mensaje[0])
            switch (estatusAries)
            {
                case 0:
                    return "Sin respuesta de Aries en la transacción. Presione el botón Aceptar/Completar e intente de nuevo.";
                case 1:
                    return " Error en respuesta de Aries: " + mensajeAries;
                case 2:
                    // ERROR
                    return "ARIES Caso 2: " + mensajeAries;
                case 3:
                    transaccionEnviada[nScaleId] = 1;
                    vehiculoEnviado[nScaleId] = ticket.PlacaVehiculo;
                    conductorEnviado[nScaleId] = ticket.CedulaConductor;
                    if(ticket.Tipo.Equals("E"))
                        ventanaOK("¡Transacción de Entrada exitosa!", "DataBridge Plugin");
                    else
                        ventanaOK("¡Transacción de Salida exitosa!", "DataBridge Plugin");
                    ticket.Estado = "T";
                    gest.GuardarTicket(ticket);
                    return "";
                //break;
                case 4:
                    return "ARIES Caso 4: " + mensajeAries;
                case 5:
                    // Error del factor de conversion(aborta el pesaje)
                    return "ARIES Caso 5: " + mensajeAries;

                default:
                    return "Sin respuesta de Aries en la transacción. Presione el botón Aceptar/Completar e intente de nuevo.";
                    //  break;
            }
        }
        private string ValidarTicketEntrada(int nScaleId, TransactionModel myTransaction, string tipo)
        {
            ticket[nScaleId] = llenarTicket(nScaleId, myTransaction, tipo);
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if (transaccionEnviada[nScaleId] == 1)
            {
                if (conductorEnviado[nScaleId] == myTransaction.Loads[0].Driver.Name && vehiculoEnviado[nScaleId] == myTransaction.Loads[0].Vehicle.Name)
                    return "";
                else
                    return "No puede terminar la transacción en DataBridge con datos diferentes a los enviados a Aries. Seleccione el conductor y el vehículo previamente seleccionados.";
            }
            else
            {

                if (gest.ExisteConductor(myTransaction.Loads[0].Driver.Name))
                {
                    if (gest.ExisteTimbrado(myTransaction.Loads[0].Driver.Name))
                    {
                        if (myTransaction.Loads[0].Pass1Weight >= (pesoActualBascula[nScaleId] - 10) && myTransaction.Loads[0].Pass1Weight <= (pesoActualBascula[nScaleId] + 10))
                        {
                            string PinConsultado = gest.ObtenerPIN(myTransaction.Loads[0].Vehicle.Name);
                            if (!PinConsultado.Equals(string.Empty))
                            {
                                string PinIngresado = ventanaImput("Ingrese el PIN ", "DataBridge Plugin", "PIN");
                                if (PinIngresado.Equals(PinConsultado))
                                {
                                    string idTicketPendiente = gest.ObtenerIdTicketPendientePIN(myTransaction.Loads[0].Vehicle.Name);
                                    ticket[nScaleId].Id = idTicketPendiente;
                                    return EnviarAries(myTransaction, nScaleId, ticket[nScaleId]);
                                }
                                else
                                {
                                    return "El PIN ingresado no coincide";
                                }
                            }
                            else
                            {
                                if (gest.ValidarVehiculo(myTransaction.Loads[0].Vehicle.Name, nScaleId, Dir_Camaras, Dir_Loop))
                                {
                                    return EnviarAries(myTransaction, nScaleId, ticket[nScaleId]);
                                }
                                else
                                {
                                    int bascula = nScaleId == 0 ? 1 : 0;
                                    if (gest.ValidarVehiculo(myTransaction.Loads[0].Vehicle.Name, bascula, Dir_Camaras, Dir_Loop))
                                    {
                                        return "La placa seleccionada se encuentra en la otra báscula";
                                    }
                                    else
                                    {
                                        if (ventanaOKCancel("Las placas no coinciden, verifique que la placa y báscula seleccionadas sean las correctas. " +
                                            "¿Desea enviar un PIN para continuar con la transacción?", "DataBridge Plugin"))
                                        {
                                            return NotificarPIN(nScaleId, ticket[nScaleId]);

                                        }
                                        else
                                        {
                                            return "Porfavor vuelva a presionar el botón Aceptar/Completar para proseguir con la transacción";
                                        }

                                    }
                                }

                            }
                        }
                        else
                        {
                            return "El peso obtenido no es igual al peso actual en la báscula, vuelva a obtener el peso porfavor";
                        }
                    }
                    else
                    {
                        return "El conductor " + myTransaction.Loads[0].Driver.Description + " no ha timbrado en el biométrico";
                    }
                }
                else
                {
                    return "El conductor " + myTransaction.Loads[0].Driver.Description + " no está creado en el biométrico";
                }




            }
        }
        private string ValidarTicketSalida(int nScaleId, TransactionModel myTransaction, string tipo)
        {
            ticket[nScaleId] = llenarTicket(nScaleId, myTransaction, tipo);
            ticket[nScaleId].Tipo = tipo;
            LoadModel myLoad = myTransaction.FirstLoad;
            foreach (LoadCDEModel myLoadCDE in myLoad.LoadCDEs)
            {
                if (String.Compare(myLoadCDE.CustomDataEntry.CustomDataCollection.Name, "Centro", true) == 0)
                {
                    centro = myLoadCDE.CustomDataEntry.Name;
                }
            }
            if (transaccionEnviada[nScaleId] == 1)
            {
                if (conductorEnviado[nScaleId] == myTransaction.Loads[0].Driver.Name && vehiculoEnviado[nScaleId] == myTransaction.Loads[0].Vehicle.Name)
                    return "";
                else
                    return "No puede terminar la transacción en DataBridge con datos diferentes a los enviados a Aries. Seleccione el conductor y el vehículo previamente seleccionados.";
            }
            else
            {
                if (gest.ExisteConductor(myTransaction.Loads[0].Driver.Name))
                {
                    if (gest.ExisteTimbrado(myTransaction.Loads[0].Driver.Name))
                    {
                        if (myTransaction.Loads[0].Pass2Weight >= (pesoActualBascula[nScaleId] - 10) && myTransaction.Loads[0].Pass2Weight <= (pesoActualBascula[nScaleId] + 10))
                        {
                            string PinConsultado = gest.ObtenerPIN(myTransaction.Loads[0].Vehicle.Name);
                            if (!PinConsultado.Equals(string.Empty))
                            {
                                string PinIngresado = ventanaImput("Ingrese el PIN ", "DataBridge Plugin", "PIN");
                                if (PinIngresado.Equals(PinConsultado))
                                {
                                    string idTicketPendiente = gest.ObtenerIdTicketPendientePIN(myTransaction.Loads[0].Vehicle.Name);
                                    ticket[nScaleId].Id = idTicketPendiente;
                                    return EnviarAries(myTransaction, nScaleId, ticket[nScaleId]);
                                }
                                else
                                {
                                    return "El PIN ingresado no coincide";
                                }
                            }
                            else
                            {
                                if (gest.ValidarVehiculo(myTransaction.Loads[0].Vehicle.Name, nScaleId, Dir_Camaras, Dir_Loop))
                                {
                                    return EnviarAries(myTransaction, nScaleId, ticket[nScaleId]);
                                }
                                else
                                {
                                    int bascula = nScaleId == 0 ? 1 : 0;
                                    if (gest.ValidarVehiculo(myTransaction.Loads[0].Vehicle.Name, bascula, Dir_Camaras, Dir_Loop))
                                    {
                                        return "La placa seleccionada se encuentra en la otra báscula";
                                    }
                                    else
                                    {
                                        if (ventanaOKCancel("Las placas no coinciden, verifique que la placa y báscula seleccionadas sean las correctas. " +
                                            "¿Desea enviar un PIN para continuar con la transacción?", "DataBridge Plugin"))
                                        {
                                            return NotificarPIN(nScaleId, ticket[nScaleId]);
                                        }
                                        else
                                        {
                                            return "Porfavor vuelva a presionar el botón Aceptar/Completar para proseguir con la transacción";
                                        }

                                    }
                                }

                            }
                        }
                        else
                        {
                            return "El peso obtenido no es igual al peso actual en la báscula, vuelva a obtener el peso porfavor";
                        }
                    }
                    else
                    {
                        return "El conductor " + myTransaction.Loads[0].Driver.Description + " no ha timbrado en el biométrico";
                    }
                }
                else
                {
                    return "El conductor " + myTransaction.Loads[0].Driver.Description + " no está creado en el biométrico";
                }




            }
        }
        private Ticket llenarTicket(int nScaleId, TransactionModel myTransaction,string tipo)
        {
            Ticket ticket = Ticket.GetInstance();
            ticket.Nuevo();
            if (!gest.ObtenerIdTicketPendiente(myTransaction.Loads[0].Vehicle.Name, tipo).Equals(string.Empty))
                ticket.Id = gest.ObtenerIdTicketPendiente(myTransaction.Loads[0].Vehicle.Name, tipo);
            else
                ticket.Id = gest.ObtenerSiguienteIdTicket();

            ticket.Bascula = nScaleId;
            ticket.PlacaVehiculo = myTransaction.Loads[0].Vehicle.Name;
            ticket.CedulaConductor = myTransaction.Loads[0].Driver.Name;
            ticket.Tipo = tipo;
            if(tipo.Equals("E"))
            {
                ticket.Numero = Convert.ToInt32(gest.consulta_TransaccionDB());
                ticket.PesoEnviado = myTransaction.Loads[0].Pass1Weight.ToString();
                ticket.Operador = myTransaction.Loads[0].Pass1Operator;
            }
            if(tipo.Equals("S"))
            {
                ticket.Numero = Convert.ToInt32(myTransaction.TransactionNumber);
                ticket.PesoEnviado = myTransaction.Loads[0].Pass2Weight.ToString();
                ticket.Operador = myTransaction.Loads[0].Pass2Operator;
            }
            ticket.Estado = "P";
            return ticket;
        }
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
        private string ObtenerImagen(string Placa,string Nom_Camara,string IP_Camara)
        {
            string directorio = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Camaras\";
            string Nombre_Archivo = "";
            Ping HacerPing = new Ping();
            int iTiempoEspera = 500;
            PingReply RespuestaPingCamara= HacerPing.Send(IP_Camara, iTiempoEspera);

            if (RespuestaPingCamara.Status== IPStatus.Success)
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
                ventanaOK("Error en la captura de la imagen de la cámara " + Nom_Camara, "DataBridge Plugin");
                return "";
            }

        }
        public string GenerarPIN()
        {
            var seed = Environment.TickCount;
            var random = new Random(seed);
            var Pin = random.Next(999, 9999);
            return Pin.ToString();
        }
        public string NotificarPIN(int nScaleId,Ticket ticket)
        {
          Correo correo;
          string IP_Camara1;
          string IP_Camara2;
          string nom_Camara1;
          string nom_Camara2;
          string pesoBascula = pesoActualBascula[nScaleId].ToString();
          if (nScaleId == 0)
          {
              IP_Camara1 = cfg.AppSettings.Settings["IP_Camara13"].Value;
              IP_Camara2 = cfg.AppSettings.Settings["IP_Camara14"].Value;
              nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara13"].Value;
              nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara14"].Value;
          }
          else
          {
              IP_Camara1 = cfg.AppSettings.Settings["IP_Camara23"].Value;
              IP_Camara2 = cfg.AppSettings.Settings["IP_Camara24"].Value;
              nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara23"].Value;
              nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara24"].Value;
          }

          string nombreImagen1 = ObtenerImagen(ticket.PlacaVehiculo, nom_Camara1, IP_Camara1);
          string nombreImagen2 = ObtenerImagen(ticket.PlacaVehiculo, nom_Camara2, IP_Camara2);
          gest.escribirImagen(nombreImagen1, pesoBascula);
          gest.escribirImagen(nombreImagen2, pesoBascula);
          correo = new Correo();
            correo.Id = gest.ObtenerSiguienteIdCorreo();
            correo.Fecha = DateTime.Now;
            correo.Pin = GenerarPIN();
            correo.Placa = ticket.PlacaVehiculo;
            correo.Tipo = "PIN";
            correo.Asunto = "Las cámaras no identificaron la placa seleccionada, para proseguir con la transacción digite el PIN en el sistema de pesaje DataBridge.";
            if (gest.EnviarCorreo(correo, nombreImagen1, nombreImagen2,ticket))
            {
                gest.GuardarCorreo(correo);
                ticket.IdCorreo = correo.Id;
                gest.GuardarTicket(ticket);
                return "Se envió un email al coordinador con un PIN para continuar con la transacción";
            }
            else
                return "No se pudo enviar el correo, revise porfavor su conexión a internet y que el servidor de correos se encuentra disponible";
                     
        }
        private void NotificacionCorreoSecuencia(int nScaleId,Secuencia secuencia, ref string ruta1, ref string ruta2)
        {
            string nom_Camara1;
            string nom_Camara2;
            string IP_Camara1;
            string IP_Camara2;
            PingReply RespuestaPingCamara1;
            PingReply RespuestaPingCamara2;
            int iTiempoEspera = 500;
            Ping HacerPing = new Ping();
            if (secuencia.Bascula == 0)
            {
                nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara13"].Value;
                nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara14"].Value;
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara13"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara14"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
            }
            else
            {
                nom_Camara1 = cfg.AppSettings.Settings["Nom_Camara23"].Value;
                nom_Camara2 = cfg.AppSettings.Settings["Nom_Camara24"].Value;
                IP_Camara1 = cfg.AppSettings.Settings["IP_Camara23"].Value;
                IP_Camara2 = cfg.AppSettings.Settings["IP_Camara24"].Value;
                RespuestaPingCamara1 = HacerPing.Send(IP_Camara1, iTiempoEspera);
                RespuestaPingCamara2 = HacerPing.Send(IP_Camara2, iTiempoEspera);
            }

            ruta1 = ObtenerImagen(ticket[nScaleId].PlacaVehiculo, nom_Camara1, IP_Camara1);
            ruta2 = ObtenerImagen(ticket[nScaleId].PlacaVehiculo, nom_Camara2, IP_Camara2);
            gest.escribirImagen(ruta1, secuencia.PesoBascula);
            gest.escribirImagen(ruta2, secuencia.PesoBascula);
            ticket[nScaleId].Nuevo();

        }
        private void SetPesoActual(ref double peso, int nScaleId)
        {
            pesoActualBascula[nScaleId] = peso;
        }



        #endregion






    }
}
