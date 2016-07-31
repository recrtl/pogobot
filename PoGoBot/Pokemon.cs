using GalaSoft.MvvmLight;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGoBot
{
    class Pokemon : ViewModelBase
    {
        #region Static data

        public static readonly ReadOnlyDictionary<PokemonId, int> CandiesToEvolve = new ReadOnlyDictionary<PokemonId, int>(new Dictionary<PokemonId, int>()
        {
            { PokemonId.Bulbasaur, 25},
            { PokemonId.Ivysaur, 100},
            { PokemonId.Charmander, 25},
            { PokemonId.Charmeleon, 100},
            { PokemonId.Squirtle, 25},
            { PokemonId.Wartortle, 100},
            { PokemonId.Caterpie, 12},
            { PokemonId.Metapod, 50},
            { PokemonId.Weedle, 12},
            { PokemonId.Kakuna, 50},
            { PokemonId.Pidgey, 12},
            { PokemonId.Pidgeotto, 50},
            { PokemonId.Rattata, 25},
            { PokemonId.Spearow, 50},
            { PokemonId.Ekans, 50},
            { PokemonId.Pikachu, 50},
            { PokemonId.Sandshrew, 50},
            { PokemonId.NidoranFemale, 25},
            { PokemonId.Nidorina, 100},
            { PokemonId.NidoranMale, 25},
            { PokemonId.Nidorino, 100},
            { PokemonId.Clefairy, 50},
            { PokemonId.Vulpix, 50},
            { PokemonId.Jigglypuff, 50},
            { PokemonId.Zubat, 50},
            { PokemonId.Oddish, 25},
            { PokemonId.Gloom, 100},
            { PokemonId.Paras, 50},
            { PokemonId.Venonat, 50},
            { PokemonId.Diglett, 50},
            { PokemonId.Meowth, 50},
            { PokemonId.Psyduck, 50},
            { PokemonId.Mankey, 50},
            { PokemonId.Growlithe, 50},
            { PokemonId.Poliwag, 25},
            { PokemonId.Poliwhirl, 100},
            { PokemonId.Abra, 25},
            { PokemonId.Kadabra, 100},
            { PokemonId.Machop, 25},
            { PokemonId.Machoke, 100},
            { PokemonId.Bellsprout, 25},
            { PokemonId.Weepinbell, 100},
            { PokemonId.Tentacool, 50},
            { PokemonId.Geodude, 25},
            { PokemonId.Graveler, 100},
            { PokemonId.Ponyta, 50},
            { PokemonId.Slowpoke, 50},
            { PokemonId.Magnemite, 50},
            { PokemonId.Doduo, 50},
            { PokemonId.Seel, 50},
            { PokemonId.Grimer, 50},
            { PokemonId.Shellder, 50},
            { PokemonId.Gastly, 25},
            { PokemonId.Haunter, 100},
            { PokemonId.Drowzee, 50},
            { PokemonId.Krabby, 50},
            { PokemonId.Voltorb, 50},
            { PokemonId.Exeggcute, 50},
            { PokemonId.Cubone, 50},
            { PokemonId.Koffing, 50},
            { PokemonId.Rhyhorn, 50},
            { PokemonId.Horsea, 50},
            { PokemonId.Goldeen, 50},
            { PokemonId.Staryu, 50},
            { PokemonId.Magikarp, 400},
            { PokemonId.Eevee, 25},
            { PokemonId.Omanyte, 50},
            { PokemonId.Kabuto, 50},
            { PokemonId.Dratini, 25},
            { PokemonId.Dragonair, 100},
        });

        public static readonly ReadOnlyDictionary<PokemonId, string> PokemonNames = new ReadOnlyDictionary<PokemonId, string>(new Dictionary<PokemonId, string>()
        {
            {PokemonId.Bulbasaur        ,"Bulbizarre"},
            {PokemonId.Ivysaur          ,"Herbizarre"},
            {PokemonId.Venusaur         ,"Florizarre"},
            {PokemonId.Charmander       ,"Salamèche"},
            {PokemonId.Charmeleon       ,"Reptincel"},
            {PokemonId.Charizard        ,"Dracaufeu"},
            {PokemonId.Squirtle         ,"Carapuce"},
            {PokemonId.Wartortle        ,"Carabaffe"},
            {PokemonId.Blastoise        ,"Tortank"},
            {PokemonId.Caterpie         ,"Chenipan"},
            {PokemonId.Metapod          ,"Chrysacier"},
            {PokemonId.Butterfree       ,"Papilusion"},
            {PokemonId.Weedle           ,"Aspicot"},
            {PokemonId.Kakuna           ,"Coconfort"},
            {PokemonId.Beedrill         ,"Dardargnan"},
            {PokemonId.Pidgey           ,"Roucool"},
            {PokemonId.Pidgeotto        ,"Roucoups"},
            {PokemonId.Pidgeot          ,"Roucarnage"},
            {PokemonId.Rattata          ,"Rattata"},
            {PokemonId.Raticate         ,"Rattatac"},
            {PokemonId.Spearow          ,"Piafabec"},
            {PokemonId.Fearow           ,"Rapasdepic"},
            {PokemonId.Ekans            ,"Abo"},
            {PokemonId.Arbok            ,"Arbok"},
            {PokemonId.Pikachu          ,"Pikachu"},
            {PokemonId.Raichu           ,"Raichu"},
            {PokemonId.Sandshrew        ,"Sabelette"},
            {PokemonId.Sandslash        ,"Sablaireau"},
            {PokemonId.NidoranFemale    ,"Nidoran♀"},
            {PokemonId.Nidorina         ,"Nidorina"},
            {PokemonId.Nidoqueen        ,"Nidoqueen"},
            {PokemonId.NidoranMale      ,"Nidoran♂"},
            {PokemonId.Nidorino         ,"Nidorino"},
            {PokemonId.Nidoking         ,"Nidoking"},
            {PokemonId.Clefairy         ,"Mélofée"},
            {PokemonId.Clefable         ,"Mélodelfe"},
            {PokemonId.Vulpix           ,"Goupix"},
            {PokemonId.Ninetales        ,"Feunard"},
            {PokemonId.Jigglypuff       ,"Rondoudou"},
            {PokemonId.Wigglytuff       ,"Grodoudou"},
            {PokemonId.Zubat            ,"Nosferapti"},
            {PokemonId.Golbat           ,"Nosferalto"},
            {PokemonId.Oddish           ,"Mystherbe"},
            {PokemonId.Gloom            ,"Ortide"},
            {PokemonId.Vileplume        ,"Rafflesia"},
            {PokemonId.Paras            ,"Paras"},
            {PokemonId.Parasect         ,"Parasect"},
            {PokemonId.Venonat          ,"Mimitoss"},
            {PokemonId.Venomoth         ,"Aéromite"},
            {PokemonId.Diglett          ,"Taupiqueur"},
            {PokemonId.Dugtrio          ,"Triopikeur"},
            {PokemonId.Meowth           ,"Miaouss"},
            {PokemonId.Persian          ,"Persian"},
            {PokemonId.Psyduck          ,"Psykokwak"},
            {PokemonId.Golduck          ,"Akwakwak"},
            {PokemonId.Mankey           ,"Férosinge"},
            {PokemonId.Primeape         ,"Colossinge"},
            {PokemonId.Growlithe        ,"Caninos"},
            {PokemonId.Arcanine         ,"Arcanin"},
            {PokemonId.Poliwag          ,"Ptitard"},
            {PokemonId.Poliwhirl        ,"Têtarte"},
            {PokemonId.Poliwrath        ,"Tartard"},
            {PokemonId.Abra             ,"Abra"},
            {PokemonId.Kadabra          ,"Kadabra"},
            {PokemonId.Alakazam         ,"Alakazam"},
            {PokemonId.Machop           ,"Machoc"},
            {PokemonId.Machoke          ,"Machopeur"},
            {PokemonId.Machamp          ,"Mackogneur"},
            {PokemonId.Bellsprout       ,"Chétiflor"},
            {PokemonId.Weepinbell       ,"Boustiflor"},
            {PokemonId.Victreebel       ,"Empiflor"},
            {PokemonId.Tentacool        ,"Tentacool"},
            {PokemonId.Tentacruel       ,"Tentacruel"},
            {PokemonId.Geodude          ,"Racaillou"},
            {PokemonId.Graveler         ,"Gravalanch"},
            {PokemonId.Golem            ,"Grolem"},
            {PokemonId.Ponyta           ,"Ponyta"},
            {PokemonId.Rapidash         ,"Galopa"},
            {PokemonId.Slowpoke         ,"Ramoloss"},
            {PokemonId.Slowbro          ,"Flagadoss"},
            {PokemonId.Magnemite        ,"Magnéti"},
            {PokemonId.Magneton         ,"Magnéton"},
            {PokemonId.Farfetchd        ,"Canarticho"},
            {PokemonId.Doduo            ,"Doduo"},
            {PokemonId.Dodrio           ,"Dodrio"},
            {PokemonId.Seel             ,"Otaria"},
            {PokemonId.Dewgong          ,"Lamantine"},
            {PokemonId.Grimer           ,"Tadmorv"},
            {PokemonId.Muk              ,"Grotadmorv"},
            {PokemonId.Shellder         ,"Kokiyas"},
            {PokemonId.Cloyster         ,"Crustabri"},
            {PokemonId.Gastly           ,"Fantominus"},
            {PokemonId.Haunter          ,"Spectrum"},
            {PokemonId.Gengar           ,"Ectoplasma"},
            {PokemonId.Onix             ,"Onix"},
            {PokemonId.Drowzee          ,"Soporifik"},
            {PokemonId.Hypno            ,"Hypnomade"},
            {PokemonId.Krabby           ,"Krabby"},
            {PokemonId.Kingler          ,"Krabboss"},
            {PokemonId.Voltorb          ,"Voltorbe"},
            {PokemonId.Electrode        ,"Électrode"},
            {PokemonId.Exeggcute        ,"Noeunoeuf"},
            {PokemonId.Exeggutor        ,"Noadkoko"},
            {PokemonId.Cubone           ,"Osselait"},
            {PokemonId.Marowak          ,"Ossatueur"},
            {PokemonId.Hitmonlee        ,"Kicklee"},
            {PokemonId.Hitmonchan       ,"Tygnon"},
            {PokemonId.Lickitung        ,"Excelangue"},
            {PokemonId.Koffing          ,"Smogo"},
            {PokemonId.Weezing          ,"Smogogo"},
            {PokemonId.Rhyhorn          ,"Rhinocorne"},
            {PokemonId.Rhydon           ,"Rhinoféros"},
            {PokemonId.Chansey          ,"Leveinard"},
            {PokemonId.Tangela          ,"Saquedeneu"},
            {PokemonId.Kangaskhan       ,"Kangourex"},
            {PokemonId.Horsea           ,"Hypotrempe"},
            {PokemonId.Seadra           ,"Hypocéan"},
            {PokemonId.Goldeen          ,"Poissirène"},
            {PokemonId.Seaking          ,"Poissoroy"},
            {PokemonId.Staryu           ,"Stari"},
            {PokemonId.Starmie          ,"Staross"},
            {PokemonId.MrMime           ,"M. Mime"},
            {PokemonId.Scyther          ,"Insécateur"},
            {PokemonId.Jynx             ,"Lippoutou"},
            {PokemonId.Electabuzz       ,"Élektek"},
            {PokemonId.Magmar           ,"Magmar"},
            {PokemonId.Pinsir           ,"Scarabrute"},
            {PokemonId.Tauros           ,"Tauros"},
            {PokemonId.Magikarp         ,"Magicarpe"},
            {PokemonId.Gyarados         ,"Léviator"},
            {PokemonId.Lapras           ,"Lokhlass"},
            {PokemonId.Ditto            ,"Métamorph"},
            {PokemonId.Eevee            ,"Évoli"},
            {PokemonId.Vaporeon         ,"Aquali"},
            {PokemonId.Jolteon          ,"Voltali"},
            {PokemonId.Flareon          ,"Pyroli"},
            {PokemonId.Porygon          ,"Porygon"},
            {PokemonId.Omanyte          ,"Amonita"},
            {PokemonId.Omastar          ,"Amonistar"},
            {PokemonId.Kabuto           ,"Kabuto"},
            {PokemonId.Kabutops         ,"Kabutops"},
            {PokemonId.Aerodactyl       ,"Ptéra"},
            {PokemonId.Snorlax          ,"Ronflex"},
            {PokemonId.Articuno         ,"Artikodin"},
            {PokemonId.Zapdos           ,"Électhor"},
            {PokemonId.Moltres          ,"Sulfura"},
            {PokemonId.Dratini          ,"Minidraco"},
            {PokemonId.Dragonair        ,"Draco"},
            {PokemonId.Dragonite        ,"Dracolosse"},
            {PokemonId.Mewtwo           ,"Mewtwo"},
            {PokemonId.Mew              ,"Mew"},
        });

        #endregion

        public Pokemon(PokemonData data, Candy family)
        {
            this.Id = data.Id.ToString();
            this.Data = data;
            this.Family = family;
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
            set
            {
                this.Set(ref _data, value);
                this.OnDataChanged();
            }
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

        private int _candiesToUpgrade;
        public int CandiesToUpgrade
        {
            get { return _candiesToUpgrade; }
            set { this.Set(ref _candiesToUpgrade, value); }
        }

        private Candy _family;
        public Candy Family
        {
            get { return _family; }
            set { this.Set(ref _family, value); }
        }

        private void OnDataChanged()
        {
            var id = this.Data.PokemonId;
            this.Name = id.ToString();
            this.CP = this.Data.Cp;

            if (CandiesToEvolve.ContainsKey(id))
                this.CandiesToUpgrade = CandiesToEvolve[id];
            else
                this.CandiesToUpgrade = 0;
        }
    }
}
