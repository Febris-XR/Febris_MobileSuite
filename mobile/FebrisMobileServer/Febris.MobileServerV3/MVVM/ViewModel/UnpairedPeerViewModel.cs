// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
namespace Febris.MobileServerV3.MVVM.ViewModel
{
    /// <summary>
    /// A peer with a live socket that this Server has no record of yet.
    ///
    /// Carries ONLY the address, and that is the point. Everything else a peer could tell us
    /// about itself at this stage is self-asserted and unauthenticated, so showing a name or an
    /// identifier here would invite an operator to trust a string an attacker picked. The socket
    /// the bytes arrived on is the one fact we can stand behind, and the human confirms identity
    /// through the six-digit comparison rather than from anything on this row.
    /// </summary>
    public class UnpairedPeerViewModel : BaseViewModel
    {
        private string _ipAddress;

        public string IPAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayLabel)); }
        }

        /// <summary>What the operator sees. Deliberately says "unpaired" rather than inventing a
        /// friendly name we have no basis for.</summary>
        public string DisplayLabel { get { return "Unpaired device at " + (_ipAddress ?? "?"); } }
    }
}
