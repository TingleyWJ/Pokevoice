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
        Mode mode;
        string pokemonPattern = @"[^A-Za-z0-9]";

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
                recognizer.Recognizing += (s, e) =>
                {
                    Trace.WriteLine($"RECOGNIZING: Text={e.Result.Text}");

                    string latest = e.Result.Text.Substring(e.Result.Text.LastIndexOf(" ") + 1);
                    string cleanName = Regex.Replace(latest, pokemonPattern, "");

                    Trace.WriteLine($"latest: {latest}");
                    Trace.WriteLine($"latest clean: {cleanName}");

                    foreach (Pokemon mon in Pokemon)
                    {
                        string cleanMonName = Regex.Replace(mon.Names.English, pokemonPattern, "");
                        if (cleanName.ToUpper() == cleanMonName.ToUpper())
                        {
                            UpdateUI(mon);
                            CurrentPokemon = mon;
                        }
                    }
                };

                recognizer.Recognized += (s, e) =>
                {
                    var result = e.Result;
                    Trace.WriteLine($"Reason: {result.Reason}");
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string cleanName = Regex.Replace(result.Text, pokemonPattern, "");

                        Trace.WriteLine($"Final result: {result.Text}");
                        Trace.WriteLine($"Final clean result: {cleanName}");

                        if (cleanName.ToUpper() == "ACTIVATERENEGADEMODE")
                        {
                            mode = Mode.Renegade;
                            Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                ModeBlock.Opacity = 1;
                                ModeBlock.Text = "Renegade Mode Active";
                                ModeBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F73"));
                            }));

                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }
                        else if (cleanName.ToUpper() == "DEACTIVATERENEGADEMODE")
                        {
                            mode = Mode.Base;
                            Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                ModeBlock.Opacity = 0;
                            }));

                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }
                        else if (cleanName.ToUpper() == "ACTIVATEETERNALMODE")
                        {
                            mode = Mode.Eternal;
                            Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                ModeBlock.Opacity = 1;
                                ModeBlock.Text = "Eternal Mode Active";
                                ModeBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D"));
                            }));

                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }
                        else if (cleanName.ToUpper() == "DEACTIVATEETERNALMODE")
                        {
                            mode = Mode.Base;
                            Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                ModeBlock.Opacity = 0;
                            }));

                            if (CurrentPokemon != null)
                                UpdateUI(CurrentPokemon);
                        }

                        foreach (Pokemon mon in Pokemon)
                        {
                            string cleanMonName = Regex.Replace(mon.Names.English, pokemonPattern, "");
                            if (cleanName.ToUpper() == cleanMonName.ToUpper())
                            {
                                UpdateUI(mon);
                                CurrentPokemon = mon;
                            }
                        }
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    Trace.WriteLine($"\n    Canceled. Reason: {e.Reason}, CanceledReason: {e.Reason}");
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Trace.WriteLine("\n    Session started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Trace.WriteLine("\n    Session stopped event.");
                };

                // Starts continuous recognition. 
                // Uses StopContinuousRecognitionAsync() to stop recognition.
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                while (true) ;

                // Stops recognition.
                //await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
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

                var typeList =
                (mode == Mode.Renegade && mon.Types.Renegade != null) ?
                mon.Types.Renegade :
                (mode == Mode.Eternal && mon.Types.Eternal != null) ?
                mon.Types.Eternal :
                mon.Types.Base;

                foreach (string t in typeList)
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

                var abilityList =
                (mode == Mode.Renegade && mon.Abilities.Renegade != null) ?
                mon.Abilities.Renegade :
                (mode == Mode.Eternal && mon.Abilities.Eternal != null) ?
                mon.Abilities.Eternal :
                mon.Abilities.Base;

                foreach (Ability a in abilityList)
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

                var evoBlock =
                (mode == Mode.Renegade && mon.Evolution.Renegade != null) ?
                mon.Evolution.Renegade :
                (mode == Mode.Eternal && mon.Evolution.Eternal != null) ?
                mon.Evolution.Eternal :
                mon.Evolution.Base;

                //Resize the EvoRow to accomodate
                EvoRow.Height = new GridLength(evoBlock.Next == null ? 1 : 1 + 0.25 * (evoBlock.Next.Count - 1), GridUnitType.Star);

                if (evoBlock.Prev != null)
                {
                    TextBlock prevName = new()
                    {
                        Text = PokeDict[evoBlock.Prev.Id].Names.English,
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
                        Text = $"({evoBlock.Prev.Condition})",
                        Style = (Style)FindResource("EvoDescText")
                    };

                    prevArrow.Children.Add(arrow);
                    prevArrow.Children.Add(condition);
                    EvoPanel.Children.Add(prevArrow);
                }

                TextBlock monName = new()
                {
                    Text = evoBlock.Prev == null && evoBlock.Next == null ? mon.Names.English + " does not evolve" : mon.Names.English,
                    FontStyle = evoBlock.Prev == null && evoBlock.Next == null ? FontStyles.Italic : FontStyles.Normal,
                    Style = (Style)FindResource("EvoText")
                };

                EvoPanel.Children.Add(monName);

                if (evoBlock.Next != null)
                {
                    StackPanel nextArrows = new()
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (evoBlock.Next.Count > 3)
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
                    else if (evoBlock.Next.Count == 3)
                    {
                        foreach (EvolutionInfo info in evoBlock.Next)
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
                        foreach (EvolutionInfo info in evoBlock.Next)
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

                    if (evoBlock.Next.Count > 3)
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
                        foreach (EvolutionInfo info in evoBlock.Next)
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

                int HPVal = mon.Stats.Base.HP;
                int AtkVal = mon.Stats.Base.Atk;
                int DefVal = mon.Stats.Base.Def;
                int SpAVal = mon.Stats.Base.SpA;
                int SpDVal = mon.Stats.Base.SpD;
                int SpeVal = mon.Stats.Base.Spe;
                if (mode == Mode.Renegade && mon.Stats.Renegade != null)
                {
                    if (mon.Stats.Renegade.HP != 0)
                        HPVal = mon.Stats.Renegade.HP;
                    if (mon.Stats.Renegade.Atk != 0)
                        AtkVal = mon.Stats.Renegade.Atk;
                    if (mon.Stats.Renegade.Def != 0)
                        DefVal = mon.Stats.Renegade.Def;
                    if (mon.Stats.Renegade.SpA != 0)
                        SpAVal = mon.Stats.Renegade.SpA;
                    if (mon.Stats.Renegade.SpD != 0)
                        SpDVal = mon.Stats.Renegade.SpD;
                    if (mon.Stats.Renegade.Spe != 0)
                        SpeVal = mon.Stats.Renegade.Spe;
                }
                else if (mode == Mode.Eternal && mon.Stats.Eternal != null)
                {
                    if (mon.Stats.Eternal.HP != 0)
                        HPVal = mon.Stats.Eternal.HP;
                    if (mon.Stats.Eternal.Atk != 0)
                        AtkVal = mon.Stats.Eternal.Atk;
                    if (mon.Stats.Eternal.Def != 0)
                        DefVal = mon.Stats.Eternal.Def;
                    if (mon.Stats.Eternal.SpA != 0)
                        SpAVal = mon.Stats.Eternal.SpA;
                    if (mon.Stats.Eternal.SpD != 0)
                        SpDVal = mon.Stats.Eternal.SpD;
                    if (mon.Stats.Eternal.Spe != 0)
                        SpeVal = mon.Stats.Eternal.Spe;
                }

                HP.Text = HPVal.ToString();
                Attack.Text = AtkVal.ToString();
                Defense.Text = DefVal.ToString();
                SpAtk.Text = SpAVal.ToString();
                SpDef.Text = SpDVal.ToString();
                Speed.Text = SpeVal.ToString();
                Total.Text = (HPVal + AtkVal + DefVal + SpAVal + SpDVal + SpeVal).ToString();

                float HPRatio = Math.Min(HPVal / 180f, 1);
                float AtkRatio = Math.Min(AtkVal / 180f, 1);
                float DefRatio = Math.Min(DefVal / 180f, 1);
                float SpAtkRatio = Math.Min(SpAVal / 180f, 1);
                float SpDefRatio = Math.Min(SpDVal / 180f, 1);
                float SpdRatio = Math.Min(SpeVal / 180f, 1);

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