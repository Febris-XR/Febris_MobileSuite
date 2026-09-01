// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileCompanionV3.BusinessLogic;
using Febris.MobileCompanionV3.MVVM.ViewModel;
using Febris.MobileCompanionV3.Resources;
using Febris.SharedMobileLibrary.FileSystem;
using Febris.SharedMobileLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileCompanionV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LocalFileRepositoryPage : ContentPage, INotifyPropertyChanged
    {
        private ModulePackageLogic _logicContext = new ModulePackageLogic();
        private VideoLogic _mediaLogicContext = new VideoLogic();
        private StatementLogic _statementLogicContext = new StatementLogic();

        public LocalFileRepositoryPage()
        {
            InitializeComponent();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository = LocalFileSystemSetup();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository;
        }
        protected override void OnAppearing()
        {
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository =
            LocalFileSystemSetup();

        }

        private void LocalFileSystemSetup()
        {
            GetModuleFiles();
            GetStatementFiles();
            GetVideoFiles();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM = GetModuleFiles();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM = GetStatementFiles();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM = GetVideoFiles();
            //LocalFileRepositoryViewModel _vm = new LocalFileRepositoryViewModel()
            //{
            //    VideoVM = GetVideoFiles(),
            //    StatementVM = GetStatementFiles(),
            //    ModuleVM = GetModuleFiles()
            //};
            //return _vm;
        }

        private async void GetStatementFiles()
        {
            #region Post Android 11
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.UnsentStatementFileList = await _statementLogicContext.GetUnsentStatementNameIndex() ?? new List<string>();//_externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.OldStatementPath).Result ?? new List<string>();
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.SentStatementFileList = await _statementLogicContext.GetSentStatementNameIndex() ?? new List<string>();//_externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.StatementPath).Result ?? new List<string>();
            #endregion
            #region pre-Android 11
            //FileManager _fileManager = new FileManager(null, null);
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.SentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath) ?? new List<string>();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.UnsentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.StatementPath) ?? new List<string>();
            #endregion


        }

        private async void GetVideoFiles()
        {
            #region Post Android 11
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.SentVideoFileList = await _mediaLogicContext.GetUploadedMediaDirectoryIndex()??new List<string>();//.get.Get.GetDirectoryContentNames(FileSystem.zipFolderPath).Result ?? new List<string>();
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.UnsentVideoFileList = await _mediaLogicContext.GetUnuploadedMediaDirectoryIndex() ?? new List<string>();//(FileSystem.RecordingsFilePath).Result ?? new List<string>();
            #endregion
            #region pre-Android 11
            //FileManager _fileManager = new FileManager(null, null);
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.SentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath) ?? new List<string>();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.UnsentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath) ?? new List<string>();
            #endregion



        }

        private async void GetModuleFiles()
        {
            #region Post Android 11
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.ZipList = await _logicContext.GetCompressedModuleNameIndex() ?? new List<string>();// _externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.ZippedModulePath).Result ?? new List<string>();
            LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.FileList = await _logicContext.GetUncompressedModuleNameIndex() ?? new List<string>();//_externalPlatformFileSystem.GetDirectoryContentNames(FileSystem.ModulePath).Result ?? new List<string>();
            #endregion
            #region pre-Android 11
            //FileManager _fileManager = new FileManager(null, null);
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.ZipList = _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath) ?? new List<string>();
            //LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.FileList = _fileManager.GetDirectoryContentNames(FileSystem.ModulePath) ?? new List<string>();
            #endregion



        }

        //private StatementViewModel GetStatementFiles()
        //{
        //    FileManager _fileManager = new FileManager(null, null);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.SentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.StatementVM.UnsentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.StatementPath);
        //    StatementViewModel _vm = new StatementViewModel()
        //    {
        //        UnsentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.StatementPath),
        //        SentStatementFileList = _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath)
        //    };
        //    return _vm;
        //}

        //private VideoViewModel GetVideoFiles()
        //{
        //    FileManager _fileManager = new FileManager(null, null);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.SentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.VideoVM.UnsentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath);

        //    VideoViewModel _vm = new VideoViewModel()
        //    {
        //        UnsentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath),
        //        SentVideoFileList = _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath)
        //    };
        //    return _vm;
        //}

        //private ModuleViewModel GetModuleFiles()
        //{
        //    FileManager _fileManager = new FileManager(null, null);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.ZipList = _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);
        //    LocalHardwareStaticDetails.StaticMainVM.LocalFileRepository.ModuleVM.FileList = _fileManager.GetDirectoryContentNames(FileSystem.ModulePath);

        //    ModuleViewModel _vm = new ModuleViewModel()
        //    {
        //        FileList = _fileManager.GetDirectoryContentNames(FileSystem.ModulePath),//.ZippedModulePath)
        //        ZipList = _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath)
        //    };
        //    return _vm;
        //}
    }
}