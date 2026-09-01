// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.BusinessLogic;
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Models.ViewModels;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LauncherPage : ContentPage
    {
        //private LauncherHandler _launcher = new LauncherHandler();
        private StatementLogic _statementLogic;
        public LauncherPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM;
            _statementLogic = new StatementLogic();
        }

        protected override void OnAppearing()
        {
            PopulateLauncherVM();
        }

        private void PopulateLauncherVM()
        {
            try
            {

                LaunchViewModel vm = LocalHardwareStaticDetails.StaticMainVM?.LaunchVM??default;
                if (LocalHardwareStaticDetails.StaticMainVM?.UserVM?.SelectedUser != default)
                {
                    vm.UserSelected = true;
                }
                if (LocalHardwareStaticDetails.StaticMainVM?.HardwareVM?.SelectedItem != default)
                {
                    vm.HardwareSelected = true;
                }
                if (LocalHardwareStaticDetails.StaticMainVM?.ModuleVM?.SelectedModule != default)
                {
                    vm.ModuleSelected = true;
                }

                if(vm.HardwareSelected && vm.UserSelected && vm.ModuleSelected)
                {
                    vm.ReadyToLaunch = true;
                }
                else
                {
                    vm.ReadyToLaunch = false;
                }

            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        /// <summary>
        /// Start a session on the selected Companion.
        ///
        /// <para>THIS PATH IS IMPLEMENTED AND ALWAYS WAS. The commented
        /// <c>_launcher.StartTraining(...)</c> line that used to sit here was a leftover from the
        /// predecessor app (Febris.MobileApp), where it was live. The work was ported into
        /// <see cref="StatementLogic.StatmentInitalizationRequest"/>, which mints an initialized
        /// statement from the API and then forwards it to the headset as a <c>_statement</c> frame
        /// carrying <c>StatementPreface</c>. That leftover line has been removed because reading it
        /// and stopping is how this feature got mistakenly recorded as "never built".</para>
        ///
        /// <para><b>Why this used to look broken.</b> Every failure on the path was silent. The
        /// three preconditions were unchecked, the API failure surfaced only as a Console line, and
        /// the result was discarded, so a backend outage, a missing selection and a successful
        /// launch were indistinguishable on screen. They are now all reported.</para>
        /// </summary>
        private async void LaunchModule_Click(object sender, EventArgs e)
        {
            try
            {
                // The three things the launch needs. Previously unchecked, so a null selection
                // threw deep inside the request and surfaced as an unrelated-looking message.
                var vm = LocalHardwareStaticDetails.StaticMainVM;
                if (vm?.HardwareVM?.SelectedItem == null)
                {
                    await DisplayAlert("Cannot launch", "No device is selected.", "OK");
                    return;
                }
                if (vm?.UserVM?.SelectedUser == null)
                {
                    await DisplayAlert("Cannot launch", "No user is selected.", "OK");
                    return;
                }
                if (vm?.ModuleVM?.SelectedModule == null)
                {
                    await DisplayAlert("Cannot launch", "No module is selected.", "OK");
                    return;
                }

                string deviceName = vm.HardwareVM.SelectedItem.CompanionDevice?.Name ?? "the selected device";
                Console.WriteLine("launch: requesting an initialized statement for module "
                    + vm.ModuleVM.SelectedModule.UUID + " on " + deviceName);

                // Awaited rather than .Result. The blocking call could deadlock on the UI context,
                // and it also hid which stage failed.
                StatementInitalizationResponseViewModel response =
                    await _statementLogic.StatmentInitalizationRequest();

                if (response?.Statement == null)
                {
                    Console.WriteLine("launch: the API returned no statement");
                    await DisplayAlert("Launch failed",
                        "The server did not return a session statement. Check the connection to the "
                        + "Febris API and that the selected user and module are valid.", "OK");
                    return;
                }

                Console.WriteLine("launch: statement sent to " + deviceName);
                await DisplayAlert("Launch sent",
                    "The session was sent to " + deviceName + ".", "OK");
            }
            catch (Exception ex)
            {
                // Was Console-only, which is invisible to the operator standing at the tablet.
                Console.WriteLine("launch failed: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                try
                {
                    await DisplayAlert("Launch failed", ex.Message, "OK");
                }
                catch { }
            }
        }

        
        //#region opt in to record video
        //private void Record_Checked(object sender, EventArgs e)
        //{
        //    //LocalHardwareStaticDetails.RecordingOptIn = RecordSession.IsChecked;
        //}

        
    }
}