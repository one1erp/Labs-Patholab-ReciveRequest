using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ADODB;

using LSExtensionWindowLib;
using System.Data.Entity;
using LSSERVICEPROVIDERLib;
using Microsoft.Win32;
using Patholab_Common;
using Patholab_XmlService;
using Recive_Request.Classes;
using Patholab_DAL_V1;
using Patholab_DAL_V1.Enums;
using Control = System.Windows.Controls.Control;
using DateTimePicker = Xceed.Wpf.Toolkit.DateTimePicker;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
//using MessageBox = Xceed.Wpf.Toolkit.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using Timer = System.Windows.Forms.Timer;
using UserControl = System.Windows.Controls.UserControl;
using RequestRemarkNet;
using System.IO;


namespace Recive_Request.Controls.ReceivePages
{
    /// <summary>
    /// Interaction logic for MasterPage.xaml
    /// </summary>
    public partial class MasterPage : UserControl, ISdgRequest
    {





        #region Ctor

        public MasterPage(INautilusServiceProvider sp, INautilusProcessXML xmlProcessor, INautilusDBConnection _ntlsCon,
                          IExtensionWindowSite2 _ntlsSite, INautilusUser _ntlsUser)
        {




            //       Debugger.Launch();
            InitializeComponent();
            //     this.SetResourceReference(Control.BackgroundProperty, System.Drawing.Color.FromName("Control"));
            this.ServiceProvider = sp;
            this.xmlProcessor = xmlProcessor;
            this._ntlsCon = _ntlsCon;
            this._ntlsSite = _ntlsSite;
            this._ntlsUser = _ntlsUser;

            contentArea.IsEnabled = false;
            //EnableBottomGrid(false);
            btnOK.IsEnabled = false;
            btnSusspened.IsEnabled = false;




            var mandatoryBrush = Constants.MANDATORYBRUSH;


            InitializationMandaoryFields(mandatoryBrush);
        }



        #endregion


        #region Private fields


        private INautilusProcessXML xmlProcessor;
        private INautilusUser _ntlsUser;
        private IExtensionWindowSite2 _ntlsSite;
        private List<ISdgRequest> pages;
        private PapPage papPage;
        private CytologyPage cytologyPage;
        public INautilusDBConnection _ntlsCon;
        public static bool DEBUG;
        private HistologyPage histologyPage;
        private U_CONTAINER _currentContainer;

        private bool _susspened;
        private U_START_SCREEN_USER _defaultStartScreen;
        private const string rdbHisName = "rdbHis";
        private const string rdbCytName = "rdbCyt";
        private const string rdbPapName = "rdbPap";
        const string emptysampleContainerMsg = "התקבלו Y / X צנצנות מתוך קבלת המשלוח";
        private Timer _timerFocus = null;
        public static bool IsQcRole = false;



        #endregion

        #region IRecive implementation

        public DataLayer dal
        {
            get;
            set;
        }

        public INautilusServiceProvider ServiceProvider
        {
            get;
            set;
        }



        //   public CLIENT CurrentClient { get; set; }

        public ManageMetaData Manage_MetaData
        {
            get;
            set;
        }

        public U_PARTS SelectedPart
        {
            get;
            set;
        }

        public ListData ListData
        {
            get;
            set;
        }
        public bool IsAssuta
        {
            get;
            set;
        }

        public SDG CurrentSdg
        {
            get;
            set;
        }

        public Connection Con
        {
            get;
            set;
        }


 

        public IEnumerable<FrameworkElement> SetTags()
        {
            cbQc.Tag = "U_RECEIVE_QC";
            txtQcMark.Tag = "U_RECEIVE_QC";
            cmbSuspensioCause.Tag = "U_SUSPENSION_CAUSE";
            btnSusspened.Tag = "U_SUSPENSION_CAUSE";
            checkBoxPrintCassette.Tag = "DO_NOT_CLEAR";
            comboBoxPrinter.Tag = "DO_NOT_CLEAR";

            dtRecived_on.Tag = Constants.MANDATORYTAG; // "U_DATE_ARRIVAL";
            var allTagedControls =
                MasterGrid.FindVisualChildren<Control>()
                          .Where(tb => tb.Tag != null && tb.Tag.ToString() != Constants.MANDATORYTAG);
            return allTagedControls;

        }

        public FrameworkElement GetParentElement()
        {
            return MasterGrid;
        }

        public bool SpecialValidation()
        {
            if (string.IsNullOrEmpty(txtContainer.Text))
            {
                txtContainer.Background = Constants.INVALIDVALUEBRUSH;
                return false;
            }
            //  if (!ValidateRecivedDate(dtRecived_on, true))
            //    return false;

            return true;
        }

        public void SetContainerDetails(string sampleMsg)
        {
            lblRecivedContainer.Text = sampleMsg;
        }

        public List<FrameworkElement> GetControls4Enter()
        {
            var controls = new List<FrameworkElement>
                {
                    rdbCyt,
                    rdbHis,
                    rdbPap,
                    cbQc,
                    txtQcMark,
                    CmbParts,
                    txtContainer,
                    dtRecived_on

                };
            return controls;
        }

        private void setComboBoxPrinter()
        {
            try
            {
                PHRASE_HEADER header = dal.FindBy<PHRASE_HEADER>(ph => ph.NAME.Equals("Vega Printer", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (header != null)
                {
                    comboBoxPrinter.ItemsSource = header.PHRASE_ENTRY;
                    comboBoxPrinter.DisplayMemberPath = "PHRASE_NAME";
                }
                PHRASE_ENTRY entry = header.PHRASE_ENTRY.Where(pe => pe.PHRASE_INFO != null && pe.PHRASE_INFO.Equals("Print Cassette", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (entry != null) comboBoxPrinter.SelectedItem = entry;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on finding Vega Printer phrase." + Environment.NewLine + ex.Message);
            }
        }

        public void DisplayRequestDetails()
        {
            SetPicture(CurrentSdg.STATUS);
            var sdgUser = CurrentSdg.SDG_USER;

            if (sdgUser.U_CONTAINER != null)
            {
                txtContainer.Text = sdgUser.U_CONTAINER.NAME;
                _currentContainer = sdgUser.U_CONTAINER;
            }
            U_ORDER currentOrder =
                CurrentSdg.SDG_USER.U_ORDER;

            if (currentOrder != null)
                CmbParts.SelectedItem = currentOrder.U_ORDER_USER.U_PARTS;


            SetContainerData();
            cbQc.IsChecked = sdgUser.U_IS_QC == "T";
            txtQcMark.Text = sdgUser.U_RECEIVE_QC;
            txtInternalNbr.Text = CurrentSdg.NAME;
            if (string.IsNullOrEmpty(sdgUser.U_PATHOLAB_NUMBER))
            {
                MessageBox.Show("לא קיים מס פתולאב\n לא ניתן להמשיך.", Constants.MboxCaption, MessageBoxButton.OK,
                                MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
            }
            txtPathoNbr.Text = sdgUser.U_PATHOLAB_NUMBER;

            //  dtRecived_on.Value = CurrentSdg.DELIVERY_DATE;
            SetSdgType();
            cmbSuspensioCause.SelectedItem = sdgUser.U_SUSPENSION_CAUSE;
            pages.ForEach(p =>
                {

                    p.CurrentSdg = CurrentSdg;
                    p.DisplayRequestDetails();
                });

        }

        public void DisplayNew()
        {
            if (CurrentSdg == null)
            {
                txtInternalNbr.Text = "";
                txtPathoNbr.Text = "";

                pages.ForEach(p => p.DisplayNew());

                return;
            }





            spRdb.IsEnabled = CurrentSdg == null;


            if ("UVP".Contains(CurrentSdg.STATUS))
            {
                //מאפשר עריכה של הפקדים
                Manage_MetaData.EnabledAll(GetParentElement(), true);
                pages.Foreach(p => p.Manage_MetaData.EnabledAll(p.GetParentElement(), true));


                //שדות שבכל מצב לא ניתנים לעריכה
                CmbParts.IsEnabled = false;
                txtContainer.IsEnabled = false;
                dtRecived_on.IsEnabled = false;

                //Controls dependent role
                cbQc.IsEnabled = IsQcRole;
                txtQcMark.IsEnabled = IsQcRole;
            }
            else
            {
                //If status isn't "V    or U or P" data won't be changed
                Manage_MetaData.EnabledAll(GetParentElement(), false);
                pages.Foreach(p => p.Manage_MetaData.EnabledAll(p.GetParentElement(), false));


                txtInternalNbr.IsEnabled = true;
                txtPathoNbr.IsEnabled = true;
                btnClose.IsEnabled = true;
                btnClean.IsEnabled = true;

            }



            btnBack.IsEnabled = false;






        }

        public void UpdateRequest()
        {
            bool statusIsU = CurrentSdg.STATUS.Equals("U");
            string msg;
            //histologyPage.comboOrgan
            CurrentSdg.SDG_USER.U_RECEIVE_QC = txtQcMark.Text;
            CurrentSdg.SDG_USER.U_IS_QC = cbQc.IsChecked == true ? "T" : "F";


            // 29/11/21 Or - שמתי בהערה כי שינוי הסטטוס דפק המון פעולות אחרות במסך שהצריכו בדיקת סטטוס.
            //CurrentSdg.SAMPLEs.OrderBy(x => x.SAMPLE_ID).Foreach(x => x.ALIQUOTs.OrderBy(y => y.ALIQUOT_ID).Foreach(y => y.STATUS = "V"));
            //CurrentSdg.SAMPLEs.Foreach(x => x.STATUS = "V");
            //CurrentSdg.STATUS = "V";
            //dal.SaveChanges();

            pages.ForEach(panel => panel.UpdateRequest());
            //CurrentSdg = dal.FindBy<SDG>(x => x.SDG_ID == CurrentSdg.SDG_ID).FirstOrDefault();
            //CurrentSdg.SAMPLEs.OrderBy(x => x.SAMPLE_ID).Foreach(x => x.ALIQUOTs.OrderBy(y => y.ALIQUOT_ID).Foreach(y => y.STATUS = "V"));
            dal.SaveChanges();
            if (_susspened)
            {
                CurrentSdg.STATUS = "S";
            }

            //Ashi If is assuta request 14/04/21
            if (statusIsU)
            {
                PrintLabel(CurrentSdg);
                msg = CmbParts.Text + " דרישה התקבלה במעבדה ";
                try
                {
                    CurrentSdg.DELIVERY_DATE = dtRecived_on.Value;
                    dal.SaveChanges();
                }

                catch(Exception ex) {
                    Logger.WriteLogFile("changing delivery date failed" + ex);
                }
                dal.InsertToSdgLog(CurrentSdg.SDG_ID, "Request received", (long)_ntlsCon.GetSessionId(),
                               msg);
                dal.SaveChanges();
            }


            MessageBox.Show(CurrentSdg.SDG_USER.U_PATHOLAB_NUMBER + " עודכנה דרישה", Constants.MboxCaption,
                            MessageBoxButton.OK, MessageBoxImage.Information);

            msg = "דרישה עודכנה";
            dal.InsertToSdgLog(CurrentSdg.SDG_ID, "Request received", (long)_ntlsCon.GetSessionId(),
                           msg);
            dal.SaveChanges();



            if (checkBoxPrintCassette.IsChecked.Value)
            {
                printAliquots(dal, CurrentSdg, statusIsU);
            }
        }

        public void UpdateOnInsert(SDG newSDg)
        {
            pages.Foreach(p => p.UpdateOnInsert(newSDg));
            if (_susspened)
            {
                newSDg.STATUS = "S";
            }
            //Ashi ממשק אסותא
            if (newSDg.STATUS == "U")
            {
                newSDg.STATUS = "V";
                newSDg.SAMPLEs.Foreach(x => x.STATUS = "V");
                newSDg.SAMPLEs.Foreach(x => x.ALIQUOTs.Foreach(y => y.STATUS = "V"));
            }
        }

   

        public void InsertRequest(LoginXmlHandler loginSdg1)
        {


            //Add order
            LoginXmlHandler loginOrder = new LoginXmlHandler(ServiceProvider, "NEW-ORDER ");
            loginOrder.CreateLoginXml("U_Order", ListData.ReceiveWorkflows.PhraseEntriesDictonary["NEW_ORDER"]);
            loginOrder.AddProperties("U_PARTS_ID", CmbParts.Text);
            pages.ForEach(x => x.InsertOrder(loginOrder));
            var sucssess = loginOrder.ProcssXml();


            if (sucssess) //לשאול את זיו האם במקרה שיצירת הזמנה נכשלת האם להמשיך עם הדרישה
            {
                var orderName = loginOrder.GetValueByTagName("NAME");
                LoginXmlHandler loginSdg = new LoginXmlHandler(ServiceProvider, "NEW-SDG ");
                string workflowName = GetWorkflowName();

                if (workflowName == "")
                {
                    return; //Exit function
                }
                loginSdg.CreateLoginXml("SDG", workflowName);
                loginSdg.AddProperties("U_RECEIVE_QC", txtQcMark.Text);
                loginSdg.AddProperties("U_IS_QC", NautilsuBoolean.ConvertBack(cbQc.IsChecked.Value));
                loginSdg.AddProperties("DELIVERY_DATE", dtRecived_on.Text);


                if (cmbSuspensioCause.SelectedValue != null)
                    loginSdg.AddProperties("U_SUSPENSION_CAUSE", cmbSuspensioCause.SelectedValue.ToString());

                loginSdg.AddProperties("U_CONTAINER_ID", _currentContainer.NAME);
                loginSdg.AddProperties("U_ORDER_ID", orderName);
                pages.ForEach(panel => panel.InsertRequest(loginSdg));


                var res = loginSdg.ProcssXml();
                if (res)
                {

                    //Get Created sdg name
                    var sdgName = loginSdg.GetValueByTagName("NAME");


                    var newsdg = dal.FindBy<SDG>(s => s.NAME == sdgName)
                        .Include(x => x.SDG_USER)
                        .Include(x => x.SDG_USER.U_ORDER)
                        .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER)
                        .Include(x => x.SDG_USER.IMPLEMENTING_PHYSICIAN)
                        .Include(x => x.SDG_USER.IMPLEMENTING_PHYSICIAN.SUPPLIER_USER)
                    .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER.U_CUSTOMER1).FirstOrDefault();
                    //string syntax = CreatePatholabNumber(newsdg);
                    //dal.RunQuery(string.Format("Update lims_sys.sdg_user set U_PATHOLAB_NUMBER='{0}' where sdg_id={1}",
                    //                           syntax, newsdg.SDG_ID));
                    //Update data not by xml processor


                    if (checkBoxPrintCassette.IsChecked.Value)
                    {
                        printAliquots(dal, newsdg, newsdg.STATUS == "U");
                    }

                    UpdateOnInsert(newsdg);





                    string msg = CmbParts.Text + " - הדרישה נקלטה במערכת ";
                    dal.InsertToSdgLog(newsdg.SDG_ID, "Request received", (long)_ntlsCon.GetSessionId(),
                                   msg);


                    //Updates patholab number just after login, for preventing fails
                    dal.SaveChanges();
                    try
                    {
                        string dup = dal.OTHER_SDG_FOR_PATIENT(newsdg.SDG_ID.ToString());
                        if (!string.IsNullOrEmpty(dup))
                        {
                            MessageBox.Show(" : הפניות נוספות לנבדק שנתקבלו בפתולאב" + dup, Constants.MboxCaption,
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLogFile(ex);
                    }

                    //שורה זאת נועדה ע"מ לרפרש את האובייקט ציידנית ולהציג את הנתונים החדשים לאחר השמירה
                    _currentContainer = newsdg.SDG_USER.U_CONTAINER;
                    //בדיקה האם הציידנית הסתיימה
                    CheckContainerStatus();

                    txtInternalNbr.Text = sdgName;
                    txtPathoNbr.Text = newsdg.SDG_USER.U_PATHOLAB_NUMBER;

                    //print
                    
                    PrintAliqPap(newsdg);
                    PrintLabel(newsdg);  
                    SendClalitReceiving(newsdg);
                    txtInternalNbr.Text = "";
                    txtPathoNbr.Text = "";

                }
                else
                {


                    MessageBox.Show(".שגיאה ביצירת דרישה" + loginSdg.ErrorResponse,
                                    Constants.MboxCaption, MessageBoxButton.OK,
                                    MessageBoxImage.Error);


                }

            }
            else
            {
                MessageBox.Show(".שגיאה ביצירת הזמנה" + loginOrder.ErrorResponse, Constants.MboxCaption,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }

        }

        private const string ClalitReceivingEvent = "Send Clalit Receiving";
        private string CLOSE_CONTAINER = "Close Container";
        private void SendClalitReceiving(SDG newsdg)
        {

            if (!newsdg.EVENTS.Contains(ClalitReceivingEvent))
                return;


            if (newsdg.SDG_USER.U_ORDER.U_ORDER_USER.U_CUSTOMER1.U_CUSTOMER_USER.U_CLALIT == null ||
                 newsdg.SDG_USER.U_ORDER.U_ORDER_USER.U_CUSTOMER1.U_CUSTOMER_USER.U_CLALIT != "T")
            {
            }
            else
            {
                var sendXml = new FireEventXmlHandler(ServiceProvider,
                    "SendXmlToClalit");
                sendXml.CreateFireEventXml("SDG", newsdg.SDG_ID, ClalitReceivingEvent);

                var s = sendXml.ProcssXml();
                if (!s)
                {
                    MessageBox.Show("Error on " + ClalitReceivingEvent, Constants.MboxCaption,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintAliqPap(SDG sdg)
        {
            if (sdg.SdgType != SdgType.Pap)
                return;


            string s2 = "P.2.9"; //"70/17.0.0.1";
            var aliq = sdg.SAMPLEs.Select(s => s.ALIQUOTs);

            foreach (ALIQUOT aliquot in aliq.SelectMany(collection => collection))
            {
                int num;
                var index = aliquot.NAME.LastIndexOf('.');
                var lastSign = aliquot.NAME[index + 1];
                if (int.TryParse(lastSign.ToString(), out num))
                {
                    if (num > 1)
                    {
                        var ExtraPAPSlide = new FireEventXmlHandler(ServiceProvider,
                                                                                    "PrintSlide For Extra PAP");
                        ExtraPAPSlide.CreateFireEventXml("ALIQUOT", aliquot.ALIQUOT_ID,
                                                         "PrintSlide For Extra PAP Slide");
                        var s = ExtraPAPSlide.ProcssXml();
                        if (!s)
                        {
                            MessageBox.Show("Error on print  Extra PAP Slide.", Constants.MboxCaption,
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        public string CreatePatholabNumber(SDG sdg)
        {


            try
            {

                U_CUSTOMER cust = sdg.SDG_USER.U_ORDER.U_ORDER_USER.U_CUSTOMER1;
                var dep = sdg.NAME[0].ToString();

                decimal val = dal.Run_PATHOLAB_SYNTAX(cust.U_CUSTOMER_ID, dep);



                string signs = "";


                switch (dep)
                {

                    case "B":
                        signs = cust.U_CUSTOMER_USER.U_LETTER_B;

                        break;

                    case "C":
                        signs = cust.U_CUSTOMER_USER.U_LETTER_C;
                        break;
                    case "P":
                        signs = cust.U_CUSTOMER_USER.U_LETTER_P;
                        break;
                    default:
                        signs = "";
                        break;

                }

                string digits = val.ToString();
                const int numOfDigits = 5;
                while (digits.Length < numOfDigits)
                {
                    digits = "0" + digits;
                }
                var today = DateTime.Now.Date;

                var year = today.ToString("yy");
                string syntax = signs + digits + "/" + year;

                return syntax;
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                return "0";
            }

        }
        private void PrintInitialLetterCyto(SDG newsdg)
        {
            try
            {
                if (newsdg.SdgType == SdgType.Cytology)
                {
                    FireEventXmlHandler fireEvent = new FireEventXmlHandler(ServiceProvider, "Print");
                    fireEvent.CreateFireEventXml("SDG", newsdg.SDG_ID, "Print an initial letter");
                    var feres = fireEvent.ProcssXml();
                }
            }
            catch (Exception ex)
            {

                System.Windows.Forms.MessageBox.Show("  שגיאה בהדפסת דף מלווה" + ex.Message, "Nautilus", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void PrintLabel(SDG newsdg)
        {
            FireEventXmlHandler fireEvent = new FireEventXmlHandler(ServiceProvider, "PrintLabel-SDG ");
            fireEvent.CreateFireEventXml("SDG", newsdg.SDG_ID, "Print Label");
            var feres = fireEvent.ProcssXml();
            if (!feres)
            {
                Patholab_Common.Logger.WriteLogFile($"Printing {newsdg.NAME} sdg label failed");
            }
            else
            {
                Patholab_Common.Logger.WriteLogFile($"Printing {newsdg.NAME} sdg label succeeded");
            }
            foreach (SAMPLE sample in newsdg.SAMPLEs)
            {
                FireEventXmlHandler printSample = new FireEventXmlHandler(ServiceProvider, "PrintLabel-SAMPLE ");
                printSample.CreateFireEventXml("SAMPLE", sample.SAMPLE_ID, "Print Label");
                var s = printSample.ProcssXml();
                if (!s)
                {
                    Patholab_Common.Logger.WriteLogFile($"Printing {sample.NAME} sample label failed");
                }
                else
                {
                    Patholab_Common.Logger.WriteLogFile($"Printing {sample.NAME} sample label succeeded");

                }
            }
            
        }

        private void CalculateDebit(SDG sdg)
        {
            //FireEventXmlHandler fireEvent = new FireEventXmlHandler(ServiceProvider, "Calculate Debit ");
            //fireEvent.CreateFireEventXml("SDG", id, "Calculate Debit");
            //var feres = fireEvent.ProcssXml();
            //if (!feres)
            //{
            //    MessageBox.Show("Error in calcualte debit", Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            string backgroundTableName = "BACKGROUND";
            object ret;
            WORKFLOW_NODE calculateDebitEvent =
                dal.FindBy<WORKFLOW_NODE>(wn => wn.NAME == "Calculate Debit"
                    && wn.WORKFLOW_ID == sdg.WORKFLOW_NODE.WORKFLOW_ID)
                   .SingleOrDefault();
            if (calculateDebitEvent == null)
            {
                MessageBox.Show(@"CONTACT SUPPORT! \r\n Could not find the event ""Calculate Debit"" for the SDG Workflow. (WorkflowID = "
                                + sdg.WORKFLOW_NODE.WORKFLOW_ID);
                return;

            }
            string command = @"Insert into lims." + backgroundTableName +
                    @" (BACKGROUND_ID,PRIORITY,SESSION_ID,SCHEDULE_ID,WORKSTATION_ID,WORKSTATION_GROUP_ID,PARAMETER,ACTIVE,BACKGROUND_TASK_TYPE_ID) "
                    + " values (lims.sq_background.nextval,1," + ((long)_ntlsCon.GetSessionId()).ToString()
                    + ",null,null,null,'D," + sdg.SDG_ID.ToString() + ","
                    + sdg.WORKFLOW_NODE.WORKFLOW_ID + ",#" + calculateDebitEvent.WORKFLOW_NODE_ID + ",T,T','F',1)";


            Con = new Connection();

            var nCon = Utils.GetNtlsCon(ServiceProvider);
            Con.ConnectionString = nCon.GetADOConnectionString();

            Con.Open();
            string limsUserPassword = nCon.GetLimsUserPwd();

            // Set role lims user
            string roleCommand;
            if (limsUserPassword == "")
            {
                // LIMS_USER is not password protected
                roleCommand = "set role lims_user";
            }
            else
            {
                // LIMS_USER is password protected.
                roleCommand = "set role lims_user identified by " + limsUserPassword;
            }
            Con.Execute(roleCommand, out ret, 0);
            // Get the session id
            double sessionId = nCon.GetSessionId();

            // Connect to the same session
            string sSql = string.Format("call lims.lims_env.connect_same_session({0})", sessionId.ToString());
            //MessageBox.Show(sSql);
            Con.Execute(sSql, out ret);
            //Clipboard.SetText(command);
            // MessageBox.Show(command);

            Con.Execute(command, out ret);
            // Execute the command


            //    if (.ExecuteNonQuery(command, _connection) == -1)
            //    {
            //        MessageBox.Show("Could not add the event 'Allow " + routeNextStep + "' to background");
            //        return;
            //    }

        }

        public void InsertOrder(LoginXmlHandler loginOrder)
        {

        }


        #endregion

        #region  Initialization

        public void InitilaizeData()
        {

            try
            {
                Constants.ROLEs = "12";
                if (!DEBUG)
                    IsQcRole = _ntlsUser.GetRoleName() == Constants.QC_ROLE;
                //    HibernatingRhinos.Profiler.Appender.EntityFramework.EntityFrameworkProfiler.Initialize();
                //Open connection
                dal = new DataLayer();

                if (DEBUG)
                {
                    var sdgName = "B000015/16";
                    {
                    }
                    string msg = string.Format(".דרישה {0} נקלטה במערכת", sdgName);
                    string aa = (" דרישה" + sdgName + " נקלטה במערכת.");


                    //   MessageBox.Show(msg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    //  MessageBox.Show(aa, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
                    //  dal.OpenListener();
                    dal.MockConnect();



                    //     CurrentSdg = dal.FindBy<SDG>(x => x.SDG_ID == 826).SingleOrDefault();
                    //         txtInternalNbr.Text = "B001343/16";// CurrentSdg.NAME;
                    //       txtContainer.Text = "0093/16";

                }
                else
                    dal.Connect(_ntlsCon);


                btnBack.IsEnabled = false;
                //אתחול עמוד 2 עבור 3 הסוגים
                histologyPage = new HistologyPage();
                cytologyPage = new CytologyPage();
                papPage = new PapPage();

                histologyPage.parentPage = this;


                ListData = new ListData(dal);






                //Add pages to list
                pages = new List<ISdgRequest> { detailsPage, histologyPage, cytologyPage, papPage };


                detailsPage.GetConnectionParams(_ntlsCon, _ntlsSite);


                pages.ForEach(panel =>
                {
                    //סדר הפעולות חשוב, אסור לשנות אותו.
                    panel.ServiceProvider = ServiceProvider;
                    panel.SelectedPart = SelectedPart;
                    panel.ListData = ListData;
                    panel.dal = dal;
                    panel.CurrentSdg = CurrentSdg;
                    panel.InitilaizeData();
                    

                });


                //Set "Suspend Reason" phrase
                ListData.SetPhrase2Combo("Suspend Reason", cmbSuspensioCause);
                //Set parts data
                CmbParts.DisplayMemberPath = "NAME";
                //  CmbParts.SelectedValuePath = "U_PARTS_ID";
                CmbParts.ItemsSource = ListData.Parts;

                setComboBoxPrinter();

                InitilaizeMetaData();


                InitEvents();

                pages = new List<ISdgRequest> { detailsPage };
                FirstFocus();

            }
            catch (Exception e)
            {

                Logger.WriteLogFile(e);
                MessageBox.Show("Error in  InitializeData " + "/n" + e.Message);

            }
        }



        private void InitEvents()
        {

            detailsPage.CustomerChanged += detailsPage_CustomerChanged;
            detailsPage.ClinicChanged += detailsPage_ClinicChanged;
            detailsPage.SupplierAdded += detailsPage_SupplierAdded;
            detailsPage.SuspenedClicked += detailsPage_SuspenedClicked;
            papPage.RequestDateChanged += Page2_RequestDateChanged;
            cytologyPage.RequestDateChanged += Page2_RequestDateChanged;
            histologyPage.RequestDateChanged += Page2_RequestDateChanged;

            detailsPage.requestRemarkNew.StatusChanged += detailsPage_StatusChanged;



            var elements4Enter = new List<FrameworkElement>();
            elements4Enter.AddRange(this.GetControls4Enter());

            foreach (var controlsPerPage in pages.Select(page => page.GetControls4Enter()))
            {
                elements4Enter.AddRange(controlsPerPage);
            }

            foreach (FrameworkElement frameworkElement in elements4Enter)
            {
                frameworkElement.KeyUp += a_KeyUp;
            }
        }

        void detailsPage_ClinicChanged(U_CLINIC clinic)
        {
            // bool   fromAst 
            this.IsAssuta = clinic.U_CLINIC_USER.U_ASSUTA_CLINIC_CODE != null &&
                                         clinic.U_CLINIC_USER.U_PRIVATE_LIBRARY != null;

            pages.ForEach(x => x.IsAssuta = this.IsAssuta);
        }

        void detailsPage_StatusChanged(string status)
        {

        }


        void detailsPage_SupplierAdded()
        {
            CleanScreen(true);
        }

        bool Page2_RequestDateChanged(DateTime? dt)
        {
            //  dtrft = !dtrft;//Because OnValueChanged event is raised twice
            //  if (dtrft)
            if (CurrentSdg == null && dt != null)
            {
                var agaa = dtRecived_on.Value;// detailsPage.dtDateBirth.Value;
                if (agaa == null)
                {
                    return true;
                }
                var h = DateTime.Now.Hour;

                if (agaa.Value < dt.Value.AddHours(-h + 1) || dt.Value > DateTime.Now)//3.1.17 /14:50     3.1.17 0000
                {
                    return false;
                }
            }
            return true;
        }

        void detailsPage_SuspenedClicked()
        {
            _susspened = true;
            BtnOK_OnClick(null, null);
        }

        private void FirstFocus()
        {
            //First focus because nautius's bag
            _timerFocus = new Timer
            {
                Interval = 10000
            };
            _timerFocus.Tick += timerFocus_Tick;
            txtContainer.Focus();
        }

        void timerFocus_Tick(object sender, EventArgs e)
        {
            txtContainer.Focus();
            _timerFocus.Stop();

        }

        public void InitilaizeMetaData()
        {

            int i = 0;
            TabOrder(ref  i);

            string roleName = "BLA";
            if (!DEBUG)
            {
                roleName = _ntlsUser.GetRoleName();
            }

            Manage_MetaData = new ManageMetaData(roleName);
            var allTagedControls = this.SetTags();
            this.Manage_MetaData.SaveMetadataControls(allTagedControls);

            pages.ForEach(panel =>
                {
                    panel.Manage_MetaData = new ManageMetaData(roleName);
                    var taggedControls = panel.SetTags();
                    panel.Manage_MetaData.SaveMetadataControls(taggedControls);
                });

            //It's for customers that haven't record in start screen table
            _defaultStartScreen = dal.FindBy<U_START_SCREEN_USER>(x => x.U_START_SCREEN.NAME == "999 - One").SingleOrDefault();
        }

        private void InitializationMandaoryFields(Brush mandatoryBrush)
        {
            //The following 3 fields are always mandatory
            txtContainer.Background = mandatoryBrush;
            CmbParts.Background = mandatoryBrush;
            spRdb.Background = mandatoryBrush;


            CmbParts.Tag = Constants.MANDATORYTAG;
            txtContainer.Tag = Constants.MANDATORYTAG;
            dtRecived_on.Tag = Constants.MANDATORYBRUSH;

        }

        public void TabOrder(ref int i)
        {



            txtContainer.TabIndex = i++;
            rdbHis.TabIndex = i++;
            rdbCyt.TabIndex = i++;
            rdbPap.TabIndex = i++;
            CmbParts.TabIndex = i++;
            // dtRecived_on.TabIndex = i++; It's read only field
            cbQc.TabIndex = i++;
            txtQcMark.TabIndex = i++;

            foreach (var page in pages)
            {
                page.TabOrder(ref  i);
            }
            btnNext.TabIndex = i++;

            cmbSuspensioCause.TabIndex = i++;
            btnSusspened.TabIndex = i;

            // btnClean.TabIndex = i++;
            btnOK.TabIndex = i++;
            btnClose.TabIndex = i++;
            btnBack.TabIndex = i++;
        }

        #endregion

        #region controls events

        private void BtnOK_OnClick(object sender, RoutedEventArgs e)
        {
            //    Logger.WriteQueries("BtnOK_OnClick");

            try
            {

                //מסך קבלה - תיקון אישור - bug 13301
                btnOK.IsEnabled=false;

                detailsPage.AddOrEditClient();

                if (!IsMandatoryFieldsOK())
                {
                    MessageBox.Show(".אנא מלא שדות חובה", Constants.MboxCaption,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Stop);
                    return;
                }
                else
                {
                    if (CurrentSdg == null)
                    {
                        //בדיקת שדות חובה


                        //if (this.SpecialValidation() == false || pages.Any(p => p.SpecialValidation() == false) ||
                        //        pages.Any(p => p.Manage_MetaData.ValidateMandatory() == false))
                        //{
                        //    MessageBox.Show(".אנא מלא שדות חובה", Constants.MboxCaption,
                        //                    MessageBoxButton.OK,
                        //                    MessageBoxImage.Stop);
                        //    return;
                        //}



                        InsertRequest(null);


                        //Reload screen by start screen table
                        //Back to previous page
                        BtnBack_OnClick(null, null);


                        //Manage_MetaData.saveCheckPrinterSettings(this.SetTags());



                        //בודק איזה שדות צריכים להישאר עם ערך לאחר שמירה 
                        //ומנקה את מה שלא 
                        this.Manage_MetaData.ReLoadValueAfterAdding(this.GetParentElement());
                        pages.ForEach(p => p.Manage_MetaData.ReLoadValueAfterAdding(p.GetParentElement()));

                        
                        DisplayNew();
                    }


                    else //עדכון ישות
                    {
                        //Manage_MetaData.saveCheckPrinterSettings(this.SetTags());

                        UpdateRequest();




                        CleanScreen();

                    }
                }
            }
            catch (Exception ex)
            {
                dal.SetRoleAndConnect();
                Logger.WriteLogFile(ex);
                MessageBox.Show("שגיאה בעדכון הדרישה  ." + ex.Message);
            }
            finally
            {

                _susspened = false;

                //מסך קבלה - תיקון אישור - bug 13301
                btnOK.IsEnabled = true;
            }
            //  Logger.WriteQueries("End BtnOK_OnClick");



        }

        private bool IsMandatoryFieldsOK()
        {
            return !(this.SpecialValidation() == false || pages.Any(p => p.SpecialValidation() == false) || pages.Any(p => p.Manage_MetaData.ValidateMandatory() == false));
        }

        private void printAliquots(DataLayer dal, SDG sdg, bool statusU)
        {
            try
            {
                if (sdg != null && statusU)
                {
                    if (comboBoxPrinter.SelectedItem != null)
                    {
                        string eventToFire = (comboBoxPrinter.SelectedItem as PHRASE_ENTRY).PHRASE_INFO;
                        sdg.SAMPLEs.Foreach(sample => sample.ALIQUOTs.Foreach(aliquot =>
                        {
                            string dt = DateTime.Now.ToString("yyyyMMddHHmmssFFF");

                            FireEventXmlHandler fireEvent = new FireEventXmlHandler(ServiceProvider, Path.Combine(eventToFire.Replace(' ', '_'), dt));
                            fireEvent.CreateFireEventXml("ALIQUOT", aliquot.ALIQUOT_ID, eventToFire);
                            bool s = fireEvent.ProcssXml();

                            if (!s)
                            {
                                Logger.WriteLogFile(fireEvent.ErrorResponse);
                                MessageBox.Show(string.Format("Aliquot ID: {0}{1}Can't print cassette more than once.", aliquot.ALIQUOT_ID, Environment.NewLine));
                            }
                        }));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error printing cassette.");
            }
        }

        /// <summary>
        /// ----->BACK BUTTON
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBack_OnClick(object sender, RoutedEventArgs e)
        {
            var checkedRdb = spRdb.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);

            if (checkedRdb == null)
                return;

            if (checkedRdb.Name == rdbHisName)
            {
                contentArea.RemoveChild(histologyPage);
            }
            else if (checkedRdb.Name == rdbCytName)
            {
                contentArea.RemoveChild(cytologyPage);
            }
            else if (checkedRdb.Name == rdbPapName)
            {
                contentArea.RemoveChild(papPage);
            }


            contentArea.Content = detailsPage;

            bool b = CurrentSdg == null;

            //right side
            spRdb.IsEnabled = b;
            CmbParts.IsEnabled = b;
            txtContainer.IsEnabled = b;


            dtRecived_on.IsEnabled = false;



            //left side  
            txtInternalNbr.IsEnabled = true;
            txtPathoNbr.IsEnabled = true;

            //buttons
            btnNext.IsEnabled = true;
            btnBack.IsEnabled = false;

            btnOK.IsEnabled = false;
            btnSusspened.IsEnabled = false;



        }

        private void BtnSusspened_OnClick(object sender, RoutedEventArgs e)
        {

            if (DEBUG && CurrentSdg == null)
                return;

            if (cmbSuspensioCause.SelectedIndex < 0)
            {
                MessageBox.Show(".חובה לבחור סיבת ההשהייה", "Nautilus", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            else
            {
                _susspened = true;
                BtnOK_OnClick(btnSusspened, null);
            }

        }

        void detailsPage_CustomerChanged(U_START_SCREEN_USER obj)
        {
            if (obj == null)
            {
                obj = _defaultStartScreen;
            }

            this.Manage_MetaData.SaveMetadata4Customer(obj);
            pages.ForEach(panel => panel.Manage_MetaData.SaveMetadata4Customer(obj));


        }

        //public bool loadExistingSDG = false;
        private void TxtInternalNbr_OnKeyDown(object o, KeyEventArgs e)
        {
            try
            {
                //loadExistingSDG = true;
                if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    CurrentSdg = null;

                    var tb = o as TextBox;
                    if (tb == null || string.IsNullOrEmpty(tb.Text))
                        return;
                    tb.Text = tb.Text.Replace(".", "/");
                    switch (tb.Name)
                    {
                        case "txtInternalNbr":
                            CurrentSdg = dal.FindBy<SDG>(x => x.NAME == tb.Text.ToUpper() ||
                                x.EXTERNAL_REFERENCE == tb.Text.ToUpper())
                                .Include(x => x.SDG_USER)
                                 .Include(x => x.SDG_USER.U_ORDER)
                        .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER)
                        .Include(x => x.SDG_USER.IMPLEMENTING_PHYSICIAN)
                        .Include(x => x.SDG_USER.IMPLEMENTING_PHYSICIAN.SUPPLIER_USER)
                        .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER.U_PARTS)
                        .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER.U_PARTS.U_PARTS_USER)
                    .Include(x => x.SDG_USER.U_ORDER.U_ORDER_USER.U_CUSTOMER1).SingleOrDefault();
                            break;
                        case "txtPathoNbr":
                            {
                                var sdgUser = dal.FindBy<SDG_USER>(x => x.U_PATHOLAB_NUMBER == tb.Text.ToUpper()
                                    && !x.SDG.NAME.Contains("V"))
                                  .Include(x => x.U_ORDER)
                                  .Include(x => x.U_ORDER.U_ORDER_USER)
                               .Include(x => x.IMPLEMENTING_PHYSICIAN)
                                .Include(x => x.IMPLEMENTING_PHYSICIAN.SUPPLIER_USER)
                                 .Include(x => x.U_ORDER.U_ORDER_USER.U_CUSTOMER1).FirstOrDefault();
                                if (sdgUser != null)
                                    CurrentSdg = sdgUser.SDG;
                            }
                            break;
                        default:
                            MessageBox.Show("Error");
                            break;
                    }

                    //ממשק אסותא
                    if (CurrentSdg == null && IsDigitsOnly(tb.Text))//Search by assuta number
                    {
                        string INPT = tb.Text.ToUpper();
                        var sample = dal.FindBy<SAMPLE_USER>(x => x.U_ASSUTA_NUMBER == INPT
                            || x.U_ASSUTA_NUMBER.Substring(0, 7) == INPT)
                               .Include(x => x.SAMPLE)
                  .Include(x => x.SAMPLE.SDG);
                        if (sample != null)
                            CurrentSdg = sample.FirstOrDefault().SAMPLE.SDG;

                    }

                    if (CurrentSdg == null)
                    {
                        pages.Foreach(x => x.CurrentSdg = CurrentSdg);

                        MessageBox.Show(".דרישה לא קיימת", Constants.MboxCaption, MessageBoxButton.OK,
                                                          MessageBoxImage.Hand);
                    }
                    else
                    {


                        pages.Foreach(x => x.CurrentSdg = CurrentSdg);
                        CleanScreen(false);


                        DisplayRequestDetails();
                    }
                }
            }
            catch
                (Exception ex)
            {
                MessageBox.Show(".שגיאה בטעינת הדרישה" + ex.Message,
        Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.WriteLogFile(ex);

            }
            //finally
            //{
            //    loadExistingSDG = false;
            //}


        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {

            var checkedRdb = this.spRdb.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
            if (checkedRdb != null)
                switch (checkedRdb.Name)
                {
                    case rdbHisName:
                        contentArea.Content = histologyPage;

                        break;
                    case rdbCytName:
                        contentArea.Content = cytologyPage;

                        break;
                    case rdbPapName:
                        contentArea.Content = papPage;


                        break;
                }


            //right side
            spRdb.IsEnabled = false;
            CmbParts.IsEnabled = false;
            txtContainer.IsEnabled = false;

            //left side
            txtInternalNbr.IsEnabled = false;
            txtPathoNbr.IsEnabled = false;

            btnNext.IsEnabled = false;
            btnBack.IsEnabled = true;

            btnOK.IsEnabled = true;
            btnSusspened.IsEnabled = true;





        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {

            _ntlsSite.CloseWindow();
            //var dgr1 = MessageBox.Show(@"?האם אתה בטוח שברצונך לצאת ", Constants.MboxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question);

            //if (dgr1 == MessageBoxResult.Yes)
            //{
            //    if (_ntlsSite != null) _ntlsSite.CloseWindow();
            //}

        }

        private void RdbSdgType_OnChecked(object sender, RoutedEventArgs e)
        {
            SelectedPart = null;
            btnNext.IsEnabled = true;
            var rdb = sender as RadioButton;
            pages.Clear();

            ISdgRequest page2 = null;

            if (rdb.Name == rdbHisName)
            {
                if (ListData != null)
                    CmbParts.ItemsSource = ListData.PartsB;
                detailsPage.SelectedDepartment = 'B';
                page2 = histologyPage;
            }
            else if (rdb.Name == rdbCytName)
            {
                detailsPage.SelectedDepartment = 'C';
                if (ListData.PartsC != null)
                    CmbParts.ItemsSource = ListData.PartsC;
                page2 = cytologyPage;

                checkBoxPrintCassette.IsChecked = false;
            }
            else if (rdb.Name == rdbPapName)
            {
                detailsPage.SelectedDepartment = 'P';
                if (ListData.PartsP != null)
                    CmbParts.ItemsSource = ListData.PartsP;

                page2 = papPage;

                checkBoxPrintCassette.IsChecked = false;
            }
            if (page2 != null)
            {
                pages.AddRange(new List<ISdgRequest> { detailsPage, page2 });

                pages.Foreach(x => x.DisplayNew());
            }
            CanContinue();
        }



        private void CmbParts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var part = CmbParts.SelectedItem as U_PARTS;
            SelectedPart = part;
            pages.Foreach(x => x.SelectedPart = part);
            CanContinue();
        }
        #endregion


        private void SetSdgType()
        {
            switch (CurrentSdg.SdgType)
            {
                case SdgType.Histology:
                    rdbHis.IsChecked = true;

                    break;

                case SdgType.Cytology:
                    rdbCyt.IsChecked = true;
                    break;

                case SdgType.Pap:
                    rdbPap.IsChecked = true;
                    break;
            }
        }


        #region Container methods

        private void SetContainerData()
        {

            _currentContainer = null;
            try
            {

                if (CurrentSdg != null)//update mode
                {

                    if (CurrentSdg.SDG_USER.U_CONTAINER == null)//Validation
                    {
                        MessageBox.Show(".לא שויכה ציידנית לדרישה", Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                        ResetContainerDetalis();
                        return;
                    }
                    _currentContainer = CurrentSdg.SDG_USER.U_CONTAINER;

                    SetContainerMsgDetails();
                    return;

                }
                else // Insert mode
                {

                    //find container from DB
                    if (!string.IsNullOrEmpty(txtContainer.Text))
                        _currentContainer = dal.FindBy<U_CONTAINER>(x => x.NAME == txtContainer.Text.Trim()).Include("U_CONTAINER_USER").SingleOrDefault();


                    if (_currentContainer != null) //If container exists
                    {



                        if (_currentContainer.U_CONTAINER_USER.U_STATUS != "V")
                        {
                            var dg = MessageBox.Show("חריגה ממספר ההפניות בציידנית, האם להמשיך?",
                                                              Constants.MboxCaption, MessageBoxButton.YesNo,
                                                              MessageBoxImage.Hand, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                            if (dg == MessageBoxResult.No)
                            {
                                ResetContainerDetalis();
                                txtContainer.Focus();
                            }
                            else
                            {
                                SetContainerMsgDetails();
                            }
                        }
                        else
                        {
                            SetContainerMsgDetails();
                        }
                    }
                    else
                    {


                        MessageBox.Show("ציידנית לא קיימת,לא ניתן להמשיך.", Constants.MboxCaption, MessageBoxButton.OK,
                                                               MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                        ResetContainerDetalis();

                        txtContainer.Focus();
                    }
                }



            }
            catch (Exception ex)
            {
                Logger.WriteQueries("exception on  SetContainerData " + ex.Message);

                Logger.WriteLogFile(ex);
                MessageBox.Show(ex.Message);
                ResetContainerDetalis();

            }

        }

        private void SetContainerMsgDetails()
        {
            //    Logger.WriteQueries("start SetContainerMsgDetails");

            if (_currentContainer == null)
                return;


            var numberOfsamplesOnShipment = _currentContainer.U_CONTAINER_USER.U_NUMBER_OF_SAMPLES;//מספר צנצנות בציידנית

            // var sdgByContainer = _currentContainer.SDG_USER.Count();//מספר הזמנות המשויכות לציידנית

            //var samplesByContainer = dal.GetSamplesByContainer(_currentContainer.U_CONTAINER_ID.ToString());
           

            var samplesByContainer = dal.GetSamplesByContainerAndStatus(_currentContainer.U_CONTAINER_ID.ToString());


            dtRecived_on.Value = _currentContainer.U_CONTAINER_USER.U_SEND_ON;


            string sampleMsg = string.Format("התקבלו {0} / {1} צנצנות מתוך קבלת המשלוח",
                               numberOfsamplesOnShipment, samplesByContainer);
            dtRecived_on.Value = _currentContainer.U_CONTAINER_USER.U_SEND_ON;
            if (_currentContainer != null && _currentContainer.U_CONTAINER_USER.U_CLINIC1 != null)
            {
                string cn = _currentContainer.U_CONTAINER_USER.U_CLINIC1.NAME;
                lblClinicSender.Text = cn;

                pages.Foreach(x => x.SetContainerDetails(sampleMsg));
                SetContainerDetails(sampleMsg);
            }

        }

        private void ResetContainerDetalis()
        {

            _currentContainer = null;

            txtContainer.Text = "";

            lblClinicSender.Text = "";

            dtRecived_on.Value = null;

            pages.Foreach(x => x.SetContainerDetails(emptysampleContainerMsg));


        }

        private void CheckContainerStatus()
        {
            if (_currentContainer == null)
                return;


            //Number of samples to container
            var numberOfsamplesOncontainer = _currentContainer.U_CONTAINER_USER.U_NUMBER_OF_SAMPLES;

            //Actual Number of samples to container
            var samplesByContainer = dal.GetSamplesByContainer(_currentContainer.U_CONTAINER_ID.ToString());

            // The number of samples received is greater than the number recorded in received container
            if (samplesByContainer > numberOfsamplesOncontainer)
            {

                MessageBox.Show("חריגה ממספר הצנצנות בציידנית.",
                                                         Constants.MboxCaption, MessageBoxButton.OK,
                                                         MessageBoxImage.Hand,
                                                         MessageBoxResult.OK, MessageBoxOptions.RtlReading);
            }

            //Is it possible to close a container
            bool possibleClosecontainer = samplesByContainer >= numberOfsamplesOncontainer
                && _currentContainer.U_CONTAINER_USER.U_STATUS != "C";

            //סגירת ציידנית
            if (possibleClosecontainer)
            {
                CloseContainer();
                return;
            }

            SetContainerData();
        }

        private void CloseContainer()
        {
            Logger.WriteLogFile("Update Container as completed by Fire Event.");
            txtContainer.Text = "";


            var CLOSE_CONTAINERXml = new FireEventXmlHandler(ServiceProvider,
                          CLOSE_CONTAINER);
            CLOSE_CONTAINERXml.CreateFireEventXml("U_CONTAINER", _currentContainer.U_CONTAINER_ID, CLOSE_CONTAINER);
            var s = CLOSE_CONTAINERXml.ProcssXml();
            if (!s)
            {
                MessageBox.Show("Error on " + CLOSE_CONTAINER, Constants.MboxCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);

                Logger.WriteLogFile(CLOSE_CONTAINERXml.ErrorResponse);
            }
            else
            {


                MessageBox.Show("ציידנית הושלמה.", Constants.MboxCaption, MessageBoxButton.OK,
                                MessageBoxImage.Information);
                Logger.WriteLogFile("Container " + _currentContainer.NAME + " Completed Succesfully");

            }
            ResetContainerDetalis();
            txtContainer.Focus();
        }

        #endregion

        private void CanContinue()
        {
            //  Logger.WriteQueries("start CanContinue");

            var bb = (this.spRdb.Children.OfType<RadioButton>().Any(r => r.IsChecked == true) &&
                      _currentContainer != null && CmbParts.SelectedIndex > -1);


            contentArea.IsEnabled = bb;

            btnNext.IsEnabled = bb;
            SetContainerMsgDetails();
            //    Logger.WriteQueries("END CanContinue");


        }

        private string GetWorkflowName()
        {

            var part = CmbParts.SelectedItem as U_PARTS;
            if (part != null && part.U_PARTS_USER.SDG_WORKFLOW != null)
                return part.U_PARTS_USER.SDG_WORKFLOW.NAME;
            else
            {
                MessageBox.Show(".לא נבחרה פריט , או לא הוגדר תהליך לפריט ", Constants.MboxCaption, MessageBoxButton.OK,
                                     MessageBoxImage.Error);
                return "";
            }
        }

        public bool CloseQuery()
        {
            if (detailsPage != null)
                detailsPage.RealseCom();
            if (Con != null)
            {
                Con.Close();

                Con = null;
            }
            if (dal != null)
                dal.Close();

            return true;
        }

        public void CleanScreen(bool setNull = true)
        {
            if (setNull)
            {
                CurrentSdg = null;


                pages.ForEach(panel =>
                    {
                        panel.CurrentSdg = null;
                        panel.IsAssuta = false;
                    });
            }

            BtnBack_OnClick(null, null);

            SetPicture("U");

            checkBoxPrintCassette.IsVisibleChanged -= checkBoxPrintCassette_IsVisibleChanged;
            spRdb.Children.OfType<RadioButton>().Foreach(r => 
            {
                    r.IsChecked = false;
            });
            checkBoxPrintCassette.IsVisibleChanged += checkBoxPrintCassette_IsVisibleChanged;

            

            pages.Clear();


            pages.Add(detailsPage);
            detailsPage.ResetRemarks();
            this.Manage_MetaData.ClearControlsValue(this.GetParentElement());
            detailsPage.Manage_MetaData.ClearControlsValue(detailsPage.GetParentElement());
            histologyPage.Manage_MetaData.ClearControlsValue(histologyPage.GetParentElement());
            cytologyPage.Manage_MetaData.ClearControlsValue(cytologyPage.GetParentElement());
            papPage.Manage_MetaData.ClearControlsValue(papPage.GetParentElement());

            ResetContainerDetalis();

            //checkBoxPrintCassette.IsChecked = false;

            spRdb.IsEnabled = true;

            DisplayNew();

        }

        private void SetPicture(string status)
        {

            try
            {


                const string ResourcePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Thermo\Nautilus\9.2\Directory";


                var path = Utils.GetResourcePath();// ( string ) Registry.GetValue ( ResourcePath, "Resource", null );

                if (path != null)
                {
                    path += "\\";
                    const string _tableName = "SDG";


                    var uri = new Uri(path + _tableName + status + ".ico");
                    var bitmap = new BitmapImage(uri);
                    //  path = @"C:\Program Files (x86)\Thermo\Nautilus\Resource\";
                    imgStatus.Source = bitmap;

                }
            }
            catch (Exception ex)
            {

                Logger.WriteLogFile(ex);
            }
        }

        private void ButtonClean_Click(object sender, RoutedEventArgs e)
        {
            var dg = MessageBox.Show("האם אתה בטוח שברצונך לנקות את המסך?", Constants.MboxCaption, MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);
            if (dg == MessageBoxResult.Yes)
            {
                CleanScreen();
                txtContainer.Focus();
            }
        }

        private void txtContainer_KeyDown(object sender, KeyEventArgs e)
        {
            txtContainer.Background = Constants.MANDATORYBRUSH;
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (CurrentSdg == null)
                {
                    SetContainerData();
                    CanContinue();

                }
            }

        }

        private void EnterLikeTab(KeyEventArgs keyEventArgs)
        {


            if (keyEventArgs.Key == Key.Enter)
            {
                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;

                if (keyboardFocus != null)
                {
                    keyboardFocus.MoveFocus(tRequest);
                }
            }
        }

        void a_KeyUp(object sender, KeyEventArgs e)
        {

            EnterLikeTab(e);

        }

        private void SpRdb_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (CurrentSdg != null)
                return;

            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                var checkedRdb = spRdb.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
                if (checkedRdb != null)
                {
                    int numTabs = 0;


                    switch (checkedRdb.Name)
                    {
                        case (rdbHisName):
                            numTabs = 2;
                            break;
                        case (rdbCytName):
                            numTabs = 1;
                            break;
                        case (rdbPapName):
                            numTabs = 0;
                            break;

                    }

                    for (int i = 0; i < numTabs; i++)
                    {

                        EnterLikeTab(e);
                    }


                }
            }
        }

        public void SetFocus()
        {
            txtContainer.Focus();
        }

        private bool ValidateRecivedDate(DateTimePicker dt, bool savingTime)
        {
            if (CurrentSdg == null && dt != null && dt.Value.HasValue)
            {
                if (dt.Value > DateTime.Now)
                {
                    MessageBox.Show(".אין אפשרות לקבוע תאריך עתידי", Constants.MboxCaption,
                                                      MessageBoxButton.OK, MessageBoxImage.Stop);
                    dt.Value = null;
                }
                else if (dt.Value <= DateTime.Now.AddDays(-10))
                {
                    if (!savingTime)
                        MessageBox.Show(".ניתן לקבוע תאריך עד 10 ימים אחורה", Constants.MboxCaption,
                                                          MessageBoxButton.OK, MessageBoxImage.Stop);
                    dt.Value = null;

                }
            }
            return true;
        }

        private void To_english(object sender, RoutedEventArgs e)
        {
            zLang.English();
        }

        private void txtPathoNbr_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtInternalNbr_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void txtContainer_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        //TODO:put it in patholab common
        bool IsDigitsOnly(string str)
        {

            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        private void detailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void checkBoxPrintCassette_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!checkBoxPrintCassette.IsVisible)
            {
                checkBoxPrintCassette.IsChecked = false;
                comboBoxPrinter.Visibility = Visibility.Hidden;
            }
        }

        private void checkBoxPrintCassette_CheckedChanged(object sender, RoutedEventArgs e)
        {
            comboBoxPrinter.Visibility = checkBoxPrintCassette.IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
        }

        private void MasterPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

      
    }
}
