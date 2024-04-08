using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Patholab_DAL;
using Xceed.Wpf.Toolkit;

namespace Recive_Request.Classes
{


    public class ManageMetaData
    {
        public ManageMetaData()
        {
            screenMetaDataList = new List<ScreenMetaData>();
        }

        private List<ScreenMetaData> screenMetaDataList;


        internal void SetMetadat2Controls(U_START_SCREEN_USER obj, IEnumerable<FrameworkElement> allTagsControls)
        {
            //Add tag control to list
            foreach (Control tb in allTagsControls)
            {
                var screenMetaData = new ScreenMetaData { screenCtrl = tb };
                screenMetaDataList.Add(screenMetaData);
            }

            //Get selected customer
            var customerFields = obj;
            if (customerFields != null)
            {

                //Get metadata for customer
                var columns = customerFields.GetType().GetProperties();
                foreach (PropertyInfo propertyInfo in columns)
                {
                    //Get column name
                    var colName = propertyInfo.Name;
                    //Get value fro column
                    var value = propertyInfo.GetValue(customerFields, null);
                    //Check if has control for this column
                    var sc = screenMetaDataList.FirstOrDefault(x => x.screenCtrl.Tag.ToString() == colName);

                    //Assign value to list
                    if (sc != null)
                        sc.Value = value.ToString();
                }

            }
        }
    }
    public class ScreenMetaData
    {
        public Control screenCtrl;
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
                        screenCtrl.IsEnabled = false;
                        SetBackGroundColor(Brushes.White);
                        break;
                    case "F1":
                        this.Field_Rule = FieldRule.Regular;
                        screenCtrl.IsEnabled = true;
                        SetBackGroundColor(Brushes.White);

                        break;
                    case "F2":
                        Field_Rule = FieldRule.MandatoryField;
                        SetBackGroundColor(Brushes.AntiqueWhite);
                        break;
                    case "F3":
                        Field_Rule = FieldRule.MandatoryAndStay;
                        SetBackGroundColor(Brushes.AntiqueWhite);
                        break;
                    case "F9":
                        Field_Rule = FieldRule.PerRole;
                        SetBackGroundColor(Brushes.AntiqueWhite);
                        break;
                }
            }
        }

        private void SetBackGroundColor(SolidColorBrush solidColorBrush)
        {
            this.screenCtrl.Background = solidColorBrush;
            return;

            //if (this.screenCtrl is TextBox)
            //{
            //    var tb = this.screenCtrl as TextBox;
            //    //tb.Background =
            //    tb.Background = solidColorBrush;
            //}
            //else if (this.screenCtrl is ComboBox)
            //{
            //    var cmb = this.screenCtrl as ComboBox;
            //    cmb.Background = solidColorBrush;

            //}
            //else if (this.screenCtrl is CheckBox)
            //{
            //    var cb = this.screenCtrl as CheckBox;
            //    cb.Background = solidColorBrush;

            //}
            //else if (this.screenCtrl is DateTimePicker)
            //{
            //    var dt = this.screenCtrl as DateTimePicker;
            //    dt.Background = solidColorBrush;

            //}

        }

        public FieldRule Field_Rule { get; set; }
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