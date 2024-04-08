using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ADODB;
using LSSERVICEPROVIDERLib;
////using Patholab_DAL;
using Patholab_DAL_V1;
using Patholab_XmlService;
using Patholab_DAL_V1;

namespace Recive_Request.Classes
{
    interface IReceive
    {
        INautilusServiceProvider ServiceProvider { get; set; }

        bool IsUpdatedMode { get; set; }

        CLIENT CurrentClient { get; set; }

        void TabOrder(ref int i);

        DataLayer dal { get; set; }

        SDG CurrentSdg { get; set; }

        Connection Con { get; set; }

        ManageMetaData Manage_MetaData { get; set; }

        U_PARTS SelectedPart { get; set; }

        ListData ListData { get; set; }

        void InitilaizeData();

        void DisplaySdgDetails();

        void DisplayNew();

        void Clear();

        void UpdateSdg();

     
     
        void InsertSdg(LoginXmlHandler loginSdg);

        void InsertOrder(LoginXmlHandler loginOrder);

       IEnumerable<FrameworkElement> SetTags();
      
       FrameworkElement GetParentElement();

        bool SpecialValidation();

        void SetScreenEditMode();
    
        void AfterSavingData(bool insertMode);

        void SetContainerDetails(string sampleMsg, string clinicName);
    }


}
