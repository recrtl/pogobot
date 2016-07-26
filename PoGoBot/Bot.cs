using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using POGOProtos.Inventory.Item;
using PokemonGo.RocketAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PoGoBot
{
    class Bot : ViewModelBase
    {
        private Client _client;
        private Timer _updateTimer;

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

        private Dictionary<string, Item> _items { get; } = new Dictionary<string, Item>();
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public ObservableCollection<Pokemon> Pokemons { get; } = new ObservableCollection<Pokemon>();

        public RelayCommand ConnectCommand { get; }

        private bool ConnectCommand_CanExecute()
        {
            return true;
        }

        private async void ConnectCommand_Execute()
        {
            var settings = new ConnectionSettings()
            {
                AuthType = PokemonGo.RocketAPI.Enums.AuthType.Ptc,
                PtcUsername = this.Username,
                PtcPassword = this.Password,
                DefaultLatitude = 48.868310,
                DefaultLongitude = 2.314876,
                DefaultAltitude = 35,
            };

            _client = new Client(settings);
            try
            {
                await _client.Login.DoPtcLogin();
            }
            catch(Exception e)
            {
                MessageBox.Show($"Could not connect to login server:{e.GetBaseException().Message}");
                return;
            }

            _updateTimer?.Dispose();
            _updateTimer = new Timer(OnTimerTick, 0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        }

        private async void OnTimerTick(object state)
        {
            var inventory = await _client.Inventory.GetInventory();

            foreach (var item in inventory.InventoryDelta.InventoryItems)
            {
                var data = item.InventoryItemData;
                if (data.Item != null)
                {
                    var id = item.InventoryItemData.Item.ItemId;
                    _items[id.ToString()].Count = item.InventoryItemData.Item.Count;
                }

                if (data.PokemonData != null)
                {
                    var existingPokemon = this.Pokemons.FirstOrDefault(x => x.Data.Id == data.PokemonData.Id);
                    if (existingPokemon != null)
                        existingPokemon.Data = data.PokemonData;
                    else
                        DispatcherHelper.CheckBeginInvokeOnUI(() => this.Pokemons.Add(new Pokemon(data.PokemonData)));
                }
            }

            var mapObjects = await _client.Map.GetMapObjects();

        }
    }
}
