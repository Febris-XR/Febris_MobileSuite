// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class VideoViewModel : BaseViewModel
    {
        private List<string> _unsentVideoFileList;

        public List<string> UnsentVideoFileList
        {
            get { return _unsentVideoFileList; }
            set { _unsentVideoFileList = value; 
                OnPropertyChanged(); }
        }


        private List<string> _sentVideoFileList;

        public List<string> SentVideoFileList
        {
            get { return _sentVideoFileList; }
            set { _sentVideoFileList = value; 
                OnPropertyChanged(); }
        }


    }
}
