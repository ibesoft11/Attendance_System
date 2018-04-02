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
using MySql.Data.MySqlClient;

namespace Attendance_System
{
    struct TFaceRecords
    {
        public Byte[] Template; // template for our face
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] FacialFeatures; //holds our facial features
        public String ImageFileName;
        public FSDK.CImage image;
        public FSDK.CImage faceImage;
    }
    public partial class form_verify : Form
    {
        System.Threading.Thread t;//= new System.Threading.Thread(camera);
        //variables for Luxand FaceSDK
        int cameraHandle;
        bool needClose;
        String userName;
        const string TrackerMemoryFile = "tracker.dat";
        int mouseX = 0;
        int mouseY = 0;
        // declare detection state
        List<TFaceRecords> FaceList;
        enum ProgramStates
        {
            psRemember,
            psRecognize
        }
        ProgramStates programstate;
        public form_verify()
        {
            InitializeComponent();
        }

        void loaddb()
        {
            MySqlConnection sqlConnect = null;
            try
            {
                sqlConnect = new MySqlConnection("server=127.0.0.1;user id=root;password=;database=verification_system");
                sqlConnect.Open();
                MySqlCommand sqlCmd = new MySqlCommand("SELECT ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, imageTemplate, image, faceimage FROM records", sqlConnect);
                MySqlDataReader reader = sqlCmd.ExecuteReader();
                while (reader.Read()) {
                    TFaceRecords fr = new TFaceRecords();
                    fr.ImageFileName = reader.GetString(0);

                    fr.FacePosition = new FSDK.TFacePosition();
                    fr.FacePosition.xc = reader.GetInt32(1);
                    fr.FacePosition.yc = reader.GetInt32(2);
                    fr.FacePosition.w = reader.GetInt32(3);
                    fr.FacePosition.angle = reader.GetFloat(4);
                    fr.FacialFeatures = new FSDK.TPoint[2];
                    fr.FacialFeatures[0] = new FSDK.TPoint();
                    fr.FacialFeatures[0].x = reader.GetInt32(5);
                    fr.FacialFeatures[0].y = reader.GetInt32(6);
                    fr.FacialFeatures[1] = new FSDK.TPoint();
                    fr.FacialFeatures[1].x = reader.GetInt32(7);
                    fr.FacialFeatures[1].y = reader.GetInt32(8);
                    fr.Template = new Byte[FSDK.TemplateSize];
                    reader.GetBytes(9, 0, fr.Template, 0, FSDK.TemplateSize);
                   // MessageBox.Show(reader.GetFieldType(10).ToString());

                    Byte[] b = (Byte[])reader.GetValue(10);//new Byte[reader.GetByte(10)];
                    Byte[] bb = (Byte[])reader.GetValue(11);//new Byte[reader.GetByte(11)];
                   // MessageBox.Show("reached");
                    System.Drawing.Image img = System.Drawing.Image.FromStream(new System.IO.MemoryStream(b));
                    System.Drawing.Image img_face = System.Drawing.Image.FromStream(new System.IO.MemoryStream(bb));
                   // MessageBox.Show("reached");
                    fr.image = new FSDK.CImage(img);
                    fr.faceImage = new FSDK.CImage(img_face);
                    bool eyesdetected = false;
                    try
                    {
                        fr.FacialFeatures = fr.image.DetectEyesInRegion(ref fr.FacePosition);
                        eyesdetected = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error detecting eyes","Verification Module");
                    }
                    if (eyesdetected)
                    {
                        fr.Template = fr.image.GetFaceTemplateInRegion(ref fr.FacePosition);
                    }
                    FaceList.Add(fr);
                    img.Dispose();
                    img_face.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception on loading database");
            }
            finally
            {
                if (sqlConnect != null)
                {
                    sqlConnect.Close();
                }
            }
        }

        void match_faces()
        {
            if (FaceList.Count == 0)
            {
                MessageBox.Show("Please enroll faces first", "Error");
            } else
            {
                try {
                    String fn = Application.StartupPath + "\\images\\verify_image.jpg";
                    TFaceRecords fr = new TFaceRecords();
                    fr.ImageFileName = fn;
                    fr.FacePosition = new FSDK.TFacePosition();
                    fr.FacialFeatures = new FSDK.TPoint[FSDK.FSDK_FACIAL_FEATURE_COUNT - 1];
                    fr.Template = new Byte[FSDK.TemplateSize - 1];
                    fr.image = new FSDK.CImage(fn);
                    fr.FacePosition = fr.image.DetectFace();
                    if (fr.FacePosition.w == 0)
                    {
                        MessageBox.Show("No faces found. Try to lower the Minimal Face Quality parameter.", "Verification error");
                    } else
                    {
                        fr.faceImage = fr.image.CopyRect((int)(fr.FacePosition.xc - System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.yc - System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.xc + System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.yc + System.Math.Round(fr.FacePosition.w * 0.5)));
                        bool eyesdetected = false;
                        try
                        {
                            fr.FacialFeatures = fr.image.DetectEyesInRegion(ref fr.FacePosition);
                            eyesdetected = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error detecting eyes...", "Verification Module");
                        }
                        if (eyesdetected)
                        {
                            fr.Template = fr.image.GetFaceTemplateInRegion(ref fr.FacePosition);
                        }
                        Go(ref fr);
                    }
                } catch (Exception ex)
                {
                    MessageBox.Show("Can't open image(s) with error: " + ex.Message.ToString(), "Error");
                }

            }
        }

        void Go(ref TFaceRecords SearchFace)
        {
            System.Drawing.Image img = SearchFace.faceImage.ToCLRImage();
            float Threshold = 0.0f;
            FSDK.GetMatchingThresholdAtFAR(70 / 100, ref Threshold);
            int MatchedCount = 0;
            int FaceCount = FaceList.Count();
            Double[] Similarities = new Double[FaceCount];
            int[] Numbers = new int[FaceCount];
            MessageBox.Show("Facelist = " + FaceList.Count);
            for (int i=0;i<=FaceList.Count - 1; i++)
            {
                float Similarity = 0.0F;
                TFaceRecords CurrentFace = FaceList[i];
                FSDK.MatchFaces(ref SearchFace.Template, ref CurrentFace.Template, ref Similarity);
                MessageBox.Show("Similarity = " + Similarity+"Threshold = "+Threshold);
                if (Similarity >= Threshold)
                {
                    Similarities[MatchedCount] = Similarity;
                    Numbers[MatchedCount] = i;
                    MatchedCount += 1;
                }
            }
            if (MatchedCount == 0)
            {
                MessageBox.Show("No matches found.\nTry Again !!!"+MatchedCount, "No matches");
            } else
            {
                MessageBox.Show("Staff Record found in database...");
            }
            /*
            */
            t.Abort();
           // button1.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //    MessageBox.Show("after");
            t = new System.Threading.Thread(camera);
            t.Start();
            button1.Enabled = false;
        }

        void camera()
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
           //button1.Enabled = false;
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
                        //button1.Enabled = true;
                        String user;
                        user = "verify_image";
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
                    //    FSDK.TFacePosition facePosition = new FSDK.TFacePosition();
                    //    FSDK.GetTrackerFacePosition(tracker, 0, IDs[i], ref facePosition);
                    //    int left, top, w;
                    //    left = facePosition.xc = (int)(facePosition.w * 0.6);
                    //    top = facePosition.yc - (int)(facePosition.w * 0.5);
                    //    w = facePosition.w * (int)1.2;
                    //    string name;
                    //    int res;
                    //    res = FSDK.GetAllNames(tracker, IDs[i], out name, 65536);
                    //    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    //    {
                    //        StringFormat format = new StringFormat();
                    //        format.Alignment = StringAlignment.Center;
                    //        gr.DrawString(name, new System.Drawing.Font("Arial", 16), new System.Drawing.SolidBrush(System.Drawing.Color.LightGreen), facePosition.xc, top + w + 5, format);
                    //    }
                    //    Pen pen = Pens.LightGreen;
                    //    //this block assigns a name to the image and saves it in our tracker file, but we don't need it since we are storing to our DB Server
                    //    if (mouseX >= left && mouseX <= left + w && mouseY >= top && mouseY <= top + w)
                    //    {
                    //        pen = Pens.Blue;
                    //        if (programstate == ProgramStates.psRemember)
                    //        {
                    //            if (FSDK.FSDKE_OK == FSDK.LockID(tracker, IDs[i]))
                    //            {
                    //                //ibe == testdata
                    //                userName = "ibe";
                    //                if (userName == null)
                    //                {
                    //                    FSDK.SetName(tracker, IDs[i], "");
                    //                }
                    //                else
                    //                {
                    //                    FSDK.SetName(tracker, IDs[i], userName);
                    //                }
                    //                FSDK.UnlockID(tracker, IDs[i]);
                    //            }
                    //        }
                    //    }
                    //    gr.DrawRectangle(pen, left, top, w, w);
                }
                programstate = ProgramStates.psRecognize;
                pictureBox1.Image = frameImage;
                //free captured resources to speed up program execution
                GC.Collect();
                Application.DoEvents();
            }
            //after capturing a valid face, tell the user and free resources
            //FSDK.SaveTrackerMemoryToFile(tracker, TrackerMemoryFile);
            //FSDK.FreeTracker(tracker);
            FSDKCam.CloseVideoCamera(cameraHandle);
            FSDKCam.FinalizeCapturing();
            MessageBox.Show("Face Image Successfully Captured!!!");
            loaddb();
            match_faces();

        }

        private void form_verify_Load(object sender, EventArgs e)
        {
            FaceList = new List<TFaceRecords>();
            this.FormClosing += new FormClosingEventHandler(closing);
        }
        void closing(Object sender, FormClosingEventArgs e)
        {
            //set staff image in main form
            if (button1.Enabled == true)
            {
                //f.update_img();
                GC.Collect();
                this.Dispose();
            }
            else
            {
                try
                {
                    //FSDK.FreeTracker(tracker);
                    needClose = true;
                    FSDKCam.CloseVideoCamera(cameraHandle);
                    //FSDKCam.FinalizeCapturing();
                    GC.Collect();
                    this.Dispose();
                    this.Close();
                }
                catch (Exception ex)
                {
                    //log error
                }

            }

        }
    }
}
