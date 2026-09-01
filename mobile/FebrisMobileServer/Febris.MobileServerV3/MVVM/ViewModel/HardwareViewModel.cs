// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System.Collections.ObjectModel;
using Febris.MobileServerV3.MVVM.View;
using Febris.MobileServerV3.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Febris.MobileServerV3.MVVM.ViewModel
{
    public class HardwareViewModel : BaseViewModel
    {
        //public RelayCommand DetailsViewCommand { get; set; }
        //public RelayCommand VideoStreamViewCommand { get; set; }
        public RelayCommand SelectDeviceCommand { get; set; }

        public HardwareViewModel()
        {
            //DetailsViewCommand = new RelayCommand(x => { OpenDetailPage(); });
            //VideoStreamViewCommand = new RelayCommand(x => { OpenVideoStreamPage(); });
            SelectDeviceCommand = new RelayCommand(x => { SelectedItem = DisplayedItem; });
        }

        //private void OpenVideoStreamPage()
        //{
        //    throw new NotImplementedException();
        //}

        //private void OpenDetailPage()
        //{
        //    try
        //    {
        //        //get device from binding
        //        //var referenceObject = (Button)sender;
        //        //CompanionDeviceViewModel selectedItem = referenceObject.CommandParameter as CompanionDeviceViewModel;
        //        //use device to start up device modal
        //        Navigation.PushAsync(new DeviceInfoModal(DisplayedItem));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("DeviceInfo_Click error: " + ex.Message);
        //    }

        //    throw new NotImplementedException();
        //}



        private CompanionDeviceViewModel _displayedItem;
        public CompanionDeviceViewModel DisplayedItem
        {
            get { return _displayedItem; }
            set
            {
                _displayedItem = value;
                //Task.Run(() => GenerateSearchResults());
                OnPropertyChanged();
            }
        }


        private CompanionDeviceViewModel _selectedItem;
        public CompanionDeviceViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                //LocalHardwareStaticDetails.selectedModule = value;
                OnPropertyChanged();
            }
        }


        private List<CompanionDeviceViewModel> _itemList;
        public List<CompanionDeviceViewModel> ItemList
        {
            get { return _itemList; }
            set
            {
                _itemList = value;
                //GenerateSearchResults();
                OnPropertyChanged();
            }
        }


        private List<CompanionDeviceViewModel> _itemResultList;
        public List<CompanionDeviceViewModel> ItemResultList
        {
            get { 
                
                return _itemResultList; }
            set
            {  
                _itemResultList = value;
                OnPropertyChanged();
            }
        }


        private string _itemSearch;
        public string ItemSearch
        {
            get { return _itemSearch; }
            set
            {
                _itemSearch = value;
                Task.Run(() => GenerateSearchResults());
                OnPropertyChanged();
            }
        }

        
        private async Task GenerateSearchResults()
        {
            if (string.IsNullOrEmpty(ItemSearch))
            {
                ItemResultList = ItemList;
            }
            else
            {
                ItemResultList = ItemList.Where(i => i.Title.ToLower().Contains(ItemSearch.ToLower())
                ).ToList();
            }

        }

        private string _pairingStatus;

        public string PairingStatus
        {
            get { return _pairingStatus; }
            set { 
                _pairingStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Peers with a live socket that this Server does NOT yet know: the devices a human can
        /// pair with right now.
        ///
        /// This list, not <see cref="ItemList"/>, is what numeric-comparison pairing works from.
        /// ItemList is the legacy record of devices onboarded through the Bluetooth-name flow, so
        /// driving pairing off it made the new ceremony an accessory to the mechanism it exists to
        /// replace. A Companion gets a socket purely by joining the group, so it can be paired
        /// before this Server has any record of it at all, and confirming the pairing is what
        /// CREATES that record.
        /// </summary>
        public ObservableCollection<UnpairedPeerViewModel> UnpairedPeers { get; }
            = new ObservableCollection<UnpairedPeerViewModel>();

        /// <summary>True when there is at least one pairable peer.</summary>
        public bool HasUnpairedPeers { get { return UnpairedPeers.Count > 0; } }

        /// <summary>Inverse of <see cref="HasUnpairedPeers"/>, so the modal can explain an empty
        /// list rather than showing a blank page, which reads as a broken screen.</summary>
        public bool HasNoUnpairedPeers { get { return UnpairedPeers.Count == 0; } }

        /// <summary>
        /// Recompute the pairable list: every live socket whose address does not belong to a
        /// device we already know.
        ///
        /// Matching on ADDRESS rather than on any identifier the peer asserts is deliberate. At
        /// this point the peer has not authenticated and its header fields are self-asserted, so
        /// the only trustworthy fact is which socket the bytes arrived on.
        /// </summary>
        public void RefreshUnpairedPeers()
        {
            try
            {
                var server = DependencyService.Get<P2pCommunication.WiFi.IWiFiP2pServer>();
                if (server == null) return;

                // "Known" means HAS A PSK, not merely "we have an address for it".
                //
                // This previously filtered on WiFiIPAddress alone, which UpdateIPAddress sets for
                // ANY resolved peer. A device therefore became "known" through ordinary traffic
                // and vanished from the pairable list before it had ever been paired, which is
                // the opposite of what this list is for.
                var store = Febris.SharedMobileLibrary.P2pNetworking.Crypto.P2pPairingCoordinator.Store;
                var known = new HashSet<string>(
                    ItemList?.Where(i => !string.IsNullOrWhiteSpace(i.WiFiIPAddress)
                                      && i.CompanionDevice != null
                                      && !string.IsNullOrWhiteSpace(i.CompanionDevice.UniqueIdentifier)
                                      && store != null
                                      && store.TryGetSecret(i.CompanionDevice.UniqueIdentifier, out _))
                             .Select(i => i.WiFiIPAddress.Replace("/", string.Empty))
                    ?? Enumerable.Empty<string>());

                var pairable = server.ConnectedPeerAddresses()
                    .Select(a => a?.Replace("/", string.Empty))
                    .Where(a => !string.IsNullOrWhiteSpace(a) && !known.Contains(a))
                    .Distinct()
                    .ToList();

                UnpairedPeers.Clear();
                foreach (string address in pairable)
                {
                    UnpairedPeers.Add(new UnpairedPeerViewModel { IPAddress = address });
                }
                OnPropertyChanged(nameof(HasUnpairedPeers));
                OnPropertyChanged(nameof(HasNoUnpairedPeers));
            }
            catch (Exception ex)
            {
                Console.WriteLine("RefreshUnpairedPeers failed: " + ex.Message);
            }
        }

        private bool _usbButtonVisability;// = true;

        public bool UsbButtonVisability
        {
            get { return _usbButtonVisability; }
            set { 
                _usbButtonVisability = value; 
                OnPropertyChanged(); 
            }
        }

    }
}
