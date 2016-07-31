using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory;
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
        private Dictionary<int, Candy> _families { get; } = new Dictionary<int, Candy>();
        private Dictionary<PokemonId, PokedexItem> _pokedex { get; } = new Dictionary<PokemonId, PokedexItem>();
        private bool _needInventoryUpdate = true;
        private bool _needMapUpdate = true;
        private bool _needRefreshView = true;

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
            this.TransferCommand = new RelayCommand(TransferCommand_Execute, TransferCommand_CanExecute);
            this.EvolveCommand = new RelayCommand(EvolveCommand_Execute, EvolveCommand_CanExecute);
            this.AutoSelectCommand = new RelayCommand(AutoSelectCommand_Execute, AutoSelectCommand_CanExecute);

            InitItemsCollections();
            InitPokedex();
            InitFamilies();
            InitCollectionSorts();
        }

        public static Bot Instance { get; } = new Bot();

        #region props

        private Properties.Settings Settings
        {
            get { return Properties.Settings.Default; }
        }

        public string PtcUsername
        {
            get { return this.Settings.PtcUsername; }
            set { this.ChangeSetting(value); }
        }

        public string PtcPassword
        {
            get { return this.Settings.PtcPassword; }
            set { this.ChangeSetting(value); }
        }

        public string GoogleUsername
        {
            get { return this.Settings.GoogleUsername; }
            set { this.ChangeSetting(value); }
        }

        public string GooglePassword
        {
            get { return this.Settings.GooglePassword; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyUsername
        {
            get { return this.Settings.ProxyUsername; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyPassword
        {
            get { return this.Settings.ProxyPassword; }
            set { this.ChangeSetting(value); }
        }

        public string ProxyUrl
        {
            get { return this.Settings.ProxyUrl; }
            set { this.ChangeSetting(value); }
        }

        public double Latitude
        {
            get { return this.Settings.Latitude; }
            set { this.ChangeSetting(value); }
        }

        public double Longitude
        {
            get { return this.Settings.Longitude; }
            set { this.ChangeSetting(value); }
        }

        public double Altitude
        {
            get { return this.Settings.Altitude; }
            set { this.ChangeSetting(value); }
        }

        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                this.Set(ref _isConnected, value);
                DispatcherHelper.CheckBeginInvokeOnUI(() => this.ConnectCommand.RaiseCanExecuteChanged());
            }
        }

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();
        public ObservableCollection<Pokemon> Pokemons { get; } = new ObservableCollection<Pokemon>();
        public ObservableCollection<Pokestop> Pokestops { get; } = new ObservableCollection<Pokestop>();
        public ObservableCollection<PokedexItem> Pokedex { get; } = new ObservableCollection<PokedexItem>();
        public Player Player { get; } = new Player();

        private bool _needTransfer = false;
        private bool NeedTransfer
        {
            get { return _needTransfer; }
            set
            {
                _needTransfer = value;
                DispatcherHelper.CheckBeginInvokeOnUI(() => this.TransferCommand.RaiseCanExecuteChanged());
            }
        }

        private bool _needEvolve = false;
        private bool NeedEvolve
        {
            get { return _needEvolve; }
            set
            {
                _needEvolve = value;
                DispatcherHelper.CheckBeginInvokeOnUI(() => this.EvolveCommand.RaiseCanExecuteChanged());
            }
        }

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

            if (!string.IsNullOrWhiteSpace(this.GoogleUsername))
            {
                settings.AuthType = PokemonGo.RocketAPI.Enums.AuthType.Google;
                settings.GoogleRefreshToken = this.Settings.GoogleRefreshToken ?? string.Empty;
            }
            else
            {
                settings.AuthType = PokemonGo.RocketAPI.Enums.AuthType.Ptc;
                settings.PtcUsername = this.PtcUsername;
                settings.PtcPassword = this.PtcUsername;
            }

            _client = new Client(settings);
            try
            {
                switch (settings.AuthType)
                {
                    case PokemonGo.RocketAPI.Enums.AuthType.Google:
                        await _client.Login.DoGoogleLogin(this.GoogleUsername, this.GooglePassword);

                        this.Settings.GoogleRefreshToken = settings.GoogleRefreshToken;
                        this.Settings.Save();
                        break;
                    case PokemonGo.RocketAPI.Enums.AuthType.Ptc:
                        await _client.Login.DoPtcLogin(this.PtcUsername, this.PtcPassword);
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Could not connect to login server:{e.GetBaseException().Message}");

                // if there is an exception the refresh token might stil be good
                if (e is PokemonGo.RocketAPI.Exceptions.AccessTokenExpiredException)
                {
                    this.Settings.GoogleRefreshToken = settings.GoogleRefreshToken;
                    this.Settings.Save();
                }
                return;
            }

            this.IsConnected = true;

            await Task.Run(async () => await Loop());
        }

        #endregion

        #region Transfer & Evolve commands

        public RelayCommand TransferCommand { get; }

        private bool TransferCommand_CanExecute()
        {
            return !this.NeedTransfer;
        }

        private void TransferCommand_Execute()
        {
            var selectedPokemons = this.Pokemons.Where(x => x.MarkedForTransfer).ToArray();
            var message = $"You are about to transfer {selectedPokemons.Length} pokemons:{Environment.NewLine}";
            message += string.Join(Environment.NewLine, selectedPokemons.Select(x => $"{x.Name} - {x.CP}CP"));
            if (MessageBox.Show(message, "Transfer confirmation", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                this.NeedTransfer = true;
            }
        }

        public RelayCommand EvolveCommand { get; }

        private bool EvolveCommand_CanExecute()
        {
            return !this.NeedEvolve;
        }

        private void EvolveCommand_Execute()
        {
            var selectedPokemons = this.Pokemons.Where(x => x.MarkedForEvolution).ToArray();
            var message = $"You are about to evolve {selectedPokemons.Length} pokemons:{Environment.NewLine}";
            message += string.Join(Environment.NewLine, selectedPokemons.Select(x => $"{x.Name} - {x.CP}CP"));
            if (MessageBox.Show(message, "Evolve confirmation", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                this.NeedEvolve = true;
            }
        }

        #endregion

        #region AutoSelectCommand

        public RelayCommand AutoSelectCommand { get; }

        private bool AutoSelectCommand_CanExecute()
        {
            return true;
        }

        private void AutoSelectCommand_Execute()
        {
            foreach(var pokemon in this.Pokemons)
            {
                pokemon.MarkedForTransfer = false;
                pokemon.MarkedForEvolution = false;
            }

            foreach (var family in Enum.GetValues(typeof(PokemonFamilyId)).Cast<PokemonFamilyId>())
            {
                var firstPokemonOfFamily = (PokemonId)family;
                // if no evol, continue
                if (!Pokemon.CandiesToEvolve.ContainsKey(firstPokemonOfFamily))
                    continue;

                var candiesCount = _families[(int)family].Candy_;
                var pokemons = this.Pokemons
                    .Where(x => x.Data.PokemonId == firstPokemonOfFamily)
                    .OrderByDescending(x => x.CP)
                    .ToArray();
                candiesCount += pokemons.Length;
                foreach (var pokemon in pokemons)
                {
                    if (candiesCount - 1 > pokemon.CandiesToUpgrade)
                    {
                        pokemon.MarkedForEvolution = true;
                        candiesCount -= 1 + pokemon.CandiesToUpgrade;
                    }
                    else
                    {
                        pokemon.MarkedForTransfer = true;
                    }
                }
                // never transfer first pokemon
                if (pokemons.Length > 0)
                    pokemons.First().MarkedForTransfer = false;
            }
        }

        #endregion

        #region Loop

        private async Task Loop()
        {
            GetPlayerResponse player = null;
            while (player == null)
                player = await TryGet(() => _client.Player.GetPlayer());
            this.Player.Name = player.PlayerData.Username;
            this.Player.Data = player.PlayerData;

            var loopStart = DateTime.Now;
            while (_client != null)
            {
                await this.Update();
                if (_needRefreshView)
                    this.RefreshCollectionViews();
                await Task.Delay(500);

                // reconnect every 20 minutes
                if ((DateTime.Now - loopStart).TotalMinutes > 20)
                {
                    _client = null;
                    this.IsConnected = false;
                }
            }

            this.ConnectCommand.Execute(null);
        }

        private async Task Update()
        {
            if (_needInventoryUpdate)
                await UpdateInventory();

            if (_needMapUpdate)
                await UpdateMap();

            if (NeedTransfer)
                await this.TransferPokemons();

            if (NeedEvolve)
                await this.EvolvePokemons();

            await this.SpinPokestops();

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

            var pokemonIds = new HashSet<string>();
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
                    pokemonIds.Add(id);
                    if (_pokemons.ContainsKey(id))
                    {
                        _pokemons[id].Data = data.PokemonData;
                    }
                    else
                    {
                        // create pokemon
                        var family = FindPokemonFamily(data);
                        var pokemon = new Pokemon(data.PokemonData, family);
                        _pokemons[id] = pokemon;
                        DispatcherHelper.CheckBeginInvokeOnUI(() => this.Pokemons.Add(pokemon));
                    }
                }

                if (data.PlayerStats != null)
                {
                    this.Player.Level = data.PlayerStats.Level;
                    this.Player.XP = data.PlayerStats.Experience - data.PlayerStats.PrevLevelXp - GetXpDiff(data.PlayerStats.Level);
                    this.Player.NextLevelXP = data.PlayerStats.NextLevelXp - data.PlayerStats.PrevLevelXp - GetXpDiff(data.PlayerStats.Level);
                    this.Player.PreviousLevelXP = 0;
                }

                if (data.Candy != null)
                {
                    var entry = _families[(int)data.Candy.FamilyId];
                    entry.MergeFrom(data.Candy);
                }

                if (data.PokedexEntry != null)
                {
                    var entry = _pokedex[data.PokedexEntry.PokemonId];
                    entry.UpdateEntry(data.PokedexEntry);
                }
            }

            this.Player.InventoryCount = this.Items.Sum(x => x.Count);

            var pokemonsToRemove = _pokemons.Values.Where(x => !pokemonIds.Contains(x.Id)).ToArray();
            foreach (var pokemon in pokemonsToRemove)
            {
                _pokemons.Remove(pokemon.Id);
                DispatcherHelper.CheckBeginInvokeOnUI(() => this.Pokemons.Remove(pokemon));
            };

            _needRefreshView = true;

            await this.DeleteUneededItems();
        }

        private async Task DeleteUneededItems()
        {
            foreach (var item in this.Items)
            {
                if (item.Count <= item.TargetCount)
                    continue;

                ItemId id;
                if (!Enum.TryParse<ItemId>(item.Name, out id))
                    continue;

                await Task.Delay(1000);
                await TryGet(() => _client.Inventory.RecycleItem(id, item.Count - item.TargetCount));
                _needInventoryUpdate = true;
            }
        }

        private async Task SpinPokestops()
        {
            foreach (var pokestop in this._pokestops.Values)
            {
                var data = pokestop.Data;
                pokestop.PlayerDistance = this.GetDistanceToPlayer(data.Latitude, data.Longitude); // we do it every time but not very costly
                if (pokestop.PlayerDistance > 50)
                    continue;

                if (pokestop.Data.Type == FortType.Checkpoint)
                {
                    if (DateTime.Now - pokestop.LastSpin < TimeSpan.FromMinutes(1))
                        continue;

                    var details = await TryGet(() => _client.Fort.GetFort(data.Id, data.Latitude, data.Longitude));
                    if (details == null)
                        continue;

                    pokestop.Name = details.Name;
                    await Task.Delay(500);

                    var search = await TryGet(() => _client.Fort.SearchFort(data.Id, data.Latitude, data.Longitude));
                    if (search == null)
                        continue;

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
                    this.RefreshCollectionViews();

                    await Task.Delay(500);
                }
            }
        }

        private async Task TransferPokemons()
        {
            foreach (var pokemon in this.Pokemons.Where(x => x.MarkedForTransfer).ToArray())
            {
                var response = await TryGet(() => _client.Inventory.TransferPokemon(pokemon.Data.Id));
                if (response == null)
                    continue;

                switch (response.Result)
                {
                    case ReleasePokemonResponse.Types.Result.Success:
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            _pokemons.Remove(pokemon.Name);
                            this.Pokemons.Remove(pokemon);
                            _needInventoryUpdate = true;
                        });
                        break;

                    case ReleasePokemonResponse.Types.Result.Unset:
                    case ReleasePokemonResponse.Types.Result.PokemonDeployed:
                    case ReleasePokemonResponse.Types.Result.Failed:
                    case ReleasePokemonResponse.Types.Result.ErrorPokemonIsEgg:
                        break;
                }
            }

            NeedTransfer = false;
        }

        private async Task EvolvePokemons()
        {
            foreach (var pokemon in this.Pokemons.Where(x => x.MarkedForEvolution).ToArray())
            {
                var response = await TryGet(() => _client.Inventory.EvolvePokemon(pokemon.Data.Id));
                if (response == null)
                    continue;

                switch (response.Result)
                {
                    case EvolvePokemonResponse.Types.Result.Success:
                        _needInventoryUpdate = true;
                        break;

                    case EvolvePokemonResponse.Types.Result.Unset:
                    case EvolvePokemonResponse.Types.Result.FailedPokemonMissing:
                    case EvolvePokemonResponse.Types.Result.FailedInsufficientResources:
                    case EvolvePokemonResponse.Types.Result.FailedPokemonCannotEvolve:
                    case EvolvePokemonResponse.Types.Result.FailedPokemonIsDeployed:
                        break;
                }
            }

            NeedEvolve = false;
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
            this.Settings.GetType()
                .GetProperty(settingName)
                .SetValue(this.Settings, value);

            this.RaisePropertyChanged(propertyName);
            this.Settings.Save();
        }

        #endregion

        private void InitItemsCollections()
        {
            var targetCounts = new Dictionary<string, int>();
            if (!string.IsNullOrWhiteSpace(this.Settings.TargetCountsJson))
            {
                try { targetCounts = JsonConvert.DeserializeObject<Dictionary<string, int>>(this.Settings.TargetCountsJson); }
                catch { }
            }

            foreach (var itemType in Enum.GetValues(typeof(ItemId)).Cast<ItemId>())
            {
                var itemName = itemType.ToString();

                var item = new Item(itemName);
                item.PropertyChanged += Item_PropertyChanged;
                _items.Add(itemName, item);
                this.Items.Add(item);

                if (targetCounts.ContainsKey(itemName))
                    item.TargetCount = targetCounts[itemName];
            }
        }

        private void InitPokedex()
        {
            foreach (var id in Enum.GetValues(typeof(PokemonId)).Cast<PokemonId>())
            {
                var entry = new PokedexItem(id);
                _pokedex.Add(id, entry);
                this.Pokedex.Add(entry);
            }
        }

        private void InitFamilies()
        {
            foreach (var id in Enum.GetValues(typeof(PokemonFamilyId)).Cast<PokemonFamilyId>())
            {
                var entry = new Candy()
                {
                    Candy_ = 0,
                    FamilyId = id,
                };

                _families.Add((int)id, entry);
            }
        }

        private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Item.TargetCount):
                    this.UpdateTargetCounts();
                    _needInventoryUpdate = true;
                    break;
            }
        }

        private void UpdateTargetCounts()
        {
            var targetCounts = this.Items.ToDictionary(
                x => x.Name,
                x => x.TargetCount);
            this.Settings.TargetCountsJson = JsonConvert.SerializeObject(targetCounts);
            this.Settings.Save();
        }

        private void InitCollectionSorts()
        {
            var view = CollectionViewSource.GetDefaultView(this.Items);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Item.Count), System.ComponentModel.ListSortDirection.Descending));

            view = CollectionViewSource.GetDefaultView(this.Pokestops);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Pokestop.PlayerDistance), System.ComponentModel.ListSortDirection.Ascending));

            view = CollectionViewSource.GetDefaultView(this.Pokedex);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(PokedexItem.Name), System.ComponentModel.ListSortDirection.Ascending));

            view = CollectionViewSource.GetDefaultView(this.Pokemons);
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Pokemon.Name), System.ComponentModel.ListSortDirection.Ascending));
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(Pokemon.CP), System.ComponentModel.ListSortDirection.Descending));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Pokemon.Family)));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Pokemon.Name)));
        }

        private void RefreshCollectionViews()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                CollectionViewSource.GetDefaultView(this.Items).Refresh();
                CollectionViewSource.GetDefaultView(this.Pokestops).Refresh();
                CollectionViewSource.GetDefaultView(this.Pokemons).Refresh();
            });
            _needRefreshView = false;
        }

        public static int GetXpDiff(int level)
        {
            if (level > 0 && level <= 40)
            {
                int[] xpTable =
                {
                    0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                    10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                    50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                    500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 1000000, 1000000
                };
                return xpTable[level - 1];
            }
            return 0;
        }

        private Candy FindPokemonFamily(InventoryItemData data)
        {
            Candy family = null;
            var familyId = (int)data.PokemonData.PokemonId;
            while (family == null && familyId >= 0)
            {
                if (_families.ContainsKey(familyId))
                    family = _families[familyId];
                else
                    familyId--;
            }

            return family;
        }
    }
}
