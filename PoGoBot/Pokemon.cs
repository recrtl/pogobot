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
        }

        private PokemonData _data;
        public PokemonData Data
        {
            get { return _data; }
            set { this.Set(ref _data, value); }
        }
    }
}
