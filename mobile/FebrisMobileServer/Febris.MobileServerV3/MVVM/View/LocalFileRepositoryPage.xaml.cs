// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileServerV3.MVVM.ViewModel;
using Febris.MobileServerV3.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LocalFileRepositoryPage : ContentPage
    {
        public LocalFileRepositoryPage()
        {
            InitializeComponent();
            BindingContext = LocalHardwareStaticDetails.StaticMainVM;            
        }

        protected async override void OnAppearing()
        {
            LocalHardwareStaticDetails.StaticMainVM.VideoVM = await VideoVMSetup();
            LocalHardwareStaticDetails.StaticMainVM.StatementVM = await StatementVMSetup();
            LocalHardwareStaticDetails.StaticMainVM.ModuleFileVM = await ModuleFileVMSetup();
        }

        private async Task<ModuleFileViewModel> ModuleFileVMSetup()
        {
            FileManager _fileManager = new FileManager(null, null);
            List<string> currentFileList = new List<string>();
            List<string> neededFileList = new List<string>();
            List<string> notNeededFileList = new List<string>();


            List<Guid> ListFromResponse = LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.Select(i=>i.UUID).ToList();
            List<string> ModuleFilesOnSystem = await 
                _fileManager.GetDirectoryContentNames(FileSystem.ZippedModulePath);

                        
            foreach(var i in ListFromResponse)
            {
                if (ModuleFilesOnSystem.Where(j => j.Contains(i.ToString())).Any())
                {
                    currentFileList.Add(i.ToString());
                }
                else
                {
                    neededFileList.Add(i.ToString());
                }
            }




            if (currentFileList.Count() != ModuleFilesOnSystem.Count())
            {
                foreach (var i in ModuleFilesOnSystem)
                {
                    // k is the entry with ".zip" stripped, which is the shape the other two
                    // lists use (they are built from Module.UUID above). It was computed here
                    // already but only used in the predicate, and the RAW entry was added to
                    // the list, so this one list rendered "<UUID>.zip" while its two siblings
                    // rendered "<UUID>". RemoveModule_Click appends ".zip" unconditionally, so
                    // deleting one of these rows targeted "<UUID>.zip.zip" and silently no-oped.
                    var k = Path.GetFileNameWithoutExtension(i);

                    // "Not needed" means this archive on disk is not one of the modules we know
                    // about. The previous predicate was currentFileList.Where(j => j != i).Any(),
                    // which compares a bare UUID against a "<UUID>.zip" entry and is therefore
                    // true whenever the list is non-empty, so it barely filtered. Same
                    // Any(!=)-instead-of-All(!=) shape as the client filter in issue 16.
                    if (!currentFileList.Any(j => j == k) && !neededFileList.Any(j => j == k))
                    {
                        notNeededFileList.Add(k);
                    }
                }
            }




            ModuleFileViewModel _vm = new ModuleFileViewModel()
            {
                CurrentFileList = currentFileList,
                NeededFileList = neededFileList,
                NotNeededFileList = notNeededFileList
            };
            return _vm;
        }

        private async Task<VideoViewModel> VideoVMSetup()
        {
            FileManager _fileManager = new FileManager(null, null);
            VideoViewModel _vm = new VideoViewModel()
            {
                UnsentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.RecordingsFilePath),
                SentVideoFileList = await _fileManager.GetDirectoryContentNames(FileSystem.zipFolderPath)
            };
            return _vm;
        }

    private async Task<StatementViewModel> StatementVMSetup()
        {
            FileManager _fileManager = new FileManager(null, null);
            StatementViewModel _vm = new StatementViewModel()
            {
                UnsentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.StatementPath),
                SentStatementFileList = await _fileManager.GetDirectoryContentNames(FileSystem.OldStatementPath)
            };
            return _vm;
        }


        async void RemoveModule_Click(object sender, EventArgs args)
        {
            var b = (Button)sender;
            string selected = b.CommandParameter as string;
            try
            {
                //RequestHelper.RemoveLocalModuleRequest(LocalHardwareStaticDetails.StaticMainVM.ModuleVM.ModuleList.DisplayedItem, selected);
                string path = Path.Combine(FileSystem.ZippedModulePath, selected+".zip");
                bool complete = await FileManager.DeleteFile(path);
                Console.WriteLine("File Removed: "+ complete);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}