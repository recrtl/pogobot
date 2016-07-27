using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PoGoBot
{
    class Bot : ViewModelBase
    {
        private Client _client;
        private Dictionary<string, Pokemon> _pokemons { get; } = new Dictionary<string, Pokemon>();
        private Dictionary<string, Pokestop> _pokestops { get; } = new Dictionary<string, Pokestop>();
        private Dictionary<string, Item> _items { get; } = new Dictionary<string, Item>();
        private bool _needInventoryUpdate = true;
        private bool _needMapUpdate = true;

        private Bot()
        {
            this.ConnectCommand = new RelayCommand(ConnectCommand_Execute, ConnectCommand_CanExecute);

            foreach (var itemType in Enum.GetValues(typeof(ItemId)).Cast<ItemId>())
            {
                var itemName = itemType.ToString();
                _items.Add(itemName, new Item(itemName));
            }

            this.Items = new ObservableCollection<Item>(_items.Values);
        }

        public static Bot Instance { get; } = new Bot();

        public string Username
        {
            get { return Properties.Settings.Default.Username; }
            set
            {
                Properties.Settings.Default.Username = value;
                Properties.Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public string Password
        {
            get { return Properties.Settings.Default.Password; }
            set
            {
                Properties.Settings.Default.Password = value;
                Properties.Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public string ProxyUsername
        {
            get { return Properties.Settings.Default.ProxyUsername; }
            set
            {
                Properties.Settings.Default.ProxyUsername = value;
                Properties.Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public string ProxyPassword
        {
            get { return Properties.Settings.Default.ProxyPassword; }
            set
            {
                Properties.Settings.Default.ProxyPassword = value;
                Properties.Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public string ProxyUrl
        {
            get { return Properties.Settings.Default.ProxyUrl; }
            set
            {
                Properties.Settings.Default.ProxyUrl = value;
                Properties.Settings.Default.Save();
                this.RaisePropertyChanged();
            }
        }

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();
        public ObservableCollection<Pokemon> Pokemons { get; } = new ObservableCollection<Pokemon>();
        public ObservableCollection<Pokestop> Pokestops { get; } = new ObservableCollection<Pokestop>();

        public RelayCommand ConnectCommand { get; }

        private bool ConnectCommand_CanExecute()
        {
            return true;
        }

        private async void ConnectCommand_Execute()
        {
            if (!string.IsNullOrWhiteSpace(this.ProxyUrl))
            {
                var credentials = string.IsNullOrWhiteSpace(this.ProxyUsername) ? null : new NetworkCredential(this.ProxyUsername, this.ProxyPassword);
                WebRequest.DefaultWebProxy = new WebProxy(this.ProxyUrl, true, new string[0], credentials);
            }

            var settings = new ConnectionSettings()
            {
                AuthType = PokemonGo.RocketAPI.Enums.AuthType.Ptc,
                PtcUsername = this.Username,
                PtcPassword = this.Password,
                DefaultLatitude = 48.822262,
                DefaultLongitude = 2.339810,
                DefaultAltitude = 35,
            };

            _client = new Client(settings);
            try
            {
                await _client.Login.DoPtcLogin();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Could not connect to login server:{e.GetBaseException().Message}");
                return;
            }

            await Task.Run(async () => await Loop());
        }

        private async Task Loop()
        {
            while (_client != null)
            {
                await this.Update();
                await Task.Delay(10000);
            }
        }

        private async Task Update()
        {
            if (_needInventoryUpdate)
                await UpdateInventory();
            if (_needMapUpdate)
                await UpdateMap();

            foreach (var pokestop in this._pokestops.Values)
            {
                var data = pokestop.Data;
                pokestop.PlayerDistance = this.GetDistanceToPlayer(data.Latitude, data.Longitude); // not very costly
                if (pokestop.PlayerDistance > 50)
                    continue;

                if (pokestop.Data.Type == FortType.Checkpoint)
                {
                    if (DateTime.Now - pokestop.LastSpin < TimeSpan.FromMinutes(5))
                        continue;

                    var details = await _client.Fort.GetFort(data.Id, data.Latitude, data.Longitude);
                    pokestop.Name = details.Name;

                    var result = await _client.Fort.SearchFort(data.Id, data.Latitude, data.Longitude);

                    switch (result.Result)
                    {
                        case FortSearchResponse.Types.Result.Success:
                            pokestop.LastSpin = DateTime.Now;
                            _needInventoryUpdate = true;
                            break;

                        case FortSearchResponse.Types.Result.OutOfRange:
                        Debug.WriteLine($"{pokestop.PlayerDistance}m is too far for pokestops");
                            break;

                        case FortSearchResponse.Types.Result.NoResultSet:
                        case FortSearchResponse.Types.Result.InCooldownPeriod:
                        case FortSearchResponse.Types.Result.InventoryFull:
                            break;
                    }
                }
            }
        }

        private async Task UpdateMap()
        {
            GetMapObjectsResponse mapObjects;
            try { mapObjects = await _client.Map.GetMapObjects(); }
            catch { return; }
            _needMapUpdate = false;

            foreach (var mapCell in mapObjects.MapCells)
            {
                foreach (var fort in mapCell.Forts)
                {
                    if (_pokestops.ContainsKey(fort.Id))
                        continue;

                    var pokestop = new Pokestop(fort);
                    _pokestops[fort.Id] = pokestop;
                    DispatcherHelper.CheckBeginInvokeOnUI(() => this.Pokestops.Add(pokestop));
                }
            }
        }

        private async Task UpdateInventory()
        {
            GetInventoryResponse inventory;
            try { inventory = await _client.Inventory.GetInventory(); }
            catch { return; }
            _needInventoryUpdate = false;

            foreach (var item in inventory.InventoryDelta.InventoryItems)
            {
                var data = item.InventoryItemData;
                if (data.Item != null)
                {
                    var id = item.InventoryItemData.Item.ItemId;
                    _items[id.ToString()].Count = item.InventoryItemData.Item.Count;
                }

                if (data.PokemonData != null && !data.PokemonData.IsEgg)
                {
                    var id = data.PokemonData.Id.ToString();
                    if (_pokemons.ContainsKey(id))
                    {
                        _pokemons[id].Data = data.PokemonData;
                    }
                    else
                    {
                        var pokemon = new Pokemon(data.PokemonData);
                        _pokemons[id] = pokemon;
                        DispatcherHelper.CheckBeginInvokeOnUI(() => this.Pokemons.Add(pokemon));
                    }
                }
            }
        }

        private double GetDistanceToPlayer(double latitude, double longitude)
        {
            var coordinates = new GeoCoordinate(latitude, longitude);
            var playerCoordinates = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            return coordinates.GetDistanceTo(playerCoordinates);
        }
    }
}
