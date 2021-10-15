using System;
using System.Collections.Generic;

namespace PokemonSpeechApp
{
    enum Mode
    {
        Base = 0,
        Renegade,
        Eternal
    }

    class Names
    {
        public string English { get; set; }
        public string French { get; set; }
    }

    class Types
    {
        public IList<string> Base { get; set; }
        public IList<string> Renegade { get; set; }
        public IList<string> Eternal { get; set; }
    }

    class Stats
    {
        public StatBlock Base { get; set; }
        public StatBlock Renegade { get; set; }
        public StatBlock Eternal { get; set; }
    }

    class StatBlock
    {
        public int HP { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int SpA { get; set; }
        public int SpD { get; set; }
        public int Spe { get; set; }
    }

    class Abilities
    {
        public IList<Ability> Base { get; set; }
        public IList<Ability> Renegade { get; set; }
        public IList<Ability> Eternal { get; set; }
    }

    class Ability
    {
        public string Name { get; set; }
        public bool Hidden { get; set; }
    }

    class Evolution
    {
        public EvolutionBlock Base { get; set; }
        public EvolutionBlock Renegade { get; set; }
        public EvolutionBlock Eternal { get; set; }
    }

    class EvolutionBlock
    {
        public EvolutionInfo Prev { get; set; }
        public IList<EvolutionInfo> Next { get; set; }
    }

    class EvolutionInfo
    {
        public string ID { get; set; }
        public string Condition { get; set; }
    }

    class Pokemon
    {
        public string ID { get; set; }
        public Names Names { get; set; }
        public Types Types { get; set; }
        public Stats Stats { get; set; }
        public Abilities Abilities { get; set; }
        public Evolution Evolution { get; set; }
    }

    class ConfigData
    {
        public string EndpointID { get; set; }
        public string MicrophoneName { get; set; }
        public string Region { get; set; }
        public string SubscriptionKey { get; set; }
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

    class UnicodeChars
    {
        public static string RightArrow { get; } = "\u21D2";
        public static string UpRightArrow { get; } = "\u21D7";
        public static string DownRightArrow { get; } = "\u21D8";
        public static string Female { get; } = "\u2640";
        public static string Male { get; } = "\u2642";
    }
}
