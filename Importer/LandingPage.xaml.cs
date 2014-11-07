using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        BackgroundWorker backgroundThread;
        #endregion

        public LandingPage()
        {
            InitializeComponent();
            LogFileSetting();

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

            openDialog.InitialDirectory = @"D:\";
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
                backgroundThread = new BackgroundWorker();
                backgroundThread.RunWorkerCompleted += backgroundThread_RunWorkerCompleted;
                backgroundThread.DoWork += backgroundThread_DoWork;
                backgroundThread.RunWorkerAsync();
                busyIndicator.IsBusy = true;
            }

        }

        private void backgroundThread_DoWork(object sender, DoWorkEventArgs e)
        {

            ProcessRecordsInFile();
        }

        private void backgroundThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            busyIndicator.IsBusy = false;
        }

        private void ProcessRecordsInFile()
        {
            //Check whether account exists for each email address.
            int emailColIndex = -1;
            int firstNameColIndex = -1;
            int lastNameColIndex = -1;
            int passwordColIndex = -1;
            int iAcColIndex = -1;

            string[] columns;

            WebClient proxyClient = new WebClient(); ;
            XElement doc;
            foreach (var item in Records)
            {
                int index = Records.IndexOf(item);
                if (index != 0)//leave 0th line as that is for column header.
                {

                    var emailData = item.Split(',')[emailColIndex];

                    string service = "http://s-cris.nelsonnet.com.au/AuthService/CheckUserIdExists/" + emailData + "," + keyForService;
                    try
                    {
                        var result = proxyClient.DownloadString(service);
                        doc = XElement.Parse(result);

                        var resultobject = doc.Elements().FirstOrDefault(c => c.Name.LocalName == "Response");
                        if (resultobject != null)
                            if (!bool.Parse(resultobject.Value))
                            {
                                WriteMessageToLog("Account is not mapped", index + 1);
                                bool isSuccess = CreateUserAccount(firstNameColIndex, lastNameColIndex, passwordColIndex, proxyClient, doc, item, index, emailData);
                                if (isSuccess)
                                {
                                    var iacData = item.Split(',')[iAcColIndex];
                                    //if(!string.IsNullOrEmpty(iacData))
                                    //TODO: next processing.
                                }
                            }
                            else
                            {
                                WriteMessageToLog("Account exists.", index + 1);
                                var iacData = item.Split(',')[iAcColIndex];
                                //if(!string.IsNullOrEmpty(iacData))
                                //TODO: next processing.
                            }
                    }
                    catch (Exception ex)
                    {
                        WriteMessageToLog("Could not able to proceede.Some error has occured..", index + 1);
                        WriteErrorMessageToLog(ex.Message);
                    }

                }
                else
                {
                    columns = item.Split(';');
                    var emailCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Email", StringComparison.InvariantCultureIgnoreCase));
                    emailColIndex = columns[0].Split(',').ToList().IndexOf(emailCol);
                    var firstNameCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("First Name", StringComparison.InvariantCultureIgnoreCase));
                    firstNameColIndex = columns[0].Split(',').ToList().IndexOf(firstNameCol);

                    var lastNameCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Last Name", StringComparison.InvariantCultureIgnoreCase));
                    lastNameColIndex = columns[0].Split(',').ToList().IndexOf(lastNameCol);

                    var passwordCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Password", StringComparison.InvariantCultureIgnoreCase));
                    passwordColIndex = columns[0].Split(',').ToList().IndexOf(passwordCol);

                    var iAcCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("IAC", StringComparison.InvariantCultureIgnoreCase));
                    iAcColIndex = columns[0].Split(',').ToList().IndexOf(iAcCol);

                }
            }
        }

        /// <summary>
        /// Create User Account.
        /// </summary>
        /// <param name="firstNameColIndex"></param>
        /// <param name="lastNameColIndex"></param>
        /// <param name="passwordColIndex"></param>
        /// <param name="proxyClient"></param>
        /// <param name="doc"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <param name="emailData"></param>
        /// <returns></returns>
        private bool CreateUserAccount(int firstNameColIndex, int lastNameColIndex, int passwordColIndex, WebClient proxyClient, XElement doc, string item, int index, string emailData)
        {
            bool isAccountCreated = false;
            //Check first name ,last name
            var firstName = item.Split(',')[firstNameColIndex];
            var lastName = item.Split(',')[lastNameColIndex];
            var Password = item.Split(',')[passwordColIndex];

            //If all the information is present then go ahead and create the account.
            if (string.IsNullOrEmpty(firstName) == false && string.IsNullOrEmpty(lastName) == false && string.IsNullOrEmpty(Password) == false)
            {
                try
                {
                    string userAccounService = "http://s-cris.nelsonnet.com.au/AuthService/CreateStudentAccount/" + emailData + "," + Password + "," + firstName + "," + lastName + "," + keyForService;
                    var accountData = proxyClient.DownloadString(userAccounService);

                    doc = XElement.Parse(accountData);
                    var responseobject = doc.Elements().FirstOrDefault(c => c.Name.LocalName == "ResultDescription");
                    if (responseobject != null)
                        if (responseobject.Value == "Success")
                        {
                            WriteMessageToLog("Account creation is successfull", index);
                            isAccountCreated = true;
                        }
                        else
                        {
                            WriteMessageToLog("Account could not be created", index);
                        }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else//if any information is missing then log it.
                WriteMessageToLog("Data is missing", index);

            return isAccountCreated;
        }

        /// <summary>
        /// Add permission to user.
        /// </summary>
        /// <param name="lineNumber"></param>
        private void AddPermission(int lineNumber)
        {
            try
            {

            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                        WriteMessageToLog("email is not correct", iCnt);

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

        private void WriteMessageToLog(string message, int lineNumber)
        {
            LogWriter.WriteLog(AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day, lineNumber != 0 ? "Line " + lineNumber +": "+ message :"");
        }

        private void WriteErrorMessageToLog(string message)
        {
            LogWriter.WriteLog(AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day, message);
        }

        private void LogFileSetting()
        {
            WriteMessageToLog("", 0);
            WriteMessageToLog("", 0);
            WriteMessageToLog("", 0);
            WriteMessageToLog("Log start time: " + DateTime.Now + "------------------------", 0);
            WriteMessageToLog("", 0);
        }


    }
}
