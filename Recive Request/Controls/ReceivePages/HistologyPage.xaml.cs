using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ADODB;
using LSSERVICEPROVIDERLib;
using Patholab_XmlService;
using Recive_Request.Annotations;
using Recive_Request.Classes;
using Patholab_DAL_V1;
using Xceed.Wpf.Toolkit;
using Control = System.Windows.Controls.Control;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using System.IO;

////////using Patholab_DAL;


namespace Recive_Request.Controls.ReceivePages
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class HistologyPage : UserControl, ISdgRequest, INotifyPropertyChanged
    {

        #region Ctor
        public HistologyPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        #endregion
        public event Func<DateTime?, bool> RequestDateChanged;

        public List<PHRASE_ENTRY> PrinterColumns { get; set; }

        public List<U_NORGAN_USER> ListOrgans { get; set; }

        public MasterPage parentPage = null;

        public INautilusServiceProvider ServiceProvider { get; set; }
        public DataLayer dal { get; set; }
        public SDG CurrentSdg { get; set; }
        public Connection Con { get; set; }
        public ManageMetaData Manage_MetaData { get; set; }
        public U_PARTS SelectedPart { get; set; }
        public ListData ListData { get; set; }
        public bool IsAssuta { get; set; }
        public ObservableCollection<SampleDetails> SampleDetailses { get; set; }
        public void TabOrder(ref int i)
        {
            numSamplesdd.TabIndex = i++;
            dtRequestDate.TabIndex = i++;
            cmpPatholog.TabIndex = i++;
            dg.TabIndex = i++;
        }

        public void InitilaizeData()
        {
            SampleDetailses = new ObservableCollection<SampleDetails>();

            var ph = dal.GetPhraseEntries("Printer Column");

            PrinterColumns = ph.ToList();

            //Load  in another  thread
            this.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(delegate()
                {
                    ListData.LoadHisData();
                    this.ListOrgans = ListData.HisOrgans;


                }));


            cmpPatholog.ItemsSource = ListData.Doctors;
            cmpPatholog.DisplayMemberPath = "OPERATOR_USER.U_HEBREW_NAME";
            cmpPatholog.SelectedValuePath = "OPERATOR_ID";

            //Set default value עבר ל XAML
            numSamplesdd.Value = 1;
        }

        public void DisplayRequestDetails()
        {

            SampleDetailses.Clear();

            if (CurrentSdg.SAMPLEs != null)
                numSamplesdd.Value = CurrentSdg.SAMPLEs.Count;

            if (CurrentSdg.SDG_USER.PATHOLOG != null)
                cmpPatholog.SelectedItem = CurrentSdg.SDG_USER.PATHOLOG;
            if (CurrentSdg.SDG_USER.U_REQUEST_DATE != null)
                dtRequestDate.Value = CurrentSdg.SDG_USER.U_REQUEST_DATE.Value;


            if (CurrentSdg.SAMPLEs != null)
                foreach (var sample in CurrentSdg.SAMPLEs.OrderBy(X => X.SAMPLE_ID))
                {

                    var blocks = sample.ALIQUOTs.Count(x => x.ALIQ_FORMULATION_PARENT.Count == 0);//PARENTS

                    SampleDetailses.Add(new SampleDetails()
                        {
                            SampleId = sample.SAMPLE_ID,
                            MarkAs = sample.SAMPLE_USER.U_MARK_AS,
                            AssutaNbr = sample.SAMPLE_USER.U_ASSUTA_NUMBER,
                            NumOfBlocks = blocks,

                            PrinterCol = sample.ALIQUOTs.First().ALIQUOT_USER.U_PRINTER_COL != null ? sample.ALIQUOTs.First().ALIQUOT_USER.U_PRINTER_COL : PrinterColumns.First().PHRASE_NAME,
                            OrganName = sample.SAMPLE_USER.U_ORGAN//,

                        });
                }
            dg.ItemsSource = SampleDetailses;
        }

        public void DisplayNew()
        {

            bool insertMode = CurrentSdg == null;
            numSamplesdd.IsEnabled = insertMode;
            dtRequestDate.IsEnabled = insertMode;
            
            if (insertMode)
            {
                numSamplesdd.Value = 1;
                numSamplesdd.Focus();//todo focus doesnt work

            }
        }


        public void UpdateRequest()
        {
            try
            {
                CurrentSdg.SDG_USER.PATHOLOG = cmpPatholog.SelectedItem as OPERATOR;
                CurrentSdg.SDG_USER.U_REQUEST_DATE = dtRequestDate.Value;
                int i = 0;
                foreach (var sample in CurrentSdg.SAMPLEs.OrderBy(X => X.SAMPLE_ID))
                {

                    sample.SAMPLE_USER.U_MARK_AS = SampleDetailses[i].MarkAs;
                    sample.SAMPLE_USER.U_ASSUTA_NUMBER = SampleDetailses[i].AssutaNbr;
                    sample.ALIQUOTs.First().ALIQUOT_USER.U_PRINTER_COL = SampleDetailses[i].PrinterCol;

                    var organ = ListData.HisOrgans.FirstOrDefault(x => x.U_ORGAN_HEBREW_NAME == SampleDetailses[i].OrganName);
                    if (organ != null)
                    {
                        sample.SAMPLE_USER.U_ORGAN_CODE = organ.U_ORGAN_CODE;
                        sample.SAMPLE_USER.U_ORGAN = SampleDetailses[i].OrganName;
                    }

                    if (CurrentSdg.STATUS == "U")
                    {
                        var blocksCount = SampleDetailses[i].NumOfBlocks;

                        if (blocksCount > 1)
                        {
                            for (int j = 1; j < blocksCount; j++)
                            {

                                FireEventXmlHandler cassetteEvent = new FireEventXmlHandler(ServiceProvider, "Add Cassette");
                                cassetteEvent.CreateFireEventXml("SAMPLE", sample.SAMPLE_ID, "Add Cassette");
                                bool s = cassetteEvent.ProcssXml();

                                var newAliquot = cassetteEvent.GetValueByTagName("ALIQUOT_ID");
                                long newAliqoutID = -1;

                                if (newAliquot != null)
                                {
                                    newAliqoutID = Convert.ToInt64(newAliquot);
                                }

                                if (!s)
                                {
                                    MessageBox.Show(cassetteEvent.ErrorResponse);
                                    return;
                                }

                                ALIQUOT aliquot = dal.FindBy<ALIQUOT>(al => al.ALIQUOT_ID == newAliqoutID).FirstOrDefault();

                                aliquot.ALIQUOT_USER.U_PRINTER_COL = SampleDetailses[i].PrinterCol;

                                if (aliquot.STATUS == "U")
                                {
                                    aliquot.STATUS = "V";
                                }
                            }
                            dal.SaveChanges();
                        }
                    }

                    i++;
                }

                changeToStatusV(CurrentSdg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in update request." + Environment.NewLine + ex.Message);
            }
        }

        private void changeToStatusV(SDG sdg)
        {
            if (sdg != null)
            {
                foreach (SAMPLE sample in sdg.SAMPLEs.OrderBy(x => x.SAMPLE_ID))
                {
                    foreach (ALIQUOT aliquot in sample.ALIQUOTs.OrderBy(y => y.ALIQUOT_ID))
                    {
                        aliquot.STATUS = "V";
                    }

                    sample.STATUS = "V";
                }
                sdg.STATUS = "V";
                dal.SaveChanges();
            }
        }
        public void UpdateOnInsert(SDG newSDg)
        {
            var samples = newSDg.SAMPLEs.OrderBy(s => s.SAMPLE_ID).ToArray();

            for (int i = 0; i < samples.Count(); i++)
            {
                var aliquots = samples[i].ALIQUOTs.OrderBy(s => s.ALIQUOT_ID).ToArray();

                for (int j = 0; j < aliquots.Count(); j++)
                {
                    aliquots[j].ALIQUOT_USER.U_PRINTER_COL = SampleDetailses[i].PrinterCol;
                }
            }

        }

        public void InsertRequest(LoginXmlHandler loginSdg)
        {
            OPERATOR oOperator = cmpPatholog.SelectedItem as OPERATOR;

            if (oOperator != null)
                loginSdg.AddProperties("U_PATHOLOG", oOperator.NAME);
            loginSdg.AddProperties("U_REQUEST_DATE", dtRequestDate.Text);

            //שומר את הערכים שהוזנו בטבלה
            dg.CommitEdit();
            dg.EndInit();


            for (int i = 0; i < SampleDetailses.Count; i++)
            {

                string wf = "";

                if (i == 0)
                    wf = ListData.ReceiveWorkflows.PhraseEntriesDictonary["BIOPSY_FIRST_SAMPLE"];
                else
                    wf = SelectedPart.U_PARTS_USER.SAMPLE_WORKFLOW.NAME;

                loginSdg.AddEntityNode("SAMPLE", wf);
                loginSdg.AddProperties2ChildEntityNode("U_MARK_AS", SampleDetailses[i].MarkAs);
                loginSdg.AddProperties2ChildEntityNode("U_ASSUTA_NUMBER", SampleDetailses[i].AssutaNbr);
                //loginSdg.AddProperties2ChildEntityNode("U_ORGAN", SampleDetailses[i].OrganName);
                var organ = ListData.HisOrgans.FirstOrDefault(x => x.U_ORGAN_HEBREW_NAME == SampleDetailses[i].OrganName);
                if (organ != null)
                {
                    loginSdg.AddProperties2ChildEntityNode("U_ORGAN_CODE", organ.U_ORGAN_CODE);
                    loginSdg.AddProperties2ChildEntityNode("U_ORGAN", SampleDetailses[i].OrganName);
                }


                var blocksCount = SampleDetailses[i].NumOfBlocks;


                if (blocksCount > 1)
                {
                    for (int j = 1; j < blocksCount; j++)
                    {

                        loginSdg.AddGrandsonEntityNode("ALIQUOT", ListData.ReceiveWorkflows.PhraseEntriesDictonary["HISTOLOGY_EMPTY_ALIQUOT"]);
                        loginSdg.AddProperties2GrandsonNode("U_PRINTER_COL", SampleDetailses[i].PrinterCol);

                    }
                }

            }
        }




        public void InsertOrder(LoginXmlHandler loginOrder)
        {

        }

        public IEnumerable<FrameworkElement> SetTags()
        {
            numSamplesdd.Tag = "U_SAMPLE_NBR";

            dtRequestDate.Tag = "U_REQUEST_DATE";

            cmpPatholog.Tag = "U_PATHOLOG";

            return HisMainGrid.FindVisualChildren<Control>().Where(tb => tb.Tag != null);

        }

        public FrameworkElement GetParentElement()
        {
            return HisMainGrid;
        }

        public bool SpecialValidation()
        {


            if (CurrentSdg == null)
            {
                if (IsAssuta && SampleDetailses.Any(t => string.IsNullOrEmpty(t.AssutaNbr)))
                {
                    MessageBox.Show(Constants.AssutaMandatoryMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                    return false;
                }
            }
            if (lbdt.Content != "")
            {
                MessageBox.Show(Constants.EqualDateMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }


            if (SampleDetailses.Any(t => string.IsNullOrEmpty(t.OrganName)))
            {
                MessageBox.Show(Constants.OrganMandatoryMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
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
                    cmpPatholog,dtRequestDate,numSamplesdd
                    
                };
            return controls;
        }

        #region Controls Events

        private void NumSamplesdd_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
            {
                SampleDetailses.Clear();
            }

            if (CurrentSdg == null) //insert mode
            {


                var newe = e.NewValue != null ? int.Parse(e.NewValue.ToString()) : 0;
                var olde = e.OldValue != null ? int.Parse(e.OldValue.ToString()) : 0;



                if (numSamplesdd.Value != null && numSamplesdd.Value.Value > 0 && newe != olde)
                {
                    if (newe > olde) //אם נוספו שורות
                    {
                        int sum = (newe - olde);

                        for (int i = 0; i < sum; i++)
                        {
                            SampleDetailses.Add(new SampleDetails()
                                {
                                    NumOfBlocks = 1,
                                    MarkAs = "",
                                    AssutaNbr = "",
                                    PrinterCol = PrinterColumns.First().PHRASE_NAME,
                                    OrganName = ""
                                });
                        }
                    }
                    else if (newe < olde) //אם הוסרו שורות
                    {
                        int sum = (olde - newe);

                        for (int i = 0; i < sum; i++)
                        {
                            var indx = (olde - 1 - i);
                            if (indx != 0)
                                SampleDetailses.RemoveAt(indx);
                        }

                    }
                    dg.ItemsSource = SampleDetailses;
                }
            }
        }
        private void Dg_OnLoaded_(object sender, RoutedEventArgs e)
        {

            //bool b = CurrentSdg != null;

            //dg.Columns[0].IsReadOnly = b;
            //dg.Columns[1].IsReadOnly = b;
            //dg.Columns[2].IsReadOnly = b;

        }
        private void Dg_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {


            //  e.Row.IsEnabled = CurrentSdg == null;
            //    dg.Columns[3].IsEnabled = false;
            //מספור שורות
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();


        }

        private void DtRequestDate_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (RequestDateChanged != null)
            {
                bool b = RequestDateChanged(dtRequestDate.Value);
                if (!b)
                {

                    lbdt.Content = Constants.EqualDateMsg;
                    dtRequestDate.ToolTip = Constants.EqualDateMsg;

                }
                else
                {
                    lbdt.Content = "";
                }
            }
        }


        private void BtnPrintCst_OnClick(object sender, RoutedEventArgs e)
        {
            var row = (SampleDetails)dg.SelectedItem;
            if (row.SampleId == null)
            {
                MessageBox.Show("לא ניתן להדפיס טרם שמירה.", Constants.MboxCaption,
                                                      MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                if (parentPage != null)
                {
                    if (parentPage.checkBoxPrintCassette.IsChecked.Value && parentPage.comboBoxPrinter.SelectedItem != null)
                    {
                        var aliqIds = from a in dal.GetAll<ALIQUOT>()
                                      where a.SAMPLE_ID == row.SampleId
                                   && a.ALIQ_FORMULATION_PARENT.Count == 0
                                      select a.ALIQUOT_ID;


                        string eventToFire = (parentPage.comboBoxPrinter.SelectedItem as PHRASE_ENTRY).PHRASE_INFO;
                        foreach (var id in aliqIds)
                        {

                            string dt = DateTime.Now.ToString("yyyyMMddHHmmssFFF");

                            FireEventXmlHandler fireEvent = new FireEventXmlHandler(ServiceProvider, Path.Combine(eventToFire.Replace(' ', '_'), dt));
                            fireEvent.CreateFireEventXml("ALIQUOT", id, eventToFire);
                            bool s = fireEvent.ProcssXml();

                            if (!s)
                            {
                                MessageBox.Show(string.Format("Aliquot ID: {0}{1}Can't print cassette more than once.", id, Environment.NewLine));
                            }
                        }
                    }
                    else
                    {
                        parentPage.checkBoxPrintCassette.IsChecked = true;

                        // Coloring the ComboBox (mandatory)
                        var textbox = (System.Windows.Controls.TextBox)parentPage.comboBoxPrinter.Template.FindName("PART_EditableTextBox", parentPage.comboBoxPrinter);
                        if (textbox != null)
                        {
                            var parent = (Border)textbox.Parent;
                            parent.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 190, 132));
                        }


                        MessageBox.Show("Please choose printer.");
                    }
                }
                else
                {
                    MessageBox.Show("Error getting printer.");
                }
            }
        }

        private void HistologyPage_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            if (CurrentSdg == null)
            {


                if ((bool)e.NewValue == true)
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.ContextIdle,
                        new Action(delegate()
                            {
                                //doesnt work first time 
                                numSamplesdd.IsEnabled = true;
                                numSamplesdd.Focusable = true;
                                numSamplesdd.Focus();

                                Keyboard.Focus(numSamplesdd);
                            }));
                }
            }
        }
        #endregion



        public bool _ColReadOnly;
        public bool ColReadOnly
        {
            get { return _ColReadOnly; }

            set
            {
                _ColReadOnly = value;
                OnPropertyChanged("ColReadOnly");
            }


        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));

        }

        private void cmbOrgans_GotFocus(object sender, RoutedEventArgs e)
        {

        }
    }


    #region SampleDetails class
    public class SampleDetails : INotifyPropertyChanged
    {
        public long? SampleId { get; set; }
        private int _numOfBlocks;
        private string _printerCol;
        private string _printerColName;
        private string _markAs;
        private string _assutaNbr;
        private string _organName;

        public string OrganName
        {
            get { return _organName; }
            set
            {
                _organName = value;
                OnPropertyChanged("OrganName");
            }
        }
        //private string _OrganCode;

        //public string OrganCode
        //{
        //    get { return _OrganCode; }
        //    set
        //    {
        //        _OrganCode = value;
        //        OnPropertyChanged("OrganCode");
        //    }
        //}
        //  private string _OrganSide;

        //     public string OrganSide
        //     {
        //         get { return _OrganSide; }
        //         set
        //         {
        //             _OrganSide = value;
        //             OnPropertyChanged("OrganSide");
        //         }
        //     }

        //     private string _OrganTopography;

        //     public string OrganTopography
        //     {
        //         get { return _OrganTopography; }
        //         set
        //         {
        //             _OrganTopography = value;
        //             OnPropertyChanged("OrganTopography");
        //         }
        //     }
        //   //  public string  { get; set; }

        ////     public string  { get; set; }

        //   //  public string  { get; set; }

        public string AssutaNbr
        {
            get { return _assutaNbr; }
            set
            {
                _assutaNbr = value;
                OnPropertyChanged("AssutaNbr");
            }
        }

        public int NumOfBlocks
        {
            get { return _numOfBlocks; }

            set
            {
                _numOfBlocks = value;
                OnPropertyChanged("NumOfBlocks");
            }


        }
        public string PrinterCol
        {
            get { return _printerCol; }
            set
            {
                _printerCol = value;
                OnPropertyChanged("PrinterCol");
            }
        }

        public string MarkAs
        {
            get { return _markAs; }
            set
            {
                _markAs = value;
                OnPropertyChanged("MarkAs");
            }
        }

        public string PrinterColName
        {
            get
            {
                var phraseEntry = ListData.PrinterColumnPhrase.Where(a => a.PHRASE_NAME == _printerCol).SingleOrDefault();
                if (phraseEntry != null)
                    return
                        phraseEntry.PHRASE_DESCRIPTION;
                else
                {
                    return "";
                }
            }
            set
            {
                _printerColName = value;
                OnPropertyChanged("PrinterColName");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


    }
    #endregion



}
