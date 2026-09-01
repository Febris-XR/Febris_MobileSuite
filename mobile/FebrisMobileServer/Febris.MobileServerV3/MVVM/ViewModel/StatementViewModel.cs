// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class StatementViewModel : BaseViewModel
    {
        private List<string> _unsentStatementFileList;

        public List<string> UnsentStatementFileList
        {
            get { return _unsentStatementFileList; }
            set { _unsentStatementFileList = value; OnPropertyChanged(); }
        }


        private List<string> _sentStatementFileList;

        public List<string> SentStatementFileList
        {
            get { return _sentStatementFileList; }
            set { _sentStatementFileList = value; OnPropertyChanged(); }
        }
    }
}
