// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class ModuleViewModel : BaseViewModel
    {
        #region Added command user selection
        private object _currentSelectedModule;

        public object CurrentSelectedModule
        {
            get { return _currentSelectedModule; }
            set
            {
                _currentSelectedModule = value;
                SelectedModule = (Module)value;
                OnPropertyChanged();
            }
        }
        public RelayCommand SelectModuleCommand { get; set; }
        public ModuleViewModel()
        {
            CurrentSelectedModule = default;
            SelectModuleCommand = new RelayCommand(x => { CurrentSelectedModule = DisplayedModule; });
        }
        #endregion
        //private Module _selectedModule;
        //public Module SelectedModule
        //{
        //    get { return _selectedModule; }
        //    set
        //    {
        //        _selectedModule = value;
        //        //OnPropertyChanged();
        //    }
        //}

        private Module _displayedModule;
        public Module DisplayedModule
        {
            get { return _displayedModule; }
            set
            {
                _displayedModule = value;
                OnPropertyChanged();
            }
        }
        private Module _selectedModule;
        public Module SelectedModule
        {
            get { return _selectedModule; }
            set
            {
                _selectedModule = value;
                //LocalHardwareStaticDetails.selectedModule = value;
                OnPropertyChanged();
            }
        }

        private List<Module> _moduleList;
        public List<Module> ModuleList
        {
            get { return _moduleList; }
            set
            {
                _moduleList = value;
              OnPropertyChanged();
            }
        }
        private List<Module> _searchResultList;
        public List<Module> SearchResultList
        {
            get { return _searchResultList; }
            set
            {
                _searchResultList = value;
                OnPropertyChanged();
            }
        }

        private string _moduleSearch;
        public string ModuleSearch
        {
            get { return _moduleSearch; }
            set
            {
                _moduleSearch = value;
                Task.Run(() => GenerateSearchResults());
                OnPropertyChanged();
            }
        }

        private async Task GenerateSearchResults()
        {
            if (string.IsNullOrEmpty(ModuleSearch))
            {
                SearchResultList = ModuleList;
            }
            else
            {
                SearchResultList = ModuleList.Where(i => i.Name.ToLower().Contains(ModuleSearch.ToLower())               
                ).ToList();
            }

        }
    }
}
