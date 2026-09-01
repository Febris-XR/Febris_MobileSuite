// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.MobileCompanionV3.DataLogic;
using Febris.MobileCompanionV3.Resources;
using Febris.MobileCompanionV3.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Febris.MobileCompanionV3.MVVM.ViewModel
{
    public class ConfigurationViewModel : BaseViewModel
    {

        public ConfigurationViewModel()
        {
            ForgetPairedServer = new RelayCommand(x => ForgetPairedServerOperation());
            RestartApplication = new RelayCommand(x => RestartApplicationOperation());
            ExitApplication = new RelayCommand(x => ExitApplicationOperation());
            DeleteOldStatementFiles = new RelayCommand(x => DeleteOldStatementFileOperation());
            DeleteOldVideoFiles = new RelayCommand(x => DeleteOldVideoFileOperation());
        }


        private P2pGroup _p2pGroup;

        public P2pGroup P2pGroup
        {
            get { return _p2pGroup; }
            set { 
                _p2pGroup = value;
                OnPropertyChanged();
            }
        }




        public RelayCommand ForgetPairedServer { get; set; }

        public RelayCommand RestartApplication { get; set; }

        public RelayCommand ExitApplication { get; set; }

        public RelayCommand DeleteOldStatementFiles { get; set; }

        public RelayCommand DeleteOldVideoFiles { get; set; }



        private void DeleteOldVideoFileOperation()
        {
            try
            {
                VideoFileContext _context = new VideoFileContext();
                bool complete = _context.DeleteOldList().Result;
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Old video files deleted: "+complete.ToString();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }            
        }
        private void DeleteOldStatementFileOperation()
        {
            try
            {
                StatementLogic _context = new StatementLogic();
                bool complete = _context.DeleteOldStatements().Result;
                
                    
                //StatementFileContext _context = new StatementFileContext();
                //bool complete = _context.DeleteOldList().Result;
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Old statement files deleted: " + complete.ToString();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }
        private void RestartApplicationOperation()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Application Restarting";
                // MP2P-6 follow-up: removed GC.Collect + GC.WaitForPendingFinalizers. App.Reset()
                // tears down everything immediately; manually forcing a blocking full GC just
                // before the runtime is about to release the entire heap is pure cargo cult.
                App.Reset();

                //Intent intent = new Intent(this, typeof(MainActivity));
                //intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                //this.StartActivity(intent);
                
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }
        private void ExitApplicationOperation()
        {
            try
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Application Closing";
                // MP2P-6 follow-up: removed GC.Collect + GC.WaitForPendingFinalizers. App.QuitAppliction()
                // ends the process; forcing a blocking full GC right before the OS reclaims the entire
                // address space serves no purpose.
                App.QuitAppliction();
                //Application.Current.Quit();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }

        private void ForgetPairedServerOperation()
        {
            try
            {
                ServerDeviceContext _context = new ServerDeviceContext();
                bool complete = _context.Delete().Result;
                
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = "Paired server data has been deleted: " + complete.ToString();
            }
            catch (Exception ex)
            {
                LocalHardwareStaticDetails.StaticMainVM.HomeVM.StatusMessage = ex.Message;
                throw;
            }
        }


        private string _hardwareLicense;
        public string HardwareLicense
        {
            get { return _hardwareLicense; }
            set
            {
                _hardwareLicense = value;
                OnPropertyChanged();
            }
        }



    }

    public class P2pGroup
    {
        public string OwnerAddress { get; set; }
        public int ConnectionStatus { get; set; }
        public string OwnerDeviceName { get; set; }
        public string NetworkName { get; set; }
        public string GroupInterface { get; set; }

    }
}
