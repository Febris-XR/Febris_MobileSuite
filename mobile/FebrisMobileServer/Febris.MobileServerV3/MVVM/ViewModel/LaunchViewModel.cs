// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class LaunchViewModel :BaseViewModel
    {
        #region Added command user selection
        private object _currentCommand;
        public object CurrentCommand
        {
            get { return _currentCommand; }
            set
            {
                _currentCommand = value;                
                OnPropertyChanged();
            }
        }

        public RelayCommand SelectUserCommand { get; set; }
        public RelayCommand SelectModuleCommand { get; set; }
        //public RelayCommand SelectRecordCommand { get; set; }

        public LaunchViewModel()
        {
            UserSelected = UserHasBeenSelected();//(LocalHardwareStaticDetails.selectedUser==default);
            ModuleSelected = ModuleHasBeenSelected();// (LocalHardwareStaticDetails.selectedModule==default);
            //CurrentCommand = new HardwareUserViewModel();
            //SelectUserCommand = new RelayCommand(x => { CurrentCommand = UserSelected; });
            //SelectModuleCommand = new RelayCommand(x => { CurrentCommand = ModuleSelected; });
            //SelectRecordCommand = new RelayCommand(x => { CurrentCommand = RecordSession; });
        }

       
        #endregion

        // ROADMAP 22: the RecordSession property that sat here is GONE with the checkbox that bound
        // to it, matching the PC launcher. Unlike PC's, this one never worked even locally: its
        // write to LocalHardwareStaticDetails.recordSession was already commented out, so the
        // property backed a checkbox that moved nothing. The static is deleted with it.
        //
        // The record decision is the node's, derived from the educator's per-cohort policy and
        // delivered as the statement's video attachment. This tier still starts no recorder.


        private bool _userSelected;
        public bool UserSelected
        {
            get { return _userSelected; }
            set { _userSelected = value; 
                OnPropertyChanged(); }
        }


        private bool _moduleSelected;
        public bool ModuleSelected
        {
            get { return _moduleSelected; }
            set { 
                _moduleSelected = value; 
                OnPropertyChanged(); 
            }
        }

        private bool _hardwareSelected;
        public bool HardwareSelected
        {
            get { return _hardwareSelected; }
            set
            {
                _hardwareSelected = value;
                OnPropertyChanged();
            }
        }

        private bool UserHasBeenSelected()
        {
            bool output = false;
            //if (LocalHardwareStaticDetails.selectedUser.ActorId != default)
            //{
            //    output = true;
            //}
            return output;
        }
        private bool ModuleHasBeenSelected()
        {
            bool output = false;
            //if (LocalHardwareStaticDetails.selectedModule.UUID != default)
            //{
            //    output = true;
            //}
            return output;
        }

        //private bool SetToRecord()
        //{
        //    bool output = false;
        //    if (LocalHardwareStaticDetails.selectedModule.UUID != default)
        //    {
        //        output = true;
        //    }
        //    return output;
        //}

        private bool _readyToLaunch;

        public bool ReadyToLaunch
        {
            get { return _readyToLaunch; }
            set { _readyToLaunch = value;
                OnPropertyChanged();
            }
        }

    }
}
