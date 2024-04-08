using System;
using System.Linq;
using System.Windows;
using LSSERVICEPROVIDERLib;
using Patholab_DAL_V1;
using Patholab_XmlService;
using Recive_Request.Classes;
using Patholab_Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace Recive_Request.Controls
{
    /// <summary>
    /// Interaction logic for AddSupplier.xaml
    /// </summary>
    public partial class AddSupplier : Window
    {


        private DataLayer dal;
        private readonly ListData _listData;
        private INautilusServiceProvider sp;
        public event Action SupplierAdded;

        public AddSupplier(DataLayer dal, ListData listData, INautilusServiceProvider serviceProvider)
        {
            InitializeComponent();
            sp = serviceProvider;
            this.dal = dal;
            _listData = listData;

            listData.SetPhrase2Combo("Degree Type", CmbDegree);
            listData.SetPhrase2Combo("Proficency Type", CmbProficency);


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            if (!ValidateSupplier())
            {
                return;
            }
            try
            {


                CreateStaticEntity addSup = new CreateStaticEntity(sp);
                addSup.Login("SUPPLIER", "Supplier", Txtlicencse.Text.Trim());
                addSup.AddProperties("U_FIRST_NAME", txtFN.Text);
                addSup.AddProperties("U_LAST_NAME", txtLN.Text);
                addSup.AddProperties("U_LICENSE_NBR", Txtlicencse.Text);
                addSup.AddProperties("U_ID_NBR", txtIdentity.Text);
                addSup.AddProperties("U_PROFICENCY", CmbProficency.Text);
                addSup.AddProperties("U_DEGREE", CmbDegree.Text);
                var s = addSup.ProcssXml();
                if (!s)
                {
                    MessageBox.Show(string.Format("Error on AddNovellusLink  {0}", addSup.ErrorResponse), "Add Supplier", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("הרופא נוסף למערכת.");
                    string newSupName = addSup.GetValueByTagName("NAME");
                    SUPPLIER ns = dal.FindBy<SUPPLIER>(sup => sup.NAME == newSupName).SingleOrDefault();
                    if (ns != null) _listData.Suppliers.Add(ns);
                    if (SupplierAdded != null) SupplierAdded();
                }
                this.Close();

            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);
                MessageBox.Show(".שגיאה בהוספת רופא" + ex.Message);
            }
        }
        public static bool IsValidEmail(string email)
        {
            try
            {
                MailAddress mailAddress = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        
        private bool ValidateSupplier()
        {
                   
            //(AE)
            if (string.IsNullOrEmpty(txtFN.Text) ||
                string.IsNullOrEmpty(txtLN.Text) ||
                string.IsNullOrEmpty(Txtlicencse.Text)||
                string.IsNullOrEmpty(CmbDegree.Text)
              )
            {
                MessageBox.Show("אנא מלא שדות חובה."); return false;
            }
            if (!(string.IsNullOrEmpty(txtEmail.Text)))
            {
                if (!IsValidEmail(txtEmail.Text))
                    MessageBox.Show("כתובת האימייל אינה חוקית");
                return false;
            }

            if (_listData.Suppliers.Any(supplier => supplier.NAME == Txtlicencse.Text.Trim()))
            {
                MessageBox.Show("רופא עם מספר רשיון זה קיים במערכת.");
                return false;
            }


            return true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();

        }




    }
}
