using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Importer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LandingPage : Window
    {
        #region private variables
        Microsoft.Win32.OpenFileDialog openDialog;
        private const string keyForService = "S-F119DA0F-A768-4D2C-A802-5C635F084F9C";

        #endregion

        public LandingPage()
        {
            InitializeComponent();

        }

        #region Properties
        private bool IsFileValid { get; set; }

        private bool IsFileProcessed { get; set; }

        private bool IsValidEmail { get; set; }

        private List<string> Records { get; set; }
        #endregion

        /// <summary>
        /// Open the csv file selector.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.DefaultExt = ".csv";
            openDialog.Filter = "CSV files (.csv)|*.csv";
            openDialog.InitialDirectory = "D";
            openDialog.Multiselect = false;

            var result = openDialog.ShowDialog();
            if (result == true)
            {

                string filename = openDialog.FileName;
                txtFileName.Text = filename;
                //Re set the variables to false when file selected.
                IsFileProcessed = false;
                IsFileValid = false;
            }
        }

        /// <summary>
        /// initiate file validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            DoFileValidation(openDialog.FileName);
        }

        /// <summary>
        /// Initiate file processing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFileProcessed)
                DoFileValidation(openDialog.FileName);

            if (IsFileValid)
            {
                CheckUserAccountExists();
            }

        }

        private void CheckUserAccountExists()
        {
            //Check whether account exists for each email address.
            int emailColIndex = -1;
            string[] columns;
            string Role = string.Empty;
            bool Response = false;

            foreach (var item in Records)
            {
                int index = Records.IndexOf(item);
                if (index != 0)//leave 0th line as that is for column header.
                {

                    var emailData = item.Split(',')[emailColIndex];

                    string service = "http://s-cris.nelsonnet.com.au/AuthService/CheckUserIdExists/" + emailData + "," + keyForService;

                    WebClient proxyClient = new WebClient();
                    var result = proxyClient.DownloadString(service);
                    XElement doc = XElement.Parse(result);

                    foreach (var dataItem in doc.Elements())
                    {
                        switch (dataItem.Name.LocalName)
                        {
                            case "Response":
                                Response = bool.Parse(dataItem.Value);
                                break;
                            case "ResultCode":
                                break;
                            case "ResultDescription":
                                break;
                            case "Role":
                                Role = dataItem.Value;
                                break;
                        }
                    }


                }
                else
                {
                    columns = item.Split(';');
                    var emailCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Email"));
                    emailColIndex = columns[0].Split(',').ToList().IndexOf(emailCol);
                }
            }
        }

        private void proxyClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {

        }

        /// <summary>
        /// Validating the file contents.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 
        private void DoFileValidation(string fileName)
        {
            if (openDialog != null && openDialog.SafeFileName.Length > 0)
            {
                string[] totalNumberOfLines = File.ReadAllLines(fileName);

                Records = totalNumberOfLines.ToList();
                //Get the number of columns.
                string[] columns = totalNumberOfLines[0].Split(';');
                //
                var emailCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Email"));
                var emailColIndex = columns[0].Split(',').ToList().IndexOf(emailCol);
                //Reading corresponding data for email column
                for (int iCnt = 1; iCnt < totalNumberOfLines.Count(); iCnt++)
                {
                    var emailData = totalNumberOfLines[iCnt].Split(',')[emailColIndex];
                    //Check emiail validity
                    var validEmailExist = emailData.Contains("@");
                    if (validEmailExist)

                        IsValidEmail = true;
                    else
                    {
                        IsValidEmail = false;
                        LogWriter.WriteLog(AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day, "Line " + iCnt + " email is not correct");
                        break;
                    }

                }

                if (IsValidEmail)
                    IsFileValid = true;
                else
                    IsFileValid = false;

                if (!IsFileValid)
                    MessageBox.Show("Email data is not valid.Please check the log file created at " + AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day);

                IsFileProcessed = true;

            }
            else
            {
                MessageBox.Show("Please select a file to validate.");
            }
        }


    }
}
