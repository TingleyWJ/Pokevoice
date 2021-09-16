using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PokemonSpeechApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Pokemon> Pokemon = new();
        Dictionary<int, Pokemon> PokeDict = new();
        Pokemon CurrentPokemon = null;
        ConfigData config = new();
        bool renegadeMode;
        string pokemonPattern = @"[^A-Za-z0-9-']";

        public MainWindow()
        {
            InitializeComponent();
            //Trace.WriteLine("\n\nWINDOW 1\n\n");
        }

        async void OnLoad(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Application Loaded");
            LoadJson();

            await SpeechContinuousRecognitionAsync();
        }

        void LoadJson()
        {
            using (StreamReader r = new StreamReader("pack://application:,,,/../../../../../Json/pokemon.json"))
            {
                string json = r.ReadToEnd();
                Pokemon = JsonConvert.DeserializeObject<List<Pokemon>>(json);
                
                foreach (Pokemon mon in Pokemon)
                {
                    PokeDict.Add(mon.id, mon);
                }
            }

            using (StreamReader r = new StreamReader("pack://application:,,,/../../../../../Json/config.json"))
            {
                string json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<ConfigData>(json);
            }
        }

        async Task SpeechContinuousRecognitionAsync()
        {
            // Creates an instance of a speech config with specified subscription key and region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var speechConfig = SpeechConfig.FromSubscription(config.SubscriptionKey, config.Region);

            // Creates a speech recognizer from microphone.
            using (var recognizer = new SpeechRecognizer(speechConfig))
            {
                var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
                int count = 0;
                foreach (Pokemon mon in Pokemon)
                {
                    phraseList.AddPhrase(mon.Names.English);
                    //Trace.WriteLine($"\nAdded {mon.Names.English}\n");
                    count++;
                }
                Trace.WriteLine($"\n{count} Pokemon Names Added to Vocab\n");
                // Subscribes to events.
                recognizer.Recognizing += (s, e) => {
                    Trace.WriteLine($"RECOGNIZING: Text={e.Result.Text}");

                    string latest = e.Result.Text.Substring(e.Result.Text.LastIndexOf(" ") + 1);
                    string cleanName = Regex.Replace(latest, pokemonPattern, "");

                    Trace.WriteLine($"latest: {latest}");
                    Trace.WriteLine($"latest clean: {cleanName}");

                    foreach (Pokemon mon in Pokemon)
                    {
                        if (cleanName.ToUpper() == mon.Names.English.ToUpper())
                        {
                            UpdateUI(mon);
                            CurrentPokemon = mon;
                        }
                    }
                };

                recognizer.Recognized += (s, e) => {
                    var result = e.Result;
                    Trace.WriteLine($"Reason: {result.Reason}");
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string cleanName = Regex.Replace(result.Text, pokemonPattern, "");

                        Trace.WriteLine($"Final result: {result.Text}");
                        Trace.WriteLine($"Final clean result: {cleanName}");

                        if (cleanName.ToUpper() == "ACTIVATERENEGADEMODE")
                        {
                            renegadeMode = true;
                            Dispatcher.BeginInvoke(new ThreadStart(() => RenegadeMode.Opacity = 1));
                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }
                        else if (cleanName.ToUpper() == "DEACTIVATERENEGADEMODE")
                        {
                            renegadeMode = false;
                            Dispatcher.BeginInvoke(new ThreadStart(() => RenegadeMode.Opacity = 0));
                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }

                        foreach (Pokemon mon in Pokemon)
                        {
                            if (cleanName.ToUpper() == mon.Names.English.ToUpper())
                            {
                                UpdateUI(mon);
                                CurrentPokemon = mon;
                            }
                        }
                    }
                };

                recognizer.Canceled += (s, e) => {
                    Trace.WriteLine($"\n    Canceled. Reason: {e.Reason}, CanceledReason: {e.Reason}");
                };

                recognizer.SessionStarted += (s, e) => {
                    Trace.WriteLine("\n    Session started event.");
                };

                recognizer.SessionStopped += (s, e) => {
                    Trace.WriteLine("\n    Session stopped event.");
                };

                // Starts continuous recognition. 
                // Uses StopContinuousRecognitionAsync() to stop recognition.
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                while (true) ;

                // Stops recognition.
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }

        void UpdateUI(Pokemon mon)
        {
            //Set Name
            Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                PokemonName.Content = mon.Names.English;

                //Clear Types & Set New Ones
                TypePanel.Children.Clear();
                foreach (string t in renegadeMode && mon.RenTypes != null ? mon.RenTypes : mon.Types)
                {
                    //Trace.WriteLine(TypeColors.GetColor(t));
                    TextBlock text = new()
                    //OutlinedText text = new()
                    {
                        Text = t.ToUpper(),
                        Style = (Style)FindResource("TypeText")
                    };

                    Border border = new()
                    {
                        Child = text,
                        Style = (Style)FindResource("TypeBorder")
                    };

                    Label type = new()
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TypeColors.GetColor(t))),
                        Style = (Style)FindResource("Rounded"),
                        Content = border
                    };
                    TypePanel.Children.Add(type);
                }

                //Clear Abilities & Set New Ones
                AbilityPanel.Children.Clear();
                foreach (Ability a in renegadeMode && mon.RenAbilities != null ? mon.RenAbilities : mon.Abilities)
                {
                    TextBlock text = new()
                    {
                        Text = a.Name,
                        Style = (Style)FindResource("AbilityText")
                    };

                    Border border = new()
                    {
                        Child = text,
                        Style = (Style)FindResource("AbilityBorder")
                    };

                    if (a.Hidden)
                        text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B200FF"));

                    AbilityPanel.Children.Add(border);
                }

                //Clear Evos & Set New Ones
                EvoPanel.Children.Clear();

                //Resize the EvoRow to accomodate
                EvoRow.Height = new GridLength(mon.Evolution.Next == null ? 1 : 1 + 0.25 * (mon.Evolution.Next.Count - 1), GridUnitType.Star);

                if (mon.Evolution.Prev.Id != 0)
                {
                    TextBlock prevName = new()
                    {
                        Text = PokeDict[mon.Evolution.Prev.Id].Names.English,
                        Style = (Style)FindResource("EvoText")
                    };

                    EvoPanel.Children.Add(prevName);

                    StackPanel prevArrow = new()
                    {
                        Orientation = Orientation.Horizontal
                    };

                    TextBlock arrow = new()
                    {
                        Text = "\u21D2",
                        Style = (Style)FindResource("EvoArrow")
                    };

                    TextBlock condition = new()
                    {
                        Text = $"({mon.Evolution.Prev.Condition})",
                        Style = (Style)FindResource("EvoDescText")
                    };

                    prevArrow.Children.Add(arrow);
                    prevArrow.Children.Add(condition);
                    EvoPanel.Children.Add(prevArrow);
                }

                TextBlock monName = new()
                {
                    Text = mon.Evolution.Prev.Id == 0 && mon.Evolution.Next == null ? mon.Names.English + " does not evolve" : mon.Names.English,
                    FontStyle = mon.Evolution.Prev.Id == 0 && mon.Evolution.Next == null ? FontStyles.Italic : FontStyles.Normal,
                    Style = (Style)FindResource("EvoText")
                };

                EvoPanel.Children.Add(monName);

                /*if (mon.Evolution.Next == null || mon.Evolution.Next.Count == 1)
                {
                    EvoRow.Height = new GridLength(1, GridUnitType.Star);
                }
                else if (mon.Evolution.Next.Count == 3)
                {
                    EvoRow.Height = new GridLength(3, GridUnitType.Star);
                }
                else
                {
                    EvoRow.Height = new GridLength(3, GridUnitType.Star);
                }*/

                if (mon.Evolution.Next != null)
                {
                    StackPanel nextArrows = new()
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (mon.Evolution.Next.Count > 3)
                    {
                        StackPanel nextArrow = new()
                        {
                            Orientation = Orientation.Horizontal
                        };

                        TextBlock arrow = new()
                        {
                            Text = "\u21D2",
                            Style = (Style)FindResource("EvoArrow")
                        };

                        TextBlock condition = new()
                        {
                            Text = "(Eevee LMAO)",
                            Style = (Style)FindResource("EvoDescText")
                        };

                        nextArrow.Children.Add(arrow);
                        nextArrow.Children.Add(condition);

                        nextArrows.Children.Add(nextArrow);
                    }
                    else if (mon.Evolution.Next.Count == 3)
                    {
                        foreach (EvolutionInfo info in mon.Evolution.Next)
                        {
                            StackPanel nextArrow = new()
                            {
                                Orientation = Orientation.Horizontal
                            };

                            TextBlock arrow = new()
                            {
                                Text = "\u21D2",
                                Style = (Style)FindResource("EvoArrow")
                            };

                            TextBlock condition = new()
                            {
                                Text = $"({info.Condition})",
                                Style = (Style)FindResource("EvoDescText")
                            };

                            nextArrow.Children.Add(arrow);
                            nextArrow.Children.Add(condition);

                            nextArrows.Children.Add(nextArrow);
                        }
                    }
                    else
                    {
                        foreach (EvolutionInfo info in mon.Evolution.Next)
                        {
                            StackPanel nextArrow = new()
                            {
                                Orientation = Orientation.Horizontal
                            };

                            TextBlock arrow = new()
                            {
                                Text = "\u21D2",
                                Style = (Style)FindResource("EvoArrow")
                            };

                            TextBlock condition = new()
                            {
                                Text = $"({info.Condition})",
                                Style = (Style)FindResource("EvoDescText")
                            };

                            nextArrow.Children.Add(arrow);
                            nextArrow.Children.Add(condition);

                            nextArrows.Children.Add(nextArrow);
                        }
                    }

                    EvoPanel.Children.Add(nextArrows);

                    StackPanel nextNames = new()
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (mon.Evolution.Next.Count > 3)
                    {
                        StackPanel nextName = new();

                        TextBlock arrow = new()
                        {
                            Text = "Everything",
                            Style = (Style)FindResource("EvoText")
                        };

                        nextName.Children.Add(arrow);

                        nextNames.Children.Add(nextName);
                    }
                    else
                    {
                        foreach (EvolutionInfo info in mon.Evolution.Next)
                        {
                            StackPanel nextName = new();

                            TextBlock name = new()
                            {
                                Text = PokeDict[info.Id].Names.English,
                                Style = (Style)FindResource("EvoText")
                            };

                            nextName.Children.Add(name);

                            nextNames.Children.Add(nextName);
                        }
                    }

                    EvoPanel.Children.Add(nextNames);
                }

                int HPVal = mon.Stats.HP;
                int AtkVal = mon.Stats.Atk;
                int DefVal = mon.Stats.Def;
                int SpAtkVal = mon.Stats.SpAtk;
                int SpDefVal = mon.Stats.SpDef;
                int SpdVal = mon.Stats.Spd;
                if (renegadeMode)
                {
                    if (mon.RenStats.HP != 0)
                        HPVal = mon.RenStats.HP;
                    if (mon.RenStats.Atk != 0)
                        AtkVal = mon.RenStats.Atk;
                    if (mon.RenStats.Def != 0)
                        DefVal = mon.RenStats.Def;
                    if (mon.RenStats.SpAtk != 0)
                        SpAtkVal = mon.RenStats.SpAtk;
                    if (mon.RenStats.SpDef != 0)
                        SpDefVal = mon.RenStats.SpDef;
                    if (mon.RenStats.Spd != 0)
                        SpdVal = mon.RenStats.Spd;
                }

                HP.Text = HPVal.ToString();
                Attack.Text = AtkVal.ToString();
                Defense.Text = DefVal.ToString();
                SpAtk.Text = SpAtkVal.ToString();
                SpDef.Text = SpDefVal.ToString();
                Speed.Text = SpdVal.ToString();
                Total.Text = (HPVal + AtkVal + DefVal + SpAtkVal + SpDefVal + SpdVal).ToString();

                float HPRatio = Math.Min(HPVal / 180f, 1);
                float AtkRatio = Math.Min(AtkVal / 180f, 1);
                float DefRatio = Math.Min(DefVal / 180f, 1);
                float SpAtkRatio = Math.Min(SpAtkVal / 180f, 1);
                float SpDefRatio = Math.Min(SpDefVal / 180f, 1);
                float SpdRatio = Math.Min(SpdVal / 180f, 1);

                //Trace.WriteLine($"{HPRatio}, {AtkRatio}, {DefRatio}, {SpAtkRatio}, {SpDefRatio}, {SpdRatio}");

                HPBar.Width = HPRatio * BarWidth.ActualWidth - 10 * HPRatio;
                AtkBar.Width = AtkRatio * BarWidth.ActualWidth - 10 * AtkRatio;
                DefBar.Width = DefRatio * BarWidth.ActualWidth - 10 * DefRatio;
                SpAtkBar.Width = SpAtkRatio * BarWidth.ActualWidth - 10 * SpAtkRatio;
                SpDefBar.Width = SpDefRatio * BarWidth.ActualWidth - 10 * SpDefRatio;
                SpdBar.Width = SpdRatio * BarWidth.ActualWidth - 10 * SpdRatio;

                HPBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(HPRatio)));
                AtkBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(AtkRatio)));
                DefBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(DefRatio)));
                SpAtkBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(SpAtkRatio)));
                SpDefBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(SpDefRatio)));
                SpdBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BarColors.GetColor(SpdRatio)));
            }));
        }
    }
}
