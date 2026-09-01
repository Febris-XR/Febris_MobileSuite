// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class CompanionAppViewModel : BaseViewModel
    {
        public CompanionAppViewModel()
        {
            LocalSoftwarePackage = new LocalSoftwarePackage();
            ExistsInSystem = false;
        }


        private LocalSoftwarePackage _localSoftwarePackage;
        public LocalSoftwarePackage LocalSoftwarePackage
        {
            get { return _localSoftwarePackage; }
            set
            {
                _localSoftwarePackage = value;                
                OnPropertyChanged();
            }
        }




        private bool _existsInSystem;
        public bool ExistsInSystem
        {
            get { return _existsInSystem; }
            set
            {
                _existsInSystem = value;                
                OnPropertyChanged();
            }
        }
    }
}
