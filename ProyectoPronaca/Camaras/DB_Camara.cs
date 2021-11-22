using DataBridge.Core.Business;
using DataBridge.Core.TransactionManager;
using DataBridge.Core.Types;
using DataBridge.VideoServerLibrary.CameraData;
using DataBridge.VideoServerManager;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using DataBridge.Core.Services;

using DataBridge.DeviceManager;

using System.Collections.Concurrent;

using System.Drawing;

using System.IO;













namespace Camaras
{
   public class DB_Camara: TransactionProcessing
    {
      

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


        public override string TransactionAccepting(int nScaleId, TransactionModel myTransaction)
        {
            CreateTransaction(1,1);
            return base.TransactionAccepting(nScaleId, myTransaction);
        }

        public override void AddOnDeviceDataReceived(int nScaleId, AddOnDeviceDataReceivedEventArgs myEventArgs)
        {
            try
            {
                if (!CompleteTransactionAutomatically.ContainsKey(nScaleId))
                {
                    CompleteTransactionAutomatically.TryAdd(nScaleId, false);
                }

                if (CompleteTransactionAutomatically[nScaleId] == false)
                {
                    TransactionProcessor myTransactionProcessor = TransactionMgr.GetProcessorByScaleId(nScaleId);
                    string sTransactionNumber = myEventArgs.FormattedDataReceived;

                    CompleteTransactionAutomatically[nScaleId] = true;
                    bool bHaveIncompleteTransaction = myTransactionProcessor.LoadIncompleteTransaction(sTransactionNumber);
                    if (!bHaveIncompleteTransaction)
                    {
                        CompleteTransactionAutomatically[nScaleId] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Failed to loads record", ex);
            }
        }

        private void CreateTransaction(int nVehicleId, int nInboundLane)
        {
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

                    using (Image image = Image.FromStream(new MemoryStream(myImageAsBytes)))
                    {
                        image.Save(@"C:\CAMARA\output.jpg", ImageFormat.Jpeg);  // Or Png
                    }


                }
            }
            catch (Exception ex)
            {
                ServiceManager.LogMgr.WriteError("Failed to create transaction", ex);
            }
        }
    }
}
