using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeepAutomation.Barcode;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;

namespace Attendance_System
{
    class logic
    {
        /// <summary>
        /// create all the required folders and files for storing program related data
        /// </summary>
     public void create_app_folders()
        {
            try {
                if (!Directory.Exists(Application.StartupPath + "\\images")) {
                    Directory.CreateDirectory(Application.StartupPath + "\\images");
                }
                if (!Directory.Exists(Application.StartupPath + "\\id_cards"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "\\id_cards");
                }
                if (!Directory.Exists(Application.StartupPath + "\\app_logs"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "\\app_logs");
                }
                if (!File.Exists(Application.StartupPath + "\\app_logs\\program_log.txt"))
                {
                    File.Create(Application.StartupPath + "\\app_logs\\program_log.txt");
                }
            } catch (Exception ex)
            {
                //log error
                MessageBox.Show("Error while creating program folders\nDetails : " + ex.Message);
            }
        }
        /// <summary>
        /// generate an identiy card for registered users and save in pdf format
        /// <param name="s"> s = location of staff's image, required</param>
        /// </summary>
        public void generate_user_id(String s,String staff_name, String dob, String gender, String output)
        {
            try { 
            //get the user's image
            Image data = Image.GetInstance(s);
            //location of our id card border image
            String b = (Application.StartupPath + "\\border.png");
            Image border = Image.GetInstance(b);
            border.ScaleAbsoluteHeight(155);
            border.ScaleAbsoluteWidth(290);
            border.SetAbsolutePosition(20, 685);
            //arrange the location of our staff's image on the card
            data.ScalePercent(0.0F);
            data.ScaleToFit(100.0F, 100.0F);
            data.Alignment = Image.ALIGN_LEFT & Image.ALIGN_LEFT;
            data.IndentationLeft = 33.0F;
            data.SpacingAfter = 33.0F;
            data.BorderWidthTop = 0.0F;
            data.SetAbsolutePosition(35.5f, 723);
            data.ScaleAbsoluteHeight(62);
            data.ScaleAbsoluteWidth(78);
            //get the qrcode
            Image vim = Image.GetInstance(output+".png");
            //position qrcode on the pdf card
            vim.ScalePercent(0.0F);
            vim.ScaleToFit(100.0F, 100.0F);
            vim.Alignment = Image.ALIGN_LEFT & Image.ALIGN_LEFT;
            vim.IndentationLeft = 33.0F;
            vim.SpacingAfter = 33.0F;
            vim.BorderWidthTop = 0.0F;
            vim.SetAbsolutePosition(220, 682);
            vim.ScaleAbsoluteHeight(70);
            vim.ScaleAbsoluteWidth(80);
            //specify text colors for the card
            Font white = FontFactory.GetFont("georgia", 8.0F);
            white.Color = BaseColor.WHITE;
            Font georgia = FontFactory.GetFont("georgia", 10.0f);
            georgia.Color = BaseColor.DARK_GRAY;
            Chunk c1 = new Chunk("Staff Name :  ", georgia);
            Chunk c2 = new Chunk(staff_name , white);
            Chunk c3 = new Chunk("Date Of birth :  ", georgia);
            Chunk c4 = new Chunk(dob, white);
            Chunk c5 = new Chunk("Gender :  ", georgia);
            Chunk c6 = new Chunk(gender, white);
            //place text data on card
            Paragraph p = new Paragraph();
            Font df;
            df = new Font(Font.FontFamily.COURIER, 10, Font.BOLD, new GrayColor(0.9F));
            Document vid = new Document();
            PdfWriter idWriter = PdfWriter.GetInstance(vid, new FileStream(Application.StartupPath + "\\id_cards\\"+ output + ".pdf", FileMode.Create));//
            vid.Open();
            vid.Add(border);
            Phrase p2 = new Phrase();
            p2.Add(c1);
            p2.Add(c2);
            Phrase p1 = new Phrase();
            p1.Add(c3);
            p1.Add(c4);
            Phrase p3 = new Phrase();
            p3.Add(c5);
            p3.Add(c6);
            vid.Add(data);
            vid.Add(vim);
            p.SpacingBefore = (9);
            p.FirstLineIndent = 90;
            p.Add(p2);
            vid.Add(p);
            Paragraph pp = new Paragraph();
            pp.SpacingBefore = (9);
            pp.FirstLineIndent = 90;
            pp.Add(p1);
            vid.Add(pp);
            Paragraph p6 = new Paragraph();
            p6.FirstLineIndent = 90;
            p6.SpacingBefore = 9;
            p6.Add(p3);
            vid.Add(p6);
            Paragraph p7 = new Paragraph();
            p7.FirstLineIndent = 60;
            p7.SpacingBefore = 4;
            vid.Add(p7);
            Paragraph p8 = new Paragraph();
            p8.FirstLineIndent = 60;
            p8.SpacingBefore = 4;
            vid.Add(p8);
            vid.Close();
        } catch (Exception ex)
            {
                MessageBox.Show("error while generating id card", "PDF error");
            }
        }
        /// <summary>
        /// create a barcode from the staff's name
        /// <param name="user_details">(Name of the staff, required)</param>
        /// <param name="phone">(Phone number of the staff, required)</param>
        /// </summary>
        public void generate_qrcode(String user_details, String phone, String output_name)
        {
            try
            {
                //create a new qrcode instance
                KeepAutomation.Barcode.Bean.BarCode qrcode = new KeepAutomation.Barcode.Bean.BarCode();
                qrcode.Symbology = KeepAutomation.Barcode.Symbology.QRCode;
                qrcode.QRCodeVersion = KeepAutomation.Barcode.QRCodeVersion.V10;
                qrcode.QRCodeECL = KeepAutomation.Barcode.QRCodeECL.H;
                qrcode.QRCodeDataMode = KeepAutomation.Barcode.QRCodeDataMode.Auto;
                //specify the value to encode
                qrcode.CodeToEncode = "Full Name : "+user_details+"\nPhone No : "+phone;
                qrcode.BarcodeUnit = KeepAutomation.Barcode.BarcodeUnit.Pixel;
                qrcode.DPI = 72;
                qrcode.X = 2;
                qrcode.Y = 2;
                //specify the size of the qrcode (must be >= 4 on all sides)
                qrcode.LeftMargin = 8;
                qrcode.RightMargin = 8;
                qrcode.TopMargin = 8;
                qrcode.BottomMargin = 8;
                //specify the format of our output
                qrcode.ImageFormat = System.Drawing.Imaging.ImageFormat.Png;
                //save our qrcode to a specified location
                qrcode.generateBarcodeToImageFile(output_name + ".png");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sorry, An error occured while generating user qrcode","QRcode error");
            }
          }
    }
}
