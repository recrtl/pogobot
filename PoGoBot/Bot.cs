using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using PokemonGo.RocketAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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
            var secretFile = "secret.json";
            if (!File.Exists(secretFile))
            {
                MessageBox.Show("secret.json file not found.");
                Environment.Exit(0);
            }

            try
            {
                var jsonObject = JsonConvert.DeserializeObject(File.ReadAllText(secretFile)) as JObject;
                PokemonGo.RocketAPI.Login.GoogleLogin.ClientId = jsonObject.Property("PokemonGo.RocketAPI.Login.GoogleLogin.ClientId")?.Value.ToString();
                PokemonGo.RocketAPI.Login.GoogleLogin.ClientSecret = jsonObject.Property("PokemonGo.RocketAPI.Login.GoogleLogin.ClientSecret")?.Value.ToString();
            }
            catch
            {
                MessageBox.Show("Could not parse secret.json");
                Environment.Exit(0);
            }

            this.ConnectCommand = new RelayCommand(ConnectCommand_Execute, ConnectCommand_CanExecute);

            foreach (var itemType in Enum.GetValues(typeof(ItemId)).Cast<ItemId>())
            {
                var itemName = itemType.ToString();
                _items.Add(itemName, new Item(itemName));
            }

            this.Items = new ObservableCollection<Item>(_items.Values);

            var view = CollectionViewSource.GetDefaultView(this.Items);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Item.Name), System.ComponentModel.ListSortDirection.Ascending));

            view = CollectionViewSource.GetDefaultView(this.Pokestops);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Pokestop.PlayerDistance), System.ComponentModel.ListSortDirection.Ascending));
        }

        public static Bot Instance { get; } = new Bot();

        #region props

        public string Username
        {
            get { return Properties.Settings.Default.Username; }
            set { this.ChangeSetting(value); }
        }

        public string Password
        {
            get { return Properties.Settings.Default.Password; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyUsername
        {
            get { return Properties.Settings.Default.ProxyUsername; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyPassword
        {
            get { return Properties.Settings.Default.ProxyPassword; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyUrl
        {
            get { return Properties.Settings.Default.ProxyUrl; }
            set { this.ChangeSetting(value); }
        }

        public double Latitude
        {
            get { return Properties.Settings.Default.Latitude; }
            set { this.ChangeSetting(value); }
        }

        public double Longitude
        {
            get { return Properties.Settings.Default.Longitude; }
            set { this.ChangeSetting(value); }
        }

        public double Altitude
        {
            get { return Properties.Settings.Default.Altitude; }
            set { this.ChangeSetting(value); }
        }

        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                this.Set(ref _isConnected, value);
                this.ConnectCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();
        public ObservableCollection<Pokemon> Pokemons { get; } = new ObservableCollection<Pokemon>();
        public ObservableCollection<Pokestop> Pokestops { get; } = new ObservableCollection<Pokestop>();
        public Player Player { get; } = new Player();

        #endregion

        #region Connect command

        public RelayCommand ConnectCommand { get; }

        private bool ConnectCommand_CanExecute()
        {
            return !this.IsConnected;
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
                DefaultLatitude = this.Latitude,
                DefaultLongitude = this.Longitude,
                DefaultAltitude = this.Altitude,
            };

            if (string.IsNullOrWhiteSpace(this.Username))
            {
                settings.AuthType = PokemonGo.RocketAPI.Enums.AuthType.Google;
                settings.GoogleRefreshToken = Properties.Settings.Default.GoogleRefreshToken ?? string.Empty;
            }
            else
            {
                settings.AuthType = PokemonGo.RocketAPI.Enums.AuthType.Ptc;
                settings.PtcUsername = this.Username;
                settings.PtcPassword = this.Password;
            }

            _client = new Client(settings);
            try
            {
                switch (settings.AuthType)
                {
                    case PokemonGo.RocketAPI.Enums.AuthType.Google:
                        _client.Login.GoogleDeviceCodeEvent += (code, uri) =>
                        {
                            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(uri))
                            {
                                MessageBox.Show("Could not get a devide code for google account.");
                                return;
                            }

                            MessageBox.Show($"A webpage will be opened to log into your google account. This code was copied to your clipboard: {code}. Paste it when asked.");
                            Clipboard.SetText(code);
                            Process.Start(uri);
                        };

                        await _client.Login.DoGoogleLogin();

                        Properties.Settings.Default.GoogleRefreshToken = settings.GoogleRefreshToken;
                        Properties.Settings.Default.Save();
                        break;
                    case PokemonGo.RocketAPI.Enums.AuthType.Ptc:
                        await _client.Login.DoPtcLogin();
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Could not connect to login server:{e.GetBaseException().Message}");
                return;
            }

            this.IsConnected = true;

            await Task.Run(async () => await Loop());
        }

        #endregion

        #region Loop

        private async Task Loop()
        {
            GetPlayerResponse player = null;
            while (player == null)
                player = await TryGet(() => _client.Player.GetPlayer());
            this.Player.Name = player.PlayerData.Username;

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
                pokestop.PlayerDistance = this.GetDistanceToPlayer(data.Latitude, data.Longitude); // we do it every time but not very costly
                if (pokestop.PlayerDistance > 50)
                    continue;

                if (pokestop.Data.Type == FortType.Checkpoint)
                {
                    if (DateTime.Now - pokestop.LastSpin < TimeSpan.FromMinutes(5))
                        continue;

                    var details = await TryGet(() => _client.Fort.GetFort(data.Id, data.Latitude, data.Longitude));
                    if (details == null)
                        continue;

                    pokestop.Name = details.Name;
                    await Task.Delay(1000);

                    var search = await TryGet(() => _client.Fort.SearchFort(data.Id, data.Latitude, data.Longitude));
                    if (search == null)
                        continue;

                    await Task.Delay(1000);

                    switch (search.Result)
                    {
                        case FortSearchResponse.Types.Result.Success:
                        case FortSearchResponse.Types.Result.InCooldownPeriod:
                        case FortSearchResponse.Types.Result.InventoryFull:
                            pokestop.LastSpin = DateTime.Now;
                            _needInventoryUpdate = true;
                            break;

                        case FortSearchResponse.Types.Result.OutOfRange:
                            Debug.WriteLine($"{pokestop.PlayerDistance}m is too far for pokestops");
                            break;

                        case FortSearchResponse.Types.Result.NoResultSet:
                            break;
                    }
                }
            }

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CollectionViewSource.GetDefaultView(this.Pokestops).Refresh();
            });
        }

        private async Task UpdateMap()
        {
            var mapObjects = await TryGet(() => _client.Map.GetMapObjects());
            if (mapObjects == null)
                return;
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
            var inventory = await TryGet(() => _client.Inventory.GetInventory());
            if (inventory == null)
                return;
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

                if (data.PlayerStats != null)
                {
                    this.Player.Level = data.PlayerStats.Level;
                    this.Player.XP = data.PlayerStats.Experience;
                    this.Player.NextLevelXP = data.PlayerStats.NextLevelXp;
                    this.Player.PreviousLevelXP = data.PlayerStats.PrevLevelXp;
                }
            }
        }

        #endregion

        #region utils

        private double GetDistanceToPlayer(double latitude, double longitude)
        {
            var coordinates = new GeoCoordinate(latitude, longitude);
            var playerCoordinates = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            return coordinates.GetDistanceTo(playerCoordinates);
        }

        private async Task<TResult> TryGet<TResult>(Func<Task<TResult>> function)
        {
            try
            {
                return await function();
            }
            catch
            {
                // TODO logging
                return default(TResult);
            }
        }

        private void ChangeSetting(object value, [CallerMemberName] string settingName = null, [CallerMemberName] string propertyName = null)
        {
            Properties.Settings.Default.GetType()
                .GetProperty(settingName)
                .SetValue(Properties.Settings.Default, value);

            this.RaisePropertyChanged(propertyName);
            Properties.Settings.Default.Save();
        }

        #endregion
    }
}
