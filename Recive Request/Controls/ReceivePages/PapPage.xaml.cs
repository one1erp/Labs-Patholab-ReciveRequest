using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ADODB;
using LSSERVICEPROVIDERLib;
using Patholab_XmlService;
using Recive_Request.Classes;
using Xceed.Wpf.Toolkit;
using Patholab_DAL_V1;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
////using Patholab_DAL;
//using MessageBox = System.Windows.Forms.MessageBox;

namespace Recive_Request.Controls.ReceivePages
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class PapPage : UserControl, ISdgRequest
    {
        private U_PARTS _selectedPart;

        public PapPage()
        {
            InitializeComponent();
        }

        public INautilusServiceProvider ServiceProvider { get; set; }

        public CLIENT CurrentClient { get; set; }


        public void TabOrder(ref int i)
        {
            dtRequestDatePap.TabIndex = i++;
            txtAstNum.TabIndex = i++;
            uidSmear.TabIndex = i++;
            uidLbc.TabIndex = i++;
            uidHpv.TabIndex = i++;
        }

        public DataLayer dal { get; set; }
        public SDG CurrentSdg { get; set; }
        public Connection Con { get; set; }
        public ManageMetaData Manage_MetaData { get; set; }
        public U_PARTS SelectedPart
        {
            get { return _selectedPart; }
            set
            {
                _selectedPart = value;
                DisplayNew();

            }
        }

        public ListData ListData { get; set; }
        public bool IsAssuta { get; set; }

        public void InitilaizeData()
        {
        }

        public void DisplayRequestDetails()
        {
            EnableControls(false);
            SAMPLE s = CurrentSdg.SAMPLEs.First();
            var sum = s.ALIQUOTs.Count(x => x.ALIQ_FORMULATION_PARENT.Count == 0);
            if (CurrentSdg.SDG_USER.U_REQUEST_DATE != null)
                dtRequestDatePap.Value = CurrentSdg.SDG_USER.U_REQUEST_DATE.Value;
            txtAstNum.Text = s.SAMPLE_USER.U_ASSUTA_NUMBER;
            sumAliq.Value = sum;
        }

        public void DisplayNew()
        {
            bool insertMode = CurrentSdg == null;
            if (!insertMode) return;

            EnableControls(CurrentSdg == null);

            if (_selectedPart != null)
                switch (_selectedPart.NAME.Substring(0, 3))
                {
                    case "220":
                        uidSmear.IsEnabled = true; uidSmear.Value = 1;
                        break;
                    case "395":
                        uidLbc.IsEnabled = true; uidLbc.Value = 1;
                        break;
                    case "414":
                        uidHpv.IsEnabled = true; uidHpv.Value = 1;
                        break;
                    case "420":
                        uidHpv.IsEnabled = true; uidHpv.Value = 1;
                        uidLbc.IsEnabled = true; uidLbc.Value = 1;
                        break;
                    case "415":
                        uidHpv.IsEnabled = true; uidHpv.Value = 1;
                        break;
                }



        }



        public void UpdateRequest()
        {
            CurrentSdg.SDG_USER.U_REQUEST_DATE = dtRequestDatePap.Value;
            SAMPLE s = CurrentSdg.SAMPLEs.First();
            s.SAMPLE_USER.U_ASSUTA_NUMBER = txtAstNum.Text;
        }

        public void UpdateOnInsert(SDG newSDg)
        {
            if (SelectedPart.U_PARTS_USER.DEFAULT_STAIN == null) return;

            var samples = newSDg.SAMPLEs.Select(s => s.ALIQUOTs);
            foreach (ICollection<ALIQUOT> collection in samples)
            {
                foreach (ALIQUOT aliquot in collection)
                {

                    aliquot.ALIQUOT_USER.U_COLOR_TYPE = SelectedPart.U_PARTS_USER.DEFAULT_STAIN.U_PARTS_USER.U_STAIN;
                }
            }

        }


        public void InsertRequest(LoginXmlHandler loginSdg)
        {

            //Set sdg details
            loginSdg.AddProperties("U_REQUEST_DATE", dtRequestDatePap.Text);

            //Add sample wf
            var wf = SelectedPart.U_PARTS_USER.SAMPLE_WORKFLOW.NAME;
            if (wf == null)
            {
                MessageBox.Show("שגיאה בקבלת הדרישה,אנא הגדר WORKFLOW ", Constants.MboxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            loginSdg.AddEntityNode("SAMPLE", wf);
            loginSdg.AddProperties2ChildEntityNode("U_ASSUTA_NUMBER", txtAstNum.Text); //מסומנת

            //Get available aliquots for add
            var childs = MainPapGrid.Children.OfType<IntegerUpDown>().Where(x => x.IsEnabled && x.Value.HasValue);

            //Sum aliquots
            var aliquotNum = childs.Sum(integerUpDown => integerUpDown.Value.Value);

            for (int i = 1; i < aliquotNum; i++)
            {
                //var aliqWF = "";
                //if (i == 0)
                //{
                //    aliqWF = ListData.ReceiveWorkflows.PhraseEntriesDictonary["PAP_FIRST_ALIQUOT"];
                //}
                //else
                //{

                //todo:color type for first aliquot
                var aliqWF = ListData.ReceiveWorkflows.PhraseEntriesDictonary["PAP_EMPTY_ALIQUOT"];


                loginSdg.AddGrandsonEntityNode("ALIQUOT", aliqWF);
                //if (SelectedPart.U_PARTS_USER.DEFAULT_STAIN != null)
                //    loginSdg.AddProperties2GrandsonNode("U_COLOR_TYPE", SelectedPart.U_PARTS_USER.DEFAULT_STAIN.NAME);

            }


            //AfterSavingData(true);

        }
        private void EnableControls(bool b)
        {
            Visibility v;
            if (b)
            {
                v = Visibility.Visible;
                spDisplayMode.Visibility = Visibility.Hidden;
            }
            else
            {
                v = Visibility.Hidden;
                spDisplayMode.Visibility = Visibility.Visible;
            }

            uidLbc.Visibility = v;
            uidHpv.Visibility = v;
            uidSmear.Visibility = v;

            //labels
            tb1.Visibility = v;
            tb2.Visibility = v;
            tb3.Visibility = v;

            uidLbc.IsEnabled = false;
            uidHpv.IsEnabled = false;
            uidSmear.IsEnabled = false;

            uidLbc.Value = null;
            uidHpv.Value = null;
            uidSmear.Value = null;

        }
        public void InsertOrder(LoginXmlHandler loginOrder)
        {

        }

        public IEnumerable<FrameworkElement> SetTags()
        {
            dtRequestDatePap.Tag = "U_REQUEST_DATE";
            txtAstNum.Tag = "U_ASSUTA_NUMBER";
            return MainPapGrid.FindVisualChildren<FrameworkElement>().Where(tb => tb.Tag != null);

        }

        public FrameworkElement GetParentElement()
        {
            return MainPapGrid;
        }



        public bool SpecialValidation()
        {


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
                 dtRequestDatePap,txtAstNum,uidHpv,uidLbc,uidSmear

                                                                                    
            }; return controls;
        }

        public event Func<DateTime?, bool> RequestDateChanged;

        private void DtRequestDatePap_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (RequestDateChanged != null)
            {
                bool b = RequestDateChanged(dtRequestDatePap.Value);
                if (!b)
                {
                    lbdt.Content = Constants.EqualDateMsg;
                    dtRequestDatePap.ToolTip = Constants.EqualDateMsg;

                }
                else
                {
                    lbdt.Content = "";
                }
            }
        }


    }
}
