using GalaSoft.MvvmLight;
using POGOProtos.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Player : ViewModelBase
    {
        public Player()
        {
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { this.Set(ref _name, value); }
        }

        private PlayerData _data;
        public PlayerData Data
        {
            get { return _data; }
            set { this.Set(ref _data, value); }
        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set { this.Set(ref _level, value); }
        }

        private long _xp;
        public long XP
        {
            get { return _xp; }
            set { this.Set(ref _xp, value); }
        }

        private long _nextLevelXP;
        public long NextLevelXP
        {
            get { return _nextLevelXP; }
            set { this.Set(ref _nextLevelXP, value); }
        }

        private long _previousLevelXP;
        public long PreviousLevelXP
        {
            get { return _previousLevelXP; }
            set { this.Set(ref _previousLevelXP, value); }
        }

        private int _inventoryCount;
        public int InventoryCount
        {
            get { return _inventoryCount; }
            set { this.Set(ref _inventoryCount, value); }
        }
    }
}
