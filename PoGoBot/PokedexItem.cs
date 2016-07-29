using GalaSoft.MvvmLight;
using POGOProtos.Data;
using POGOProtos.Map.Fort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POGOProtos.Enums;

namespace PoGoBot
{
    class PokedexItem : ViewModelBase
    {
        public PokedexItem(PokemonId id)
        {
            this.Id = id;
            this.Name = id.ToString();
        }

        public PokemonId Id { get;}
        public string Name { get; }

        private int _timesEncountered = 0;
        public int TimesEncountered
        {
            get { return _timesEncountered; }
            set { this.Set(ref _timesEncountered, value); }
        }

        private int _timesCaptured = 0;
        public int TimesCaptured
        {
            get { return _timesCaptured; }
            set { this.Set(ref _timesCaptured, value); }
        }

        public void UpdateEntry(PokedexEntry entry)
        {
            this.TimesEncountered = entry.TimesEncountered;
            this.TimesCaptured = entry.TimesCaptured;
        }
    }
}
