// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class ModuleFileViewModel : BaseViewModel
    {
        private List<string> _neededFileList;

        public List<string> NeededFileList
        {
            get { return _neededFileList; }
            set { 
                _neededFileList = value;
                OnPropertyChanged();
            }
        }

        private List<string> _notNeededFileList;

        public List<string> NotNeededFileList
        {
            get { return _notNeededFileList; }
            set { _notNeededFileList = value; OnPropertyChanged(); }
        }

        private List<string> _currentFileList;

        public List<string> CurrentFileList
        {
            get { return _currentFileList; }
            set { _currentFileList = value; OnPropertyChanged(); }
        }

    }
}
