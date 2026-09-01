// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.SharedMobileLibrary.Models;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Models.ViewModels;
using Febris.SharedMobileLibrary.Services;
using Febris.ModelLibrary.Models.XApiModels;
//using System.Collections.Generic;
//using System.IO;

namespace Febris.MobileServerV3.Resources
{
    public class LocalHardwareStaticDetails : BaseViewModel
    {
        //this static variable is needed to communicate        
        public static string testData = string.Empty;
        // ---------------------------------------------------------------------------------
        // SEVERANCE (OSS_NODE_PLAN 3.2/3.5): the mobile Server must point at the OPERATOR'S
        // node, not Febris's central SaaS. ApiUrl is populated by URLSettingUtility.SetURL()
        // from the persisted ConfigModel (Domain/DomainPrefix/DomainPort/DomainPath, or
        // DeveloperUrl when the DeveloperAccount federation flag is opted in), so it ships
        // EMPTY across all build configs (the RELEASE branch already did). Superseded compiled
        // defaults are preserved in the [Historical] region below.
        // ---------------------------------------------------------------------------------
        public static string ApiUrl = string.Empty;

        // DeveloperUrl is the opt-in "developer account" federation endpoint (used only when
        // ConfigModel.DeveloperAccount == true) -- the mobile Server's ONLY direct central-tier
        // hit. Externalized to an install-time / environment parameter with an EMPTY last-resort
        // fallback (never the Febris SaaS host). Set FEBRIS_DEVELOPER_API_URL to target a node.
        public static string DeveloperUrl =
            System.Environment.GetEnvironmentVariable("FEBRIS_DEVELOPER_API_URL") ?? string.Empty;

        // Retained public symbol; was only used to build the pre-severance DeveloperUrl.
        public static string prefix = string.Empty;


        //main viewModel for multipule references
        private static MainViewModel _staticMainVM { get; set; }
        public static MainViewModel StaticMainVM
        {
            get { return _staticMainVM; }
            set { 
                _staticMainVM = value; 
                //OnPropertyChanged(); 
            }
        }



        //config data
        public static string Prefix = string.Empty;

        //launch data        
        public static Module selectedModule = default;//new Module();
        public static HardwareUserViewModel selectedUser = default;// new HardwareUserViewModel();
        // ROADMAP 22: `recordSession` is deleted here too. On this tier it had no live writer at
        // all -- the LaunchViewModel assignment was already commented out -- so it was a client-side
        // copy of a decision that never arrived. The node derives the decision now.

        //public static bool testing = false;
        //public static StatementInitializerGetViewModel statementInitalizer = new StatementInitializerGetViewModel();
        public static Statement statement = new Statement();
        public static string serializedStatement = string.Empty;

        //public static bool testing = false;
        //public static StatementInitializerGetViewModel statementInitalizer = new StatementInitializerGetViewModel();
        //public static Statement statement = new Statement();
        //public static string serializedStatement = string.Empty;

        //API pulls
        public static HardwareAuthenticationResponse _hardwareAuthenticationResponse { get; set; }

        public static HardwareInitializationResponse HardwareInitializationResponse { get; set; }
        //private HardwareInitializationResponse _hardwareInitializationResponse;

        //public HardwareInitializationResponse HardwareInitializationResponse
        //{
        //    get { return _hardwareInitializationResponse; }
        //    set {
        //        _hardwareInitializationResponse = value; 
        //        OnPropertyChanged();
        //    }
        //}


        internal static int CheckTimeSpan { get; set; } = 600000;

        public static bool TimerLoopRunning = true;

        #region Loop Timer
        internal static int APIDataRequestFrequency { get; set; } = 600000;


        #endregion


        //public const string _CompanionFileName = "com.febris.mobilecompanionv3.zip";
        public const string _CompanionFileName = "febris.mobilecompanionv3.zip";
        //public static string _CompanionFileFullPath = Path.Combine(System.Environment.CurrentDirectory,"Resources", _CompanionFileName);
        //public static string _CompanionFileFullPath = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, "resources", _CompanionFileName);
        //public static string _CompanionFileFullPath = "/data/user/0/febris.mobileserverv3/resources/com.febris.mobilecompanionv3.zip";
        //public static string _CompanionFileFullPath = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, "resources", _CompanionFileName);
        //public static string _CompanionFileFullPath = Path.Combine(, _CompanionFileName);


        //API domain links - These obviously all need to change        
        //public static string userImageLink = LauncherSharedDetails.ApiUrl + "HealthCareProfessionals/GetProfessionalImage/"; // this will no longer work

        //search variables
        //public static string ModuleSearch = string.Empty;
        //public static string UserSearch = string.Empty;

        //test variables
        //public static bool isObsolete = false;
        ////opt in for video recording
        //public static bool RecordingOptIn = false;

        //this static variable is needed to communicate
        //internal static Professional selectedProfessional;
        //        //internal static string testData = string.Empty;
        //#if (DEBUG)
        //        //internal static string url = "https://localhost:5001/";
        //        internal static string prefix = "www";
        //#elif (STAGING)        
        //#else                
        //        internal static string prefix = "www";
        //#endif



        //launch data        
        //internal static Module selectedTest = new Module();
        //internal static bool testing = false;
        ////internal static StatementInitializerGetViewModel statementInitalizer = new StatementInitializerGetViewModel();
        //internal static Statement statement = new Statement();
        //internal static string serializedStatement = string.Empty;

        ////API pulls
        ////internal static LauncherViewModel launcherViewModel = new LauncherViewModel(); //this is the new single pull
        //internal static List<Module> _testList = new List<Module>();
        //internal static List<MessageBoard> _providerMessageBoard = new List<MessageBoard>();
        //internal static List<MessageBoard> _locationMessageBoard = new List<MessageBoard>();
        //internal static List<AdminMessageBoard> _febrisMessageBoard = new List<AdminMessageBoard>();
        //internal static List<Professional> _professionalList = new List<Professional>();

        //internal static List<ProfessionalViewModel> ProfessionalViewModelList = new List<ProfessionalViewModel>();
        //internal static List<ModuleBaseViewModel> ModuleBaseViewModelList = new List<ModuleBaseViewModel>();

        //API domain links - These obviously all need to change        
        //internal static string professionalImageLink = LocalHardwareStaticDetails.ApiUrl + "HealthCareProfessionals/GetProfessionalImage/"; // this will no longer work

        ////search variables
        //internal static string EduSearch = string.Empty;
        //internal static string ProSearch = string.Empty;

        ////test variables
        //internal static bool isObsolete = false;
        ////opt in for video recording
        //internal static bool RecordingOptIn = false;

        #region Network settings
        //internal static List<string> IPAddressList= new List<string>();
        //internal static string WatcherDeviceIP = string.Empty;
        //internal static string IPSubnet = string.Empty;
        //internal static string PublisherDeviceIP = string.Empty;        
        //public static CompanionDeviceViewModel SelectedDevice = new CompanionDeviceViewModel();        
        //public static List<CompanionDeviceViewModel> PairedDeviceViewModelList = new List<CompanionDeviceViewModel>();
        //public static List<HardwareStatusUpdate> HardwareStatusList = new List<HardwareStatusUpdate>();

        //internal const int HeaderLength = 200;
        internal const int BodyByteBuffer = 1024;

        internal const int ExpectedHeaderLength = 4;
        //internal const string FebrisDelimiterString = "||";
        //internal static byte[] FebrisDelimiter = WiFiP2pServerProcessing.String2Bytes(FebrisDelimiterString);
        #endregion
    }
}
