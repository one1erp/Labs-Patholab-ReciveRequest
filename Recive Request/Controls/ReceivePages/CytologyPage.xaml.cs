using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ADODB;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
using Patholab_DAL_V1;
using Patholab_XmlService;
using Recive_Request.Annotations;
using Recive_Request.Classes;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;
//using Patholab_DAL;


//using MessageBox = System.Windows.Forms.MessageBox;


namespace Recive_Request.Controls.ReceivePages
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CytologyPage : UserControl, ISdgRequest
    {
        #region Ctor
        public CytologyPage()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        #endregion

        #region Fields

        public event Func<DateTime?, bool> RequestDateChanged;
        public List<U_NORGAN_USER> ListOrgans { get; set; }
        public List<PHRASE_ENTRY> ListCytoSlideType { get; set; }
        public List<PHRASE_ENTRY> ListNextStep { get; set; }
        public List<PHRASE_ENTRY> ListLiquidPType { get; set; }
        public ObservableCollection<CytoDetails> SampleDetailses { get; set; }

        //Interfac fields
        public INautilusServiceProvider ServiceProvider { get; set; }

        public DataLayer dal { get; set; }
        public SDG CurrentSdg { get; set; }
        public Connection Con { get; set; }
        public ManageMetaData Manage_MetaData { get; set; }
        public U_PARTS SelectedPart { get; set; }
        public ListData ListData { get; set; }
        public bool IsAssuta { get; set; }

        #endregion

        #region ISdgRequest

        public void TabOrder(ref int i)
        {

        }

        public void InitilaizeData()
        {
            //   ListData.SetOperatorsByRole("Doctor");

            cmpPatholog.ItemsSource = ListData.Doctors;
            cmpPatholog.DisplayMemberPath = "OPERATOR_USER.U_HEBREW_NAME";
            cmpPatholog.SelectedValuePath = "OPERATOR_ID";
            //System.Diagnostics.Debugger.Launch();
            SampleDetailses = new ObservableCollection<CytoDetails>();

            //Load  in another  thread
            this.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(delegate()
                {
                    ListData.LoadCytoData();
                    ListCytoSlideType = ListData.CytoSlideType;
                    ListNextStep = ListData.CytoNextStep;
                    ListOrgans = ListData.CytoOrgans;
                }));
            //Set default value
            numSamplesdd.Value = 1;



        }

        public void DisplayRequestDetails()
        {

            try
            {
                SampleDetailses.Clear();

                if (CurrentSdg.SAMPLEs != null)
                {
                    numSamplesdd.ValueChanged -= NumSamplesdd_OnValueChanged;
                    numSamplesdd.Value = CurrentSdg.SAMPLEs.Count;
                    numSamplesdd.ValueChanged += NumSamplesdd_OnValueChanged;
                }

                if (CurrentSdg.SDG_USER.PATHOLOG != null)
                    cmpPatholog.SelectedItem = CurrentSdg.SDG_USER.PATHOLOG;

                if (CurrentSdg.SDG_USER.U_REQUEST_DATE != null)
                    dtRequestDate.Value = CurrentSdg.SDG_USER.U_REQUEST_DATE.Value;


                if (CurrentSdg.SAMPLEs != null)
                    foreach (var sample in CurrentSdg.SAMPLEs)
                    {
                        var su = sample.SAMPLE_USER;


                        var smp = new CytoDetails();

                        smp.IsReadOnly = false;
                        smp.MarkAs = su.U_MARK_AS;
                        smp.Mark = su.U_MARK;

                        smp.AssutaNbr = su.U_ASSUTA_NUMBER;

                        smp.CytoSlideType = su.U_CYTOLOGY_SLIDE_TYPE;
                        smp.Organ = su.U_ORGAN;
                        smp.NextStep = su.U_CYTOLOGY_NEXT_STEP;
                        smp.Volume = su.U_VOLUME.HasValue ? su.U_VOLUME.Value : 0;

                        SampleDetailses.Add(smp);
                    }
                numSamplesdd.IsEnabled = true;//false; Change here

            }
            catch (Exception e)
            {
                MessageBox.Show("Error in display cyto");
                Logger.WriteLogFile(e);

            }
        }

        public void DisplayNew()
        {

            bool insertMode = CurrentSdg == null;
            if (insertMode)
            {
                numSamplesdd.Value = 1;

            }

            numSamplesdd.IsEnabled = insertMode;
            dtRequestDate.IsEnabled = insertMode;

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
                    EditSample(i, sample);

                    i++;
                }

                // if the user added more samples, this will log them to the sdg in db
                int newSamples = SampleDetailses.Count - CurrentSdg.SAMPLEs.Count;
                for (int j = 0; j < newSamples; j++)
                {
                    long sampleID = addSample();

                    if (sampleID != -1)
                    {
                        SAMPLE sample = dal.FindBy<SAMPLE>(smpl => smpl.SAMPLE_ID.Equals(sampleID)).FirstOrDefault();

                        if (sample != null)
                            EditSample(j + i, sample);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in update request." + Environment.NewLine + ex.Message);
            }
        }

        private long addSample()
        {
            long sample_id = -1;

            FireEventXmlHandler addSample = new FireEventXmlHandler(ServiceProvider, "Add sample to SDG");
            addSample.CreateFireEventXml("SDG", CurrentSdg.SDG_ID, "Add Sample");
            bool res = addSample.ProcssXml();

            if (!res)
            {
                MessageBox.Show(addSample.ErrorResponse);
                return -1;
            }
            try
            {
                sample_id = Convert.ToInt64(addSample.GetValueByTagName("SAMPLE_ID"));
            }
            catch (Exception ex)
            {
                sample_id = -1;
            }

            return sample_id;
        }

        private void EditSample(int i, SAMPLE sample)
        {

            sample.SAMPLE_USER.U_MARK_AS = SampleDetailses[i].MarkAs;
            sample.SAMPLE_USER.U_MARK = SampleDetailses[i].Mark;
            sample.SAMPLE_USER.U_ASSUTA_NUMBER = SampleDetailses[i].AssutaNbr;
            sample.SAMPLE_USER.U_CYTOLOGY_SLIDE_TYPE = SampleDetailses[i].CytoSlideType;
            sample.SAMPLE_USER.U_ORGAN = SampleDetailses[i].Organ;
            sample.SAMPLE_USER.U_CYTOLOGY_NEXT_STEP = SampleDetailses[i].NextStep;

            sample.SAMPLE_USER.U_VOLUME = SampleDetailses[i].Volume;


            int countExistAliquots = 0;
            foreach (ALIQUOT aliq in sample.ALIQUOTs)
            {

                countExistAliquots++;
            }

            int blocksCount = SampleDetailses[i].NumOfBlocks;
            int newAliquotsNeeded = blocksCount - countExistAliquots;
            if (newAliquotsNeeded > 0)
            {

                for (int j = countExistAliquots; j < blocksCount; j++)
                {
                    FireEventXmlHandler cassetteEvent = new FireEventXmlHandler(ServiceProvider, "Add Cassette");
                    cassetteEvent.CreateFireEventXml("SAMPLE", sample.SAMPLE_ID, "Add Cassette");
                    bool s = cassetteEvent.ProcssXml();


                    if (!s)
                    {
                        MessageBox.Show(cassetteEvent.ErrorResponse);
                        break;
                    }


                    var newAliquot = cassetteEvent.GetValueByTagName("ALIQUOT_ID");
                    long newAliqoutID = -1;

                    if (newAliquot != null)
                    {
                        newAliqoutID = Convert.ToInt64(newAliquot);
                    }

                    ALIQUOT aliquot = dal.FindBy<ALIQUOT>(al => al.ALIQUOT_ID == newAliqoutID).FirstOrDefault();


                    if (aliquot.STATUS == "U")
                    {
                        aliquot.STATUS = "V";
                    }
                }

                dal.SaveChanges();
            }
        }

        public void UpdateOnInsert(SDG newSDg)
        {

        }

        public void InsertRequest(LoginXmlHandler loginSdg)
        {
            OPERATOR oOperator = cmpPatholog.SelectedItem as OPERATOR;

            if (oOperator != null)
                loginSdg.AddProperties("U_PATHOLOG", oOperator.NAME);
            loginSdg.AddProperties("U_REQUEST_DATE", dtRequestDate.Text);


            for (int i = 0; i < SampleDetailses.Count; i++)
            {

                string wf = "";

                if (i == 0)
                    wf = ListData.ReceiveWorkflows.PhraseEntriesDictonary["CYTOLOGY_FIRST_SAMPLE"];
                else
                    wf = SelectedPart.U_PARTS_USER.SAMPLE_WORKFLOW.NAME;

                loginSdg.AddEntityNode("SAMPLE", wf);
                loginSdg.AddProperties2ChildEntityNode("U_MARK_AS", SampleDetailses[i].MarkAs); //מסומנת
                loginSdg.AddProperties2ChildEntityNode("U_MARK", SampleDetailses[i].Mark); //הערה
                loginSdg.AddProperties2ChildEntityNode("U_ASSUTA_NUMBER", SampleDetailses[i].AssutaNbr); //מסומנת

                loginSdg.AddProperties2ChildEntityNode("U_CYTOLOGY_SLIDE_TYPE", SampleDetailses[i].CytoSlideType);
                //הגיע
                loginSdg.AddProperties2ChildEntityNode("U_ORGAN", SampleDetailses[i].Organ); //איבר

          


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

            return CytoMainGrid.FindVisualChildren<Control>().Where(tb => tb.Tag != null);
        }

        public FrameworkElement GetParentElement()
        {
            return CytoMainGrid;

        }

        public bool SpecialValidation()
        {
            return true;
            if (CurrentSdg == null && IsAssuta)
            {
                if (SampleDetailses.Any(t => string.IsNullOrEmpty(t.AssutaNbr)))
                {
                    MessageBox.Show(Constants.AssutaMandatoryMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

                    return false;
                }
            }

            foreach (CytoDetails cd in SampleDetailses)
            {
                

                if (string.IsNullOrEmpty(cd.CytoSlideType))
                {
                    MessageBox.Show(Constants.CytologyTypeMandatoryMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
              
             
            }


            if (lbdt.Content != "")
            {
                MessageBox.Show(Constants.EqualDateMsg, Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);

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
                    cmpPatholog,
                    dtRequestDate,
                    numSamplesdd

                };
            return controls;
        }

        #endregion

        #region Events

        private void NumSamplesdd_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
            {
                SampleDetailses.Clear();
            }
     
            var newe = e.NewValue != null ? int.Parse(e.NewValue.ToString()) : 0;
            var olde = e.OldValue != null ? int.Parse(e.OldValue.ToString()) : 0;


            if (numSamplesdd.Value != null && numSamplesdd.Value.Value > 0 && newe != olde)
            {
                if (newe > olde) //אם נוספו שורות
                {
                    int sum = (newe - olde);

                    for (int i = 0; i < sum; i++)
                    {
                        var cd = new CytoDetails
                        {
                            NumOfBlocks = 1,
                            AssutaNbr = "",
                            MarkAs = "",
                            Mark = "",
                            Volume = 0

                        };
          
                        SampleDetailses.Add(cd);
                    }
                }
                else if (newe < olde) //אם הוסרו שורות
                {
                    if (CurrentSdg != null)
                    {
                        if (newe >= CurrentSdg.SAMPLEs.Count)
                        {
                            removeSample(newe, olde);
                        }
                    }
                    else
                    {
                        removeSample(newe, olde);
                    }
                }

                if (CurrentSdg != null && newe < CurrentSdg.SAMPLEs.Count)
                {
                    numSamplesdd.ValueChanged -= NumSamplesdd_OnValueChanged;
                    numSamplesdd.Value = CurrentSdg.SAMPLEs.Count;
                    numSamplesdd.ValueChanged += NumSamplesdd_OnValueChanged;
                }
            }
        }

        private void removeSample(int newe, int olde)
        {
            int sum = (olde - newe);

            for (int i = 0; i < sum; i++)
            {
                SampleDetailses.RemoveAt(olde - 1 - i);
            }
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

        private void UpDownBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {


            //CytoDetails selectedSample = lb.SelectedItem as CytoDetails;
            //if (selectedSample == null) return;


            IntegerUpDown gg = sender as IntegerUpDown;



            var newe = e.NewValue != null ? int.Parse(e.NewValue.ToString()) : 0;
            var olde = e.OldValue != null ? int.Parse(e.OldValue.ToString()) : 0;

            //Ashi -fix bug 13300 ()

            if (e.NewValue == null)
            {
                newe = olde;
                gg.Value = olde;
                return;
            }
            if (olde == 0)
            {
                return;
            }

        }

        private void cmbCytoSlideType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set default color by selected CytoSlideType
            var combo = sender as ComboBox;


            if (combo != null && combo.SelectedIndex < 0) return;

            var entry = combo.SelectedItem as PHRASE_ENTRY;
            if (entry == null) return;

        }



        #endregion




    }

    #region Cytology Details classes

    public class CytoDetails : INotifyPropertyChanged
    {

        private int _numOfBlocks;

        private string _markAs;
        private string _mark;
        private string _assutaNbr;
        public decimal Volume { get; set; }
        public string CytoType { get; set; }

        public string Organ { get; set; }

        public string CytoSlideType { get; set; }

        public string NextStep { get; set; }
        bool _isReadOnly;
        public int NumOfBlocks
        {
            get { return _numOfBlocks; }

            set
            {
                _numOfBlocks = value;
                OnPropertyChanged("NumOfBlocks");
            }


        }
        public bool IsReadOnly
        {
            get { return _isReadOnly; }

            set
            {
                _isReadOnly = value;
                OnPropertyChanged("IsReadOnly");
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
        public string Mark
        {
            get { return _mark; }
            set
            {
                _mark = value;
                OnPropertyChanged("Mark");
            }
        }


        public string AssutaNbr
        {
            get { return _assutaNbr; }
            set
            {
                _assutaNbr = value;
                OnPropertyChanged("AssutaNbr");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public CytoDetails()
        {

            Volume = 0;
            IsReadOnly = true;

        }



    }



    #endregion

}
