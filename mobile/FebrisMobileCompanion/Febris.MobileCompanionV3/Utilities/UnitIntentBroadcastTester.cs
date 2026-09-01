// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
///Tried but I am not sure how to test without the ability to access UnityEngine

//using Febris.CsharpSimulationLibraryNetStandard.Statement;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace Febris.MobileCompanionV3.Utilities
//{
//    public class UnitIntentBroadcastTester
//    {
        
//        //Initalization
//        private Stopwatch __stopwatch = new Stopwatch();
//        private float __nextUpdate = 10f;
//        [SerializeField]
//        public float __period = 10f;
//        [SerializeField]
//        public static float __maxScore = 100f;
//        [SerializeField]
//        public static float __minScore = 0f;
//        [SerializeField]
//        public static float __scaledScore = 0f;
//        [SerializeField]
//        public static float __rawScore = 0f;

//        Initializer __initializer = new Initializer();
//#region Android Test
//        private void Start()
//        {
//            try
//            {
//                __stopwatch.Start();
//                //GetDataFromCommandLineArgs();
//                //string[] arguments = Environment.GetCommandLineArgs();

//                #region debugging            

//                //UnityEngine.Debug.Log("number of arguments: " + arguments.Length);
//                ////if (arguments == null || arguments.Length < 2)
//                ////{
//                //string statementString = "-febrisData={\"Timestamp\":\"2021-05-22T16:59:43.412519Z\",\"Actor\":{\"Id\":92,\"UUID\":\"02ded85d-ae0a-42fb-a558-515ef4fe025d\",\"ObjectType\":\"Agent\",\"Name\":\"Vernell_-_Mink\",\"Mbox_sha1sum\":\"3ceb27522d6dc1c626dc4804bf236dfb24130ed7\"},\"Verb\":{\"Key\":1,\"UUID\":\"f72789c6-47ee-460f-8b68-05ca7d6f1cf9\",\"Id\":\"https://febr.is/xAPI/VerbDetails/Attempted\",\"Display\":{\"en\":\"Attempted\"}},\"Object\":{\"Key\":3,\"UUID\":\"3f1b8ce2-f785-462a-aaee-821de909e9e1\",\"Id\":\"https://febr.is/ModuleBase/30d58194-ef1d-4d13-a57e-01c21f47bf36\",\"ObjectType\":\"Activity\",\"Definition\":{\"Id\":3,\"UUID\":\"20a2ac4c-7fef-43bf-8b0c-3cb94ef821cb\",\"Name\":{\"en\":\"Sterile_-_Field_-_Preparation\"},\"Description\":{\"en\":\"Steps_-_for_-_setting_-_up_-_a_-_sterile_-_field\"},\"Type\":\"https://febr.is/ModuleBase/3\",\"MoreInfo\":\"https://febr.is/ModuleBase/3\",\"InteractionType\":\"performance\",\"CorrectResponsesPattern\":\"[,]\",\"InteractionComponents\":{\"Step1\":\"_-_Get_-_Cloth\",\"Step2\":\"_-_Clean_-_table\",\"Step3\":\"_-_Select_-_Sterile_-_pack\",\"Step3.1\":\"_-_Check_-_Sterile_-_pack_-_for_-_date_-_and_-_damage\",\"Step4\":\"_-_etc\"}}}}";
//                //List<string> argList = new List<string>();
//                //argList.Add(fileLoc);
//                //argList.Add(statementString);
//                //arguments = argList.ToArray();
//                ////}            
//                //UnityEngine.Debug.Log("number of arguments after adding: " + arguments.Length);
//                //foreach (var arg in arguments)
//                //{
//                //    UnityEngine.Debug.Log(arg);
//                //}

//                #endregion

//                #region febris initalizer           
//                ///Get device type so it can iniitalize properly;

//                ///Need to gather file path
//                //string startpath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;//Application.persistentDataPath;
                
//                bool isInitialized = false;

//                ///This is a different test
//                #region (currently unused) tester for platform --need interp  
//                ////////if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
//                ////////    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
//                ////////    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
//                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//                //{
//                //    AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//                //    AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
//                //    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

//                //    bool hasExtra = intent.Call<bool>("hasExtra", "arguments");
//                //    if (!hasExtra)
//                //    {
//                //        UnityEngine.Debug.Log("*********************Intent has not extras******************************");
//                //    }

//                //    //AndroidJavaObject extras = GetExtras(intent);

//                //    AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras");
//                //    string arguments = string.Empty;

//                //    try
//                //    {
//                //        arguments = extras.Call<string>("getString", "arguments");
//                //        UnityEngine.Debug.Log(arguments);
//                //    }
//                //    catch
//                //    {
//                //        UnityEngine.Debug.Log("*********************FAILED TO GET ARGUMENTS******************************");
//                //    }

//                //    if (string.IsNullOrEmpty(arguments))
//                //    {

//                //    }

//                //    string path = GetAndroidExternalStoragePath();
//                //    isInitialized = __initializer.Initialize(arguments, path);
//                //}                
//                //else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
//                //{
//                //    string[] arguments = Environment.GetCommandLineArgs();
//                //    isInitialized = __initializer.Initialize(arguments);
//                //}
//                //else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
//                //{
//                //    string[] arguments = Environment.GetCommandLineArgs();
//                //    isInitialized = __initializer.Initialize(arguments);
//                //}
//                //else
//                //{

//                //}
//                #endregion

//                #region Application.platform tests
//                string[] argumentArray = default;
//                string[,] statementOutputArray = default;
//                //string arguments = string.Empty;

//                ///Gathering Command line Arguments so xApi statements can be used.
//                switch (Application.platform)
//                {
//                    case RuntimePlatform.Android:
//                        {
//                            AndroidJavaClass UnityPlayer = new AndroidJavaClass(AndroidIntentConst.UnityPlayerTag);
//                            AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>(AndroidIntentConst.GetCurrentActivityTag);
//                            AndroidJavaObject inputIntent = currentActivity.Call<AndroidJavaObject>(AndroidIntentConst.GetIntentTag);

//                            bool hasExtra = inputIntent.Call<bool>(AndroidIntentConst.HasExtrasTag, AndroidIntentConst.ArgumentExtraTag);
//                            if (!hasExtra)
//                            {
//                                UnityEngine.Debug.Log("*********************Intent has no extras******************************");
//                            }

//                            //AndroidJavaObject extras = GetExtras(intent);

//                            AndroidJavaObject extras = inputIntent.Call<AndroidJavaObject>(AndroidIntentConst.GetExtrasTag);
//                            //string arguments = string.Empty;

//                            try
//                            {
//                                string arguments = extras.Call<string>(AndroidIntentConst.GetStringTag, AndroidIntentConst.ArgumentExtraTag);
//                                argumentArray = new string[] { arguments };
//                                UnityEngine.Debug.Log(argumentArray);
//                            }
//                            catch
//                            {
//                                UnityEngine.Debug.Log("*********************FAILED TO GET ARGUMENTS******************************");
//                            }

//                            break;
//                        }
//                    case RuntimePlatform.WindowsPlayer:
//                        {
//                            argumentArray = Environment.GetCommandLineArgs();
//                            break;
//                        }
//                    default:
//                        {
//                            Application.Quit();
//                            break;
//                        }
//                }



//                //if (Application.platform == RuntimePlatform.Android)
//                //{


//                //    //string path = GetAndroidExternalStoragePath();
//                //    //isInitialized = __initializer.Initialize(arguments, path);
//                //}
//                //else if (Application.platform == RuntimePlatform.WindowsPlayer)
//                //{
//                //    //string[] 

//                //    //isInitialized = __initializer.Initialize(argumentArray);
//                //}
//                //else
//                //{

//                //}


//                try
//                {
//                    (isInitialized, statementOutputArray) = Initializer.Initialize(argumentArray, ExpectedOperatingSystem.Android).Result;
//                    Android_SendBroadcast(statementOutputArray, AndroidIntentConst.StatementCreation);
//                    //switch (Application.platform)
//                    //{
//                    //    case RuntimePlatform.WindowsPlayer:
//                    //        {
//                    //            (isInitialized, statementOutputArray) = Initializer.Initialize(argumentArray, ExpectedOperatingSystem.WindowsPC).Result;
//                    //            break;
//                    //        }
//                    //    case RuntimePlatform.Android:
//                    //        {
//                    //            (isInitialized, statementOutputArray) = Initializer.Initialize(argumentArray, ExpectedOperatingSystem.Android).Result;
//                    //            Android_SendBroadcast(statementOutputArray, AndroidIntentConst.StatementCreation);

//                    //            break;
//                    //        }
//                    //    case RuntimePlatform.IPhonePlayer:
//                    //        {
//                    //            (isInitialized, statementOutputArray) = Initializer.Initialize(argumentArray, ExpectedOperatingSystem.iOSvariant).Result;
//                    //            break;
//                    //        }
//                    //        //case RuntimePlatform.:
//                    //        //    {
//                    //        //        break;
//                    //        //    }
//                    //        //case RuntimePlatform.Android:
//                    //        //    {
//                    //        //        break;
//                    //        //    }
//                    //}
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Error initalizing in Unity: " + ex.Message);
//                    throw;
//                }


//                #endregion

//                #region SystemInfo.deviceType tests
//                //if (SystemInfo.deviceType != DeviceType.Desktop)
//                //{
//                //    AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//                //    AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
//                //    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

//                //    bool hasExtra = intent.Call<bool>("hasExtra", "arguments");
//                //    if (!hasExtra)
//                //    {
//                //        UnityEngine.Debug.Log("*********************Intent has not extras******************************");
//                //    }

//                //    //AndroidJavaObject extras = GetExtras(intent);

//                //    AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras");
//                //    string arguments = string.Empty;

//                //    try
//                //    {
//                //        arguments = extras.Call<string>("getString", "arguments");
//                //        UnityEngine.Debug.Log(arguments);
//                //    }
//                //    catch
//                //    {
//                //        UnityEngine.Debug.Log("*********************FAILED TO GET ARGUMENTS******************************");
//                //    }

//                //    if (string.IsNullOrEmpty(arguments))
//                //    {

//                //    }

//                //    string path = GetAndroidExternalStoragePath();
//                //    isInitialized = __initializer.Initialize(arguments, path);
//                //}
//                //else
//                //{
//                //    string[] arguments = Environment.GetCommandLineArgs();
//                //    isInitialized = __initializer.Initialize(arguments);
//                //}
//                #endregion

//                string startpath = Application.persistentDataPath;

//                //bool isInitialized = __initializer.Initialize(arguments, SystemInfo.deviceType);                
//                UnityEngine.Debug.Log("isInitalized:" + isInitialized.ToString());
//                if (!isInitialized)
//                {
//                    UnityEngine.Debug.LogError("has not been initalized: " + isInitialized.ToString());
//                    //end simulation-refuse to start
//#if (!UNITY_EDITOR)
//                    Application.Quit();
//#endif
//                }

//                new WaitForSeconds(1f);
//                #endregion



//                #region set up scores
//                try
//                {
//                    Febris.CsharpSimulationLibraryNetStandard.Statement.StatementHandler.UpdateStatement(XAPIProperties.Result, ResultOptions.ScoreMin, __minScore);
//                    Febris.CsharpSimulationLibraryNetStandard.Statement.StatementHandler.UpdateStatement(XAPIProperties.Result, ResultOptions.ScoreMax, __maxScore);

//                    switch (Application.platform)
//                    {
//                        case RuntimePlatform.WindowsPlayer:
//                            {
//                                ///Nothing is need here for updates because windowsPlayer uses the file system.                                 
//                                break;
//                            }
//                        case RuntimePlatform.Android:
//                            {
//                                ///Can run both at the same time because the Statement is being tracked independently of the broadcasts                               
//                                ///Build the Intent and send it.
//                                //(statementUpdateArray) = StatementHandler.UpdateStatement(argumentArray).Result;
//                                //Android_SendBroadcast(statementUpdateArray, AndroidIntentConst.StatementUpdate);// ("sendBroadcast", intentObject);
//                                (isInitialized, statementOutputArray) = StatementHandler.GetSendableUpdate().Result;
//                                Android_SendBroadcast(statementOutputArray, AndroidIntentConst.StatementUpdate);
//                                break;
//                            }
//                        case RuntimePlatform.IPhonePlayer:
//                            {
//                                (isInitialized, statementOutputArray) = StatementHandler.GetSendableUpdate().Result;
//                                break;
//                            }
//                            //case RuntimePlatform.:
//                            //    {
//                            //        break;
//                            //    }
//                            //case RuntimePlatform.Android:
//                            //    {
//                            //        break;
//                            //    }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Error updating statement in Unity: " + ex.Message);
//                    throw;
//                }

//                //Febris.CsharpSimulationLibraryNetStandard.Statement.StatementHandler.UpdateStatement(XAPIProperties.Result, ResultOptions.ScoreMin, __minScore);
//                //Febris.CsharpSimulationLibraryNetStandard.Statement.StatementHandler.UpdateStatement(XAPIProperties.Result, ResultOptions.ScoreMax, __maxScore);

//                #endregion
//            }
//            catch (Exception ex)
//            {
//                UnityEngine.Debug.Log(ex.Message);
//                UnityEngine.Debug.Log(ex.Data);
//#if (!UNITY_EDITOR)
//                Application.Quit();
//#endif
//            }
//        }

//        #region Android Operations

//        private async static Task Android_SendBroadcast(string[,] intputArray, string IntentActionString)
//        {
//            // Create an Android Java class for Intent
//            AndroidJavaClass intentClass = new AndroidJavaClass(AndroidIntentConst.IntentObject);// ("android.content.Intent");

//            // Create an intent object with your custom action
//            AndroidJavaObject intentObject = new AndroidJavaObject(AndroidIntentConst.IntentObject);
//            intentObject.Call<AndroidJavaObject>(AndroidIntentConst.IntentSetAction, IntentActionString);// AndroidIntentConst.StatementCreation);// ("setAction", "com.example.CUSTOM_ACTION");

//            // Optionally, add extra data to the intent
//            //intentObject.Call<AndroidJavaObject>("putExtra", "extraKey", "extraValue");
//            ///This is really the intent Array
//            foreach (var intentExtra in intputArray)
//            {
//                intentObject.Call<AndroidJavaObject>(AndroidIntentConst.PutExtraTag, intentExtra[0], intentExtra[1]);
//            }

//            // Get the Unity player activity
//            AndroidJavaClass unityPlayer = new AndroidJavaClass(AndroidIntentConst.IntentClass); //("com.unity3d.player.UnityPlayer");
//            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>(AndroidIntentConst.GetCurrentActivityTag);// ("currentActivity");

//            // Send the broadcast
//            currentActivity.Call(AndroidIntentConst.SendBroadcast, intentObject);
//        }
//        #endregion
//        #endregion

//    }
//}
