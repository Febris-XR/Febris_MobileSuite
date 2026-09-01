// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.APIInteractions;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Utilites;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Febris.MobileServerV3.BusinessLogic
{
    class CheckTimer
    {
        private static Timer _timer;// = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };//;        
        private readonly ILogger _log;
        //private readonly Operations.SimulationReflector _simulationReflector;
        //private readonly PCDataProtection _dataProtection;
        //private readonly StatementFolderChecker _statementFolderChecker;
        private readonly ModuleLogic _moduleContext;
        private readonly StatementLogic _statementContext;
        private readonly VideoLogic _videoContext;
        private readonly FileManager _fileManager;

        private static Timer _apiRequestTimer;
        private readonly InitalizationLogic _initalizationLogic;


        public CheckTimer()
        {
            _timer = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;


            //_timer.Enabled = true;            
            //_log = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));
            //_simulationReflector = new Operations.SimulationReflector(_log);
            //_dataProtection = new PCDataProtection(_log);
            //_statementFolderChecker = new StatementFolderChecker(_log);
            //_moduleFileChecker = new ModuleFileChecker(_log);
            _fileManager = new FileManager(_log, null);

            _apiRequestTimer = new Timer(LocalHardwareStaticDetails.APIDataRequestFrequency) { AutoReset = true };
            _apiRequestTimer.Elapsed += APIRequestTimerElapsed;

        }

        #region P2P
        public static async Task StartCheckingLoop()
        {
            _timer = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        public async Task TimerLoop()
        {
            _timer = new Timer(LocalHardwareStaticDetails.CheckTimeSpan) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
            while (LocalHardwareStaticDetails.TimerLoopRunning)
            {

                //TimerElapsed();
            }

        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //Operations.SimulationReflector reflector = new Operations.SimulationReflector();
                //bool simulationIsRunning = _simulationReflector.IsSimulationRunning();
                // Was PingRequest.IsConnnectedToInternet(), an ICMP ping to a hardcoded
                // google.com. The work below talks to the NODE, so the node is what has to
                // be reachable. See NodeReachability for why the old check was wrong on
                // air-gapped and ICMP-blocking networks.
                bool nodeReachable = NodeReachability.IsNodeReachable(LocalHardwareStaticDetails.ApiUrl);
                if (nodeReachable)
                {
                    Stop();
                    //To Api
                    ModuleLogic _moduleContext = new ModuleLogic();// _log);
                    bool checkModules = _moduleContext.ModuleChecker();

                    StatementLogic _statementContext = new StatementLogic();
                    var checkStatements = _statementContext.ProcessFiles();

                    VideoLogic _videoContext = new VideoLogic();
                    var checkVideos = _videoContext.ProcessFiles();

                    Start();
                }
            }
            catch (Exception ex)
            {
                // _log.Warning(ex.Message);
                try
                {
                    Start();
                }
                catch { }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
            //_timer.Enabled = false;

        }

        public static bool Start()
        {
            try
            {
                _timer.Start();
            }
            catch (Exception ex)
            {
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return true;
        }

        public static bool Stop()
        {
            try
            {
                _timer.Stop();
            }
            catch (Exception ex)
            {
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return false;
        }
        #endregion

        #region Topshelf.serilog creation
        //private static Serilog.ILogger CreateLogger()
        //{
        //    //var builder = new ConfigurationBuilder();
        //    //BuildConfig(builder);

        //    string path = FileSystem.ModuleManagerLogPath;


        //    //var logger = new LoggerConfiguration().ReadFrom
        //    //    .Configuration(builder.Build())
        //    //    .Enrich.FromLogContext()
        //    //    .WriteTo.File(Path.Combine(path, "log.json"), rollingInterval: RollingInterval.Day)
        //    //    .CreateLogger();
        //    var logger = new LoggerConfiguration()
        //        .WriteTo.File(Path.Combine(path, "log.json"), rollingInterval: RollingInterval.Day)
        //        .MinimumLevel.Debug()
        //        .CreateLogger();
        //    return logger;
        //}
        #endregion

        #region Start API check loop
        public static async Task StartApiCheckingLoop()
        {
            //LocalHardwareStaticDetails.StaticMainVM = new MainViewModel();
            InitalizationLogic.Get();
            _apiRequestTimer = new Timer(LocalHardwareStaticDetails.APIDataRequestFrequency) { AutoReset = true };
            _apiRequestTimer.Elapsed += APIRequestTimerElapsed;
            _apiRequestTimer.Start();
        }

        public async Task ApiTimerLoop()
        {
            _apiRequestTimer = new Timer(LocalHardwareStaticDetails.APIDataRequestFrequency) { AutoReset = true };
            _apiRequestTimer.Elapsed += APIRequestTimerElapsed;
            while (LocalHardwareStaticDetails.TimerLoopRunning)
            {

                //TimerElapsed();
            }

        }

        private static void APIRequestTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //Operations.SimulationReflector reflector = new Operations.SimulationReflector();
                //bool simulationIsRunning = _simulationReflector.IsSimulationRunning();
                // Was PingRequest.IsConnnectedToInternet(), an ICMP ping to a hardcoded
                // google.com. The work below talks to the NODE, so the node is what has to
                // be reachable. See NodeReachability for why the old check was wrong on
                // air-gapped and ICMP-blocking networks.
                bool nodeReachable = NodeReachability.IsNodeReachable(LocalHardwareStaticDetails.ApiUrl);
                if (nodeReachable)
                {
                    StopAPIRequest();
                    //To Api
                    //InitalizationLogic _context = new InitalizationLogic();// _log);
                    //bool checkModules = 
                    InitalizationLogic.Get();
                    StartAPIRequest();
                }
            }
            catch (Exception ex)
            {
                // _log.Warning(ex.Message);
                try
                {
                    Start();
                }
                catch { }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
            //_timer.Enabled = false;

        }

        public static bool StartAPIRequest()
        {
            try
            {
                _apiRequestTimer.Start();
            }
            catch (Exception ex)
            {
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return true;
        }

        public static bool StopAPIRequest()
        {
            try
            {
                _apiRequestTimer.Stop();
            }
            catch (Exception ex)
            {
                //LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return false;
        }

        #endregion
    }
}
