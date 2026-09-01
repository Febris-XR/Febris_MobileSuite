// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Febris.MobileCompanionV3.MVVM.ViewModel
{
    public class LocalFileRepositoryViewModel : BaseViewModel
    {
        private VideoViewModel _videoVM;

        public VideoViewModel VideoVM
        {
            get { return _videoVM; }
            set { _videoVM = value;
                OnPropertyChanged();
            }
        }



        private StatementViewModel _statementVM;

        public StatementViewModel StatementVM
        {
            get { return _statementVM; }
            set {
                _statementVM = value;
                OnPropertyChanged();
                }
        }


        private ModuleViewModel _moduleVM;

        public ModuleViewModel ModuleVM
        {
            get { return _moduleVM; }
            set
            {
                _moduleVM = value;
                OnPropertyChanged();
            }
        }

    }

    public class VideoViewModel : BaseViewModel
    {
        private List<string> _unsentVideoFileList;

        public List<string> UnsentVideoFileList
        {
            get { return _unsentVideoFileList; }
            set
            {
                _unsentVideoFileList = value;
                OnPropertyChanged();
            }
        }


        private List<string> _sentVideoFileList;

        public List<string> SentVideoFileList
        {
            get { return _sentVideoFileList; }
            set
            {
                _sentVideoFileList = value;
                OnPropertyChanged();
            }
        }


    }

    public class StatementViewModel : BaseViewModel
    {
        private List<string> _unsentStatementFileList;

        public List<string> UnsentStatementFileList
        {
            get { return _unsentStatementFileList; }
            set { _unsentStatementFileList = value;
                OnPropertyChanged();
            }
        }


        private List<string> _sentStatementFileList;

        public List<string> SentStatementFileList
        {
            get { return _sentStatementFileList; }
            set { _sentStatementFileList = value;
                OnPropertyChanged();
            }
        }
    }

    public class ModuleViewModel : BaseViewModel
    {
        private List<string> _fileList;

        public List<string> FileList
        {
            get { return _fileList; }
            set { 
                _fileList = value;
                OnPropertyChanged();
            }
        }


        private List<string> _zipList;

        public List<string> ZipList
        {
            get { return _zipList; }
            set
            {
                _zipList = value;
                OnPropertyChanged();
            }
        }

    }
}
