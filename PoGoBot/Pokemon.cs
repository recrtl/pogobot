using GalaSoft.MvvmLight;
using POGOProtos.Data;
using POGOProtos.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Pokemon : ViewModelBase
    {
        public Pokemon(PokemonData data, Candy family)
        {
            this.Id = data.Id.ToString();
            this.UpdateData(data);
            this.UpdateFamily(family);
        }

        public string Id { get; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { this.Set(ref _name, value); }
        }

        private int _cp;
        public int CP
        {
            get { return _cp; }
            set { this.Set(ref _cp, value); }
        }

        private PokemonData _data;
        public PokemonData Data
        {
            get { return _data; }
            set { this.Set(ref _data, value); }
        }

        private bool _markedForTransfer;
        public bool MarkedForTransfer
        {
            get { return _markedForTransfer; }
            set { this.Set(ref _markedForTransfer, value); }
        }

        private bool _markedForEvolution;
        public bool MarkedForEvolution
        {
            get { return _markedForEvolution; }
            set { this.Set(ref _markedForEvolution, value); }
        }

        private Candy _family;
        public Candy Family
        {
            get { return _family; }
            set { this.Set(ref _family, value); }
        }

        private int _candiesToUpgrade;
        public int CandiesToUpgrade
        {
            get { return _candiesToUpgrade; }
            set { this.Set(ref _candiesToUpgrade, value); }
        }

        public void UpdateData(PokemonData data)
        {
            this.Data = data;
            this.Name = data.PokemonId.ToString();
            this.CP = data.Cp;
        }

        public void UpdateFamily(Candy family)
        {
            this.Family = family;
        }
    }
}
