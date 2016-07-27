using GalaSoft.MvvmLight;
using POGOProtos.Data;
using POGOProtos.Map.Fort;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Pokestop : ViewModelBase
    {
        public Pokestop(FortData data)
        {
            this.Data = data;
        }

        public FortData Data { get; } 

        private string _name = null;
        public string Name
        {
            get { return _name; }
            set { this.Set(ref _name, value); }
        }

        private DateTime _lastSpin = DateTime.MinValue;
        public DateTime LastSpin
        {
            get { return _lastSpin; }
            set { this.Set(ref _lastSpin, value); }
        }

        private double _playerDistance = double.NaN;
        public double PlayerDistance
        {
            get { return _playerDistance; }
            set { this.Set(ref _playerDistance, value); }
        }
    }
}
