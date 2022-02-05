using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAssist
{
    public class User : IDataErrorInfo
    {
        private string UserAvatar;
        private string UserLogin;
        private string UserName;
        public string username { get; set; }
        public string userlogin { get; set; }
        public string userpass { get; set; }

        public string GetName()
        {
            return UserName;
        }

        public void SetName(string name)
        {
            UserName = name;
        }

        public string GetLogin()
        {
            return UserLogin;
        }

        public void SetLogin(string login)
        {
            UserLogin = login;
        }

        public string GetAvatar()
        {
            return UserAvatar;
        }

        public void SetAvatar(string avatar)
        {
            UserAvatar = avatar;
        }

        private bool ValCheckLogin(string login)
        {
            string relativePath = @"db\MainDB.db";

            using (SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100"))
            {
                sqlCon.Open();
                SQLiteCommand sqlCom = new SQLiteCommand($"SELECT log FROM acc WHERE log='{login}'", sqlCon);
                object rd = sqlCom.ExecuteScalar();
                string log = (rd == null ? "" : rd.ToString());
                if (log == "")
                {
                    return false;
                }

                return true;

            }
        }

        #region IDataErrorInfo Members

        string IDataErrorInfo.Error
        {
            get { return null; }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                string forbidden = " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
                if (username != null | userlogin != null | userpass != null)
                {
                    switch (columnName)
                    // Validate property and return a string if there is an error
                    {
                        case "username":
                            if (forbidden.Any(username.Contains))
                                return "Forbidden symbols";
                            else if (username == null | username == "")
                                return "Name cannot be empty";
                            else if (username.Length < 4)
                                return "Name must be at least 4 characters";
                            break;
                        case "userlogin":
                            if (forbidden.Any(userlogin.Contains))
                                return "Forbidden symbols";
                            else if (userlogin == null | userlogin == "")
                                return "Login cannot be empty";
                            else if (userlogin.Length < 4)
                                return "Login must be at least 4 characters";
                            else if (ValCheckLogin(userlogin))
                                return "Login already exists";
                            break;
                    }
                }

                // If there's no error, null gets returned
                return null;
            }
        }
        #endregion
    }
}
