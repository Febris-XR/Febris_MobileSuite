// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.FileSystem;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatementFiles : ContentPage
    {
        //FileManager _fileManager;
        //private FileListViewModel _fileListViewModel { get; set; }
        //public FileListViewModel fileListViewModel { get { return _fileListViewModel; } }
        public StatementFiles()
        {
            InitializeComponent();
            //_fileManager = new FileManager();
            //PopulatePage();
        }
        //protected override void OnAppearing()
        //{                        
        //    //_deviceList = new ObservableCollection<CompanionDeviceViewModel>(LocalHardwareStaticDetails.PairedDeviceViewModelList);
        //    //_deviceList = new ObservableCollection<CompanionDevice>(LocalHardwareStaticDetails.PairedDeviceList);
        //    //DeviceList.ItemsSource = deviceList;
                       

        //    try { UnsentStatementFileList.ItemsSource = fileListViewModel.UnsentStatementFileList; } catch { }
        //    try { SentStatementFileList.ItemsSource = fileListViewModel.SentStatementFileList; } catch { }
        //    try { UnsentVideoFileList.ItemsSource = fileListViewModel.UnsentVideoFileList; } catch { }
        //    try { SentVideoFileList.ItemsSource = fileListViewModel.SentVideoFileList; } catch { }


        //}
        //private void PopulatePage()
        //{
        //    List<string> _unsentStatementFileList = new List<string>();
        //    List<string> _sentStatementFileList = new List<string>();
        //    List<string> _unsentVideoFileList = new List<string>();
        //    List<string> _sentVideoFileList = new List<string>();

        //    _unsentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
        //    _sentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath);
        //    _unsentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath);
        //    _sentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);


        //    _fileListViewModel = new FileListViewModel() 
        //    {
        //        UnsentStatementFileList = _unsentStatementFileList,
        //        SentStatementFileList = _sentStatementFileList,
        //        UnsentVideoFileList = _unsentVideoFileList,
        //        SentVideoFileList = _sentVideoFileList
        //    };

        //    //try { UnsentStatementFileList.ItemsSource = _unsentStatementFileList; } catch { }
        //    //try { SentStatementFileList.ItemsSource = _sentStatementFileList; } catch { }
        //    //try { UnsentVideoFileList.ItemsSource = _unsentVideoFileList; } catch { }
        //    //try { SentVideoFileList.ItemsSource = _sentVideoFileList; } catch { }
        //    //try { UnsentStatementFileList.ItemsSource = _filesListViewModel.UnsentStatementFileList; } catch { }
        //    //try { SentStatementFileList.ItemsSource = _filesListViewModel.SentStatementFileList; } catch { }
        //    //try { UnsentVideoFileList.ItemsSource = _filesListViewModel.UnsentVideoFileList; } catch { }
        //    //try { SentVideoFileList.ItemsSource = _filesListViewModel.SentVideoFileList; } catch { }
        //}
    }
}