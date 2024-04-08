using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Patholab_Common;
////using Patholab_DAL;
using Patholab_DAL_V1;
using Recive_Request.Classes;
using Recive_Request.Controls.ReceivePages;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Forms.UserControl;
using ADODB;

namespace Recive_Request
{
    [ComVisible(true)]
    [ProgId("Receive_request.Receive_requestCls")]
    public partial class Receive_requestCls : UserControl, IExtensionWindow
    {


        #region Private fields
        private INautilusProcessXML xmlProcessor;
        private INautilusUser _ntlsUser;
        private IExtensionWindowSite2 _ntlsSite;
        private INautilusServiceProvider sp;
        private INautilusDBConnection _ntlsCon;

        private DataLayer dal;
        public bool DEBUG;

        #endregion
        public Receive_requestCls()
        {
            InitializeComponent();
            this.Disposed += Receive_requestCls_Disposed;
            BackColor = Color.FromName("Control");
            this.Dock = DockStyle.Fill;

        }

        void Receive_requestCls_Disposed(object sender, EventArgs e)
        {
            GC.Collect();
        }

        public bool CloseQuery()
        {
            var dgr1 = MessageBox.Show(@"?האם אתה בטוח שברצונך לצאת ", Constants.MboxCaption, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dgr1 == MessageBoxResult.Yes)
            {
                if (_ntlsSite != null) w.CloseQuery();
                this.Dispose();

                return true;
            }
            else
            {
                return false;
            }

        }
        public WindowRefreshType DataChange()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;

        }

        public WindowButtonsType GetButtons()
        {
            return LSExtensionWindowLib.WindowButtonsType.windowButtonsNone;


        }

        public void Internationalise()
        {

        }

        public void PreDisplay()
        {
            xmlProcessor = Utils.GetXmlProcessor(sp);

            _ntlsUser = Utils.GetNautilusUser(sp);

            InitializeData();
        }

        public void RestoreSettings(int hKey)
        {
        }

        public bool SaveData()
        {
            return true;
        }

        public void SaveSettings(int hKey)
        {
        }

        public void SetParameters(string parameters)
        {
        }

        public void SetServiceProvider(object serviceProvider)
        {
            sp = serviceProvider as NautilusServiceProvider;
            _ntlsCon = Utils.GetNtlsCon(sp);

            //INautilusUser nUser = sp.QueryServiceProvider("user") as INautilusUser;
            //MessageBox.Show(nUser.GetWorkstationName().ToString());
        }

        public void SetSite(object site)
        {
            _ntlsSite = (IExtensionWindowSite2)site;
            _ntlsSite.SetWindowInternalName("Receive_request");
            _ntlsSite.SetWindowRegistryName("Receive_request");
            _ntlsSite.SetWindowTitle("קבלת הזמנה");
        }

        public void Setup()
        {
            w.SetFocus();
        }



        public WindowRefreshType ViewRefresh()
        {
            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;
        }

        public void refresh()
        {

        }

        private MasterPage w;
        private void InitializeData()
        {

            w = new MasterPage(sp, xmlProcessor, _ntlsCon, _ntlsSite, _ntlsUser);
            elementHost1.Child = w;
            w.InitilaizeData();
        }



    }
}
