using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Data.SQLite;
using System.Data;
using System.IO;
using Microsoft.Win32;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Text;

namespace DoctorAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckDB();
            CheckLogin(false);
        }

        // Settings
        public bool colorfulChangesTable = false;

        // Global Variables
        public User user = new User();
        public bool PasHasError = true;
        public DataTable dataTablePub;
        public char actionFlag;

        // Animation
        private void Fade(UIElement uiElem, bool fadeIn, int durMS, int delMS, double toVal)
        {
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, durMS);
            TimeSpan delay = new TimeSpan(0, 0, 0, 0, delMS);

            var animation = new DoubleAnimation
            {
                To = toVal,
                BeginTime = delay,
                Duration = duration,
                FillBehavior = FillBehavior.Stop,
            };

            if (fadeIn)
            {
                uiElem.Opacity = 0;
                uiElem.Visibility = Visibility.Visible;
                animation.Completed += (s, a) => uiElem.Opacity = toVal;
                animation.Completed += (s, a) => uiElem.Visibility = Visibility.Visible;
            }
            else
            {
                animation.Completed += (s, a) => uiElem.Opacity = toVal;
                animation.Completed += (s, a) => uiElem.Visibility = Visibility.Collapsed;
            }

            uiElem.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        // Checks
        private void CheckLogin(bool islogged)
        {
            if (islogged == false)
            {
                Menu.IsEnabled = false;
                Drawer.IsEnabled = true;
            } else
            {
                Profile.IsSelected = true;
                Menu.IsEnabled = true;
                Drawer.IsEnabled = true;
                Fade(Dashboard, true, 300, 800, 1.0);
            }
        }
        private void CheckDB()
        {
            string relativePath = @"db\MainDB.db";

            if (!File.Exists(relativePath))
            {
                if (!Directory.Exists(relativePath))
                {
                    Directory.CreateDirectory(@"db\");
                }

                SQLiteConnection.CreateFile($"{relativePath}");
                string[] sqlQuery = { "CREATE TABLE acc (log STRING, pas STRING, name STRING, icon STRING)",
                                        "CREATE TABLE Medicine (prodname STRING, acomp STRING, company STRING, price STRING, amount STRING)",
                                        "CREATE TABLE Rooms (id INTEGER, capacity NUMERIC)"};

                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.Open();
                for (int i = 0; i < sqlQuery.Length; i++)
                {
                    SQLiteCommand sqlCom = new SQLiteCommand(sqlQuery[i], sqlCon);
                    sqlCom.ExecuteNonQuery();
                }
                sqlCon.Close();
            }
        }
        private string PassCheck(string Login, string Password)
        {
            string relativePath = @"db\MainDB.db";

            using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
            sqlCon.Open();
            SQLiteCommand sqlCom = new SQLiteCommand($"SELECT pas FROM acc WHERE log='{Login}'", sqlCon);
            object rd = sqlCom.ExecuteScalar();
            string savedPasswordHash = (rd == null ? "" : rd.ToString());
            if (savedPasswordHash == "")
            {
                return "errlogin";
            }

            sqlCon.Close();
            if (BCrypt.Net.BCrypt.Verify(Password + "M8~lD", savedPasswordHash))
                return "ok";
            else
                return "errpass";
        }
        private void ValCheckPass(string password)
        {
            if (password.Length < 8 | password == null | password == "")
            {
                PasHasError = true;
                PassTooltip.Visibility = Visibility.Visible;
                MaterialDesignThemes.Wpf.TextFieldAssist.SetUnderlineBrush(RegFieldPassword, new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)));
                RegFieldPassword.ToolTip = "Password must be at least 8 characters";
            } else
            {
                PassTooltip.Visibility = Visibility.Collapsed;
                MaterialDesignThemes.Wpf.TextFieldAssist.SetUnderlineBrush(RegFieldPassword, Brushes.White);
                RegFieldPassword.BorderBrush = Brushes.White;
                PasHasError = false;
                
            }
        }
        private void RegValidation()
        {
            if (!Validation.GetHasError(RegFieldLogin) && !Validation.GetHasError(RegFieldName) && !PasHasError)
            {
                RegisterBtn.IsEnabled = true;
            }
            else
            {
                RegisterBtn.IsEnabled = false;
            }
        }
        private static string CreateHash(string input)
        {
            string pwdToHash = input + "M8~lD";
            string hash = BCrypt.Net.BCrypt.HashPassword(pwdToHash, BCrypt.Net.BCrypt.GenerateSalt());
            
            return hash;
        }

        // Avatar
        private void ChangeAvatar(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog
            {
                Title = "Select a picture",
                Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                        "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                        "Portable Network Graphic (*.png)|*.png",
                FilterIndex = 0,
                RestoreDirectory = true
            };

            if (op.ShowDialog() == true) {
                if (!File.Exists($"images\\{op.SafeFileName}"))
                {
                    if (!Directory.Exists(@"images\"))
                    {
                        Directory.CreateDirectory(@"images\");
                    }
                    File.Copy(op.FileName, $"images\\{op.SafeFileName}");
                }

                string prevAvatar = GetFromDB($"SELECT icon FROM acc WHERE log='{user.GetLogin()}'");

                ControlDB($"UPDATE acc SET icon='{op.SafeFileName}' WHERE log='{user.GetLogin()}'");
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + $"images\\{op.SafeFileName}");
                bitmap.DecodePixelWidth = 128;
                bitmap.DecodePixelHeight = 128;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.EndInit();
                bitmap.Freeze();

                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"images\\{prevAvatar}"))
                {
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + $"images\\{prevAvatar}");
                }

                AvatarImgProfile.Source = bitmap;
                AvatarImg.Source = bitmap;
            }
        }
        private void InitializeAvatar(string userAvatar)
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + $"images\\{userAvatar}"))
            {
                ControlDB($"UPDATE acc SET icon='{null}' WHERE log='{user.GetLogin()}'");
            } else
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + $"images\\{userAvatar}");
                bitmap.DecodePixelWidth = 128;
                bitmap.DecodePixelHeight = 128;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bitmap.EndInit();
                bitmap.Freeze();
                AvatarImgProfile.Source = bitmap;
                AvatarImg.Source = bitmap;
            }
            
        }

        // DB interaction
        private void ControlDB(string sqlQuery)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.OpenAsync();
                SQLiteCommand sqlCom = new SQLiteCommand(sqlQuery, sqlCon);
                sqlCom.ExecuteNonQuery();
                sqlCon.Close();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
            }
        }
        private void ControlDB(string sqlQuery, string tableFrom, DataGrid sqlDataGrid)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.OpenAsync();
                SQLiteCommand sqlCom = new SQLiteCommand(sqlQuery, sqlCon);
                sqlCom.ExecuteNonQuery();
                SQLiteDataAdapter sqlAdapter = new SQLiteDataAdapter(sqlCom);
                DataTable dataTable = new DataTable($"{tableFrom}");
                sqlAdapter.Fill(dataTable);
                dataTablePub = dataTable;

                sqlDataGrid.ItemsSource = dataTable.AsDataView();
                sqlAdapter.Update(dataTable);
                sqlCon.Close();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
            }
        }
        private string GetFromDB(string sqlQuery)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.OpenAsync();
                SQLiteCommand sqlCom = new SQLiteCommand(sqlQuery, sqlCon);
                object rd = sqlCom.ExecuteScalar();
                string data = (rd == null ? "" : rd.ToString());

                sqlCon.Close();
                return data;
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
                return "nullerr";
            }
        }
        private DataTable GetTableFromDb(string sqlQuery)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.OpenAsync();
                SQLiteCommand sqlCom = new SQLiteCommand(sqlQuery, sqlCon);
                SQLiteDataReader dr = sqlCom.ExecuteReader();
                DataTable dt = new DataTable();
                dt.Load(dr);
                sqlCon.Close();
                return dt;
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
                return new DataTable();
            }
        }
        
        // Events
        private void DataGridChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dataTablePub.GetChanges() != null)
            {
                PendingBtn.IsEnabled = true;

                if (CountingBadge.Badge == null)
                    CountingBadge.Badge = 1;
                else if (CountingBadge.Badge.ToString() != dataTablePub.GetChanges().Rows.Count.ToString())
                    CountingBadge.Badge = dataTablePub.GetChanges().Rows.Count;
            } else
            {
                CountingBadge.Badge = null;
            }
        }
        private void LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (colorfulChangesTable)
            {
                if (PendingBtn.IsChecked == true)
                {
                    DataGridRow gridRow = e.Row;

                    if (gridRow.DataContext as DataRowView != null)
                    {
                        DataRow row = (gridRow.DataContext as DataRowView).Row;
                        switch (row.RowState)
                        {
                            case DataRowState.Added:
                                gridRow.Background = new SolidColorBrush(Color.FromRgb(0x47, 0x5B, 0x42));
                                break;
                            case DataRowState.Modified:
                                gridRow.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x58, 0x43));
                                break;
                            case DataRowState.Deleted:
                                gridRow.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x42, 0x42));
                                break;
                        }
                    }
                }
            }
            
        }
        private void PendingChecked(object sender, RoutedEventArgs e)
        {
            Menu.IsEnabled = false;
            SearchBar.IsEnabled = false;
            SearchFilter.IsEnabled = false;
            SearchBtn.IsEnabled = false;
            EditBtn.IsEnabled = false;
            AddBtn.IsEnabled = false;
            DeleteBtn.IsEnabled = false;
            ReloadBtn.IsEnabled = false;

            if (dataTablePub.GetChanges() != null) { 
                if (colorfulChangesTable)
                {
                    ColorHint.Visibility = Visibility.Visible;
                    DataGrid2.IsReadOnly = true;
                    DataTable changes = dataTablePub.GetChanges();
                    changes.DefaultView.RowStateFilter = DataViewRowState.Deleted | DataViewRowState.Added | DataViewRowState.ModifiedCurrent;
                    DataGrid2.ItemsSource = (changes).DefaultView;
                } else
                {
                    DataGrid2.IsReadOnly = true;
                    DataTable changes = dataTablePub.GetChanges();
                    changes.DefaultView.RowStateFilter = DataViewRowState.Deleted | DataViewRowState.Added | DataViewRowState.ModifiedCurrent;
                    changes.Columns.Add("RowState", typeof(string));
                    foreach (DataRow row in changes.Rows)
                    {
                        try
                        {
                            row["RowState"] = row.RowState;
                        }
                        catch (DeletedRowInaccessibleException)
                        {
                        }

                    }

                    CollectionViewSource mycollection = new CollectionViewSource
                    {
                        Source = changes
                    };
                    mycollection.GroupDescriptions.Add(new PropertyGroupDescription("RowState"));
                    DataGrid2.ItemsSource = mycollection.View;
                    DataGrid2.Columns[DataGrid2.Columns.IndexOf(DataGrid2.Columns.Last())].Visibility = Visibility.Collapsed;
                }
            }
        }
        private void PendingUnChecked(object sender, RoutedEventArgs e)
        {
            Menu.IsEnabled = true;
            SearchBar.IsEnabled = true;
            SearchFilter.IsEnabled = true;
            SearchBtn.IsEnabled = true;
            EditBtn.IsEnabled = true;
            AddBtn.IsEnabled = true;
            DeleteBtn.IsEnabled = true;
            ReloadBtn.IsEnabled = true;
            ColorHint.Visibility = Visibility.Collapsed;
            DataGrid2.IsReadOnly = false;
            DataGrid2.ItemsSource = dataTablePub.AsDataView();
        }
        
        private void ApplyBtnClicked(object sender, RoutedEventArgs e)
        {
            PendingBtn.IsChecked = false;
            string relativePath = @"db\MainDB.db";

            try
            {
                
                using (SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100"))
                {
                    sqlCon.Open();
                    SQLiteCommand Command = new SQLiteCommand();
                    switch (dataTablePub.TableName)
                    {
                        case "Medicine":
                            Command = new SQLiteCommand($"SELECT * FROM Medicine", sqlCon);
                            break;
                        case "Patients":
                            Command = new SQLiteCommand($"SELECT * FROM {user.GetLogin()}Patients", sqlCon);
                            break;
                        case "History":
                            Command = new SQLiteCommand($"SELECT * FROM {user.GetLogin()}History", sqlCon);
                            break;
                    }
                    SQLiteDataAdapter sqlData = new SQLiteDataAdapter
                    {
                        SelectCommand = Command
                    };
                    SQLiteCommandBuilder cb = new SQLiteCommandBuilder(sqlData);
                    sqlData.UpdateCommand = cb.GetUpdateCommand();
                    sqlData.Update(dataTablePub);
                    sqlCon.Close();
                }
                CountingBadge.Badge = null;
                if (dataTablePub.GetChanges() == null)
                {
                    PendingBtn.IsEnabled = false;
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
            }
        }
        private void CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            PendingBtn.IsChecked = false;
            dataTablePub.RejectChanges();
            CountingBadge.Badge = null;
            if (dataTablePub.GetChanges() == null)
            {
                PendingBtn.IsEnabled = false;
            }
        }
        private void CardSwitchBtnClick(object sender, RoutedEventArgs e)
        {
            if (((Hyperlink)e.Source).Name == "TOReg")
            {
                if (PassTooltip.Visibility == Visibility.Hidden)
                    ValCheckPass(RegFieldPassword.Password);
                Fade(LoginScreen, false, 200, 0, 0.0);
                Fade(RegisterScreen, true, 200, 200, 1.0);
            }
            else
            {
                Fade(RegisterScreen, false, 200, 0, 0.0);
                Fade(LoginScreen, true, 200, 200, 1.0);
                PassTooltip.Visibility = Visibility.Hidden;
            }
        }
        private void ErrorOK(object sender, RoutedEventArgs e)
        {
            Fade(WrongIcon, false, 50, 0, 0.0);
            Fade(LoginScreen, true, 200, 200, 1.0);
        }
        private void LoginBtnClick(object sender, RoutedEventArgs e)
        {
            string passCheck = PassCheck(LoginFieldLogin.Text, LoginFieldPassword.Password);

            switch (passCheck)
            {
                case "ok":
                    LoginScreen.Visibility = Visibility.Collapsed;
                    Fade(CheckIcon, true, 500, 0, 0.4);
                    Fade(LoginCard, false, 200, 800, 0.0);
                    GridDialog2.Visibility = Visibility.Visible;

                    user.SetLogin(LoginFieldLogin.Text);
                    user.SetName(GetFromDB($"SELECT name FROM acc WHERE log='{LoginFieldLogin.Text}'"));
                    user.SetAvatar(GetFromDB($"SELECT icon FROM acc WHERE log='{LoginFieldLogin.Text}'"));

                    LoginNameProfile.Text = user.GetName();
                    if (user.GetAvatar() != null)
                    {
                        InitializeAvatar(user.GetAvatar());
                    }

                    CheckLogin(true);
                    break;
                case "errlogin":
                    LoginScreen.Visibility = Visibility.Collapsed;
                    ErrorText.Text = "Wrong login";
                    Fade(WrongIcon, true, 100, 0, 1.0);
                    break;
                case "errpass":
                    LoginScreen.Visibility = Visibility.Collapsed;
                    ErrorText.Text = "Wrong password";
                    Fade(WrongIcon, true, 100, 0, 1.0);
                    break;
            }
        }
        private void RegisterBtnClick(object sender, RoutedEventArgs e)
        {
            if (RegFieldLogin.Text.Length > 3 && RegFieldName.Text.Length > 3 && RegFieldPassword.Password.Length > 7)
            {
                string insertCommand = "INSERT INTO acc (log,pas,name)values('" + RegFieldLogin.Text + "','" + CreateHash(RegFieldPassword.Password) + "','" + RegFieldName.Text + "')";
                ControlDB(insertCommand);

                RegisterScreen.Visibility = Visibility.Collapsed;
                Fade(CheckIcon, true, 800, 0, 0.3);
                Fade(LoginCard, false, 200, 800, 0.0);

                user.SetLogin(RegFieldLogin.Text);
                user.SetName(RegFieldName.Text);

                LoginNameProfile.Text = user.GetName();

                ControlDB("CREATE TABLE " + $"{user.GetLogin()}" + "Patients ( snils STRING, fname STRING, lname STRING, phone STRING, gender STRING, address STRING, job STRING, bill STRING, prescription STRING, room STRING )");
                ControlDB("CREATE TABLE " + $"{user.GetLogin()}" + "Apointments ( name TEXT, date STRING, time STRING, phone STRING )");
                ControlDB("CREATE TABLE " + $"{user.GetLogin()}" + "History ( snils STRING, name STRING, date STRING, diagnosis STRING, bill STRING )");

                CheckLogin(true);
            } else
            {
                MessageBox.Show("Wrong input");
            }
        }
        private void ValPassChanged(object sender, RoutedEventArgs e)
        {
            ValCheckPass(RegFieldPassword.Password);
            RegValidation(); 
        }
        private void ValTextChanged(object sender, TextChangedEventArgs e)
        {
            RegValidation();
        }
        private void LogoutBtnClick(object sender, RoutedEventArgs e)
        {
            DrawerHost.CloseDrawerCommand.Execute(Drawer, Drawer);
            CheckLogin(false);
            for (int i = 2; i < 8;i++)
            {
                ((UIElement)VisualTreeHelper.GetChild(MainGrid, i)).Visibility = Visibility.Collapsed;
            }
            CheckIcon.Visibility = Visibility.Collapsed;
            Dashboard.Visibility = Visibility.Collapsed;
            Grid0.Visibility = Visibility.Visible;
            Fade(LoginCard, true, 200, 0, 1.0);
            Fade(LoginScreen, true, 200, 0, 1.0);
        }
        private void ExitBtnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void SettingsBtnClick(object sender, RoutedEventArgs e)
        {
            SlideState.IsChecked = !SlideState.IsChecked;
        }
        private void ColorfulChangesTableChecked(object sender, RoutedEventArgs e)
        {
            colorfulChangesTable = true;
        }
        private void ColorfulChangesTableUnChecked(object sender, RoutedEventArgs e)
        {
            colorfulChangesTable = false;
        }
        private void AboutBtnClick(object sender, RoutedEventArgs e)
        {
            DrawerHost.CloseDrawerCommand.Execute(Drawer, Drawer);
            About modalWindow = new About
            {
                Owner = this
            };
            modalWindow.ShowDialog();
        }
        private void PlannerCalendarSelectDate(object sender, SelectionChangedEventArgs e)
        {
            Mouse.Capture(null);
            PlannerCalendarText.SelectedDate = PlannerCalendarSelector.SelectedDate;

            string normalizedDate = PlannerCalendarText.SelectedDate.Value.ToString().Split(' ')[0];
            ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + $"Apointments WHERE date='{normalizedDate}'", "Apointments", PlannerDataGrid);
        }
        private void PlannerCalendarTodayClick(object sender, RoutedEventArgs e)
        {
            PlannerCalendarSelector.SelectedDate = DateTime.Now;
            PlannerCalendarSelector.DisplayDate = DateTime.Now;
            PlannerCalendarSelector.DisplayMode = CalendarMode.Month;
        }
        private void PlannerCalendarText_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            PlannerCalendarSelector.SelectedDate = PlannerCalendarText.SelectedDate;
            PlannerCalendarSelector.DisplayDate = PlannerCalendarText.SelectedDate.Value;
            PlannerCalendarSelector.DisplayMode = CalendarMode.Month;
        }
        private readonly byte[] SelectedHistory = new byte[2];
        private void Profile_Selected(object sender, RoutedEventArgs e)
        {
            byte SelectedTabUid = Convert.ToByte(((ListBoxItem)e.Source).Uid);
            SelectedHistory[0] = SelectedHistory[1];
            SelectedHistory[1] = SelectedTabUid;
            PendingBtn.IsEnabled = false;
            switch (SelectedTabUid)
            {
                case 0:
                    Grid0.Visibility = Visibility.Visible;
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "History LIMIT 5", "History", DataGridLatestHistory);
                    break;  
                case 1:
                    Grid2Title.Text = "Medicine";
                    Grid2.Visibility = Visibility.Visible;
                    ControlDB("SELECT * FROM Medicine", "Medicine", DataGrid2);
                    SearchBar.Text = "";
                    using (DataTable SearchDt = GetTableFromDb("SELECT * FROM Medicine"))
                    {
                        SearchFiltersFill(SearchDt, 1);
                        DialogInputsAdd(SearchDt);
                    }
                    CountingBadge.Badge = null;
                    break;
                case 2:
                    Grid2Title.Text = "Patients";
                    Grid2.Visibility = Visibility.Visible;
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Patients", "Patients", DataGrid2);
                    SearchBar.Text = "";
                    using (DataTable SearchDt = GetTableFromDb("SELECT * FROM " + $"{user.GetLogin()}" + "Patients"))
                    {
                        SearchFiltersFill(SearchDt, 1);
                        DialogInputsAdd(SearchDt);
                    }
                    CountingBadge.Badge = null;
                    break;
                case 3:
                    GenerateRooms();
                    Grid3.Visibility = Visibility.Visible;
                    break;
                case 4:
                    Grid4.Visibility = Visibility.Visible;
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Apointments", "Apointments", PlannerDataGrid);
                    break;
                case 5:
                    Grid2Title.Text = "History";
                    Grid2.Visibility = Visibility.Visible;
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "History", "History", DataGrid2);
                    SearchBar.Text = "";
                    using (DataTable SearchDt = GetTableFromDb("SELECT * FROM " + $"{user.GetLogin()}" + "History"))
                    {
                        SearchFiltersFill(SearchDt, 1);
                        DialogInputsAdd(SearchDt);
                    }
                    CountingBadge.Badge = null;
                    break;
            };

            if (SelectedHistory[0] != SelectedTabUid)
            {
                if ((SelectedHistory[0] == 1 && SelectedTabUid == 2) || (SelectedHistory[0] == 2 && SelectedTabUid == 1) ||
                    (SelectedHistory[0] == 5 && SelectedTabUid == 1) || (SelectedHistory[0] == 1 && SelectedTabUid == 5) ||
                    (SelectedHistory[0] == 2 && SelectedTabUid == 5) || (SelectedHistory[0] == 5 && SelectedTabUid == 2))
                {
                    return;
                }

                switch (SelectedHistory[0])
                {
                    case 0:
                        Grid0.Visibility = Visibility.Collapsed;
                        break;
                    case 1:
                        Grid2.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        Grid2.Visibility = Visibility.Collapsed;
                        break;
                    case 3:
                        Grid3.Visibility = Visibility.Collapsed;
                        break;
                    case 4:
                        Grid4.Visibility = Visibility.Collapsed;
                        break;
                    case 5:
                        Grid2.Visibility = Visibility.Collapsed;
                        break;
                };
            }
        }
        private void SearchFiltersFill(DataTable datatable, short startIndex)
        {
            Collection<string> columnHeaders = new Collection<string>();
            foreach (DataColumn column in datatable.Columns)
            {
                
                columnHeaders.Add((string)column.ColumnName);
            }
            SearchFilter.ItemsSource = columnHeaders;
            SearchFilter.SelectedIndex = startIndex;
        }
        private void DialogInputsAdd(DataTable datatable)
        {
            inputs.Clear();
            int sort = datatable.Columns.Count;
            int columnCount = sort;
            DialogInputsOne.Children.Clear();
            DialogInputsTwo.Children.Clear();
            foreach (DataColumn column in datatable.Columns)
            {
                sort -= 1;
                if (sort >= columnCount / 2)
                {
                    DialogInputsOne.Children.Add(GenerateInput("text", (string)column.ColumnName));
                    DialogInputsOne.Children.Add(GenerateInput("textinput", (string)column.ColumnName));
                }
                else
                {
                    DialogInputsTwo.Children.Add(GenerateInput("text", (string)column.ColumnName));
                    DialogInputsTwo.Children.Add(GenerateInput("textinput", (string)column.ColumnName));
                }
            }
        }
        private void DialogInputsFill(DataTable datatable, DataRowView values=null)
        {
            inputs.Clear();
            DialogInputsOne.Children.Clear();
            DialogInputsTwo.Children.Clear();
            int sort = datatable.Columns.Count;
            int columnCount = sort;
            foreach (DataColumn column in datatable.Columns)
            {
                if (values != null)
                {
                    try
                    {
                        string inpValue = (string)values.Row[datatable.Columns.IndexOf(column)];
                        sort -= 1;
                        if (sort >= columnCount / 2)
                        {
                            DialogInputsOne.Children.Add(GenerateInput("text", (string)column.ColumnName));
                            DialogInputsOne.Children.Add(GenerateInput("textinput", (string)column.ColumnName, inpValue));
                        }
                        else
                        {
                            DialogInputsTwo.Children.Add(GenerateInput("text", (string)column.ColumnName));
                            DialogInputsTwo.Children.Add(GenerateInput("textinput", (string)column.ColumnName, inpValue));
                        }
                    } catch
                    {
                        string inpValue = "";
                        sort -= 1;
                        if (sort >= columnCount / 2)
                        {
                            DialogInputsOne.Children.Add(GenerateInput("text", (string)column.ColumnName));
                            DialogInputsOne.Children.Add(GenerateInput("textinput", (string)column.ColumnName, inpValue));
                        }
                        else
                        {
                            DialogInputsTwo.Children.Add(GenerateInput("text", (string)column.ColumnName));
                            DialogInputsTwo.Children.Add(GenerateInput("textinput", (string)column.ColumnName, inpValue));
                        }
                    }
                
               
                }
            }
        }
        private readonly Collection<UIElement> inputs = new Collection<UIElement>();
        private UIElement GenerateInput(string name, string columnName="", string inpValue="")
        {
            switch (name)
            {
                case "text":
                    TextBlock txtblk = new TextBlock
                    {
                        Text = columnName,
                        FontSize = 16,
                        Margin = new Thickness(5, 5, 15, 0)
                    };
                    return txtblk;
                case "textinput":
                    if (columnName == "gender")
                    {
                        ComboBox combobox = new ComboBox
                        {
                            Width = 200,
                            FontSize = 18,
                            Margin = new Thickness(5, 0, 15, 5),
                            SelectedItem = inpValue
                        };
                        Collection<string> comboitems = new Collection<string>
                        {
                            "Female",
                            "Male"
                        };
                        combobox.ItemsSource = comboitems;
                        inputs.Add(combobox);
                        return combobox;
                    }
                    if (columnName == "date")
                    {
                        if (!String.IsNullOrEmpty(inpValue))
                        {
                            DatePicker datepick = new DatePicker
                            {
                                Width = 200,
                                FontSize = 18,
                                Margin = new Thickness(5, 0, 15, 5),
                                SelectedDate = DateTime.Parse(inpValue)
                            };
                            inputs.Add(datepick);
                            return datepick;
                        } else
                        {
                            DatePicker datepick = new DatePicker
                            {
                                Width = 200,
                                FontSize = 18,
                                Margin = new Thickness(5, 0, 15, 5)
                            };
                            inputs.Add(datepick);
                            return datepick;
                        }
                        
                    }
                    if (columnName == "prescription")
                    {

                        ComboBox combobox = new ComboBox
                        {
                            Width = 200,
                            FontSize = 18,
                            Margin = new Thickness(5, 0, 15, 5),
                            SelectedItem = inpValue
                        };
                        Collection<string> comboitems = new Collection<string>();
                        DataTable dt = GetTableFromDb("SELECT DISTINCT prodname FROM Medicine");
                        foreach (DataRow row in dt.Rows)
                        {
                            comboitems.Add((string)row.ItemArray[0]);
                        }
                        combobox.ItemsSource = comboitems;
                        inputs.Add(combobox);
                        return combobox;
                    }
                    TextBox txtbox = new TextBox
                    {
                        Width = 200,
                        Margin = new Thickness(5, 0, 15, 5),
                        FontSize = 18,
                        Text = inpValue
                    };
                    inputs.Add(txtbox);
                    return txtbox;
                default:
                    return null;
            }
            
                
            
        }
        private void SearchBtnClick(object sender, RoutedEventArgs e)
        {
            switch (SelectedHistory[1])
            {
                case 1:
                    ControlDB($"SELECT * FROM Medicine WHERE {SearchFilter.SelectedItem} LIKE '%{SearchBar.Text}%'", "Medicine", DataGrid2);
                    break;
                case 2:
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}Patients WHERE {SearchFilter.SelectedItem} LIKE '%{SearchBar.Text}%'", "Patients", DataGrid2);
                    break;
                case 5:
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}History WHERE {SearchFilter.SelectedItem} LIKE '%{SearchBar.Text}%'", "History", DataGrid2);
                    break;
            }
        }
        private void DialogAcceptBtnClick(object sender, RoutedEventArgs e)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.OpenAsync();
                SQLiteCommand sqlCom = new SQLiteCommand
                {
                    Connection = sqlCon
                };
                StringBuilder SB = new StringBuilder();
                switch (actionFlag)
                {
                    case 'b':
                        return;
                    case 'a':
                        switch (Grid2Title.Text)
                        {
                            case "Medicine":
                                SB.Append("INSERT INTO Medicine" + " (prodname, acomp, company, price, amount) VALUES (");
                                foreach (var input in inputs)
                                {
                                    if (input is TextBox inputtxt)
                                    {
                                        SB.Append($"'{inputtxt.Text}', ");
                                    }
                                    else if (input is ComboBox inputcomb)
                                    {
                                        SB.Append($"'{inputcomb.SelectedItem}', ");
                                    }

                                }
                                SB.Append(")");
                                string commandmed = SB.ToString();
                                sqlCom.CommandText = commandmed.Remove(commandmed.Length - 3, 1);
                                sqlCom.ExecuteNonQuery();
                                ControlDB("SELECT * FROM Medicine", "Medicine", DataGrid2);
                                break;
                            case "Patients":
                                SB.Append("INSERT INTO " + $"{user.GetLogin()}" + "Patients" + " (snils, fname, lname, phone, gender, address, job, bill, prescription, room) VALUES (");
                                foreach (var input in inputs)
                                {
                                    if (input is TextBox inputtxt)
                                    {
                                        SB.Append($"'{inputtxt.Text}', ");
                                    }
                                    else if (input is ComboBox inputcomb)
                                    {
                                        SB.Append($"'{inputcomb.SelectedItem}', ");
                                    }

                                }
                                SB.Append(")");
                                string commandpat = SB.ToString();
                                sqlCom.CommandText = commandpat.Remove(commandpat.Length - 3, 1);
                                sqlCom.ExecuteNonQuery();
                                ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Patients", "Patients", DataGrid2);
                                break;
                            case "History":
                                SB.Append("INSERT INTO " + $"{user.GetLogin()}" + "History" + " (snils, name, date, diagnosis, bill) VALUES (");
                                foreach (var input in inputs)
                                {
                                    if (input is TextBox inputtxt)
                                    {
                                        SB.Append($"'{inputtxt.Text}', ");
                                    }
                                    else if (input is ComboBox inputcomb)
                                    {
                                        SB.Append($"'{inputcomb.SelectedItem}', ");
                                    }
                                    else if (input is DatePicker inputdate)
                                    { 
                                        SB.Append($"'{inputdate.SelectedDate.ToString().Split(' ')[0]}', ");
                                    }

                                }
                                SB.Append(")");
                                string command = SB.ToString();
                                Console.WriteLine(command);
                                sqlCom.CommandText = command.Remove(command.Length - 3, 1);
                                sqlCom.ExecuteNonQuery();
                                ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "History", "History", DataGrid2);
                                break;
                        }
                        break;
                    case 'e':
                        switch (Grid2Title.Text)
                        {
                            case "Medicine":
                                Collection<string> columnsmed = new Collection<string> {
                                "prodname", "acomp", "company", "price", "amount"
                            };
                                SB.Append("UPDATE Medicine" + " SET  ");
                                short countermed = 0;
                                string idmed = "";
                                foreach (var input in inputs)
                                {
                                    if (input is TextBox inputtxt)
                                    {
                                        if (idmed == "")
                                        {
                                            idmed = inputtxt.Text;
                                        };
                                        SB.Append($"{columnsmed[countermed]} = '{inputtxt.Text}', ");
                                        countermed++;
                                    }
                                    else if (input is ComboBox inputcomb)
                                    {
                                        SB.Append($"{columnsmed[countermed]} = '{inputcomb.SelectedItem}', ");
                                        countermed++;
                                    }

                                }
                                SB.Remove(SB.Length - 2, 1);
                                SB.Append($"WHERE prodname = '{idmed}'");
                                string commandmed = SB.ToString();
                                Console.WriteLine(commandmed);
                                sqlCom.CommandText = commandmed;
                                sqlCom.ExecuteNonQuery();
                                ControlDB("SELECT * FROM Medicine", "Medicine", DataGrid2);
                                break;
                            case "Patients":
                                Collection<string> columnspat = new Collection<string> {
                                "snils", "fname",
                                "lname",  "phone",
                                "gender", "address",
                                "job",  "bill",
                                "prescription", "room"
                            };
                                SB.Append("UPDATE " + $"{user.GetLogin()}" + "Patients" + " SET  ");
                                short counterpat = 0;
                                string idpat = "";
                                foreach (var input in inputs)
                                {
                                    if (input is TextBox inputtxt)
                                    {
                                        if (idpat == "")
                                        {
                                            idpat = inputtxt.Text;
                                        };
                                        SB.Append($"{columnspat[counterpat]} = '{inputtxt.Text}', ");
                                        counterpat++;
                                    }
                                    else if (input is ComboBox inputcomb)
                                    {
                                        SB.Append($"{columnspat[counterpat]} = '{inputcomb.SelectedItem}', ");
                                        counterpat++;
                                    }

                                }
                                SB.Remove(SB.Length - 2, 1);
                                SB.Append($"WHERE snils = '{idpat}' ");
                                string commandpat = SB.ToString();
                                sqlCom.CommandText = commandpat;
                                sqlCom.ExecuteNonQuery();
                                ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Patients", "Patients", DataGrid2);
                                break;
                            case "History":
                                Collection<string> columns = new Collection<string> {
                                "snils", "name", "date", "diagnosis", "bill"
                            };
                            SB.Append("UPDATE " + $"{user.GetLogin()}" + "History" + " SET  ");
                            short counter = 0;
                            string id = "";
                            foreach (var input in inputs)
                            {
                                if (input is TextBox inputtxt)
                                {
                                    if (id == "")
                                    {
                                            id = inputtxt.Text;
                                    };
                                    SB.Append($"{columns[counter]} = '{inputtxt.Text}', ");
                                    counter++;
                                }
                                else if (input is ComboBox inputcomb)
                                {
                                    SB.Append($"{columns[counter]} = '{inputcomb.SelectedItem}', ");
                                    counter++;
                                }

                            }
                            SB.Remove(SB.Length - 2, 1);
                            SB.Append($"WHERE snils = '{id}' ");
                            string command = SB.ToString();
                            sqlCom.CommandText = command;
                            sqlCom.ExecuteNonQuery();
                            ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "History", "History", DataGrid2);
                            break;

                        }
                        break;
                }
                
                sqlCon.Close();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error:" + ex.ToString());
            }
        }
        private void EditBtnClicked(object sender, RoutedEventArgs e)
        {
            if (DataGrid2.SelectedItem != null)
            {
                actionFlag = 'e';
                string tableName = null;
                switch (Grid2Title.Text)
                {
                    case "Patients":
                        tableName = $"{user.GetLogin()}" + "Patients";
                        break;
                    case "Medicine":
                        tableName = "Medicine";
                        break;
                    case "History":
                        tableName = $"{user.GetLogin()}" + "History";
                        break;
                }

                if (tableName != null)
                {
                    DialogTitle.Text = "Edit";
                    using DataTable SearchDt = GetTableFromDb("SELECT * FROM " + tableName);
                    DialogInputsFill(SearchDt, (DataRowView)DataGrid2.SelectedItem);
                }

            } else
            {
                inputs.Clear();
                DialogInputsOne.Children.Clear();
                DialogInputsTwo.Children.Clear();
                DialogInputsOne.Children.Add(new TextBlock
                {
                    Margin = new Thickness(40),
                    FontSize = 28,
                    Text = "Select item first"
                });
                actionFlag = 'b';
            }
        }
        private void AddBtnClicked(object sender, RoutedEventArgs e)
        {
            actionFlag = 'a';
            string tableName = null;
            switch (Grid2Title.Text)
            {
                case "Patients":
                    tableName = $"{user.GetLogin()}" + "Patients";
                    break;
                case "Medicine":
                    tableName = "Medicine";
                    break;
                case "History":
                    tableName = $"{user.GetLogin()}" + "History";
                    break;
            }

            if (tableName != null)
            {
                DialogTitle.Text = "Add new Entry";
                using DataTable SearchDt = GetTableFromDb("SELECT * FROM " + tableName);
                DialogInputsAdd(SearchDt);
            }

        }
        private void GenerateRooms()
        {
            RoomCards.Children.Clear();
            DataTable rooms = GetTableFromDb("SELECT * FROM Rooms");
            foreach (DataRow row in rooms.Rows)
            {
                int inRoomCount = GetTableFromDb("SELECT * FROM " + $"{user.GetLogin()}Patients WHERE room = '{row.ItemArray[0]}'").Rows.Count;
                Button delBtn = new Button
                {
                    Name = "DeleteRoomBtn" + row.ItemArray[0].ToString(),
                    Style = this.FindResource("MaterialDesignFloatingActionMiniLightButton") as Style,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 5, 5, 0),
                    Height = 35,
                    Width = 35,
                    Background = Brushes.DimGray,
                    BorderBrush = Brushes.DimGray,
                    Content = new PackIcon
                    {
                        Foreground = Brushes.White,
                        Kind = PackIconKind.Close,
                        Width = 20,
                        Height = 20,
                        HorizontalAlignment = HorizontalAlignment.Right
                    }
                };
                delBtn.Click += new RoutedEventHandler(this.delBtn_Click);
                    Button cardBtn = new Button
                    {
                        Style = this.FindResource("MaterialDesignFlatButton") as Style,
                        Background = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)),
                        Height = 200,
                        Width = 150,
                        Padding = new Thickness(0),
                        Margin = new Thickness(10),
                        Content = new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                        {
                            new PackIcon {
                                Kind = PackIconKind.Bed,
                                Width = 100,
                                Height = 100,
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new TextBlock
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                FontSize = 28,
                                Text = $"Room {row.ItemArray[0]}"
                            },
                            new TextBlock
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                FontSize = 20,
                                Margin = new Thickness(0,5,0,0),
                                Text = "Capacity"
                            },
                            new StackPanel
                            {
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Orientation = Orientation.Horizontal,
                                Children = {
                                    new TextBlock
                                    {
                                        Text = $"{inRoomCount}",
                                        Foreground = (inRoomCount > Convert.ToInt32(row.ItemArray[1].ToString())) ? Brushes.Red : Brushes.White
                                    },
                                     new TextBlock
                                    {
                                        Margin = new Thickness(5,0,5,0),
                                        Text = "/"
                                    },
                                      new TextBlock
                                    {
                                        Text = $"{row.ItemArray[1]}"
                                    }
                                }
                            }
                        }
                    }
                };
                cardBtn.Click += new RoutedEventHandler(this.cardBtn_Click);

                Grid roomItem = new Grid
                {
                    Children = {
                    cardBtn,
                    delBtn,
                    }
                };
                RoomCards.Children.Add(roomItem);
            }
        }
        private void delBtn_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ControlDB($"DELETE FROM Rooms WHERE id = '{b.Name.Replace("DeleteRoomBtn", "")}'");
            GenerateRooms();
            Console.WriteLine(b.Name);
        }
        private void cardBtn_Click(object sender, RoutedEventArgs e)
        {
            Menu.IsEnabled = false;
            RoomInfo.Visibility = Visibility.Visible;
            Grid3.Visibility = Visibility.Collapsed;
            string roomName = (((sender as Button).Content as StackPanel).Children[1] as TextBlock).Text;
            RoomInfoRoomName.Text = roomName;
            ControlDB($"SELECT * FROM {user.GetLogin()}Patients WHERE room = '{roomName.Split(' ')[1]}'", "Patients", RoomInfoDataGrid);
            
        }
        private void RoomsDialogAcceptBtnClick(object sender, RoutedEventArgs e)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.Open();
                SQLiteCommand sqlCom = new SQLiteCommand
                {
                    Connection = sqlCon
                };
                sqlCom.CommandText = $"INSERT INTO Rooms (id, capacity) VALUES ({RoomNameInp.Text}, {RoomCapacityInp.Text}) ";
                sqlCom.ExecuteNonQuery();
                sqlCon.Close();
                GenerateRooms();
            }
            catch (SQLiteException ex)
            {
            }
        }
        private void PlannerDialogAcceptBtnClick(object sender, RoutedEventArgs e)
        {
            string relativePath = @"db\MainDB.db";

            try
            {
                using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                sqlCon.Open();
                SQLiteCommand sqlCom = new SQLiteCommand
                {
                    Connection = sqlCon
                };

                switch (actionFlag)
                {
                    case 'a':
                        sqlCom.CommandText = $"INSERT INTO {user.GetLogin()}Apointments (name, date, time, phone) VALUES ('{PlannerDialogInpName.Text}', '{PlannerDialogInpDate.SelectedDate.ToString().Split(' ')[0]}', '{PlannerDialogInpTime.SelectedTime.ToString().Split(' ')[1]}', '{PlannerDialogInpPhone.Text}') ";
                        break;
                    case 'e':
                        DataRowView row = (DataRowView)PlannerDataGrid.SelectedItems[0];
                        sqlCom.CommandText = $"UPDATE {user.GetLogin()}Apointments SET name = '{PlannerDialogInpName.Text}', date='{PlannerDialogInpDate.SelectedDate.ToString().Split(' ')[0]}', time='{PlannerDialogInpTime.SelectedTime.ToString().Split(' ')[1]}', phone='{PlannerDialogInpPhone.Text}'" +
                            $" WHERE name = '{row["name"]}' AND date='{row["date"]}' AND time='{row["time"]}' AND phone='{row["phone"]}'";
                        break;
                }

                sqlCom.ExecuteNonQuery();
                sqlCon.Close();
                ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Apointments", "Apointments", PlannerDataGrid);
            }
            catch (SQLiteException ex)
            {
            }
        }
        private void AppointDelBtnClick(object sender, RoutedEventArgs e)
        {
            if (PlannerDataGrid.SelectedItem != null)
            {
                string relativePath = @"db\MainDB.db";

                try
                {
                    using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                    sqlCon.Open();
                    SQLiteCommand sqlCom = new SQLiteCommand
                    {
                        Connection = sqlCon
                    };

                    DataRowView row = (DataRowView)PlannerDataGrid.SelectedItems[0];

                    sqlCom.CommandText = $"DELETE FROM {user.GetLogin()}Apointments WHERE name ='{row["name"]}' AND date = '{row["date"]}' AND time = '{row["time"]}' AND phone = '{row["phone"]}' ";
                    sqlCom.ExecuteNonQuery();
                    sqlCon.Close();
                    ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Apointments", "Apointments", PlannerDataGrid);
                }
                catch (SQLiteException ex)
                {
                }
            }
        }
        private void AppointEditBtnClick(object sender, RoutedEventArgs e)
        {
            ClearPlannerInputs();
            PlannerDialogTitle.Text = "Edit";
            actionFlag = 'e';
            if (PlannerDataGrid.SelectedItem != null)
            {
                DataRowView row = (DataRowView)PlannerDataGrid.SelectedItems[0];

                PlannerDialogInpName.Text = row["name"].ToString();
                PlannerDialogInpDate.SelectedDate = DateTime.Parse(row["date"].ToString());
                PlannerDialogInpTime.SelectedTime = DateTime.Parse(row["time"].ToString());
                PlannerDialogInpPhone.Text = row["phone"].ToString();
            }
        }
        private void AppointAddBtnClick(object sender, RoutedEventArgs e)
        {
            ClearPlannerInputs();
            PlannerDialogTitle.Text = "Add new Entry";
            actionFlag = 'a';
        }
        private void ClearPlannerInputs()
        {
            PlannerDialogInpName.Text = "";
            PlannerDialogInpDate.SelectedDate = null;
            PlannerDialogInpTime.SelectedTime = null;
            PlannerDialogInpPhone.Text = "";
        }
        private void ReloadBtnClick(object sender, RoutedEventArgs e)
        {
            var data = DataGrid2.ItemsSource;
            DataGrid2.ItemsSource = null;
            DataGrid2.ItemsSource = data;
        }
        private void DeleteBtnClick(object sender, RoutedEventArgs e)
        {
            if (DataGrid2.SelectedItem != null)
            {
                string relativePath = @"db\MainDB.db";

                try
                {
                    using SQLiteConnection sqlCon = new SQLiteConnection($"Data Source={relativePath};Version=3;Max Pool Size=100");
                    sqlCon.Open();
                    SQLiteCommand sqlCom = new SQLiteCommand
                    {
                        Connection = sqlCon
                    };

                    DataRowView row = (DataRowView)DataGrid2.SelectedItems[0];

                    switch (Grid2Title.Text)
                    {
                        case "Medicine":
                            sqlCom.CommandText = $"DELETE FROM Medicine WHERE prodname ='{row["prodname"]}' AND acomp = '{row["acomp"]}' AND company = '{row["company"]}' AND price = '{row["price"]}' AND amount = '{row["amount"]}' ";
                            break;
                        case "Patients":
                            sqlCom.CommandText = $"DELETE FROM {user.GetLogin()}Patients WHERE snils ='{row["snils"]}' AND fname = '{row["fname"]}' AND lname = '{row["lname"]}' AND phone = '{row["phone"]}' AND gender = '{row["gender"]}' AND address = '{row["address"]}' AND job = '{row["job"]}' AND bill = '{row["bill"]}' AND prescription = '{row["prescription"]}' AND room = '{row["room"]}' ";
                            break;
                        case "History":
                            sqlCom.CommandText = $"DELETE FROM {user.GetLogin()}History WHERE snils ='{row["snils"]}' AND name = '{row["name"]}' AND date = '{row["date"]}' AND diagnosis = '{row["diagnosis"]}' AND bill = '{row["bill"]}' ";
                            break;
                    }
                    sqlCom.ExecuteNonQuery();
                    sqlCon.Close();
                    switch (Grid2Title.Text)
                    {
                        case "Medicine":
                            ControlDB("SELECT * FROM Medicine", "Medicine", DataGrid2); break;
                        case "Patients":
                            ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "Patients", "Patients", DataGrid2); break;
                        case "History":
                            ControlDB("SELECT * FROM " + $"{user.GetLogin()}" + "History", "History", DataGrid2); break;
                    }
                }
                catch (SQLiteException ex)
                {
                }
            }
        }
        private void ChangeNameBtnClick(object sender, RoutedEventArgs e)
        {
            actionFlag = 'n';
            DashboardDialogInpName.Text = "";
            DialogChangeUsername.Visibility = Visibility.Visible;
            DialogChangePassword.Visibility = Visibility.Collapsed;
            DashboardDialogTitle.Text = "Change username";
        }
        private void ChangePasswordBtnClick(object sender, RoutedEventArgs e)
        {
            actionFlag = 'p';
            DashboardDialogInpCurPass.Password = "";
            DashboardDialogInpNewPass.Password = "";
            DashboardDialogInpRepPass.Password = "";
            DialogChangeUsername.Visibility = Visibility.Collapsed;
            DialogChangePassword.Visibility = Visibility.Visible;
            DashboardDialogTitle.Text = "Change password";
        }
        private void DashboardDialogAcceptBtnClick(object sender, RoutedEventArgs e)
        {
            switch (actionFlag)
            {
                case 'n':
                    if (!String.IsNullOrEmpty(DashboardDialogInpName.Text))
                    {
                        user.SetName(DashboardDialogInpName.Text);
                        LoginNameProfile.Text = user.GetName();
                        ControlDB($"UPDATE acc SET name = '{user.GetName()}' WHERE log = '{user.GetLogin()}'");
                    }
                    break;
                case 'p':
                    if (!String.IsNullOrEmpty(DashboardDialogInpCurPass.Password) && !String.IsNullOrEmpty(DashboardDialogInpNewPass.Password) && !String.IsNullOrEmpty(DashboardDialogInpRepPass.Password))
                    {
                        if (DashboardDialogInpNewPass.Password == DashboardDialogInpRepPass.Password)
                        {
                            string passCheck = PassCheck(user.GetLogin(), DashboardDialogInpCurPass.Password);

                            switch (passCheck)
                            {
                                case "ok":
                                    ControlDB($"UPDATE acc SET pas = '{CreateHash(DashboardDialogInpNewPass.Password)}' WHERE log = '{user.GetLogin()}'");
                                    break;
                                case "errpass":
                                    MessageBox.Show("Неправильный пароль");
                                    break;
                            }
                        } else
                        {
                            MessageBox.Show("Пароли не совпадают");
                        }
                    } else
                    {
                        MessageBox.Show("Заполните все поля");
                    }
                    break;
            }
        }
        private void RoomInfoBackBtnClick(object sender, RoutedEventArgs e)
        {
            Menu.IsEnabled = true;
            RoomInfo.Visibility = Visibility.Collapsed;
            Grid3.Visibility = Visibility.Visible;
        }
    }
}
