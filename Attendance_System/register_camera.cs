using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Luxand;
using System.Runtime.InteropServices;
/// <summary>
/// This class handles our staff image capture during the registration process
/// Author : Ibesoft
/// </summary>
namespace Attendance_System
{
    public partial class register_camera : Form
    {   
        form_main f;
        //variables for Luxand FaceSDK
        int cameraHandle;
        bool needClose;
        String userName;
        const string TrackerMemoryFile = "tracker.dat";
        int mouseX = 0;
        int mouseY = 0;
        // declare detection state
        enum ProgramStates
        {
            psRemember,
            psRecognize
        }
        ProgramStates programstate;
        public register_camera()
        {
            InitializeComponent();
        }
        //lets overload our main method so as to transfer data between our forms
        public register_camera(form_main fr)
        {
            InitializeComponent();
            f = new form_main();
            f = fr;
        }

        private void register_camera_Load(object sender, EventArgs e)
        {
            this.FormClosing += new FormClosingEventHandler(closing);
        }
        void closing(Object sender, FormClosingEventArgs e)
        {
            //set staff image in main form
            if (button1.Enabled == true)
            {
                f.update_img();
                GC.Collect();
                this.Dispose();
            } else
            {
                try {
                    //FSDK.FreeTracker(tracker);
                    needClose = true;
                    FSDKCam.CloseVideoCamera(cameraHandle);
                    //FSDKCam.FinalizeCapturing();
                    GC.Collect();
                    this.Dispose();
                    this.Close();
                }catch (Exception ex)
                {
                    //log error
                }

            }
         
        }
        /// <summary>
        /// Image capture and face detection
        /// Using Luxand Face SDK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // activate face sdk before using it...
            FSDK.ActivateLibrary("ANj63QzeUGKbORKF7KmC+s5J0f8hF7moXNMr1QrCeFStmCw3DTYD55rPZOERChnfpSbr3TguoGSPOPdrTwOodvoDuCeE3Jp/18G1GSeyvZT/uqK6q9MtvgSHtNFpna2sHVTdb1Az2rXxy8mHOOBgZ/PT5olt1Tsu0Gv8Go+3rdU=");
            //initialize sdk to enable capture
            FSDK.InitializeLibrary();
            FSDKCam.InitializeCapturing();
            String[] cameralist = new String[] { };
            int count;
            //get clist of connected cameras and select the first one
            FSDKCam.GetCameraList(out cameralist, out count);
            if (count == 0)
            {
                MessageBox.Show("Please attach a camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            FSDKCam.VideoFormatInfo[] formatList;
            FSDKCam.GetVideoFormatList(ref cameralist[0], out formatList, out count);
            String cameraName;
            cameraName = cameralist[0];
            if (FSDKCam.OpenVideoCamera(ref cameraName, ref cameraHandle) != FSDK.FSDKE_OK)
            {
                MessageBox.Show("Error opening the first camera", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            //a camera is opened, so disable controls unitl a face is detected
            button1.Enabled = false;
            needClose = false;
            int tracker = 0;
            if (FSDK.FSDKE_OK != FSDK.LoadTrackerMemoryFromFile(ref tracker, TrackerMemoryFile))
            {
                FSDK.CreateTracker(ref tracker);
            }
            int err = 0;
            FSDK.SetTrackerMultipleParameters(tracker, "HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=100; FaceDetectionThreshold=5;", ref err);
            
            FSDK.CImage image;
            Image frameImage;
            while (!needClose)
            {
                int ImageHandle = new int();
                if (FSDKCam.GrabFrame(cameraHandle, ref ImageHandle) != FSDK.FSDKE_OK)
                {
                    Application.DoEvents();
                    continue;
                }
                image = new FSDK.CImage(ImageHandle);
                long[] IDs = new long[256];
                long faceCount = new long();
                long sizeOfLong = 8;
                FSDK.FeedFrame(tracker, 0, image.ImageHandle, ref faceCount, out IDs, sizeOfLong * 256);
                Array.Resize(ref IDs, (int)faceCount);
                frameImage = image.ToCLRImage();
                Graphics gr;
                gr = Graphics.FromImage(frameImage);
                int i;
                for (i = 0; i <= IDs.Length - 1;)
                {
                    if (pictureBox1.Image != null)
                    {
                        // a face has been detected, grab it and close our preview source
                        needClose = true;
                        button1.Enabled = true;
                        String user;
                        user = f.get_staffname();
                        try
                        {
                            if (System.IO.Directory.Exists(Application.StartupPath + "\\images"))
                            {
                                pictureBox1.Image.Save(Application.StartupPath + "\\images\\" + user + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            else
                            {
                                System.IO.Directory.CreateDirectory(Application.StartupPath + "\\images");
                                pictureBox1.Image.Save(Application.StartupPath + "\\images\\" + user + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            
                        }
                        catch (Exception Ex)
                        {
                            Console.WriteLine(Ex.Message);
                        }
                        GC.Collect();
                        Application.DoEvents();
                        break;
                    }
                    //highlight face position in image
                    FSDK.TFacePosition facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(tracker, 0, IDs[i], ref facePosition);
                    int left, top, w;
                    left = facePosition.xc = (int)(facePosition.w * 0.6);
                    top = facePosition.yc - (int)(facePosition.w * 0.5);
                    w = facePosition.w * (int)1.2;
                    string name;
                    int res;
                    res = FSDK.GetAllNames(tracker, IDs[i], out name, 65536);
                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    {
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        gr.DrawString(name, new System.Drawing.Font("Arial", 16), new System.Drawing.SolidBrush(System.Drawing.Color.LightGreen), facePosition.xc, top + w + 5, format);
                    }
                    Pen pen = Pens.LightGreen;
                    //this block assigns a name to the image and saves it in our tracker file, but we don't need it since we are storing to our DB Server
                    if (mouseX >= left && mouseX <= left + w && mouseY >= top && mouseY <= top + w)
                    {
                        pen = Pens.Blue;
                        if (programstate == ProgramStates.psRemember)
                        {
                            if (FSDK.FSDKE_OK == FSDK.LockID(tracker, IDs[i]))
                            {
                                //ibe == testdata
                                userName = "ibe";
                                if (userName == null)
                                {
                                    FSDK.SetName(tracker, IDs[i], "");
                                }
                                else
                                {
                                    FSDK.SetName(tracker, IDs[i], userName);
                                }
                                FSDK.UnlockID(tracker, IDs[i]);
                            }
                        }
                    }
                    gr.DrawRectangle(pen, left, top, w, w);
                }
                programstate = ProgramStates.psRecognize;
                pictureBox1.Image = frameImage;
                //free captured resources to speed up program execution
                GC.Collect();
                Application.DoEvents();
            }
            //after capturing a valid face, tell the user and free resources
            FSDK.SaveTrackerMemoryToFile(tracker, TrackerMemoryFile);
            FSDK.FreeTracker(tracker);
            FSDKCam.CloseVideoCamera(cameraHandle);
            FSDKCam.FinalizeCapturing();
            MessageBox.Show("Face Image Successfully Captured!!!");
        }
    }
}
