using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
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
        #region Private variables
        Microsoft.Win32.OpenFileDialog openDialog;
        private LogWriter logger;
        private const string keyForService = "S-F119DA0F-A768-4D2C-A802-5C635F084F9C";
        private const string accountCheckService = "http://s-cris.nelsonnet.com.au/AuthService/CheckUserIdExists/";
        private const string accountCreationService = "http://s-cris.nelsonnet.com.au/AuthService/CreateStudentAccount/";
        private const string roleService = "http://s-cris.nelsonnet.com.au/AuthService/ConsumeAccessCode/";
        BackgroundWorker backgroundThread;
        #endregion

        #region Page Constructor
        public LandingPage()
        {
            InitializeComponent();
            //Write log setting(initial setting values)
            LogFileSetting();

        }
        #endregion

        #region Properties
        /// <summary>
        /// Is file valid to process
        /// </summary>
        private bool IsFileValid { get; set; }
        /// <summary>
        /// is file validated.
        /// </summary>
        private bool IsFileProcessed { get; set; }
        /// <summary>
        /// Is email valid.
        /// </summary>
        private bool IsValidEmail { get; set; }
        /// <summary>
        /// Number of records in csv file.
        /// </summary>
        private List<string> Records { get; set; }

        private bool IsAllTheRecordSucceeded { get; set; }

        #endregion

        #region Methods

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
            if (openDialog != null && openDialog.FileName.Length > 0)
            {
                //Starting a background process
                if (backgroundThread == null)
                    backgroundThread = new BackgroundWorker();
                backgroundThread.DoWork += backgroundThread_DoEmailValidation;
                backgroundThread.RunWorkerCompleted += backgroundThread_RunEmailValidationCompleted;
                backgroundThread.RunWorkerAsync();
                busyIndicator.IsBusy = true;
            }
            else
                MessageBox.Show("Please select a file to validate.");
        }

        /// <summary>
        /// Initiate file processing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (openDialog != null && openDialog.FileName != "")
            {
                if (backgroundThread == null)
                    backgroundThread = new BackgroundWorker();

                backgroundThread.RunWorkerCompleted += backgroundThread_RunWorkerCompleted;
                backgroundThread.DoWork += backgroundThread_DoWork;
                backgroundThread.RunWorkerAsync();
                busyIndicator.IsBusy = true;
                IsAllTheRecordSucceeded = true;
            }
            else
                MessageBox.Show("Please select a file to process.");


        }

        /// <summary>
        /// Email validation thread start.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundThread_DoEmailValidation(object sender, DoWorkEventArgs e)
        {
            if (openDialog != null && openDialog.FileName.Length > 0)
                DoFileValidation(openDialog.FileName);
            else
                MessageBox.Show("Please select a file to validate.");
        }

        /// <summary>
        /// Background thread doWork
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundThread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Start file validation(if not done through validate button) and processing.
            if (!IsFileProcessed)
                DoFileValidation(openDialog.FileName);
            if (IsFileValid)
            {
                ProcessRecordsInFile();
            }
        }

        /// <summary>
        /// Email validation thread completes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundThread_RunEmailValidationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            busyIndicator.IsBusy = false;
            //Un-subscribe the background thread events
            backgroundThread.DoWork -= backgroundThread_DoEmailValidation;
            backgroundThread.RunWorkerCompleted -= backgroundThread_RunEmailValidationCompleted;
        }

        /// <summary>
        /// Notification from bakcground thread when it completes action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Set the busy indicator.
            busyIndicator.IsBusy = false;
            if (IsFileValid)
            {
                if (e.Error == null && IsAllTheRecordSucceeded == true)
                    MessageBox.Show("Import successful.");
                else if (e.Error == null && IsAllTheRecordSucceeded == false)
                    MessageBox.Show("All records could not be processed successfully." + Environment.NewLine + " Please check log file created at " + AppDomain.CurrentDomain.BaseDirectory);
                else
                {

                    WriteErrorMessageToLog(e.Error.Message, e.Error);
                    //string filename = AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;
                    MessageBox.Show("Some error has occured while importing data.Check the log file created at " + AppDomain.CurrentDomain.BaseDirectory);
                }

            }
            //Reset the open diallog box.
            openDialog.Reset();
            txtFileName.Text = string.Empty;//Reset the filename.
            //Un-subscribe the background thread events
            backgroundThread.DoWork -= backgroundThread_DoWork;
            backgroundThread.RunWorkerCompleted -= backgroundThread_RunWorkerCompleted;
        }

        /// <summary>
        /// Create role for user.
        /// </summary>
        private void CreateRoleForUser(string email, string IACCode, WebClient proxyClient, int index)
        {
            string service = roleService + email + "," + IACCode + "," + keyForService;
            var response = proxyClient.DownloadString(service);

            XElement doc = XElement.Parse(response);
            var responseobject = doc.Elements().FirstOrDefault(c => c.Name.LocalName == "ResultCode");
            if (responseobject != null)
                if (responseobject.Value == "0")//for valid operation.result code =0
                {
                    WriteMessageToLog("Role creation is successfull for email id- " + email + "for IAC code- " + IACCode, index, ErrorType.Info);

                }
                else
                {
                    WriteMessageToLog("Role could not be created for email id- " + email + "for IAC code- " + IACCode, index, ErrorType.Error);
                    IsAllTheRecordSucceeded = false;
                }
        }

        /// <summary>
        /// Process data in imported file.
        /// </summary>
        private void ProcessRecordsInFile()
        {
            //Check whether account exists for each email address.
            int emailColIndex = -1;
            int firstNameColIndex = -1;
            int lastNameColIndex = -1;
            int passwordColIndex = -1;
            int iAcColIndex = -1;
            int iCountryColIndex = -1;
            int iStateColIndex = -1;
            int iSchoolColIndex = -1;

            string[] columns;

            WebClient proxyClient = new WebClient(); ;
            XElement doc;
            foreach (var item in Records)
            {
                int index = Records.IndexOf(item);
                if (index != 0)//leave 0th line as that is for column header.
                {
                    //Get the email id.
                    var emailData = item.Split(',')[emailColIndex];

                    string service = accountCheckService + emailData + "," + keyForService;
                    try
                    {
                        var result = proxyClient.DownloadString(service);
                        doc = XElement.Parse(result);

                        var resultobject = doc.Elements().FirstOrDefault(c => c.Name.LocalName == "Response");
                        if (resultobject != null)
                            if (!bool.Parse(resultobject.Value))
                            {
                                WriteMessageToLog("Account is not mapped for email id - " + emailData, index + 1, ErrorType.Error);
                                bool isSuccess = CreateUserAccount(firstNameColIndex, lastNameColIndex, passwordColIndex, iCountryColIndex, iStateColIndex, iSchoolColIndex,
                                    proxyClient, doc, item, index, emailData);
                                if (isSuccess)
                                {
                                    var iacData = item.Split(',')[iAcColIndex];
                                    if (iacData.Length != 0)
                                    {
                                        //Create user role
                                        foreach (var iacItem in iacData.Split('|').ToList())
                                        {
                                            //Create the Role for valid account and emailID.
                                            CreateRoleForUser(emailData, iacItem, proxyClient, index);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                WriteMessageToLog("Account exists for email id - " + emailData, index + 1, ErrorType.Error);
                                var iacData = item.Split(',')[iAcColIndex];
                                if (iacData.Length != 0)
                                {
                                    //Create user role
                                    foreach (var iacItem in iacData.Split('|').ToList())
                                    {
                                        CreateRoleForUser(emailData, iacItem, proxyClient, index);
                                    }
                                }
                            }
                    }
                    catch (Exception ex)
                    {
                        WriteMessageToLog("Could not able to proceede.Some error has occured  while processing.", index + 1, ErrorType.Error);
                        WriteErrorMessageToLog(ex.Message, ex);
                        throw ex;
                    }

                }
                else
                {
                    //Get the header column index of each column.
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

                    var iCountryCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Country", StringComparison.InvariantCultureIgnoreCase));
                    iCountryColIndex = columns[0].Split(',').ToList().IndexOf(iCountryCol);

                    var iStateCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("State", StringComparison.InvariantCultureIgnoreCase));
                    iStateColIndex = columns[0].Split(',').ToList().IndexOf(iStateCol);

                    var iSchoolCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("School", StringComparison.InvariantCultureIgnoreCase));
                    iSchoolColIndex = columns[0].Split(',').ToList().IndexOf(iSchoolCol);

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
        private bool CreateUserAccount(int firstNameColIndex, int lastNameColIndex, int passwordColIndex, int iCountryColIndex, int iStateColIndex, int iSchoolColIndex,
            WebClient proxyClient, XElement doc,
            string item, int index, string emailData)
        {
            bool isAccountCreated = false;
            //Get first name ,last name etc
            var firstName = item.Split(',')[firstNameColIndex];
            var lastName = item.Split(',')[lastNameColIndex];
            var Password = item.Split(',')[passwordColIndex];
            var Country = item.Split(',')[iCountryColIndex];
            var State = item.Split(',')[iStateColIndex];
            var School = item.Split(',')[iSchoolColIndex];


            //If all the information is present then go ahead and create the account.
            if (string.IsNullOrEmpty(firstName) == false && string.IsNullOrEmpty(lastName) == false && string.IsNullOrEmpty(Password) == false)
            {
                try
                {
                    //Get Country and State.

                    string retrivedCountry = Utility.ConvertToCountry(Country).ToString();
                    string retrivedState = Utility.ConvertToState(State).ToString();

                    if (string.IsNullOrEmpty(Password))
                    {
                        WriteMessageToLog("Password is empty.Generating new password.-", index, ErrorType.Info);
                        string generatedPassword = Utility.GeneratePassword(8);
                        Password = generatedPassword;
                    }

                    string userAccounService = accountCreationService + emailData + "," + Password + "," + firstName + "," + lastName + "," + retrivedCountry + "," + retrivedState + "," + School + "," + keyForService;
                    var accountData = proxyClient.DownloadString(userAccounService);

                    doc = XElement.Parse(accountData);
                    var responseobject = doc.Elements().FirstOrDefault(c => c.Name.LocalName == "ResultDescription");
                    if (responseobject != null)
                        if (responseobject.Value == "Success")
                        {
                            WriteMessageToLog("Account creation is successfull for email id- " + emailData, index, ErrorType.Info);
                            isAccountCreated = true;
                        }
                        else
                        {
                            WriteMessageToLog("Account could not be created for email id- " + emailData, index, ErrorType.Error);
                            IsAllTheRecordSucceeded = false;
                        }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else//if any information is missing then log it.
                WriteMessageToLog("Data is missing ", index, ErrorType.Error);

            return isAccountCreated;
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
                IsFileValid = true;
                this.logger.LogInfo("File validation is being done.");

                Records = totalNumberOfLines.ToList();
                //Get the number of columns.
                string[] columns = totalNumberOfLines[0].Split(';');
                //
                var emailCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Email"));
                var emailColIndex = columns[0].Split(',').ToList().IndexOf(emailCol);
                var iCOuntryCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Country"));
                var iCOuntryIndex = columns[0].Split(',').ToList().IndexOf(iCOuntryCol);
                var iStateCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("State"));
                var iStateIndex = columns[0].Split(',').ToList().IndexOf(iStateCol);
                var iFrstNameCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("First Name"));
                var iFirstNameIndex = columns[0].Split(',').ToList().IndexOf(iFrstNameCol);
                var iLstNameCol = columns[0].Split(',').FirstOrDefault(x => x.StartsWith("Last Name"));
                var iLastNameIndex = columns[0].Split(',').ToList().IndexOf(iLstNameCol);
                //Reading corresponding data for email column
                for (int iCnt = 1; iCnt < totalNumberOfLines.Count(); iCnt++)
                {
                    var emailData = totalNumberOfLines[iCnt].Split(',')[emailColIndex];
                    var country = totalNumberOfLines[iCnt].Split(',')[iCOuntryIndex];
                    var state = totalNumberOfLines[iCnt].Split(',')[iStateIndex];

                    string retrivedCountry = Utility.ConvertToCountry(country).ToString();
                    string retrivedState = Utility.ConvertToState(state).ToString();
                    string firstName = totalNumberOfLines[iCnt].Split(',')[iFirstNameIndex];
                    string lastName = totalNumberOfLines[iCnt].Split(',')[iLastNameIndex];

                    if (retrivedCountry == "NA" || retrivedState == "NA")
                    {
                        WriteMessageToLog("Country or State name is not valid- ", iCnt+1, ErrorType.Info);
                        IsFileValid = false;
                    }

                    if (string.IsNullOrEmpty(firstName) == true || string.IsNullOrEmpty(lastName) == true)
                    {
                        WriteMessageToLog("First Name or Last name is not valid- ", iCnt+1, ErrorType.Info);
                        IsFileValid = false;
                    }
                    //Check emiail validity
                    var validEmailExist = IsValidEmailAddressByRegex(emailData);
                    if (!validEmailExist)
                    {
                        IsFileValid = false;
                        WriteMessageToLog("Email is not correct" + "at line number- ", iCnt+1, ErrorType.Error);
                        break;
                    }
                }

                if (!IsFileValid)
                    MessageBox.Show("File is not valid.Please check the log file created at " + AppDomain.CurrentDomain.BaseDirectory);

                IsFileProcessed = true;
                this.logger.LogInfo("File validation is complete.Check the log file for information");
            }
            else
            {
                MessageBox.Show("Please select a file.");
            }
        }

        /// <summary>
        /// Write log in a log file.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="lineNumber"></param>
        private void WriteMessageToLog(string message, int lineNumber, ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.Error:
                    this.logger.LogError(message + " at linenumber- " + lineNumber);
                    break;
                case ErrorType.Info:
                    this.logger.LogInfo(message + " at linenumber- " + lineNumber);
                    break;
            }
        }

        /// <summary>
        /// Write error messages to log file
        /// </summary>
        /// <param name="message"></param>
        private void WriteErrorMessageToLog(string msg, Exception ex)
        {
            this.logger.LogError(msg, ex);
        }

        /// <summary>
        /// Is email is valid.
        /// </summary>
        /// <param name="mailAddress"></param>
        /// <returns></returns>
        public bool IsValidEmailAddressByRegex(string mailAddress)
        {
            Regex mailIDPattern = new Regex(@"[\w-]+@([\w-]+\.)+[\w-]+");//Regular expression to check email id is valid or not.

            //Check whether mail id is valid.
            if (!string.IsNullOrEmpty(mailAddress) && mailIDPattern.IsMatch(mailAddress))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Default settings for the log file.
        /// </summary>
        private void LogFileSetting()
        {
            //Create the logger file.
            this.logger = new LogWriter(MethodBase.GetCurrentMethod().DeclaringType);
            this.logger.LogInfo("Logger started");

        }

        #endregion


    }
}
