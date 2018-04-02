using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Windows.Forms;

namespace Attendance_System
{
   
    /// <summary>
    /// Class to handle all database related processes
    /// </summary>
    class db_handle
    {
        #region _public_declarations
        static string _url = "Server=localhost;user id=root;password=; database=verification_system";
        public static MySqlConnection _conn;
        public static MySqlCommand _cmd = new MySqlCommand();
        public static MySqlDataAdapter _ada = new MySqlDataAdapter();
        public static DataTable _datat = new DataTable();
        public String n, m;
        #endregion
        #region db_fields
        public string name, email, phone, gender, marital, address, dob, ImageFileName;
        public int FacePositionXc, FacePositionYc, FacePositionW, Eye1X, Eye1Y, Eye2X, Eye2Y;
        public double FacePositionAngle;
        public byte[] imageTemplate = new byte[] { };
        public byte[] Image = new byte[] { };
        public byte[] FaceImage = new byte[] { };
        #endregion
        /// <summary>
        /// lets check if our table exists in our database server
        /// </summary>
        public void _verify_db_exists()
        {
            try
            {
                _conn = new MySqlConnection(_url);
                _cmd.Connection = _conn;
                _cmd.CommandText = "create table if not exists records(" +
                "ID int primary key auto_increment," +
              "name varchar(100)," +
              "email varchar(100)," +
              "phone varchar(15)," +
              "gender varchar(7)," +
              "marital varchar(8)," +
              "address varchar(100)," +
              "dob varchar(20)," +
              "ImageFileName varchar(260)," +
              "FacePositionXc int(10)," +
              "FacePositionYc int(10)," +
              "FacePositionW int(10)," +
              "FacePositionAngle double," +
              "Eye1X int(10)," +
              "Eye1Y int(10)," +
              "Eye2X int(10)," +
              "Eye2Y int(10)," +
              "imageTemplate varbinary(10000)," +
              "Image varbinary(10000)," +
              "FaceImage varbinary(10000)" +
              ");";
                _conn.Open();
                _cmd.ExecuteNonQuery();
                n = "Application Connected to server . . .";
                _conn.Close();
            }
            catch (Exception ex)
            {
                n = "Unable to connect to server . . .";
                //log error 
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt","\ndb error -> "+ex.Message );
                
            }
            finally
            {
                _conn.Close();
            }
            
        }
        /// <summary>
        /// lets populate our datagrid view
        /// </summary>
        public DataTable _load_records()
        {
            try
            {
                _conn = new MySqlConnection(_url);
                _cmd.Connection = _conn;
                _cmd.CommandText = "Select `name`,`email`,`phone`,`gender`,`marital`,`address` from `records`";
                _conn.Open();
                _ada.SelectCommand = _cmd;
                _cmd.ExecuteNonQuery();
                _ada.Fill(_datat);
                m = "\nRecords Loaded, ready ...";
                _conn.Close();
                return _datat;
            }
            catch (Exception ex)
            {
                //log error here
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt","\ndb error -> " + ex.Message);
                return null;
            }
            finally
            {
                _conn.Close();
            }
        }

        /// <summary>
        /// return a list of staffs that match the given name
        /// </summary>
        /// <param name="staff_name"></param>
        /// <returns></returns>
        public List<String> _search_staff(String staff_name)
        {
            //return a list
            List<String> list = new List<String>();
            try
            {
                _conn = new MySqlConnection(_url);
                _cmd.Connection = _conn;
                _cmd.CommandText = "Select `name` from `records` where `name` like '"+staff_name+"%'";
                _conn.Open();
                _ada.SelectCommand = _cmd;
                _cmd.ExecuteNonQuery();
                _ada.Fill(_datat);
                MySqlDataReader _dr;
                _dr = _cmd.ExecuteReader();
                _dr.Read();
                do
                {
                    for (int i = 0; i <= _dr.FieldCount - 1; i++)
                    {
                        list.Add(_dr.GetString(i).ToString());
                    }
                } while (_dr.Read() == true);
                return list;
            }
            catch (Exception ex)
            {
                //log error here
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt","db error -> " + ex.Message);
                return null;
            }
            finally
            {
                _conn.Close();
            }
        }
        public string view_image(String staff)
        {
            try
            {
                String res="";
                _conn = new MySqlConnection(_url);
                _cmd.Connection = _conn;
                _cmd.CommandText = "Select `ImageFileName` from `records` where `name` ='" + staff + "'";
                _conn.Open();
                _ada.SelectCommand = _cmd;
                _cmd.ExecuteNonQuery();
                _ada.Fill(_datat);
                MySqlDataReader _dr;
                _dr = _cmd.ExecuteReader();
                _dr.Read();
                do {
                    for (int i = 0; i <= _dr.FieldCount - 1; i++)
                    {
                      res = _dr.GetString(i).ToString();
                    }
                } while (_dr.Read() == true);
                _conn.Close();
                return res;
            }
            catch (Exception ex)
            {
                //log error here
                System.IO.File.AppendAllText(Application.StartupPath + "\\app_logs\\program_log.txt","\ndb error -> " + ex.Message);
                return null;
            }
            finally
            {
                _conn.Close();
            }
        }
    }
}
