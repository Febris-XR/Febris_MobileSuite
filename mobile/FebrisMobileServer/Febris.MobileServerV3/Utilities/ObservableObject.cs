// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Text;

//namespace Febris.MobileServerV3.Utilities
//{
//    public class ObservableObject : INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        bool isBusy = false;
//        public bool IsBusy
//        {
//            get { return isBusy; }
//            set { SetProperty(ref isBusy, value); }
//        }

//        string title = string.Empty;
//        public string Title
//        {
//            get { return title; }
//            set { SetProperty(ref title, value); }
//        }

//        protected bool SetProperty<T>(ref T backingStore, T value,
//            [CallerMemberName] string propertyName = "",
//            Action onChanged = null)
//        {
//            if (EqualityComparer<T>.Default.Equals(backingStore, value))
//                return false;

//            backingStore = value;
//            onChanged?.Invoke();
//            OnPropertyChange(propertyName);
//            return true;
//        }

//        #region INotifyPropertyChanged
//        public event PropertyChangedEventHandler PropertyChange;
//        protected void OnPropertyChange([CallerMemberName] string propertyName = "")
//        {
//            var changed = PropertyChanged;
//            if (changed == null)
//                return;

//            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//        #endregion
//        //protected void OnPropertyChange([CallerMemberName] string name = null)
//        //{
//        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
//        //}
//    }
//}
