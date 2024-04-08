using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
////using Patholab_DAL;
using Xceed.Wpf.Toolkit;
using Patholab_DAL_V1;

namespace Recive_Request.Classes
{


    public class ManageMetaData
    {
        private List<FieldMetaData> _fieldsMetaDataList;
        private readonly string roleName;
        


        public ManageMetaData(string roleName)
        {
            this.roleName = roleName;
            _fieldsMetaDataList = new List<FieldMetaData>();
        }


        internal void SaveMetadataControls(IEnumerable<FrameworkElement> taggedFrameworkElements)
        {

            //Add tag FrameworkElement to list
            foreach (Control tb in taggedFrameworkElements)
            {
             //   tb.KeyDown += tb_KeyDown;
                var fieldMetaData = new FieldMetaData(roleName) { ScreenCtrl = tb };
                _fieldsMetaDataList.Add(fieldMetaData);
            }
        //    SaveMetadata4Customer();
        }

        //internal void saveCheckPrinterSettings(IEnumerable<FrameworkElement> taggedFrameworkElements)
        //{
        //    foreach (Control tb in taggedFrameworkElements)
        //    {
        //        if (tb.Name.Equals("comboBoxPrinter"))
        //        {
        //            _fieldsMetaDataList.Find(meta => meta.ScreenCtrl.Name == tb.Name).ScreenCtrl = tb;
        //            _fieldsMetaDataList.Find(meta => meta.ScreenCtrl.Name == tb.Name).Field_Rule = FieldRule.MandatoryAndStay;
        //        }

        //        if (tb.Name.Equals("checkBoxPrintCassette"))
        //        {
        //            _fieldsMetaDataList.Find(meta => meta.ScreenCtrl.Name == tb.Name).ScreenCtrl = tb;
        //        }
        //    }
        //}

        void tb_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
      var ctrl=      sender as Control;
            if (ctrl.Tag==Constants.MANDATORYTAG)
            {
                ctrl.Background = Constants.MANDATORYBRUSH;
            }
            else
            {
                ctrl.Background = Constants.REGULARBRUSH;
            }
        }

        internal void SaveMetadata4Customer(U_START_SCREEN_USER customerFields)
        {

            if (customerFields == null) //אם לא קיימת רשומה עבור לקוח זה יוכנס ערך ברירת מחדל
            {
                foreach (FieldMetaData fieldMetaData in _fieldsMetaDataList)
                {
                    fieldMetaData.Value = "F1";
                }
            }
            else
            {

                //Get metadata for customer
                var columns = customerFields.GetType().GetProperties();
                foreach (PropertyInfo propertyInfo in columns)
                {
                    //Get column name
                    var colName = propertyInfo.Name;
                    //Get value from column
                    var value = propertyInfo.GetValue(customerFields, null);
                    //Check if has FrameworkElement for this column
                    var sc = _fieldsMetaDataList.Where(x => x.ScreenCtrl.Tag.ToString() == colName);

                    foreach (FieldMetaData metaData in sc)
                    {



                        //Assign value to list
                        if (metaData != null && value != null)
                            metaData.Value = value.ToString();
                    }
                }
            }

        }
        //Check mandatory fields
        internal bool ValidateMandatory()


        {
            foreach (FieldMetaData data in _fieldsMetaDataList)
            {
                if (data.Field_Rule == FieldRule.MandatoryAndStay ||(data.Field_Rule == FieldRule.MandatoryField)|| 
                    (data.Field_Rule == FieldRule.PerRole && roleName == Constants.QC_ROLE))
                {
                    if (data.ScreenCtrl is TextBox)
                    {
                        var tb = data.ScreenCtrl as TextBox;
                        if (string.IsNullOrEmpty(tb.Text) && tb.IsEnabled)
                        {
                          // data.ScreenCtrl.Background = Constants.INVALIDVALUEBRUSH;
                            data.ScreenCtrl.Focus();
                            return false;
                        }

                    }
                    else if (data.ScreenCtrl is ComboBox)
                    {
                        var cmb = data.ScreenCtrl as ComboBox;
                        if (cmb.SelectedIndex < 0)
                        {
                            data.ScreenCtrl.Focus();
                            return false;
                        }

                    }
                    else if (data.ScreenCtrl is CheckBox)
                    {
                        //todo:How to check

                    }
                    else if (data.ScreenCtrl is DateTimePicker)
                    {
                        var dt = data.ScreenCtrl as DateTimePicker;
                        if (dt.Value == null)
                        {
                            data.ScreenCtrl.Focus();
                            return false;
                        }

                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Reload screen by U_START_SCREEN table
        /// </summary>
        /// <param name="parent"></param>
        public void ReLoadValueAfterAdding(FrameworkElement parent)
        {

            //בודק איזה שדות צריכים להישאר עם ערך לאחר שמירה 
            //ומנקה את מה שלא 
            foreach (FieldMetaData data in _fieldsMetaDataList)
            {
                if ((data.Field_Rule != FieldRule.MandatoryAndStay || data.ScreenCtrl == null))
                {
                    if(!data.ScreenCtrl.Tag.Equals("DO_NOT_CLEAR"))
                        ClearSingleControlValue(data.ScreenCtrl);
                }
            }

           //Clear value of rest controls
            var notagedFrameworkElements = parent.FindVisualChildren<Control>().Where(tb => tb.Tag == null && tb.Name != "" && tb.TemplatedParent == null);
            foreach (var element in notagedFrameworkElements)
            {
                ClearSingleControlValue(element);
            }
        }

 


        //הפונקציה נקראת לאחר עדכון ישות
        public void ClearControlsValue(FrameworkElement element)
        {
            //Clear all controls
            var elements = element.FindVisualChildren<Control>().Where(tb => tb.TemplatedParent == null);
            foreach (var elm in elements)
            {
                if(elm.Tag != null)
                {
                    if(!elm.Tag.Equals("DO_NOT_CLEAR"))
                        ClearSingleControlValue(elm);
                }
            }
        }


        private void ClearSingleControlValue(FrameworkElement element)
        {
            if (element is TextBox)
            //  if (element. is TextBox)
            {
                var tb = element as TextBox;
                tb.Text = string.Empty;

            }
            else if (element is ComboBox)
            {
                var cmb = element as ComboBox;
                cmb.SelectedIndex = -1;


            }
            else if (element is CheckBox)
            {
                var dt = element as CheckBox;
                dt.IsChecked = false;

            }
            else if (element is DateTimePicker)
            {
                var dt = element as DateTimePicker;
                dt.Text = "";
                dt.Value = null;

            }
            else if (element is DecimalUpDown)
            {
                var dd = element as DecimalUpDown;
                dd.Value = null;

            }
            else if (element is IntegerUpDown)
            {
                var dd = element as IntegerUpDown;
                dd.Value =null;
              


            }
            else if (element is DataGrid)
            {
                var dg = element as DataGrid;
                dg.ItemsSource = null;
                dg.Items.Clear();

                dg.Items.Refresh();

            }
        }



        internal void EnabledAll(FrameworkElement element, bool val)
        {
            var controls = element.FindVisualChildren<Control>().Where(tb => tb.TemplatedParent == null);
            foreach (Control child in controls)
            {
                
                child.IsEnabled = val;
            }

        }

    }
    internal class FieldMetaData
    {

        private readonly string roleName;

        public FieldMetaData(string roleName)
        {
            this.roleName = roleName;

        }
        public Control ScreenCtrl { get; set; }

        public FieldRule Field_Rule { get; set; }

        private string _value;

        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;

                switch (_value)
                {
                    case "F0":
                        Field_Rule = FieldRule.Disabled;
                        ScreenCtrl.IsEnabled = false;
                        SetBackGroundColor(Brushes.White);
                        break;
                    case "F1":
                        this.Field_Rule = FieldRule.Regular;
                        ScreenCtrl.IsEnabled = true;
                        SetBackGroundColor(Brushes.White);

                        break;
                    case "F2":
                        Field_Rule = FieldRule.MandatoryField;
                        ScreenCtrl.IsEnabled = true;
                        SetBackGroundColor(Constants.MANDATORYBRUSH);
                        break;
                    case "F3":
                        Field_Rule = FieldRule.MandatoryAndStay;
                        ScreenCtrl.IsEnabled = true;
                        SetBackGroundColor(Constants.MANDATORYBRUSH);
                        break;
                    case "F9":
                        Field_Rule = FieldRule.PerRole;
                        if (this.roleName == Constants.QC_ROLE)
                        {

                            ScreenCtrl.IsEnabled = true;
                            SetBackGroundColor(Brushes.White);
                        }
                        else
                        {

                            ScreenCtrl.IsEnabled = false;
                           // cbObligation.IsEnabled = false;
                            SetBackGroundColor(Brushes.White);
                        }

                        break;
                }
            }
        }

        private void SetBackGroundColor(Brush solidColorBrush)
        {
 
            if (ScreenCtrl.Tag != null && ScreenCtrl.Tag.ToString() == Constants.MANDATORYTAG) return;

            this.ScreenCtrl.Background = solidColorBrush;
            return;

        }


    }

    public enum FieldRule
    {
        Disabled,//F0
        Regular,//F1
        MandatoryField,//F2
        MandatoryAndStay,//F3
        PerRole,//F9
        None
    }
}