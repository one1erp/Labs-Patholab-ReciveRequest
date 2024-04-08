using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ADODB;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
using Patholab_DAL_V1;
using Patholab_DAL_V1.Enums;
using Patholab_XmlService;
using Recive_Request.Classes;
//using VB6Bridge;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using DateTimePicker = Xceed.Wpf.Toolkit.DateTimePicker;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;
using RequestRemarkNet;
using LSExtensionWindowLib;

//////using Patholab_DAL;
////using Patholab_DAL.Enums;


namespace Recive_Request.Controls.ReceivePages
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DetailsPage : UserControl, ISdgRequest
    {
        #region CTOR
        public DetailsPage()
        {
            InitializeComponent();
            cmbCustomer.Background = Constants.MANDATORYBRUSH;
            cmbClinic.Background = Constants.MANDATORYBRUSH;
            cmbClinicCode.Background = Constants.MANDATORYBRUSH;
            txtClientIdentity.Background = Constants.MANDATORYBRUSH;
            dtDateBirth.Minimum = new DateTime(1817, 1, 1);
            
        }
        #endregion

        #region Private fields

        private INautilusDBConnection _ntlsCon;
        private IExtensionWindowSite2 _ntlsSite;

        private INautilusUser _ntlsUser;
        private Connection Con { get; set; }
        private CLIENT _currentClient;
        private char _selectedDepartment;
        public RequestRemarkControl requestRemarkNew;
        #endregion

        #region public fields

        public event Action SupplierAdded;
        public event Action SuspenedClicked;
        public event Action<U_START_SCREEN_USER> CustomerChanged;
        public event Action<U_CLINIC> ClinicChanged;

        public char SelectedDepartment
        {
            get { return _selectedDepartment; }
            set
            {
                _selectedDepartment = value;
                if (CurrentSdg == null)//insert mode
                {
                    var cust = cmbCustomer.SelectedItem as U_CUSTOMER;

                    if (cust != null) SetSigns(cust.U_CUSTOMER_USER);
                    if (_selectedDepartment == 'P')
                    {
                        SetValueInPhrase("Gender", "F", cmbGender);

                    }
                    else
                    {
                        cmbGender.SelectedIndex = -1;
                    }
                }
            }
        }
        public bool DEBUG;

        public INautilusServiceProvider ServiceProvider { get; set; }
        public DataLayer dal { get; set; }
        public SDG CurrentSdg { get; set; }
        public ManageMetaData Manage_MetaData { get; set; }
        public U_PARTS SelectedPart { get; set; }
        public ListData ListData { get; set; }
        public bool IsAssuta { get; set; }

        #endregion

        #region Private methods
        /// <summary>
        //Set customer signs for patholab number
        /// </summary>
        /// <param name="cust">Selected customer</param>
        private void SetSigns(U_CUSTOMER_USER cust)
        {

            //todo:במצב יצירת הזמנה לשלוף את הנתון של איזו מעבדה נבחרה
            if (CurrentSdg != null)
                switch (CurrentSdg.SdgType)
                {

                    case SdgType.Histology:
                        txtSigns.Text = cust.U_LETTER_B;

                        break;

                    case SdgType.Cytology:
                        txtSigns.Text = cust.U_LETTER_C;

                        break;

                    case SdgType.Pap:
                        txtSigns.Text = cust.U_LETTER_P;

                        break;
                    default:
                        txtSigns.Text = "";
                        break;

                }
            else
            {
                switch (SelectedDepartment)
                {

                    case 'B':
                        txtSigns.Text = cust.U_LETTER_B;

                        break;

                    case 'C':
                        txtSigns.Text = cust.U_LETTER_C;

                        break;

                    case 'P':
                        txtSigns.Text = cust.U_LETTER_P;

                        break;
                    default:
                        txtSigns.Text = "";
                        break;

                }
            }
        }

        private void SetValueInPhrase(string phraseName, string entryName, ComboBox cmb)
        {

            try
            {


                var phrase = (from item in ListData.PhraseHeaders where item.NAME == phraseName select item).FirstOrDefault();
                if (phrase != null && entryName != null)
                {
                    if (phrase.PhraseEntriesDictonary.ContainsKey(entryName))
                    {
                        cmb.Text = phrase.PhraseEntriesDictonary[entryName];
                    }
                    else
                    {
                        cmb.Text = null;

                    }

                }


            }
            catch (Exception e)
            {

                Logger.WriteLogFile(e);
                MessageBox.Show("Error in SetValueInPhrase");

            }
        }

        public void AddOrEditClient()
        {



            _currentClient = null;

            var cn = OrganizeIdentity(txtClientIdentity.Text);


            _currentClient = dal.FindBy<CLIENT>(client => client.NAME == cn).Include(x => x.CLIENT_USER).SingleOrDefault();

            bool addTodb = false;

            if (_currentClient == null)
            {

                addTodb = true;
                _currentClient = new CLIENT();
                _currentClient.CLIENT_USER = new CLIENT_USER();
                _currentClient.NAME = cn;


                var clientId = dal.GetNewId("SQ_CLIENT");
                _currentClient.CLIENT_ID = (long)clientId;
                _currentClient.CLIENT_USER.CLIENT_ID = (long)clientId;
                _currentClient.VERSION = "1";
                _currentClient.VERSION_STATUS = "A";

            }

            _currentClient.CLIENT_USER.U_PHONE = txtClientPhone.Text;
            _currentClient.CLIENT_USER.U_FIRST_NAME = txtFirtName.Text;
            _currentClient.CLIENT_USER.U_LAST_NAME = txtLastName.Text;
            _currentClient.CLIENT_USER.U_PREV_LAST_NAME = txtPrevLastName.Text;

            _currentClient.CLIENT_USER.U_DATE_OF_BIRTH = dtDateBirth.Value;

            _currentClient.CLIENT_USER.U_PASSPORT = NautilsuBoolean.ConvertBack(cbPasport.IsChecked.Value);

            if (cmbGender.SelectedValue != null)
                _currentClient.CLIENT_USER.U_GENDER = cmbGender.SelectedValue.ToString();
            if (addTodb)
            {
                dal.Add(_currentClient);
            }
            else
            {
                dal.Edit(_currentClient);
            }

            dal.SaveChanges();


        }

        public string CreatePatholabNumber()
        {


            try
            {

                U_CUSTOMER cust = cmbCustomer.SelectedValue as U_CUSTOMER;


                decimal val = dal.Run_PATHOLAB_SYNTAX(cust.U_CUSTOMER_ID, SelectedDepartment.ToString());//todo: InsertMode is temp



                string signs = "";

                //Debugger.Launch();
                switch (SelectedDepartment)
                {
                       

                    case 'B':
                        signs = cust.U_CUSTOMER_USER.U_LETTER_B;

                        break;

                    case 'C':
                        signs = cust.U_CUSTOMER_USER.U_LETTER_C;

                        break;

                    case 'P':
                        signs = cust.U_CUSTOMER_USER.U_LETTER_P;

                        break;
                    default:
                        signs = "";
                        break;

                }

                string Digits = val.ToString();
                const int numOfDigits = 5;
                while (Digits.Length < numOfDigits)
                {
                    Digits = "0" + Digits;
                }
                var today = DateTime.Now.Date;

                var year = today.ToString("yy");
                string syntax = signs + Digits + "/" + year;
                return syntax;
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                return "0";
            }

        }

        private void LoadRequestRemark()
        {
            try
            {
             
                _ntlsUser = Utils.GetNautilusUser(ServiceProvider);

                requestRemarkNew = new RequestRemarkControl();
                requestRemarkNew.GetConnectionParams(_ntlsCon, _ntlsSite, _ntlsUser);
                requestRemarkNew.InitializeConnection();

                hostRemarks.Child = requestRemarkNew;


            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                // MessageBox.Show("Error  on load  RequestRemarks");
            }
        }

        public void GetConnectionParams(INautilusDBConnection ntlsCon, IExtensionWindowSite2 ntlsSite)
        {
            _ntlsCon = ntlsCon;
            _ntlsSite = ntlsSite;
        }

        private bool CheckClientIdentity()
        {
            try
            {
                if (cbPasport.IsChecked == false)
                {
                    string cn0 = OrganizeIdentity(txtClientIdentity.Text);
                    bool validId = dal.Run_CheckId(cn0);

                    if (!validId)
                    {
                        MessageBox.Show("תעודת זהות אינה תקנית.", Constants.MboxCaption, MessageBoxButton.OK,
                            MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                        txtClientIdentity.Background = Brushes.Red;
                        return false;
                    }

                }


                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בבדיקת חוקיות ת.ז." + ex.Message);
                Logger.WriteLogFile(ex);
                return false;

            }
        }

        private string OrganizeIdentity(string cn)
        {
            cn = cn.Trim();
            if (cbPasport.IsChecked == true)
            {
                return cn;
            }
            var cn0 = cn;
            while (cn0.Length < 9)
            {
                cn0 = "0" + cn0;
            }
            return cn0;
        }
        #endregion

        #region Events of Controls



        private void Phyisician_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var cb = sender as ComboBox;

            if (cb != null)
            {
                if (cb.SelectedValue != null)
                {
                    var supp = cb.SelectedValue as SUPPLIER;
                    if (cb.Name == "Cmb_FN_Referringphysician")
                    {
                        textReferringphysician.Text = supp.SUPPLIER_USER.U_LICENSE_NBR;
                        physician_TextChanged(textReferringphysician, null);
                    }
                    else if (cb.Name == "Cmb_FN_Implementingphysician")
                    {
                        textImplementingphysician.Text = supp.SUPPLIER_USER.U_LICENSE_NBR;
                        physician_TextChanged(textImplementingphysician, null);
                    }
                }
            }
        }

        #region  Customer and clinic

        private void CmbCustomer_OnLostFocus(object sender, RoutedEventArgs e)
        {
            txtSigns.Text = "";
            var cb = sender as ComboBox;

            var cust = cb.SelectedValue as U_CUSTOMER;

            if (cust != null)
            {
                //if (CurrentSdg == null)
                //{
                    //init screen by selected customer
                    var startScreen = cust.U_START_SCREEN_USER.FirstOrDefault();
                    if (CustomerChanged != null)
                        CustomerChanged(startScreen);

                    //Init clinics combo box by selected customer
                    SetClinicsCombo(cust);
                //}
                //Set customer signs for patholab number
                SetSigns(cust.U_CUSTOMER_USER);
            }
        }

        private void SetClinicsCombo(U_CUSTOMER cust)
        {
            try
            {
                var c = cmbClinic.SelectedItem as U_CLINIC;


                if (cust.U_CUSTOMER_USER.U_GRP_CLINIC_CODE != null)
                {
                    var q =
                        ListData.Clinics.Where(x => x.U_CLINIC_USER.U_GRP_CODE == cust.U_CUSTOMER_USER.U_GRP_CLINIC_CODE)
                                .ToList();
                    if (c != null && q.Contains(c))
                    {
                        //נתנאל ביקש להוריד קביעת ערך ברירת מחדל לגורם שולח
                        //cmbClinic.SelectedItem = c;
                    }
                    cmbClinic.ItemsSource = q;
                    cmbClinicCode.ItemsSource = q;
                }
                else if (cust.U_CLINIC_USER.Count > 0)
                {
                    var s2 = cust.U_CLINIC_USER.Select(cl => cl.U_CLINIC).ToList();
                    if (c != null && s2.Contains(c))
                    {
                        //      cmbClinic.SelectedItem = c;
                    }
                    cmbClinic.ItemsSource = s2;
                    cmbClinicCode.ItemsSource = s2;
                    if (s2.Count == 1)
                    {
                        //      cmbClinic.SelectedItem = s2.FirstOrDefault();
                    }
                }
                else
                {
                    cmbClinic.ItemsSource = ListData.Clinics;
                    cmbClinicCode.ItemsSource = ListData.Clinics;
                    if (c != null)
                    {
                        //      cmbClinic.SelectedItem = c;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                MessageBox.Show(ex.Message);
            }
        }

        private void CmbClinic_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var cb = sender as ComboBox;

            var clinic = cb.SelectedValue as U_CLINIC;


            if (cb.Name == "cmbClinic")
            {
                cmbClinicCode.SelectedItem = clinic;
            }
            else if (cb.Name == "cmbClinicCode")
            {
                cmbClinic.SelectedItem = clinic;
            }
            if (clinic != null)
            {
                ClinicChanged(clinic);
            }


            if (CurrentSdg == null)
            {
                if (clinic != null)
                {


                    var startScreen = clinic.U_START_SCREEN_USER.FirstOrDefault();
                    if (startScreen != null && !startScreen.U_CUSTOMER_CODE.HasValue)
                        if (CustomerChanged != null) CustomerChanged(startScreen);
                }
            }
        }



        #endregion


        #region Supplier

        private void BtnAddSupplier_OnClick(object sender, RoutedEventArgs e)
        {

            AddSupplier ns = new AddSupplier(dal, ListData, ServiceProvider);
            ns.SupplierAdded += ns_SupplierAdded;
            ns.ShowDialog();

        }



        private void ns_SupplierAdded()
        {
            if (SupplierAdded != null) SupplierAdded();
        }

        #endregion

        //HospitalNbr
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }


        #region Obligation

        private void cbObligation_Checked(object sender, RoutedEventArgs e)
        {
            txtAccordNbr.IsEnabled = !cbObligation.IsChecked == true;
        }


        private void cbObligation_Click(object sender, RoutedEventArgs e)
        {
            txtAccordNbr.IsEnabled = !cbObligation.IsChecked == true;

        }

        #endregion



        #endregion

        #region Display Sdg
        private void DisplaySdgGroupA()
        {
            txtAccordNbr.Text = CurrentSdg.SDG_USER.U_ACCORD_NUMBER;
            var sdgUser = CurrentSdg.SDG_USER;
            if (sdgUser.U_HOSPITAL_NUMBER != null)
                txtHospitalNbr.Text = sdgUser.U_HOSPITAL_NUMBER.Value.ToString();

            cmbClinic.SelectedItem = sdgUser.IMPLEMENTING_CLINIC;





            U_ORDER_USER currentOrder = sdgUser.U_ORDER.U_ORDER_USER;

            if (currentOrder != null)
            {
                if (currentOrder.U_CUSTOMER1 != null)
                {

                    cmbCustomer.Text = currentOrder.U_CUSTOMER1.NAME;
                    if (cmbCustomer.SelectedItem != null)
                    {
                        CmbCustomer_OnLostFocus(cmbCustomer, null);
                    }

                    SetSigns(currentOrder.U_CUSTOMER1.U_CUSTOMER_USER);
                }
                else
                {
                    MessageBox.Show("לא קיים לקוח\n לא ניתן להמשיך.", Constants.MboxCaption, MessageBoxButton.OK,
                            MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.RtlReading);
                    return;
                }
                if (currentOrder.U_CUSTOMER2 != null)
                    cmbSecondCustomer.Text = currentOrder.U_CUSTOMER2.NAME;

                cbInAdvance.IsChecked = currentOrder.U_IN_ADVANCE == "T";
                if (currentOrder.U_PAY_TYPE != null)
                    SetValueInPhrase("Payment Type", currentOrder.U_PAY_TYPE, cmbPayType);

                numAmount.Text = currentOrder.U_PAY_AMOUNT.HasValue ? currentOrder.U_PAY_AMOUNT.Value.ToString() : null;



            }
            SetValueInPhrase("Priority", sdgUser.U_PRIORITY.ToString(), cmbPriority);

            cbObligation.IsChecked = sdgUser.U_NO_OBLIGATION == "T";


            txtHospitalNbr.Text = sdgUser.U_HOSPITAL_NUMBER.ToString();

        }
        private void DisplaySdgGroupB()
        {
            var client = CurrentSdg.SDG_USER.CLIENT;

            if (client != null)
            {
                var clientUser = client.CLIENT_USER;

                txtClientIdentity.Text = client.NAME;
                txtLastName.Text = clientUser.U_LAST_NAME;
                txtPrevLastName.Text = clientUser.U_PREV_LAST_NAME;
                txtFirtName.Text = clientUser.U_FIRST_NAME;
                txtClientPhone.Text = clientUser.U_PHONE;
                cbPasport.IsChecked = clientUser.U_PASSPORT == "T";


                SetValueInPhrase("Gender", clientUser.U_GENDER, cmbGender);

                //dtDateBirth Must be updated before age                
                dtDateBirth.Value = clientUser.U_DATE_OF_BIRTH;
                txtAge.Value = CalculateAge();
            }
        }
        private void DisplaySdgGroupC()
        {
            var sdgUser = CurrentSdg.SDG_USER;
            if (sdgUser.IMPLEMENTING_PHYSICIAN != null)
            {
                textImplementingphysician.Text = sdgUser.IMPLEMENTING_PHYSICIAN.SUPPLIER_USER.U_LICENSE_NBR;
                fillComboBox(textImplementingphysician, Cmb_FN_Implementingphysician);
            }

            if (sdgUser.REFERRING_PHYSIC != null)
            {
                textReferringphysician.Text = sdgUser.REFERRING_PHYSIC.SUPPLIER_USER.U_LICENSE_NBR;
                fillComboBox(textReferringphysician, Cmb_FN_Referringphysician);
            }
            // Convert.ToString(sdgUser.IMPLEMENTING_PHYSICIAN);

            CmbreferringClinic.SelectedItem = sdgUser.REFERRAL_PHYSICIAN_CLINIC;
            //     cmbSuspensioCause.SelectedItem = sdgUser.U_SUSPENSION_CAUSE;
            try
            {

                //requestRemarkBridge.sampleInput(CurrentSdg.NAME);//TODO:לבדוק למה לא עובד
                //requestRemarkNew.sampleInput(CurrentSdg.NAME);
                requestRemarkNew.sampleInput(CurrentSdg.NAME);
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);
            }
        }

        #endregion

        #region     ISdgRequest

        public void InitilaizeData()
        {
            ListData.SetPhrase2Combo("Payment Type", cmbPayType);

            ListData.SetPhrase2Combo("Gender", cmbGender);

            ListData.SetPhrase2Combo("Priority", cmbPriority);



            cmbPriority.Text = "רגיל";

            cmbCustomer.ItemsSource = ListData.Customers;
            cmbCustomer.DisplayMemberPath = "NAME";

            cmbSecondCustomer.ItemsSource = ListData.Customers;
            cmbSecondCustomer.DisplayMemberPath = "NAME";


            cmbClinic.ItemsSource = ListData.Clinics;
            cmbClinicCode.ItemsSource = ListData.Clinics;

            CmbreferringClinic.ItemsSource = ListData.Clinics;


            CmbreferringClinic_code.ItemsSource = ListData.Clinics;



            LoadRequestRemark();

            //  if (!MasterPage.DEBUG)
            //      btnAddSupplier.IsEnabled = MasterPage.IsQcRole;// _ntlsUser.GetRoleName() == Constants.QC_ROLE;
        }

        public void TabOrder(ref int i)
        {

            cmbCustomer.TabIndex = i++;
            cmbClinicCode.TabIndex = i++;
            cmbClinic.TabIndex = i++;

            //  cmbPriority.TabIndex = i++;
            cmbSecondCustomer.TabIndex = i++;
            txtAccordNbr.TabIndex = i++;
            txtHospitalNbr.TabIndex = i++;
            cmbPriority.TabIndex = i++;
            cbObligation.TabIndex = i++;
            cbInAdvance.TabIndex = i++;

            cmbPayType.TabIndex = i++;
            numAmount.TabIndex = i++;


            textImplementingphysician.TabIndex = i++;
            Cmb_FN_Implementingphysician.TabIndex = i++;

            textReferringphysician.TabIndex = i++;
            Cmb_FN_Referringphysician.TabIndex = i++;

            CmbreferringClinic_code.TabIndex = i++;
            CmbreferringClinic.TabIndex = i++;


            cbPasport.TabIndex = i++;
            txtClientIdentity.TabIndex = i++;
            txtLastName.TabIndex = i++;
            txtFirtName.TabIndex = i++;
            dtDateBirth.TabIndex = i++;
            cmbGender.TabIndex = i++;
            txtPrevLastName.TabIndex = i++;


        }

        public void DisplayRequestDetails()
        {

            try
            {

                DisplaySdgGroupA();
                DisplaySdgGroupB();
                DisplaySdgGroupC();

                if (CurrentSdg.STATUS == "S")
                {
                    hostRemarks.IsEnabled = true;
                    
                    requestRemarkNew.Enabled = true;
                }

            }
            catch (Exception e)
            {
                Logger.WriteLogFile(e); MessageBox.Show("Error in Display Sdg " + e.Message);
            }
        }

        public void DisplayNew()
        {
            _currentClient = null;

            // OR - changed from (currentSdg == null) to (= true), because we want to enable insert in both cases.
            //var insertMode = CurrentSdg == null;
            var insertMode = true;
            if (insertMode)
            {
                cmbPriority.Text = "רגיל";
                txtClientIdentity.Text = "";
                if (cmbCustomer.SelectedItem != null)
                {
                    CmbCustomer_OnLostFocus(cmbCustomer, null);
                }
                else
                {
                    cmbClinic.SelectedItem = null;
                    cmbClinic.ItemsSource = ListData.Clinics;
                    cmbClinicCode.ItemsSource = ListData.Clinics;
                }
            }
            cmbCustomer.IsEnabled = insertMode;
            txtClientIdentity.IsEnabled = insertMode;
        }

        public void UpdateRequest()
        {
            var sdgUser = CurrentSdg.SDG_USER;
            sdgUser.U_ACCORD_NUMBER = txtAccordNbr.Text;

            decimal hn;
            if (decimal.TryParse(txtHospitalNbr.Text, out hn))
            {
                sdgUser.U_HOSPITAL_NUMBER = decimal.Parse(txtHospitalNbr.Text);
            }

            sdgUser.IMPLEMENTING_CLINIC = cmbClinic.SelectedValue as U_CLINIC;

            //Don't save age in DB - ashi 24.5.18
            //sdgUser.U_AGE_AT_ARRIVAL = string.IsNullOrEmpty(txtAge.Text) ? CalculateAge() : decimal.Parse(txtAge.Text);


            string implementID = textImplementingphysician.Text;
            string referID = textReferringphysician.Text;

            sdgUser.IMPLEMENTING_PHYSICIAN = dal.FindBy<SUPPLIER>(s => s.SUPPLIER_USER.U_LICENSE_NBR == implementID).FirstOrDefault();
            sdgUser.REFERRING_PHYSIC = dal.FindBy<SUPPLIER>(s => s.SUPPLIER_USER.U_LICENSE_NBR == referID).FirstOrDefault();
            sdgUser.REFERRAL_PHYSICIAN_CLINIC = CmbreferringClinic.SelectedValue as U_CLINIC;

            //ashi - 27/12/18
            decimal pr;
            if (cmbPriority.SelectedValue != null && decimal.TryParse(cmbPriority.SelectedValue.ToString(), out pr))
                sdgUser.U_PRIORITY = pr;

            //2 update order
            var currentOrder = sdgUser.U_ORDER;
            if (currentOrder != null)
            {
                currentOrder.U_ORDER_USER.U_CUSTOMER1 = cmbCustomer.SelectedValue as U_CUSTOMER;
                currentOrder.U_ORDER_USER.U_CUSTOMER2 = cmbSecondCustomer.SelectedValue as U_CUSTOMER;
                currentOrder.U_ORDER_USER.U_IN_ADVANCE = NautilsuBoolean.ConvertBack(cbInAdvance.IsChecked.Value);

                currentOrder.U_ORDER_USER.U_PAY_TYPE = cmbPayType.SelectedValuePath;
                currentOrder.U_ORDER_USER.U_PAY_AMOUNT = numAmount.Value;
            }

            //3 update client
            var client = CurrentSdg.SDG_USER.CLIENT;
            var clientUser = client.CLIENT_USER;
            clientUser.U_PHONE = txtClientPhone.Text;
            clientUser.U_FIRST_NAME = txtFirtName.Text;
            clientUser.U_LAST_NAME = txtLastName.Text;
            clientUser.U_PREV_LAST_NAME = txtPrevLastName.Text;

            clientUser.U_DATE_OF_BIRTH = dtDateBirth.Value;
            clientUser.U_PASSPORT = NautilsuBoolean.ConvertBack(cbPasport.IsChecked.Value);
            if (cmbGender.SelectedValue != null)
                clientUser.U_GENDER = cmbGender.SelectedValue.ToString();
            ///////3//////////


        }

        public void UpdateOnInsert(SDG newSDg)
        {

        }

        public void InsertRequest(LoginXmlHandler loginSdg)
        {



            if (_currentClient != null)
                loginSdg.AddProperties("U_PATIENT", _currentClient.NAME);
            else
            {
                MessageBox.Show("תעודת זהות אינה תקנית , או לקוח אינו קיים.");
                return;
            }

            if (cmbPriority.SelectedValue != null)
                loginSdg.AddProperties("U_PRIORITY", cmbPriority.SelectedValue.ToString());

            loginSdg.AddProperties("U_HOSPITAL_NUMBER", txtHospitalNbr.Text);
            loginSdg.AddProperties("U_REFERRING_PHYSICIAN", textReferringphysician.Text);
            loginSdg.AddProperties("U_IMPLEMENTING_PHYSICIAN", textImplementingphysician.Text);
            loginSdg.AddProperties("U_IMPLEMENTING_CLINIC", cmbClinic.Text);
            loginSdg.AddProperties("U_REFERRAL_PHYSICIAN_CLINI", CmbreferringClinic.Text);
            loginSdg.AddProperties("U_ACCORD_NUMBER", txtAccordNbr.Text);



            if (cbObligation.IsEnabled)
            {
                loginSdg.AddProperties("U_NO_OBLIGATION", NautilsuBoolean.ConvertBack(cbObligation.IsChecked.Value));

            }
            var ag = 0m;
            if (!string.IsNullOrEmpty(txtAge.Text))
                ag = decimal.Parse(txtAge.Text);


            //Don't save age in DB - ashi 24.5.18
            //loginSdg.AddProperties("U_AGE_AT_ARRIVAL", ag.ToString());


            loginSdg.AddProperties("U_PATHOLAB_NUMBER", CreatePatholabNumber(/*cust*/));
        }

        public void InsertOrder(LoginXmlHandler loginOrder)
        {
            U_CUSTOMER cust = cmbCustomer.SelectedValue as U_CUSTOMER;

            loginOrder.AddProperties("U_CUSTOMER", cust.NAME);
            if (cust != null) loginOrder.AddProperties("U_CUSTOMER", cust.NAME);
            loginOrder.AddProperties("U_IN_ADVANCE", NautilsuBoolean.ConvertBack(cbInAdvance.IsChecked.Value));
            if (cbInAdvance.IsChecked.Value)
            {
                if (cmbPayType.SelectedValue != null)
                    loginOrder.AddProperties("U_PAY_TYPE", cmbPayType.SelectedValue.ToString());
                if (numAmount.Value.HasValue)
                    loginOrder.AddProperties("U_PAY_AMOUNT", numAmount.Value.Value.ToString());
            }

            U_CUSTOMER secCust = cmbSecondCustomer.SelectedValue as U_CUSTOMER;
            if (secCust != null) loginOrder.AddProperties("U_SECOND_CUSTOMER", secCust.NAME);
        }

        public IEnumerable<FrameworkElement> SetTags()
        {

            txtClientIdentity.Tag = "U_C_NAME";// Constants.MANDATORYTAG;
            cmbClinic.Tag = "U_CLINIC";
            cmbClinicCode.Tag = "U_CLINIC";
            cmbCustomer.Tag = "U_CUSTOMER";

            // cmbSuspensioCause.Tag = "U_SUSPENSION_CAUSE";
            txtAge.Tag = "U_AGE_AT_ARRIVAL";
            txtFirtName.Tag = "U_FIRST_NAME";
            txtLastName.Tag = "U_LAST_NAME";
            txtPrevLastName.Tag = "U_PREV_LAST_NAME";


            dtDateBirth.Tag = "U_DATE_OF_BIRTH";
            txtAccordNbr.Tag = "U_ACCORD_NUMBER";
            txtHospitalNbr.Tag = "U_HOSPITAL_NUMBER";
            textImplementingphysician.Tag = "U_IMPLEMENTING_PHYSICIAN";
            Cmb_FN_Implementingphysician.Tag = "U_IMPLEMENTING_PHYSICIAN";

            cbInAdvance.Tag = "U_IN_ADVANCE";
            cmbPayType.Tag = "U_IN_ADVANCE";
            numAmount.Tag = "U_IN_ADVANCE";

            cbObligation.Tag = "U_ACCORD_NUMBER";
            cbPasport.Tag = "U_PASSPORT_Y_N";
            txtClientPhone.Tag = "U_PHONE";
            textReferringphysician.Tag = "U_REFERRAL_PHYSICIAN";
            Cmb_FN_Referringphysician.Tag = "U_REFERRAL_PHYSICIAN";
            CmbreferringClinic.Tag = "U_REFERRAL_CLINIC";
            CmbreferringClinic_code.Tag = "U_REFERRAL_CLINIC";
            cmbSecondCustomer.Tag = "U_SECOND_CUSTOMER";
            cmbGender.Tag = "U_SEX";
            cmbPriority.Tag = "U_URGENT";
            //     btnAddSupplier.Tag = "U_RECEIVE_QC";//It's also for qc role only
            return MainGrid.FindVisualChildren<Control>().Where(tb => tb.Tag != null && tb.Tag.ToString() != Constants.MANDATORYTAG);
        }

        public FrameworkElement GetParentElement()
        {
            return MainGrid;
        }

        public bool SpecialValidation()
        {



            if (string.IsNullOrEmpty(txtClientIdentity.Text) || !CheckClientIdentity())
            {
                return false;
            }
            return true;
        }



        public void SetContainerDetails(string sampleMsg, string clinicName)
        {
            if (CurrentSdg == null)
            {     //todo: if combo contains value                
                //    U_CLINIC c = ListData.Clinics.FirstOrDefault(x => x.NAME == clinicName);
                //  cmbClinic.SelectedItem = c;
            }
        }

        public List<FrameworkElement> GetControls4Enter()
        {
            var controls = new List<FrameworkElement>
                {
           cmbCustomer, txtSigns, cmbClinic,cmbClinicCode, cmbSecondCustomer,txtAccordNbr,cbInAdvance,cmbPayType,numAmount,cbObligation,txtHospitalNbr
        ,cmbPriority, Cmb_FN_Implementingphysician, textImplementingphysician, textReferringphysician,
        Cmb_FN_Referringphysician,CmbreferringClinic,CmbreferringClinic_code,
     txtClientIdentity,  txtClientPhone,txtFirtName,txtLastName,txtPrevLastName,dtDateBirth,cbPasport,txtAge,cmbGender,hostRemarks,
    }; return controls;
        }

        #endregion

        private void txtClientIdentity_KeyDown(object sender, KeyEventArgs e)
        {


            txtClientIdentity.Background = Constants.MANDATORYBRUSH;

            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                _currentClient = null;

                if (string.IsNullOrEmpty(txtClientIdentity.Text))
                {
                    e.Handled = true;
                }

                else
                {
                    string cn = txtClientIdentity.Text.Trim();

                    //add 0 before identity
                    string cn0 = OrganizeIdentity(cn);

                    txtClientIdentity.Text = cn0;

                    _currentClient = dal.FindBy<CLIENT>(client => client.NAME == cn0).Include(x => x.CLIENT_USER).SingleOrDefault();

                    
                    if (_currentClient != null)
                    {
                        cbPasport.IsChecked = _currentClient.CLIENT_USER.U_PASSPORT == "T";
                        txtClientPhone.Text = _currentClient.CLIENT_USER.U_PHONE;
                        txtFirtName.Text = _currentClient.CLIENT_USER.U_FIRST_NAME;
                        txtLastName.Text = _currentClient.CLIENT_USER.U_LAST_NAME;
                        txtPrevLastName.Text = _currentClient.CLIENT_USER.U_PREV_LAST_NAME;
                        if (_currentClient.CLIENT_USER.U_DATE_OF_BIRTH.HasValue)
                            dtDateBirth.Value = _currentClient.CLIENT_USER.U_DATE_OF_BIRTH;

                        SetValueInPhrase("Gender", _currentClient.CLIENT_USER.U_GENDER, cmbGender);
                        if (e.Key == Key.Enter)
                        {
                            //  txtClientIdentity.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }


                    }
                    else
                    {
                        ClearClient();
                        var isValid = CheckClientIdentity();
                        if (!isValid)
                            e.Handled = true;

                    }
                }

            }

        }

        private void ClearClient()
        {
            //   cbPasport.IsChecked = false;
            txtClientPhone.Text = string.Empty;
            txtFirtName.Text = string.Empty;
            txtLastName.Text = string.Empty;
            txtPrevLastName.Text = string.Empty;
            dtDateBirth.Value = null;
            ageMsg.Visibility = Visibility.Hidden;

            txtAge.Value = 0;
        }

        private void TxtAge_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            if (txtAge.Text != null)
                e.Handled = txtAge.Text.Length > 2;

        }
 
        private void CbPasport_OnChecked(object sender, RoutedEventArgs e)
        {


            if (CurrentSdg == null && cbPasport.IsChecked == true)
                txtClientIdentity.Background = Constants.MANDATORYBRUSH;
        }
 
        private void DetailsPage_OnLoaded(object sender, RoutedEventArgs e)
        {

            try
            {
                
                if (ListData.Suppliers != null)
                {
                    return;
                }

                //עבודה מהבית
                Thread test = new Thread(() =>
                {

                    ListData.LoadSuppliers();

                    this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            Cmb_FN_Referringphysician.ItemsSource = ListData.Suppliers;
                            Cmb_FN_Implementingphysician.ItemsSource = ListData.Suppliers;
                        }));

                });
                test.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);


            }
        }
        private void Cmb_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab) return;

            var cb = sender as ComboBox;
            if (cb != null)
                cb.IsDropDownOpen = true;
        }
      
        private void Cmb_supp_OnKeyDown(object sender, KeyEventArgs e)
        {
            //הורדת אפס מההתחלה עבור הברקוד
            if (e.Key != Key.Enter) return;
            var cb = sender as ComboBox;

            if (string.IsNullOrEmpty(cb.Text)) return;

            int i = 0;
            while (cb.Text[i] == '0')
            {
                i++;
            }
            string ns = cb.Text.Substring(i);
            cb.Text = ns;
        }
        private void ToHebrew(object sender, KeyboardFocusChangedEventArgs e)
        {
            zLang.Hebrew();
        }
        
        private void CmbreferringClinic_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb != null)
            {
                var clinic = cb.SelectedValue as U_CLINIC;
                if (cb.Name == "CmbreferringClinic")
                {
                    CmbreferringClinic_code.SelectedItem = clinic;
                }
                else if (cb.Name == "CmbreferringClinic_code")
                {
                    CmbreferringClinic.SelectedItem = clinic;
                }
                else
                {
                    return;
                }
            }
        }

        internal void RealseCom()
        {
            //  this.reque stRemarkBridge.Dispose();

        }
   
        internal void ResetRemarks()
        {
            //

            //requestRemarkBridge.Reset();

            requestRemarkNew.Reset();
        }



        #region Date of birth and age



        private void DtDateBirth_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            string exStr = "";
            ageMsg.Visibility = Visibility.Hidden;

            //חישוב גיל לפי תאריך לידה במקרה ומדובר בהוספת דרישה חדשה
            try
            {
                exStr += " e.NewValue" + e.NewValue + "\n";
                if (e.NewValue == null) return;

                var nv = (DateTime)e.NewValue;// DateTime.Parse ( e.NewValue.ToString ( ) );
                exStr += nv + "\n";
                if (nv > dtDateBirth.Maximum.Value || nv < dtDateBirth.Minimum.Value || nv > DateTime.Now)
                {
                    dtDateBirth.Value = null;
                    //    MessageBox.Show("ערך אינו חוקי.", Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //   if (CurrentSdg == null) //Insert mode
                //  {
                exStr += "txtAge.Value=" + txtAge.Value + "\n";

                var calcAge = CalculateAge();
                exStr += "calcAge=" + calcAge + "\n";

                txtAge.Value = calcAge;

                var age = txtAge.Value;
                if (age == null) return;



                if ((age.Value - calcAge) > 1)
                {

                    ageMsg.Visibility = Visibility.Visible;
                    //    MessageBox.Show("גיל אינו תואם תאריך לידה! מוכנס גיל מחושב");
                    return;
                }

                //  }
            }
            catch (Exception ex)
            {



                Logger.WriteLogFile(exStr);
                Logger.WriteLogFile(ex);
                MessageBox.Show("Error on DtDateBirth_OnValueChanged", Constants.MboxCaption, MessageBoxButton.OK,
                                MessageBoxImage.Error);

            }
        }

        private decimal CalculateAge()
        {
            if (dtDateBirth.Value != null)
            {
                //12/12/2000
                DateTime bday = dtDateBirth.Value.Value;
                //2/1/2018
                DateTime today = DateTime.Today;
                //2018-2000=18
                int age = today.Year - bday.Year;

                //1/1/2000 - 
                if (bday > today.AddYears(-age))
                    if(age > 0)
                        age--;

                return age;
            }
            return txtAge.Value ?? 0;
        }

        #endregion
        private void TxtAge_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            return;
            //אם קיים או הוזן תאריך לידה לא לשנות אותו בהתאם לגיל
            if (dtDateBirth.Value.HasValue) return;

            //else
            var age = txtAge.Value; //36 for example
            //Get this year
            var current_year = DateTime.Today.Year; //2018  For example
            //2018-36
            int YearOfBirth = (int)(current_year - age);

            dtDateBirth.Value = new DateTime(YearOfBirth, 1, 1);

        }
     
        private void txtAge_LostFocus(object sender, RoutedEventArgs e)
        {

            //אם קיים או הוזן תאריך לידה לא לשנות אותו בהתאם לגיל
            if (dtDateBirth.Value.HasValue) return;

            //else
            var age = txtAge.Value; //36 for example
            //Get this year
            var current_year = DateTime.Today.Year; //2018  For example
            //2018-36
            int YearOfBirth = (int)(current_year - age);

            dtDateBirth.Value = new DateTime(YearOfBirth, 1, 1);
        }



        public void SetContainerDetails(string sampleMsg)
        {
         //   throw new NotImplementedException();
        }

        private void textImplementingphysician_KeyDown(object sender, KeyEventArgs e)
        {
            // fill the comboBox with the supplier that license number = textbox.text
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                TextBox textBox = sender as TextBox;
                ComboBox relatedComboBox = null;

                switch (textBox.Name)
                {
                    case "textImplementingphysician":
                        relatedComboBox = Cmb_FN_Implementingphysician;
                        break;
                    case "textReferringphysician":
                        relatedComboBox = Cmb_FN_Referringphysician;
                        break;
                    default:
                        break;
                }

                relatedComboBox.SelectedItem = null;

                fillComboBox(textBox, relatedComboBox);
            }
        }

        private void fillComboBox(TextBox textBox, ComboBox relatedComboBox)
        {
            // fill the comboBox with the supplier that has the lincense number in the textbox.

            if (!string.IsNullOrEmpty(textBox.Text))
            {
                string id = textBox.Text;
                var supp = dal.FindBy<SUPPLIER>(s => s.SUPPLIER_USER.U_LICENSE_NBR == id).SingleOrDefault();

                if (supp != null)
                {
                    relatedComboBox.SelectedItem = supp;
                }
            }
        }

        private void deleteLeadingZero(TextBox textBox)
        {
            if(!string.IsNullOrEmpty(textBox.Text)) 
            {
                while (textBox.Text[0].Equals('0'))
                {
                    textBox.Text = textBox.Text.Substring(1);
                }
            }
        }

        private void physician_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            long dummyLong; // this variable exist so we can use the TryParse method.

            if (!string.IsNullOrEmpty(textBox.Text) && Int64.TryParse(textBox.Text, out dummyLong))
            {
                deleteLeadingZero(textBox);
                textBox.Select(textBox.Text.Length, 0);
            }
        }
    }


}
