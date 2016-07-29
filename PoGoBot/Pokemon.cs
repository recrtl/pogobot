using GalaSoft.MvvmLight;
using POGOProtos.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Pokemon : ViewModelBase
    {
        public Pokemon(PokemonData data)
        {
            this.Data = data;
            this.Name = data.PokemonId.ToString();
            this.CP = data.Cp;
        }

        public string Name { get; }
        public int CP { get; }

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
    }
}
