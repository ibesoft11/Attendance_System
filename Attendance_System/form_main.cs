using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Luxand;
using MySql.Data.MySqlClient;


/// <summary>
/// Our parent form, calls other functions of the system
/// Author : Ibesoft
/// </summary>
namespace Attendance_System
{
    struct TFaceRecord
    {
        public Byte[] Template; // template for our face
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] FacialFeatures; //holds our facial features
        public String ImageFileName;
        public FSDK.CImage image;
        public FSDK.CImage faceImage;
    }
    public partial class form_main : Form
    {
       
        TFaceRecord fr = new TFaceRecord();
        public int xp, yp;
        Single FaceDetectionThreshold = 3;
        Single FARValue = 100;
        List<TFaceRecord> FaceList;
        public int x, y;
        public bool _is_form_filled=false;
        public List<String> search_items = new List<string>();
        
        public form_main()
        {
          InitializeComponent();
            //create necessary program folders
            logic log = new logic();
            log.create_app_folders();
            //declare eventhandlers for form events
            this.FormClosing += new FormClosingEventHandler(form_closing);
            this.MouseDown += new MouseEventHandler(form_mouse_down);
            this.MouseMove += new MouseEventHandler(form_mouse_move);
        }
      public void updater()
        {
            try
            {
                staff_picture.Image = Image.FromFile(Application.StartupPath + "\\images\\" +  get_staffname() + ".jpg");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt", "\nimg err -> " + ex.Message);
            }
        }
        private void label2_Click(object sender, EventArgs e)
        {
            panel_staff_rec.Size = new Size(844, 406);
            panel_staff_rec.Location = new Point(2, 143);
            panel_admin.Visible = false;
            panel_verification.Visible = false;
            panel_staff_rec.Visible = true;
            group_register.Size = new Size(826, 375);
            group_register.Location = new Point(10, 27);
            group_register.Visible = true;
            group_view_rec.Visible = false;
        }
        void form_mouse_down(object sender, MouseEventArgs e)
        {
            //hnadles our form move action
            x = Cursor.Position.X - this.Left;
            y = Cursor.Position.Y - this.Top;
        }
        void form_mouse_move(object sender, MouseEventArgs e)
        {
            //form move action
            int nx, ny;
        if (MouseButtons == MouseButtons.Left){
                nx = MousePosition.X - x;
                ny = MousePosition.Y - y;
                this.Location = new Point(nx, ny);
                this.Cursor = Cursors.SizeAll;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }
        void form_closing(object sender, FormClosingEventArgs e)
        {
            //make sure the user triggered the formclose() event
            var msg = MessageBox.Show("Do you want to exit ?", "Exit App", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (msg == DialogResult.Yes)
            {
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt", "\nApplication Closed");
                //close app, GC should collect resources.
            }
            else if (msg == DialogResult.No)
            {
                //cancel close and return to app
                e.Cancel = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            curve_border();
            tool_tip();
            //show verification panel on load
            panel_verification.Size = new Size(844, 406);
            panel_verification.Location = new Point(2, 143);
            panel_admin.Visible = false;
            panel_staff_rec.Visible = false;
            panel_verification.Visible = true;
            //connect to server and laod records
            db_handle db = new db_handle();
            db._verify_db_exists();
            lbl_status.Text = db.n;
            //db._load_records();
          
            //append launch date and time to log_file to make error tracing easier
            try
            {
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt","\n--------" +
                    "--------Program Log for " + DateTime.Now.ToShortDateString() + "---at---" + DateTime.Now.ToLongTimeString() +
                    "----------------");
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("error " + ex.Message);
            }
        }
        /// <summary>
        /// lets update our staff image after successfully getting face image
        /// </summary>
        public void update_img()
        {
            try {
                staff_picture.Image = Image.FromFile(Application.StartupPath + "\\images\\" + get_staffname() + ".jpg");
            } catch (Exception ex)
            {
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt", "\nimg err -> "+ex.Message);
            }
        }
        /// <summary>
        /// this method gets our staff name and sends to our registration form
        /// </summary>
        /// <returns></returns>
        public string get_staffname()
        {
            return txt_firstname.Text + " " + txt_surname.Text;
        }
        /// <summary>
        /// Method to show control tool-tips to users
        /// </summary>
        void tool_tip()
        {
            ToolTip _hint = new ToolTip();
            _hint.InitialDelay = 500;
            _hint.AutoPopDelay = 5000;
            _hint.UseFading = true;
            _hint.ShowAlways = true;
            _hint.SetToolTip(lbl_close, "Exit Application");
            _hint.SetToolTip(lbl_minimize, "Minimize Application");
            _hint.SetToolTip(lbl_admin_panel, "View reports and log files");
            _hint.SetToolTip(lbl_attendance, "Verify Staff here");
            _hint.SetToolTip(lbl_staff_rec, "Add, Delete Or View Staff record");
        }
        /// <summary>
        /// Method to curve our form border
        /// </summary>
        void curve_border()
        {
            System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath();
            p.StartFigure();
            p.AddArc(new System.Drawing.RectangleF(0, 0, 40, 40), 180, 90);
            p.AddLine(40, 0, this.Width - 40, 0);
            p.AddArc(new RectangleF(this.Width - 40, 0, 40, 40), -90, 90);
            p.AddLine(this.Width, 40, this.Width, this.Height - 40);
            p.AddArc(new RectangleF(this.Width - 40, this.Height - 40, 40, 40), 0, 90);
            p.AddLine(this.Width - 40, this.Height, 40, this.Height);
            p.AddArc(new Rectangle(0, this.Height - 40, 40, 40), 90, 90);
            p.CloseFigure();
            this.Region = new Region(p);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbl_time.Text = DateTime.Now.ToLongTimeString();
        }

        private void lbl_minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void lbl_close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var msg = MessageBox.Show("Do you want to clear all input values ?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (msg == DialogResult.Yes)
            {
                //clear input here
                txt_surname.Text = ""; txt_firstname.Text = ""; txt_email.Text = "";
                txt_address.Text = ""; txt_phone.Text = ""; staff_picture.Image = null;
            }
        }
        //validate user input first
        #region "Input Validation"
        private void txt_surname_TextChanged(object sender, EventArgs e)
        {
           if (txt_surname.TextLength > 0)
            {
                err_surname.Visible = false;
            } else
            {
                err_surname.Visible = true;
            }
        }

        private void txt_firstname_TextChanged(object sender, EventArgs e)
        {
            if (txt_firstname.TextLength > 0)
            {
                err_firstname.Visible = false;
            }
            else
            {
                err_firstname.Visible = true;
            }
        }

        private void txt_phone_TextChanged(object sender, EventArgs e)
        {
            if (txt_phone.TextLength == 11)
            {
                err_phone.Visible = false;
            }
            else
            {
                err_phone.Visible = true;
            }
        }

        private void txt_address_TextChanged(object sender, EventArgs e)
        {
            if (txt_address.TextLength > 0)
            {
                err_address.Visible = false;
            }
            else
            {
                err_address.Visible = true;
            }
        }

        private void cbo_marital_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbo_marital.SelectedIndex == 0 || cbo_marital.SelectedIndex == 1 || cbo_marital.SelectedIndex == 2)
            {
                err_marital.Visible = false;
            }
            else
            {
                err_marital.Visible = true;
            }
        }

        private void cbo_gender_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbo_gender.SelectedIndex == 0 || cbo_gender.SelectedIndex == 1)
            {
                err_gender.Visible = false;
            }
            else
            {
                err_gender.Visible = true;
            }
        }

        private void dt_staff_ValueChanged(object sender, EventArgs e)
        {
            int age =DateTime.Now.Year - dt_staff.Value.Year;
            if (age >= 18)
            {
                err_date.Visible = false;
            }
            else
            {
                err_date.Visible = true;
            }
        }

        private void txt_email_TextChanged(object sender, EventArgs e)
        {
            if (email_validator(txt_email.Text))
            {
                err_email.Visible = false;
            } else
            {
                err_email.Visible = true;
            }
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            group_register.Size = new Size(826, 375);
            group_register.Location = new Point(10, 27);
            group_register.Visible = true;
            group_view_rec.Visible = false;
            group_search.Visible = false;
        }

        private void viewRecordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            group_view_rec.Size = new Size(826, 375);
            group_view_rec.Location = new Point(10, 27);
            group_view_rec.Visible = true;
            group_register.Visible = false;
            group_search.Visible = false;
            db_handle db = new db_handle();
            dataGridView1.DataSource = db._load_records();
        }

        private void img_attendance_Click(object sender, EventArgs e)
        {
            panel_verification.Size = new Size(844, 406);
            panel_verification.Location = new Point(2, 143);
            panel_admin.Visible = false;
            panel_staff_rec.Visible = false;
            panel_verification.Visible = true;
        }

        private void lbl_attendance_Click(object sender, EventArgs e)
        {
            panel_verification.Size = new Size(844, 406);
            panel_verification.Location = new Point(2, 143);
            panel_admin.Visible = false;
            panel_staff_rec.Visible = false;
            panel_verification.Visible = true;
        }

        private void img_staff_rec_Click(object sender, EventArgs e)
        {
            panel_staff_rec .Size = new Size(844, 406);
            panel_staff_rec.Location = new Point(2, 143);
            panel_admin.Visible = false;
            panel_verification.Visible = false;
            panel_staff_rec.Visible = true;
            group_register.Size = new Size(826, 375);
            group_register.Location = new Point(10, 27);
            group_register.Visible = true;
            group_view_rec.Visible = false;
        }

        private void img_admin_panel_Click(object sender, EventArgs e)
        {
            panel_admin.Size = new Size(844, 406);
            panel_admin.Location = new Point(2, 143);
            panel_verification.Visible = false;
            panel_staff_rec.Visible = false;
            panel_admin.Visible = true;
            //load our log file for preview
            try
            {
                richTextBox1.LoadFile(Application.StartupPath + "\\app_logs\\program_log.txt", RichTextBoxStreamType.PlainText);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while loading log_file\nDetails : " + ex.Message);
            }
        }

        private void lbl_admin_panel_Click(object sender, EventArgs e)
        {
            panel_admin.Size = new Size(844, 406);
            panel_admin.Location = new Point(2, 143);
            panel_verification.Visible = false;
            panel_staff_rec.Visible = false;
            panel_admin.Visible = true;
            //load our log file for preview
            try {
                richTextBox1.LoadFile(Application.StartupPath + "\\app_logs\\program_log.txt", RichTextBoxStreamType.PlainText);
            } catch (Exception ex)
            {
                MessageBox.Show("Error while loading log_file\nDetails : " + ex.Message);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", Application.StartupPath + "\\id_cards");
            } catch (Exception ex)
            {
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt", "\ndir error -> "+ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (err_address.Visible ==false && err_date.Visible == false && err_email.Visible == false && err_firstname.Visible ==false 
                && err_gender.Visible ==false && err_marital.Visible == false && err_phone.Visible == false && err_surname.Visible ==false)
            {
                _is_form_filled = true;
                try
                {
                    //register_camera reg = new register_camera(this);
                    //reg.ShowDialog();
                    emgu_add emgu = new emgu_add(this);
                    emgu.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            } else
            {
                MessageBox.Show("Some fields are empty\nfill them before capturing image...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
           
          
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_is_form_filled && staff_picture.Image != null)
            {
                //register here
                //_is_face_in_image(Application.StartupPath + "\\images\\" + get_staffname() + ".jpg");
            }
            else
            {
                MessageBox.Show("Some fields are empty\nFill them before submitting...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Method to check if a string is a valid email address, REGEX
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        Boolean email_validator(string val)
        {
            bool v=false;
            if (val == "")
            {
                v = false; ;
            }
            if (!(new Regex(@"^[a-zA-Z0-9_\-\.]+@[a-zA-Z0-9_\-\.]+\.[a-zA-Z]{2,}$")).IsMatch(val))
            {
                v=false ;
            }
            else { v = true; }
             return v;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            db_handle db = new db_handle();
            dataGridView1.Dispose();
            dataGridView1.DataSource = db._load_records();
            dataGridView1.Update();
        }

        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            db_handle db = new db_handle();
            list_search.DataSource = null;
            list_search.Items.Clear();
            list_search.DataSource = db._search_staff(txt_search.Text);
           
        }

        private void list_search_SelectedIndexChanged(object sender, EventArgs e)
        {
            //
            db_handle db = new db_handle();
            //MessageBox.Show(db.view_image(list_search.SelectedItem.ToString()));
            pic_search.Image =Image.FromFile(db.view_image(list_search.SelectedItem.ToString()));
        }

        private void deleteStaffRecordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            group_search.Size = new Size(826, 375);
            group_search.Location = new Point(10, 27);
            group_search.Visible = true;
            group_view_rec.Visible = false;
            group_register.Visible = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            form_verify veri = new form_verify();
            veri.ShowDialog();

        }
        #endregion

        #region staff_registration
        void _is_face_in_image(string filename)
        {
            try
            {
                //assuming that faces are vertical (handlearbitraryrotations=false) to speed up face detection
                FSDK.SetFaceDetectionParameters(false, true, 384);
                FSDK.SetFaceDetectionThreshold((int)(FaceDetectionThreshold));
                fr = new TFaceRecord();
                fr.ImageFileName = filename;
                fr.FacePosition = new FSDK.TFacePosition();
                fr.FacialFeatures = new FSDK.TPoint[1];
                fr.Template = new Byte[FSDK.TemplateSize - 1];
                fr.image = new FSDK.CImage(filename);
                fr.FacePosition = fr.image.DetectFace();
                if (fr.FacePosition.w == 0)
                {
                    MessageBox.Show("no face found, make sure you face the camera then try capturing image again...");
                } else
                {
                    fr.faceImage = fr.image.CopyRect((int)(fr.FacePosition.xc - System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.yc - System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.xc + System.Math.Round(fr.FacePosition.w * 0.5)), (int)(fr.FacePosition.yc + System.Math.Round(fr.FacePosition.w * 0.5)));
                    bool eyesDetected = false; 
                    try
                    {
                        fr.FacialFeatures = fr.image.DetectEyesInRegion(ref fr.FacePosition);
                        eyesDetected = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error detecting eyes during registration!!!");
                    }
                    if (eyesDetected)
                    {
                        fr.Template = fr.image.GetFaceTemplateInRegion(ref fr.FacePosition);
                    }
                    //call savemethod here ->SAVE(fr);
                    save_records(fr);
                    FaceList = new List<TFaceRecord>();
                    FaceList.Add(fr);
                }
            } 
            catch (Exception ex)
            {
                MessageBox.Show("Can't open image(s) with error: " + ex.Message.ToString());
            }
        }
        void save_records(TFaceRecord tr)
        {
            MySqlConnection sqlconnect = null;
            try
            {
                //lets prepare the face record to save
                Image img = null;
                Image img_face = null;
                System.IO.MemoryStream strm = new System.IO.MemoryStream();
                System.IO.MemoryStream strm_face = new System.IO.MemoryStream();
                img = fr.image.ToCLRImage();
                img_face = fr.faceImage.ToCLRImage();
                img.Save(strm, System.Drawing.Imaging.ImageFormat.Jpeg);
                img_face.Save(strm_face, System.Drawing.Imaging.ImageFormat.Jpeg);
                Byte[] img_array = new byte[strm.Length - 1];
                Byte[] img_array_face = new byte[strm_face.Length - 1];
                strm.Position = 0;
                strm.Read(img_array, 0, img_array.Length);
                strm_face.Position = 0;
                strm_face.Read(img_array_face, 0, img_array_face.Length);
                //lets connect to ur mysql database and save record
                sqlconnect = new MySqlConnection("server=127.0.0.1;user id=root;password=;database=verification_system");
                sqlconnect.Open();
                MySqlCommand sqlcmd = new MySqlCommand();
                sqlcmd.Connection = sqlconnect;
                sqlcmd.CommandText = "INSERT INTO records(ImageFileName, FacePositionXc, FacePositionYc, FacePositionW, FacePositionAngle, Eye1X, Eye1Y, Eye2X, Eye2Y, imagetemplate, Image, FaceImage, name, email, phone, gender, marital, address, dob) " + " VALUES(@ImageFileName, @FacePositionXc, @FacePositionYc, @FacePositionW, @FacePositionAngle, @Eye1X, @Eye1Y, @Eye2X, @Eye2Y, @imageTemplate, @Image, @FaceImage, @name, @email, @phone, @gender, @marital, @address, @dob)";
                //
                sqlcmd.Parameters.AddWithValue("@ImageFileName", fr.ImageFileName);
                sqlcmd.Parameters.AddWithValue("@FacePositionXc", fr.FacePosition.xc);
                sqlcmd.Parameters.AddWithValue("@FacePositionYc", fr.FacePosition.yc);
                sqlcmd.Parameters.AddWithValue("@FacePositionW", fr.FacePosition.w);
                sqlcmd.Parameters.AddWithValue("@FacePositionAngle", (Single)(fr.FacePosition.angle));
                sqlcmd.Parameters.AddWithValue("@Eye1X", fr.FacialFeatures[0].x);
                sqlcmd.Parameters.AddWithValue("@Eye1Y", fr.FacialFeatures[0].y);
                sqlcmd.Parameters.AddWithValue("@Eye2X", fr.FacialFeatures[1].x);
                sqlcmd.Parameters.AddWithValue("@Eye2Y", fr.FacialFeatures[1].y);
                sqlcmd.Parameters.AddWithValue("@imageTemplate", fr.Template);
                sqlcmd.Parameters.AddWithValue("@Image", img_array);
                sqlcmd.Parameters.AddWithValue("@FaceImage", img_array_face);
                //sqlcmd.Parameters.AddWithValue("@ID", bio.c);
                sqlcmd.Parameters.AddWithValue("@name", txt_firstname.Text + " " + txt_surname.Text);
                sqlcmd.Parameters.AddWithValue("@email", txt_email.Text);
                sqlcmd.Parameters.AddWithValue("@phone", txt_phone.Text);
                sqlcmd.Parameters.AddWithValue("@gender", cbo_gender.Text);
                sqlcmd.Parameters.AddWithValue("@marital", cbo_marital.Text);
                sqlcmd.Parameters.AddWithValue("@address", txt_address.Text);
                sqlcmd.Parameters.AddWithValue("@dob", dt_staff.Value.ToShortDateString());
                sqlcmd.ExecuteNonQuery();
                logic log = new logic();
                //log action here -> ID card generation in progress
                log.generate_qrcode(txt_firstname.Text + " " + txt_surname.Text, txt_phone.Text, get_staffname());
                log.generate_user_id(Application.StartupPath + "\\images\\" + get_staffname() + ".jpg", txt_firstname.Text + " " + txt_surname.Text, dt_staff.Value.ToShortDateString(), cbo_gender.Text, get_staffname());
                MessageBox.Show("Staff successfully registered", "Registration");
                 //clear all input fields
                txt_address.Text = "";
                txt_email.Text = "";
                txt_firstname.Text = "";
                txt_phone.Text = "";
                txt_surname.Text = "";
                staff_picture.Image = null;
                img.Dispose();
                img_face.Dispose();
                cbo_gender.SelectedItem = null;
                cbo_marital.SelectedItem = null;
                dt_staff.Value = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception on saving to database");
                //log error here
            }
            finally
            {
                sqlconnect.Close();
            }
        }
        #endregion
        
        /*   
        */
    }
}
