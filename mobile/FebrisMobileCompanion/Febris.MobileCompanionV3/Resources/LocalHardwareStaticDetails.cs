// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.MVVM.ViewModel;
using Febris.SharedMobileLibrary.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.Resources
{
    public class LocalHardwareStaticDetails
    {
        //this static variable is needed to communicate        
        public static string testData = string.Empty;
#if (DEBUG)
        //public static string ApiUrl = "https://localhost:5001/api/";
        //public static string ApiUrl = "http://192.168.1.8:5000/api/";
#elif (STAGING)        
#else                
        //public static string prefix = "www";
#endif

        //main viewModel for multipule references
        private static MainViewModel _staticMainVM { get; set; }
        public static MainViewModel StaticMainVM
        {
            get { return _staticMainVM; }
            set { _staticMainVM = value;}
        }

        public static bool StreamVideo { get; set; }

        #region loop Frequencies
        //public static int StatusUpdateFrequency { get; internal set; } = 60000;
        //public static int FileUploadFrequency { get; internal set; } = 60000;
        //public static int ServiceListenerLoopFrequency { get; internal set; } = 30000;
        //public static int PeerDiscoveryListenerLoopFrequency { get; internal set; } = 60000;
        //public static int GroupOwnerConnectAttemptLoopFrequency { get; internal set; } = 5000;
        public const int StatusUpdateFrequency = 60000;
        public const int FileUploadFrequency = 60000;
        public const int ServiceListenerLoopFrequency = 30000;
        public const int PeerDiscoveryListenerLoopFrequency  = 60000;
        public const int GroupOwnerConnectAttemptLoopFrequency = 5000;
        #endregion

        public static bool ActionProccessingBlocker = false;

        #region Simulation run state
        /// <summary>
        /// The package name of the simulation currently running on this device, or empty.
        ///
        /// <para><b>Why this exists.</b> A statement must not be uploaded while its simulation is
        /// still running. The simulation is still writing to it: the Companion receives statements
        /// through the STATEMENT_CREATE broadcast and a run can emit several, so uploading
        /// mid-run ships a partial record and, worse, marks it Uploaded so the final version is
        /// never sent. Uploads are therefore held from the moment a module is launched until it is
        /// over or closed.</para>
        ///
        /// <para>Set by <c>ModulePackageUtility.RunModule</c>, cleared when the Companion's own
        /// activity returns to the foreground, which is what "the simulation closed" looks like
        /// from here. Statements are NOT dropped while held: they are already durable in sqlite
        /// and the upload loop simply skips them, so the hold delays sending and never loses
        /// anything.</para>
        /// </summary>
        public static string RunningSimulationPackage = string.Empty;

        /// <summary>When the current simulation was launched, used only by the safety net below.</summary>
        public static DateTime SimulationStartedUtc = DateTime.MinValue;

        /// <summary>
        /// Hard cap on the upload hold.
        ///
        /// <para><b>This is the important half.</b> A hold that can only be released by one event
        /// is a hold that strands data the first time that event does not arrive, and a simulation
        /// that crashes, is killed by the OS, or never returns focus is entirely ordinary. This
        /// codebase has produced that exact shape repeatedly: a flag with no reliable writer
        /// (issues 14, 21, 25). So the hold expires on its own and the statements go.</para>
        ///
        /// <para>Four hours is deliberately far longer than any plausible session, so expiry means
        /// something went wrong rather than "a long run", and it is logged as such.</para>
        /// </summary>
        public static readonly TimeSpan MaxSimulationHold = TimeSpan.FromHours(4);

        /// <summary>
        /// True while statement uploads should be held for a running simulation.
        ///
        /// Returns false once <see cref="MaxSimulationHold"/> has elapsed, so a simulation that
        /// never signals completion delays uploads rather than blocking them forever.
        /// </summary>
        public static bool SimulationUploadHoldActive()
        {
            if (string.IsNullOrWhiteSpace(RunningSimulationPackage))
            {
                return false;
            }

            if (SimulationStartedUtc != DateTime.MinValue
                && DateTime.UtcNow - SimulationStartedUtc > MaxSimulationHold)
            {
                Console.WriteLine("simulation hold: '" + RunningSimulationPackage + "' has been marked "
                    + "running for over " + MaxSimulationHold.TotalHours + "h, releasing the upload "
                    + "hold. The simulation almost certainly ended without the Companion noticing.");
                ClearRunningSimulation("hold expired");
                return false;
            }

            return true;
        }

        /// <summary>Records that a simulation has been launched and holds statement uploads.</summary>
        public static void SetRunningSimulation(string packageName)
        {
            RunningSimulationPackage = packageName ?? string.Empty;
            SimulationStartedUtc = DateTime.UtcNow;
            Console.WriteLine("simulation hold: '" + RunningSimulationPackage
                + "' launched, holding statement uploads until it closes");
        }

        /// <summary>Releases the hold so the upload loop can send on its next pass.</summary>
        public static void ClearRunningSimulation(string reason)
        {
            if (string.IsNullOrWhiteSpace(RunningSimulationPackage))
            {
                return;
            }

            Console.WriteLine("simulation hold: released for '" + RunningSimulationPackage
                + "' (" + reason + "), statement uploads resume");
            RunningSimulationPackage = string.Empty;
            SimulationStartedUtc = DateTime.MinValue;
        }
        #endregion

        #region Network settings                
        internal const int BodyByteBuffer = 1024;
        internal const int ExpectedHeaderLength = 4;

        public static bool RunServiceListener = true;
        public static bool RunPeerDiscovery = true;
        #endregion

        #region Local Device Data
        public static CompanionDevice _thisDevice { get; set; }
        #endregion

        
    }
}
