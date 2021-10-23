using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Label = System.Windows.Controls.Label;
using Orientation = System.Windows.Controls.Orientation;

namespace PokemonSpeechApp
{
    /* Ideas & To-do
     * -Make UI larger for better readability
     * -Integrate Twitch rewards for memes
     * -Catch rate
     * -Weight
     * -Potentially type matchups
    */

    public partial class MainWindow : Window
    {
        ConfigData Config { get; set; }
        SpeechRecognizer Recognizer { get; set; }
        MMDevice MicToUse { get; set; } = null;

        List<Pokemon> Pokemon { get; set; } = new();
        Dictionary<string, Pokemon> PokeDict { get; set; } = new();

        Pokemon CurrentPokemon { get; set; } = null;

        Mode Mode { get; set; } = Mode.Base;
        string PokemonPattern { get; } = @"[^A-Za-z0-9]";
        string[] LatestWords { get; set; } = new string[3] { "", "", "" };

        bool CloseApp { get; set; }
        bool Recording { get; set; }

        LowLevelKeyboardHook KBH { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoad;
            Closing += OnClosing;
            //Trace.WriteLine("\n\nWINDOW 1\n\n");
        }

        #region Callbacks
        void OnLoad(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Application Loaded");
            LoadJson();

            InitMicrophone();
            //InitRecognizer();
            //InitRecognizerCallbacks();

            KBH = new();
            KBH.OnKeyPressed += KBH_OnKeyDown;
            KBH.OnKeyUnpressed += KBH_OnKeyUp;
            KBH.HookKeyboard();

            //Old but going to keep for posterity for now
            //await SpeechContinuousRecognitionAsync();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        void KBH_OnKeyDown(object sender, Keys e)
        {
            //Trace.WriteLine($"KBH Key Down: {e}");
            if (e == Keys.F13 && !Recording)
            {
                Trace.WriteLine("Recording Beginning");
                UpdateRecordingUI();
                Recording = true;

                InitRecognizer();
                InitRecognizerCallbacks();
                Recognizer.StartContinuousRecognitionAsync();
            }
        }

        async void KBH_OnKeyUp(object sender, Keys e)
        {
            //Trace.WriteLine($"KBH Key Up: {e}");
            if (e == Keys.F13)
            {
                Trace.WriteLine("Recording Ending");
                UpdateRecordingUI();
                Recording = false;

                await Recognizer.StopContinuousRecognitionAsync();
                TerminateRecognizer();
            }
        }

        void OnClosing(object sender, CancelEventArgs e)
        {
            Trace.WriteLine("Window is Closing");
            //Recognizer.Dispose();
        }

        void InitRecognizerCallbacks()
        {
            Recognizer.Recognizing += (s, e) =>
            {
                Trace.WriteLine($"RECOGNIZING: Text={e.Result.Text}");

                string latest = e.Result.Text.Substring(e.Result.Text.LastIndexOf(" ") + 1);

                LatestWords[0] = LatestWords[1];
                LatestWords[1] = LatestWords[2];
                LatestWords[2] = latest;

                string[] cleanLatest = new string[3] { "", "", "" };
                for (int i = 0; i < 3; i++)
                {
                    if (LatestWords[i] != "")
                        cleanLatest[i] = Regex.Replace(LatestWords[i], PokemonPattern, "");
                }

                SwapMode(string.Join("", LatestWords));

                Trace.WriteLine($"LatestWords: {string.Join(", ", LatestWords)}");
                Trace.WriteLine($"cleanLatest: {string.Join(", ", cleanLatest)}");

                foreach (Pokemon mon in Pokemon)
                {
                    string cleanMonName = Regex.Replace(mon.Names.English, PokemonPattern, "");
                    if ((cleanLatest[0] + cleanLatest[1] + cleanLatest[2]).ToUpper() == cleanMonName.ToUpper() ||
                        (cleanLatest[1] + cleanLatest[2]).ToUpper() == cleanMonName.ToUpper() ||
                        cleanLatest[2].ToUpper() == cleanMonName.ToUpper())
                    {
                        UpdateUI(mon);
                        CurrentPokemon = mon;
                    }
                }
            };

            Recognizer.Recognized += (s, e) =>
            {
                var result = e.Result;
                Trace.WriteLine($"Reason: {result.Reason}");

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    string cleanName = Regex.Replace(result.Text, PokemonPattern, "");

                    Trace.WriteLine($"Final result: {result.Text}");
                    Trace.WriteLine($"Final clean result: {cleanName}");

                    SwapMode(cleanName);

                    foreach (Pokemon mon in Pokemon)
                    {
                        string cleanMonName = Regex.Replace(mon.Names.English, PokemonPattern, "");
                        if (cleanName.ToUpper() == cleanMonName.ToUpper())
                        {
                            UpdateUI(mon);
                            CurrentPokemon = mon;
                        }
                    }
                }
            };

            Recognizer.Canceled += (s, e) =>
            {
                Trace.WriteLine($"\n    Canceled. Error Code: {e.ErrorCode}. Error Details: {e.ErrorDetails}. Reason: {e.Reason}.");
            };

            Recognizer.SessionStarted += (s, e) =>
            {
                Trace.WriteLine("\n    Session started event.");
            };

            Recognizer.SessionStopped += (s, e) =>
            {
                Trace.WriteLine("\n    Session stopped event.");
            };
        }

        #endregion

        #region Functions
        void InitMicrophone()
        {
            //MMDevice micToUse = null;
            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                //Trace.WriteLine($"Mic Name: {endpoint.FriendlyName}, ID: {endpoint.ID}");
                //Trace.WriteLine($"Compare: {Config.MicrophoneName}");
                //Trace.WriteLine($"Same: {endpoint.FriendlyName == Config.MicrophoneName}");
                if (endpoint.FriendlyName == Config.MicrophoneName)
                {
                    MicToUse = endpoint;
                }
            }
        }

        void InitRecognizer()
        {
            var speechConfig = SpeechConfig.FromSubscription(Config.SubscriptionKey, Config.Region);
            speechConfig.EndpointId = Config.EndpointID;

            if (MicToUse == null)
            {
                Trace.WriteLine("Using Default Microphone");
                Recognizer = new(speechConfig);
            }
            else
            {
                Trace.WriteLine($"Using {MicToUse.FriendlyName}");
                using var audioConfig = AudioConfig.FromMicrophoneInput(MicToUse.ID);
                Recognizer = new(speechConfig, audioConfig);
            }
        }

        void TerminateRecognizer()
        {
            Recognizer.Dispose();
            Recognizer = null;
        }

        void LoadJson()
        {
            using (StreamReader r = new StreamReader("pack://application:,,,/../../../../../Json/pokemon.json"))
            {
                string json = r.ReadToEnd();
                Pokemon = JsonConvert.DeserializeObject<List<Pokemon>>(json);

                foreach (Pokemon mon in Pokemon)
                {
                    PokeDict.Add(mon.ID, mon);
                }
            }

            using (StreamReader r = new StreamReader("pack://application:,,,/../../../../../Json/config.json"))
            {
                string json = r.ReadToEnd();
                Config = JsonConvert.DeserializeObject<ConfigData>(json);
            }
        }

        [Obsolete]
        async Task SpeechContinuousRecognitionAsync()
        {
            // Creates an instance of a speech config with specified subscription key and region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var speechConfig = SpeechConfig.FromSubscription(Config.SubscriptionKey, Config.Region);
            speechConfig.EndpointId = Config.EndpointID;

            MMDevice micToUse = null;
            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                //Trace.WriteLine($"Mic Name: {endpoint.FriendlyName}, ID: {endpoint.ID}");
                //Trace.WriteLine($"Compare: {Config.MicrophoneName}");
                //Trace.WriteLine($"Same: {endpoint.FriendlyName == Config.MicrophoneName}");
                if (endpoint.FriendlyName == Config.MicrophoneName)
                {
                    micToUse = endpoint;
                }
            }

            SpeechRecognizer recognizer = null;
            AudioConfig audioConfig = null;
            if (micToUse == null)
            {
                Trace.WriteLine("Using Default Microphone");
                recognizer = new(speechConfig);
            }
            else
            {
                Trace.WriteLine($"Using {micToUse.FriendlyName}");
                audioConfig = AudioConfig.FromMicrophoneInput(micToUse.ID);
                recognizer = new(speechConfig, audioConfig);
            }

            // Creates a speech recognizer from microphone.
            //using var recognizer = new SpeechRecognizer(speechConfig);

            /*var phraseList = PhraseListGrammar.FromRecognizer(recognizer);

            int count = 0;
            foreach (Pokemon mon in Pokemon)
            {
                phraseList.AddPhrase(mon.Names.English);
                //Trace.WriteLine($"\nAdded {mon.Names.English}\n");
                count++;
            }
            Trace.WriteLine($"\n{count} Pokemon Names Added to Vocab\n");*/

            // Subscribes to events.
            recognizer.Recognizing += (s, e) =>
            {
                Trace.WriteLine($"RECOGNIZING: Text={e.Result.Text}");

                string latest = e.Result.Text.Substring(e.Result.Text.LastIndexOf(" ") + 1);

                LatestWords[0] = LatestWords[1];
                LatestWords[1] = LatestWords[2];
                LatestWords[2] = latest;

                string[] cleanLatest = new string[3] { "", "", "" };
                for (int i = 0; i < 3; i++)
                {
                    if (LatestWords[i] != "")
                        cleanLatest[i] = Regex.Replace(LatestWords[i], PokemonPattern, "");
                }

                SwapMode(string.Join("", LatestWords));

                Trace.WriteLine($"LatestWords: {string.Join(", ", LatestWords)}");
                Trace.WriteLine($"cleanLatest: {string.Join(", ", cleanLatest)}");

                foreach (Pokemon mon in Pokemon)
                {
                    string cleanMonName = Regex.Replace(mon.Names.English, PokemonPattern, "");
                    if ((cleanLatest[0] + cleanLatest[1] + cleanLatest[2]).ToUpper() == cleanMonName.ToUpper() ||
                        (cleanLatest[1] + cleanLatest[2]).ToUpper() == cleanMonName.ToUpper() ||
                        cleanLatest[2].ToUpper() == cleanMonName.ToUpper())
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
                    string cleanName = Regex.Replace(result.Text, PokemonPattern, "");

                    Trace.WriteLine($"Final result: {result.Text}");
                    Trace.WriteLine($"Final clean result: {cleanName}");

                    SwapMode(cleanName);

                    foreach (Pokemon mon in Pokemon)
                    {
                        string cleanMonName = Regex.Replace(mon.Names.English, PokemonPattern, "");
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

            while (!CloseApp) ;
            /*{
                if (RecordDown)
                {
                    RecordDown = false;
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                }
                else if (RecordUp)
                {
                    RecordUp = false;
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }*/

            // Stops recognition.
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            //Disposables
            recognizer.Dispose();
            if (audioConfig != null)
                audioConfig.Dispose();
            Dispatcher.Invoke(() =>
            {
                Close();
            });
        }

        void SwapMode(string cleanName)
        {
            if (cleanName.ToUpper() == "RENEGADEMODE")
            {
                Mode = Mode.Renegade;
                Dispatcher.Invoke(() =>
                {
                    ModeLabel.Opacity = 1;
                    ModeLabel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F73"));
                    ModeText.Text = "R";
                });

                if (CurrentPokemon != null)
                    UpdateUI(CurrentPokemon);
            }
            /*else if (cleanName.ToUpper() == "DEACTIVATERENEGADEMODE")
            {
                Mode = Mode.Base;
                Dispatcher.Invoke(() =>
                {
                    ModeLabel.Opacity = 0;
                });

                if (CurrentPokemon != null)
                    UpdateUI(CurrentPokemon);
            }*/
            else if (cleanName.ToUpper() == "ETERNALMODE")
            {
                Mode = Mode.Eternal;
                Dispatcher.Invoke(() =>
                {
                    ModeLabel.Opacity = 1;
                    ModeLabel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D"));
                    ModeText.Text = "E";
                });

                if (CurrentPokemon != null)
                    UpdateUI(CurrentPokemon);
            }
            else if (cleanName.ToUpper() == "NOMODE")
            {
                Mode = Mode.Base;
                Dispatcher.Invoke(() =>
                {
                    ModeLabel.Opacity = 0;
                });

                if (CurrentPokemon != null)
                    UpdateUI(CurrentPokemon);
            }
        }

        void UpdateRecordingUI()
        {
            Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                RecordingLabel.Opacity = !Recording ? 1 : 0;
            }));
        }

        void UpdateUI(Pokemon mon)
        {
            //Set Name
            Dispatcher.BeginInvoke(new ThreadStart(() =>
            {
                Match nameMatch = Regex.Match(mon.Names.English, @"(\w+)\[(\w+)\]");
                string formattedName = nameMatch.Success ? nameMatch.Groups[1].Value + (nameMatch.Groups[2].Value == "Male" ? UnicodeChars.Male : UnicodeChars.Female) : mon.Names.English;

                PokemonName.Content = formattedName;

                //Clear Types & Set New Ones
                TypePanel.Children.Clear();

                var typeList =
                (Mode == Mode.Renegade && mon.Types.Renegade != null) ?
                mon.Types.Renegade :
                (Mode == Mode.Eternal && mon.Types.Eternal != null) ?
                mon.Types.Eternal :
                mon.Types.Base;

                foreach (string t in typeList)
                {
                    TextBlock text = new()
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
                (Mode == Mode.Renegade && mon.Abilities.Renegade != null) ?
                mon.Abilities.Renegade :
                (Mode == Mode.Eternal && mon.Abilities.Eternal != null) ?
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
                (Mode == Mode.Renegade && mon.Evolution.Renegade != null) ?
                mon.Evolution.Renegade :
                (Mode == Mode.Eternal && mon.Evolution.Eternal != null) ?
                mon.Evolution.Eternal :
                mon.Evolution.Base;

                //Resize the EvoRow to accomodate
                EvoRow.Height = new GridLength(evoBlock.Next == null ? 1 : evoBlock.Next.Count > 3 ? 1 : 1 + 0.25 * (evoBlock.Next.Count - 1), GridUnitType.Star);

                if (evoBlock.Prev != null)
                {
                    TextBlock prevName = new()
                    {
                        Style = (Style)FindResource("EvoText")
                    };

                    Match prevNameMatch = Regex.Match(PokeDict[evoBlock.Prev.ID].Names.English, @"(\w+)\[(\w+)\]");
                    if (prevNameMatch.Success)
                        prevName.Text = prevNameMatch.Groups[1].Value + (prevNameMatch.Groups[2].Value == "Male" ? UnicodeChars.Male : UnicodeChars.Female);
                    else
                        prevName.Text = PokeDict[evoBlock.Prev.ID].Names.English;

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
                    Text = evoBlock.Prev == null && evoBlock.Next == null ? formattedName + " does not evolve" : formattedName,
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
                                Style = (Style)FindResource("EvoText")
                            };

                            Match nextNameMatch = Regex.Match(PokeDict[info.ID].Names.English, @"(\w+)\[(\w+)\]");
                            if (nextNameMatch.Success)
                                name.Text = nextNameMatch.Groups[1].Value + (nextNameMatch.Groups[2].Value == "Male" ? UnicodeChars.Male : UnicodeChars.Female);
                            else
                                name.Text = PokeDict[info.ID].Names.English;

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
                if (Mode == Mode.Renegade && mon.Stats.Renegade != null)
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
                else if (Mode == Mode.Eternal && mon.Stats.Eternal != null)
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
        #endregion
    }
}