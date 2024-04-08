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
    interface ISdgRequest
    {
        INautilusServiceProvider ServiceProvider { get; set; }

        void TabOrder(ref int i);

        DataLayer dal { get; set; }

        SDG CurrentSdg { get; set; }

        ManageMetaData Manage_MetaData { get; set; }

        U_PARTS SelectedPart { get; set; }

        ListData ListData { get; set; }
        bool IsAssuta { get; set; }

        void InitilaizeData();

        void DisplayRequestDetails();

        void DisplayNew();
      
        void UpdateRequest();

        void UpdateOnInsert(SDG  newSDg);
     
        void InsertRequest(LoginXmlHandler loginSdg);

        void InsertOrder(LoginXmlHandler loginOrder);

       IEnumerable<FrameworkElement> SetTags();
      
       FrameworkElement GetParentElement();

        bool SpecialValidation();
       

        void SetContainerDetails(string sampleMsg);

        List<FrameworkElement> GetControls4Enter();
    }


}
