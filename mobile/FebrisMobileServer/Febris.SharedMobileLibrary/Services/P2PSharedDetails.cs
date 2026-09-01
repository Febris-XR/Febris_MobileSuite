// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.SharedMobileLibrary.Services
{
    public class P2PSharedDetails
    {
        public const string UrlStart = "https://";

        // Empty and operator-set, never baked in per build configuration. This field has no
        // consumers today -- the mobile suite resolves its endpoint through
        // LocalHardwareStaticDetails.ApiUrl -- but it was still compiling retired-tier and
        // personal hosts into every build, and the DEBUG arm declared nothing at all, so the
        // field did not exist in a Debug build.
        public static string ApiUrl = string.Empty;


        //Url things
        //public static string VideoUploaderUrl = ApiUrl + "VideoUploader";
        //public static string xAPIStatementUrl = ApiUrl + "xapi";
        //public static string ModuleDownloaderUrl = ApiUrl + @"ModuleDownload/ModuleDownloader/";
        //public static string ModuleCheckingUrl = ApiUrl + @"ModuleDownload/ModuleChecker";
        //public static string LauncherInitializer = ApiUrl + @"Launcher";
        //public static string StatementInitializer = ApiUrl + @"Launcher/StatementInitializer";

        //credentials
        //public static string getToken = ApiUrl + "token";

        //service Names
        //public const string uploaderName = "FebrisBackgroundUploader";
        //public const string ModuleManagerName = "FebrisModuleManager";

        //video data
        //public static string videoName;
        public static bool SimulationIsRunning = false;

        //argument constants
        public const string StatementPreface = "-febrisData=";
        public const int StatementPrefaceLength = 12;
        public const string VideoDataPreface = "-videoData=";
        public const int VideoDataPrefaceLength = 11;
        public const string SimulationProcessIdPreface = "-simulationProcessId=";
        public const int SimulationProcessIdPrefaceLength = 21;
        public const string SimulationProcessName = "-simulationProcessName=";
        public const int SimulationProcessNameLength = 23;

    }
    public enum ServiceOptions
    {
        Uploader,
        Downloader
    }
    public enum ProcessOptions
    {
        UniqueID,
        ScreenRecorder
    }

    public class InternalStaticDetails
    {
        public static string PairedDeviceList = "PairedDeviceList.json";
    }
    
    public class StatementPassingStaticDetails
    {
        /// <summary>
        /// Gathering arguments
        /// </summary>
        public const string ArgumentExtraTag = "arguments";

        //credentials
        public const string IntentTag = "Intent";
        public const string ReferenceUUIDIntentExtraTag = "ReferenceUUID";
        public const string StatementJsonIntentExtraTag = "RawStatementJson";

        // SIM-T13 G9: Android Intent action strings for the simulation library to
        // signal the Companion's StatementReceiver. Single source of truth -- the
        // C# simulation library (AndroidStatementPassingStaticDetails) and the
        // Companion's StatementReceiver (StatementStaticDetails) both alias these.
        // Drift between the three sites silently broke the integration before
        // the G9 audit; tests in FebrisSimulationLibraryTests assert alignment.
        public const string StatementCreationAction = "com.febris.STATEMENT_CREATE";
        public const string StatementUpdateAction = "com.febris.STATEMENT_UPDATE";
        public const string StatementErrorAction = "com.febris.STATEMENT_ERROR";
    }
}
