// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models.Data;
using System.Collections.Generic;


namespace Febris.MobileServerV3.MVVM.ViewModel
{

    public class MessageBoardViewModel : BaseViewModel
    {

        private List<MessageBoard> _localMessageBoard;
        public List<MessageBoard> LocalMessageBoard
        {
            get { return _localMessageBoard; }
            set
            {
                _localMessageBoard = value;
                OnPropertyChanged();
            }
        }

     

    }
}
