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
        ConfigData config = new();
        //TypeColors TypeColors = new();

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
            using (StreamReader r = new StreamReader("pack://application:,,,/../../../../../Json/pokemon-small.json"))
            {
                string json = r.ReadToEnd();
                Pokemon = JsonConvert.DeserializeObject<List<Pokemon>>(json);
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
                    count++;
                }
                Trace.WriteLine($"\n{count} Pokemon Names Added to Vocab\n");
                // Subscribes to events.
                recognizer.Recognizing += (s, e) => {
                    Trace.WriteLine($"RECOGNIZING: Text={e.Result.Text}");

                    string latest = e.Result.Text.Substring(e.Result.Text.LastIndexOf(" ") + 1);
                    string cleanName = Regex.Replace(latest, @"[^A-Za-z0-9-]", "");

                    Trace.WriteLine($"latest: {latest}");
                    Trace.WriteLine($"latest clean: {cleanName}");

                    foreach (Pokemon mon in Pokemon)
                    {
                        if (cleanName.ToUpper() == mon.Names.English.ToUpper())
                        {
                            //Set Name
                            Dispatcher.BeginInvoke(new ThreadStart(() =>
                            {
                                PokemonName.Content = mon.Names.English;

                                //Clear Types & Set New Ones
                                TypePanel.Children.Clear();
                                foreach (string t in mon.Types)
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
                                foreach (Ability a in mon.Abilities)
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

                                HP.Text = mon.Stats.HP.ToString();
                                Attack.Text = mon.Stats.Atk.ToString();
                                Defense.Text = mon.Stats.Def.ToString();
                                SpAtk.Text = mon.Stats.SpAtk.ToString();
                                SpDef.Text = mon.Stats.SpDef.ToString();
                                Speed.Text = mon.Stats.Spd.ToString();
                                Total.Text = mon.Stats.GetTotal().ToString();

                                float HPRatio = Math.Min(mon.Stats.HP / 180f, 1);
                                float AtkRatio = Math.Min(mon.Stats.Atk / 180f, 1);
                                float DefRatio = Math.Min(mon.Stats.Def / 180f, 1);
                                float SpAtkRatio = Math.Min(mon.Stats.SpAtk / 180f, 1);
                                float SpDefRatio = Math.Min(mon.Stats.SpDef / 180f, 1);
                                float SpdRatio = Math.Min(mon.Stats.Spd / 180f, 1);

                                Trace.WriteLine($"{HPRatio}, {AtkRatio}, {DefRatio}, {SpAtkRatio}, {SpDefRatio}, {SpdRatio}");

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
                };

                recognizer.Recognized += (s, e) => {
                    var result = e.Result;
                    Trace.WriteLine($"Reason: {result.Reason}");
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string cleanName = Regex.Replace(result.Text, @"[^A-Za-z0-9-]", "");

                        Trace.WriteLine($"Final result: {result.Text}");
                        Trace.WriteLine($"Final clean result: {cleanName}");

                        /*foreach (Pokemon mon in Pokemon)
                        {
                            if (cleanName.ToUpper() == mon.Names.English.ToUpper())
                            {
                                //Set Name
                                Dispatcher.BeginInvoke(new ThreadStart(() =>
                                {
                                    PokemonName.Content = mon.Names.English;

                                    //Clear Types & Set New Ones
                                    TypePanel.Children.Clear();
                                    foreach (string t in mon.Types)
                                    {
                                        //Trace.WriteLine(TypeColors.GetColor(t));
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
                                    foreach (Ability a in mon.Abilities)
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

                                    HP.Text = mon.Stats.HP.ToString();
                                    Attack.Text = mon.Stats.Atk.ToString();
                                    Defense.Text = mon.Stats.Def.ToString();
                                    SpAtk.Text = mon.Stats.SpAtk.ToString();
                                    SpDef.Text = mon.Stats.SpDef.ToString();
                                    Speed.Text = mon.Stats.Spd.ToString();
                                    Total.Text = mon.Stats.GetTotal().ToString();

                                    float HPRatio = Math.Min(mon.Stats.HP / 180f, 1);
                                    float AtkRatio = Math.Min(mon.Stats.Atk / 180f, 1);
                                    float DefRatio = Math.Min(mon.Stats.Def / 180f, 1);
                                    float SpAtkRatio = Math.Min(mon.Stats.SpAtk / 180f, 1);
                                    float SpDefRatio = Math.Min(mon.Stats.SpDef / 180f, 1);
                                    float SpdRatio = Math.Min(mon.Stats.Spd / 180f, 1);

                                    Trace.WriteLine($"{HPRatio}, {AtkRatio}, {DefRatio}, {SpAtkRatio}, {SpDefRatio}, {SpdRatio}");

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
                        }*/
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
    }
}
