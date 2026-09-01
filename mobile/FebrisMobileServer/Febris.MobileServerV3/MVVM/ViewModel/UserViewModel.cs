// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.MobileApp.Resources;
using Febris.MobileServerV3.Utilities;
using Febris.SharedMobileLibrary.Models.Data;
using Febris.SharedMobileLibrary.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class UserViewModel : BaseViewModel
    {
        #region Added command user selection
        private object _currentSelectedUser;
        public object CurrentSelectedUser
        {
            get { return _currentSelectedUser; }
            set
            {
                _currentSelectedUser = value;
                SelectedUser = (HardwareUserViewModel)value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SelectUserCommand { get; set; }
        public UserViewModel()
        {
            CurrentSelectedUser = default;
            SelectUserCommand = new RelayCommand(x => { CurrentSelectedUser = DisplayedUser; });
            UserSearch = string.Empty;
        }
        #endregion


        private List<Cohort> _cohortList;
        public List<Cohort> CohortList
        {
            get { return _cohortList; }
            set { _cohortList = value; }
        }


        private HardwareUserViewModel _displayedUser;
        public HardwareUserViewModel DisplayedUser
        {
            get { return _displayedUser; }
            set
            {
                _displayedUser = value;
                OnPropertyChanged();
            }
        }

        private List<HardwareUserViewModel> _userList;
        public List<HardwareUserViewModel> UserList
        {
            get { return _userList; }
            set
            {
                _userList = value;
                OnPropertyChanged();
            }
        }

        private string _userSearch;
        public string UserSearch
        {
            get { return _userSearch; }
            set
            {
                _userSearch = value;
                Task.Run(()=> GenerateSearchResults());
                OnPropertyChanged();
            }
        }

        private List<HardwareUserViewModel> _searchResultList;
        public List<HardwareUserViewModel> SearchResultList
        {
            get { return _searchResultList; }
            set
            {
                _searchResultList = value;
                OnPropertyChanged();
            }
        }

        private HardwareUserViewModel _selectedUser;
        public HardwareUserViewModel SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                _selectedUser = value;        
                //LocalHardwareStaticDetails.selectedUser = value;
                OnPropertyChanged();
            }
        }

        private async Task GenerateSearchResults()
        {
            if (string.IsNullOrEmpty(UserSearch))
            {
                SearchResultList = UserList;
            }
            else
            {
                SearchResultList = UserList.Where(i => i.FirstName.ToLower().Contains(UserSearch.ToLower())
                || i.IdentificationNumber.ToLower().Contains(UserSearch.ToLower())
                || i.LastName.ToLower().Contains(UserSearch.ToLower())
                ).ToList();
            }

        }
        
    }
}
