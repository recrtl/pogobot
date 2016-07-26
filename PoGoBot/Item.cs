using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Item : ViewModelBase
    {
        public Item(string name )
        {
            this.Name = name;
        }

        public string Name { get; }

        private int _targetCount = 1000;
        public int TargetCount
        {
            get { return _targetCount; }
            set { this.Set(ref _targetCount, value); }
        }

        private int _count = 0;
        public int Count
        {
            get { return _count; }
            set { this.Set(ref _count, value); }
        }
    }
}
