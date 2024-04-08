using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using Patholab_Common;
using Patholab_DAL_V1;
using Xceed.Wpf.Toolkit;

namespace Recive_Request.Classes
{
    public class ListData
    {
        private DataLayer dal;

        public ListData(DataLayer dal)
        {
            int i = 0;
            this.dal = dal;

            Clinics = dal.GetAll<U_CLINIC>().Include("U_CLINIC_USER").OrderBy(x => x.NAME).ToList();

            Customers = dal.GetAll<U_CUSTOMER>().Include("U_CUSTOMER_USER").OrderBy(x => x.NAME).ToList();

            StartScreen = dal.GetAll<U_START_SCREEN_USER>().Include(x => x.U_START_SCREEN);

            LoadDoctors();

            //LoadSuppliers();



            ReceiveWorkflows = dal.GetPhraseByName("System Parameters");

            PhraseHeaders = new List<PHRASE_HEADER>();

            var parts =
                dal.FindBy<U_PARTS>(
                    x =>
                    x.U_PARTS_USER.U_PART_TYPE == "B" || x.U_PARTS_USER.U_PART_TYPE == "P" ||
                    x.U_PARTS_USER.U_PART_TYPE == "C").Include("U_PARTS_USER").ToList().OrderBy(x => x.NAME);


            PartsB = parts.Where(x => x.U_PARTS_USER.U_PART_TYPE == "B").OrderBy(x => x.NAME).ToList();
            PartsC = parts.Where(x => x.U_PARTS_USER.U_PART_TYPE == "C").OrderBy(x => x.NAME).ToList();
            PartsP = parts.Where(x => x.U_PARTS_USER.U_PART_TYPE == "P").OrderBy(x => x.NAME).ToList();

            PrinterColumnPhrase = dal.GetPhraseByName("Printer Column").PHRASE_ENTRY.ToList();
        }


        private void LoadDoctors()
        {
            string q = "";
            try
            {
                var qDoctors =
                    dal.FindBy<OPERATOR>(o => o.LIMS_ROLE.NAME == "DOCTOR" && o.OPERATOR_USER.U_ORDER > 0 ).Include(x => x.OPERATOR_USER).OrderBy(x => x.NAME);
                q = qDoctors.ToString();
                Doctors = qDoctors.ToList();
            }
            catch (Exception e)
            {
                Logger.WriteQueries(e.Message + " נופל אצל זיו " + " " + q);
            }
        }

        public void LoadSuppliers()
        {
            Suppliers = dal.GetAll<SUPPLIER>().Include("SUPPLIER_USER")/*.Take(100)*/.OrderBy(x => x.NAME).ToList();
        }


        public IQueryable<U_START_SCREEN_USER> StartScreen { get; set; }
        public List<U_CLINIC> Clinics { get; set; }
        public List<U_CLINIC> ClinicsPerCustomer { get; set; }
        public List<U_NORGAN_USER> CytoOrgans { get; set; }
        public List<U_NORGAN_USER> HisOrgans { get; set; }
        public List<U_PARTS> Parts { get; set; }
        public List<U_PARTS> PartsB { get; set; }
        public List<U_PARTS> PartsC { get; set; }
        public List<U_PARTS> PartsP { get; set; }
        public List<U_CUSTOMER> Customers { get; set; }

        //    public List<OPERATOR> Operators { get; set; }
        public List<OPERATOR> Doctors { get; set; }
        public List<SUPPLIER> Suppliers { get; set; }
        public List<PHRASE_HEADER> PhraseHeaders;



        public PHRASE_HEADER ReceiveWorkflows;
        public List<PHRASE_ENTRY> DegreeTypesPhrase;
        public List<PHRASE_ENTRY> PayType;

        public List<PHRASE_ENTRY> ProficencyTypesPhrase;
        public List<PHRASE_ENTRY> GenderPhrase;
        public List<PHRASE_ENTRY> AusspensionCausePhrae;
        public static List<PHRASE_ENTRY> PrinterColumnPhrase;
        public List<PHRASE_ENTRY> ColorPhrase;
        public List<PHRASE_ENTRY> CytoNextStep;
        public List<PHRASE_ENTRY> CytoSlideType;
        public List<PHRASE_ENTRY> LiquidTypePhrase;



        public void SetPhrase2Combo(string phraseName, ComboBox comboBox)
        {
            try
            {

                PHRASE_HEADER ph = PhraseHeaders.FirstOrDefault(phraseHeader => phraseHeader.NAME == phraseName);
                if (ph == null)
                {
                    ph = dal.GetPhraseByName(phraseName);
                    PhraseHeaders.Add(ph);
                }


                comboBox.ItemsSource = ph.PHRASE_ENTRY;
                comboBox.DisplayMemberPath = "PHRASE_DESCRIPTION";
                comboBox.SelectedValuePath = "PHRASE_NAME";
            }
            catch (Exception e)
            {
                Logger.WriteLogFile(e); MessageBox.Show("Error in load " + phraseName + " Phrase " + e.Message);
            }
        }
        public void LoadAddSupplierdata()
        {

            if (DegreeTypesPhrase != null)
            {
                var pdt = dal.GetPhraseByName("Degree Type");
                if (pdt != null)
                {
                    DegreeTypesPhrase = pdt.PHRASE_ENTRY.ToList();
                }
            }
            if (ProficencyTypesPhrase != null)
            {
                var pdt = dal.GetPhraseByName("Proficency Type");
                if (pdt != null)
                {
                    ProficencyTypesPhrase = pdt.PHRASE_ENTRY.ToList();
                }
            }


        }


        internal void LoadHisData()
        {
            HisOrgans = dal.GetAll<U_NORGAN_USER>().Where(organ => organ.U_IS_ACTIVE != null && organ.U_IS_ACTIVE.Equals("T")).OrderBy(x => x.U_ORGAN_HEBREW_NAME).ToList();
        }


        public void LoadCytoData()
        {
            //Cytology
            CytoOrgans = dal.FindBy<U_NORGAN_USER>(o => o.U_ORGAN_TYPE == "C").OrderBy(x => x.U_ORGAN_HEBREW_NAME).ToList();
            ColorPhrase = dal.GetPhraseEntries("Color").ToList();
            CytoNextStep = dal.GetPhraseEntries("Cytology Next Step").ToList();
            LiquidTypePhrase = dal.GetPhraseEntries("Liquid Type").ToList();
            CytoSlideType = dal.GetPhraseEntries("Cytology Slide Type").ToList();
        }
    }

    public class CustomSupplier
    {


        public string ln { get; set; }
        public string fn { get; set; }
        public long id { get; set; }
        public string name { get; set; }
    }
}
