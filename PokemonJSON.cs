using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PokemonSpeechApp
{
    struct Names
    {
        public string English { get; set; }
        public string French { get; set; }
    }

    struct Stats
    {
        public int HP { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int SpAtk { get; set; }
        public int SpDef { get; set; }
        public int Spd { get; set; }

        /*public int GetTotal()
        {
            return HP + Atk + Def + SpAtk + SpDef + Spd;
        }*/
    }

    struct Ability
    {
        public string Name { get; set; }
        public bool Hidden { get; set; }
    }

    struct Evolution
    {
        public EvolutionInfo Prev { get; set; }
        public IList<EvolutionInfo> Next { get; set; }
    }

    struct EvolutionInfo
    {
        public int Id { get; set; }
        public string Condition { get; set; }
    }

    class Pokemon
    {
        public ushort id { get; set; }
        public Names Names { get; set; }
        public IList<string> Types { get; set; }
        public IList<string> RenTypes { get; set; }
        public Stats Stats { get; set; }
        public Stats RenStats { get; set; }
        public IList<Ability> Abilities { get; set; }
        public IList<Ability> RenAbilities { get; set; }
        public Evolution Evolution { get; set; }
    }

    class ConfigData
    {
        public string SubscriptionKey { get; set; }
        public string Region { get; set; }
    }

    class TypeColors
    {
        public static string Normal { get; } = "#AAAA99";
        public static string Fire { get; } = "#FF4422";
        public static string Water { get; } = "#3399FF";
        public static string Electric { get; } = "#FFCC33";
        public static string Grass { get; } = "#77CC55";
        public static string Ice { get; } = "#66CCFF";
        public static string Fighting { get; } = "#BB5544";
        public static string Poison { get; } = "#AA5599";
        public static string Ground { get; } = "#DDBB55";
        public static string Flying { get; } = "#8899FF";
        public static string Psychic { get; } = "#FF5599";
        public static string Bug { get; } = "#AABB22";
        public static string Rock { get; } = "#BBAA66";
        public static string Ghost { get; } = "#6666BB";
        public static string Dragon { get; } = "#7766EE";
        public static string Dark { get; } = "#775544";
        public static string Steel { get; } = "#AAAABB";
        public static string Fairy { get; } = "#EE99EE";

        public static string GetColor(string s)
        {
            Type type = typeof(TypeColors);
            return type.GetProperty(s).GetValue(type).ToString();
        }
    }

    class BarColors
    {
        static string Red { get; } = "#F34444";
        static string YellowRed { get; } = "#FF7F0F";
        static string Yellow { get; } = "#FFDD57";
        static string YellowGreen { get; } = "#A0E515";
        static string Green { get; } = "#23CD5E";
        static string GreenBlue { get; } = "#00C2B8";

        public static string GetColor(float percent)
        {
            if (percent >= 5 / 6f)
                return GreenBlue;
            else if (percent >= 4 / 6f)
                return Green;
            else if (percent >= 3 / 6f)
                return YellowGreen;
            else if (percent >= 2 / 6f)
                return Yellow;
            else if (percent >= 1 / 6f)
                return YellowRed;
            else
                return Red;
        }
    }

    class Arrows
    {
        static string RightArrow { get; } = "\u21D2";
        static string UpRightArrow { get; } = "\u21D7";
        static string DownRightArrow { get; } = "\u21D8";
    }
}
