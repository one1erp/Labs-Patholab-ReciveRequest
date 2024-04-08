using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Recive_Request.Classes
{
    public static class Constants
    {
        public const string QC_ROLE = "QC";
        public static string ROLEs = "QC";
        public const string MANDATORYTAG = "MANDATORY_TAG";
        public const string MboxCaption = "קבלת הזמנה";

       
        public static Brush MANDATORYBRUSH = Brushes.AntiqueWhite;
        public static Brush INVALIDVALUEBRUSH = Brushes.Red;
        public static Brush REGULARBRUSH = Brushes.White;


        public static string EqualDateMsg = "תאריך הפנייה אינו יכול להיות גדול מהיום או מתאריך הגעה.";
        public static string AssutaMandatoryMsg = "חובה להזין מס אסותא .";
        public static string CytologyTypeMandatoryMsg = "חובה להזין סוג ציטולוגיה .";
        public static string CytologyTreatmentTypeMandatoryMsg = "חובה להזין סוג טיפול .";
        public static string OrganMandatoryMsg = "חובה לבחור איבר.";

    }
 
    
}
