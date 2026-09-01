// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class HomeViewModel : BaseViewModel
    {
        private string _statusMessage;

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { 
                _statusMessage = value;
                OnPropertyChanged();

            }
        }

    }
}
