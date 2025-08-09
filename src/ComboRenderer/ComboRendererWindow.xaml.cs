using ComboInterpreter;
using ComboInterpreter.ComboInterpreters;
using ComboInterpreter.Types;
using ComboRenderer.Interop;
using Newtonsoft.Json;
using OBSWebsocketDotNet;
using Slippi.NET.Console.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ComboRenderer;

public partial class ComboRendererWindow : Window
{
    private BaseComboRenderer _comboRenderer;
    private DolphinWindowTracker? _dolphinTracker = null;
    private BaseComboInterpreter? _comboBot;

    private readonly OBSWebsocket? _obs = null;
    private readonly string? _restoreOutputPath;

    private DateTime _lastDi = DateTime.MinValue;

    private readonly MenuItem _dolphinStatus;

    [SupportedOSPlatform("windows10.0")]
    public ComboRendererWindow()
    {
        InitializeComponent();
        if (Process.GetProcessesByName("ComboRenderer").Length > 1)
        {
            this.Close();
        }

        SettingsManager.Instance.OnPausePlay += Settings_OnPausePlay;
        SettingsManager.Instance.Settings.TrackWindowChanged += Settings_TrackWindowChanged;
        SettingsManager.Instance.Settings.FollowDolphinChanged += Settings_FollowDolphinChanged;
        SettingsManager.Instance.Settings.CharacterChanged += Settings_CharacterChanged;

        BitmapImage icon = new BitmapImage(new Uri(@"Assets\gamecube.png", UriKind.Relative));
        this.Icon = icon;
        this.ShowInTaskbar = false;

        _dolphinStatus = new MenuItem() { Header = "Dolphin Status: Disconnected", IsEnabled = false };

        this.TaskbarIcon.ContextMenu = new ContextMenu();

        List<object> menuItems =
        [
            new MenuItem() { Header = "Slippi Combo Renderer", IsEnabled = false },
            new Separator(),
            _dolphinStatus,
            new MenuItem()
            {
                Header = "Settings", Command = new RelayCommand((_) => HandleOpenSettings())
            },
            new MenuItem() { Header = "Reconnect", Command = new RelayCommand((_) => HandleReconnect()) },
            new MenuItem() { Header = "Quit", Command = new RelayCommand((_) => this.Close()) }
        ];

        foreach (var item in menuItems)
        {
            this.TaskbarIcon.ContextMenu.Items.Add(item);
        }

        string[] args = Environment.GetCommandLineArgs();
        if (args.Length == 1)
        {
            if (SettingsManager.Instance.IsFirstLaunch)
            {
                HandleOpenSettings();
            }

            StartLiveComboRenderer();
        }
        else
        {
            // replay
            if (args.Length > 2)
            {
                if (args[1] == "queue")
                {
                    string launchArgsPath = args[2];
                    var dolphinArgs = JsonConvert.DeserializeObject<DolphinLaunchArgs>(System.IO.File.ReadAllText(launchArgsPath));

                    _comboRenderer = new ReplayComboRenderer(this, dolphinArgs?.Queue ?? throw new ArgumentException());
                    _obs = new OBSWebsocket();
                    ConnectToOBS();

                    _restoreOutputPath = _obs?.GetProfileParameter("Output", "FilenameFormatting").GetValue("parameterValue")!.ToObject<string>()!;
                }
                else
                {
                    string replayPath = args[1];
                    int startFrame = args.Length > 2 ? int.Parse(args[2]) : (int)Frames.FIRST;
                    _comboRenderer = new ReplayComboRenderer(this, replayPath, startFrame);
                }
            }
            else
            {
                string replayPath = args[1];
                _comboRenderer = new ReplayComboRenderer(this, replayPath);
            }

            _comboRenderer.OnNewGame += (_, comboBot) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ComboRow.Children.Clear();
                    DIContainer.Child = null;
                });

                _comboBot?.Dispose();
                _comboBot = comboBot;

                _ = Task.Run(() => ProcessInterpretedCombos());

                if (SettingsManager.Instance.Settings.FollowDolphin)
                {
                    ResetDolphinTracker(isPlaybackDolphin: true);
                }
                else
                {
                    AdjustWindowForExplicit();
                }
            };

            _comboRenderer.OnDI += HandleDI;
            _comboRenderer.OnStatusChange += HandleStatusChange;
            _comboRenderer.OnGameEnd += HandleGameEnd;
            _comboRenderer.Begin(_obs);
        }
    }

    private void Settings_CharacterChanged(object? sender, string e)
    {
        Dispatcher.Invoke(() =>
        {
            _dolphinTracker?.Dispose();
        });
        _comboBot?.Dispose();
        
        if (_comboRenderer is not null)
        {
            _comboRenderer.OnDI -= HandleDI;
            _comboRenderer.OnStatusChange -= HandleStatusChange;
            _comboRenderer.OnGameEnd -= HandleGameEnd;
            _comboRenderer.Dispose();
        }

        StartLiveComboRenderer();
    }

    private void Settings_FollowDolphinChanged(object? sender, bool e)
    {
        if (e)
        {
            ResetDolphinTracker(isPlaybackDolphin: SettingsManager.Instance.Settings.TrackWindow == "Replay");
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                if (_dolphinTracker is not null)
                {
                    _dolphinTracker.OnDolphinMoved -= OnDolphinMoved;
                    _dolphinTracker.Dispose();
                }
            });

            AdjustWindowForExplicit();
        }
    }

    private void Settings_TrackWindowChanged(object? sender, string e)
    {
        if (SettingsManager.Instance.Settings.FollowDolphin)
        {
            ResetDolphinTracker(isPlaybackDolphin: e == "Replay");
        }
    }

    private void Settings_OnPausePlay(object? sender, bool e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            this.ComboBorder.Visibility = e ? Visibility.Visible : Visibility.Collapsed;
            this.DIContainer.Visibility = e ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    [MemberNotNull(nameof(_comboRenderer))]
    private void StartLiveComboRenderer()
    {
        _comboRenderer = new LiveComboRenderer();
        _comboRenderer.OnNewGame += (_, comboBot) =>
        {
            _comboBot?.Dispose();
            _comboBot = comboBot;

            _ = Task.Run(() =>
            {
                _ = Task.Run(async () => await comboBot.WaitForLiveGameEndAsync());

                ProcessInterpretedCombos();
            });

            if (SettingsManager.Instance.Settings.FollowDolphin)
            {
                ResetDolphinTracker(isPlaybackDolphin: SettingsManager.Instance.Settings.TrackWindow == "Replay");
            }
            else
            {
                AdjustWindowForExplicit();
            }
        };

        _comboRenderer.OnDI += HandleDI;
        _comboRenderer.OnStatusChange += HandleStatusChange;
        _comboRenderer.OnGameEnd += HandleGameEnd;
        _comboRenderer.Begin(_obs);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _obs?.SetProfileParameter("Output", "FilenameFormatting", _restoreOutputPath ?? string.Empty);
        _obs?.Disconnect();
        _comboBot?.Dispose();
        _dolphinTracker?.Dispose();
        TaskbarIcon.Dispose();
    }

    private void OnDolphinMoved(object? sender, EventArgs args)
    {
        if (_dolphinTracker is not null)
        {
            AdjustWindowToDolphin(_dolphinTracker.GetMovedDolphinWindowInfo());
        }
    }

    private void AdjustWindowToDolphin(WindowInfo? dolphinWindow)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (dolphinWindow is not null)
            {
                this.Left = dolphinWindow.Left + 100;
                this.Top = dolphinWindow.Top + 45;
                this.Width = dolphinWindow.Width - 200;
                this.Height = dolphinWindow.Height - 45;
            }
        });
    }

    private void AdjustWindowForExplicit()
    {
        Dispatcher.BeginInvoke(() =>
        {
            this.Width = SettingsManager.Instance.Settings.ExplicitWidth;
            this.Height = SettingsManager.Instance.Settings.ExplicitHeight;
            this.Top = 0;
            this.Left = 0;
        });
    }

    private void HandleDI(object? sender, DIEventArgs e)
    {
        if (SettingsManager.Instance.Settings.EnableDICam)
        {
            double angle = Math.Atan2(e.PreFrameUpdate.JoystickY ?? 0, e.PreFrameUpdate.JoystickX ?? 0);
            double angleDegrees = angle * (180 / Math.PI);
            Dispatcher.BeginInvoke(() =>
            {
                const int SCALE = 10;
                BitmapImage bmp = new BitmapImage(new Uri(@"Assets\analog-dd-left.png", UriKind.Relative));
                Image img = new Image()
                {
                    Source = bmp,
                    Width = bmp.Width / SCALE,
                    Height = bmp.Height / SCALE,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                img.RenderTransform = new RotateTransform(angleDegrees + 180, img.Width / 2, img.Height / 2);
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

                int stocksLeft = e.PostFrameUpdate.StocksRemaining!.Value;
                StackPanel panel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                //panel.Background = Brushes.Black;
                panel.HorizontalAlignment = HorizontalAlignment.Left;

                var text = ComboImageBuilder.GetStrokeText(this, "DI CAM", 36);
                text.Margin = new Thickness(0, 0, 0, 5);
                panel.Children.Add(text);
                panel.Children.Add(img);
                panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Border panelBorder = new Border()
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(2),
                    Child = panel,
                    Margin = new Thickness((e.PlayerIndex * 424) + (stocksLeft * 50) + 135, 0, 0, 320),
                    Background = new SolidColorBrush(Colors.Black) { Opacity = 0.5 },
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(5)
                };

                bool animate = this.DIContainer.Child is null;
                this.DIContainer.Child = panelBorder;

                if (animate)
                {
                    panelBorder.PopInOut();
                }

                _lastDi = DateTime.Now;

                _ = Task.Run(async () =>
                {
                    await DITimeout();
                });
            });
        }
        else
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.DIContainer.Child = null;
            });
        }
    }

    private async Task DITimeout()
    {
        await Task.Delay(750);
        if (_lastDi != DateTime.MinValue)
        {
            if (DateTime.Now.Subtract(_lastDi).Duration().TotalMilliseconds >= 650)
            {
                _ = Dispatcher.BeginInvoke(() =>
                {
                    this.DIContainer.Child = null;
                });
            }
        }
    }

    private void HandleStatusChange(object? sender, ConnectionStatus status)
    {
        SettingsManager.Instance.DolphinConnectionStatus = status;

        Dispatcher.BeginInvoke(() =>
        {
            _dolphinStatus.Header = "Dolphin status: " + status switch
            {
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Connecting => "Connecting",
                ConnectionStatus.Disconnected => "Disconnected",
                _ => "Unknown (try reconnect)"
            };

            if (status == ConnectionStatus.Disconnected)
            {
                ComboRow.Children.Clear();
                DIContainer.Child = null;

                HandleReconnect();
            }
        });
    }

    private void HandleGameEnd(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            ComboRow.Children.Clear();
            DIContainer.Child = null;
        });
    }

    internal void HandleReconnect()
    {
        Dispatcher.Invoke(() =>
        {
            _comboBot?.Dispose();
            _dolphinTracker?.Dispose();
        });

        if (_comboRenderer is not null)
        {
            _comboRenderer.OnDI -= HandleDI;
            _comboRenderer.OnStatusChange -= HandleStatusChange;
            _comboRenderer.OnGameEnd -= HandleGameEnd;
            _comboRenderer.Dispose();
        }

        StartLiveComboRenderer();
    }

    private void HandleOpenSettings()
    {
        this.Hide();
        this.ShowInTaskbar = true;

        SettingsWindow settings = new SettingsWindow(this);
        settings.Show();
    }

    [MemberNotNull(nameof(_dolphinTracker))]
    private void ResetDolphinTracker(bool isPlaybackDolphin)
    {
        Dispatcher.Invoke(() =>
        {
            if (_dolphinTracker is not null)
            {
                _dolphinTracker.OnDolphinMoved -= OnDolphinMoved;
                _dolphinTracker.Dispose();
            }
        });

        _dolphinTracker = new DolphinWindowTracker(isPlaybackDolphin);
        _dolphinTracker.OnDolphinMoved += OnDolphinMoved;

        Dispatcher.BeginInvoke(() =>
        {
            AdjustWindowToDolphin(_dolphinTracker.GetDolphinWindowInfo());
        });
    }

    private void ConnectToOBS()
    {
        if (_obs is not null)
        {
            TaskCompletionSource tcsOnConnect = new TaskCompletionSource();

            _obs.Connected += (o, e) =>
            {
                tcsOnConnect.SetResult();
            };

            // ew
            Task.Run(async () =>
            {
                _obs.ConnectAsync($"ws://{SettingsManager.Instance.Settings.OBSAddress}:{SettingsManager.Instance.Settings.OBSPort}", string.Empty);
                await tcsOnConnect.Task;
            }).Wait();
        }
    }

    private void ProcessInterpretedCombos()
    {
        CancellationToken cancellation = _comboRenderer.CancellationToken;

        // flag set after a combo is processed in the inner loop
        bool activeLine = false;   // 
        bool continuation = false; // indicates a continuation (->) is coming up. 
        bool queueFlush = false;   // forcibly clear the row. always reset before the next combo.
        Actions previousAction = Actions.None;

        Stopwatch s = new Stopwatch();
        s.Start();
        while (!cancellation.IsCancellationRequested)
        {
            // Wait for a combo
            InterpretedCombo combo = _comboBot?.ComboStream.Take(cancellation) ?? throw new Exception("No combo interpreter set up");
            s.Stop();

            Dispatcher.Invoke(() =>
            {
                if (queueFlush || (s.ElapsedMilliseconds >= 450 && activeLine))
                {
                    if (!continuation && !previousAction.IsLongStartupAction())
                    {
                        ComboRow.Children.Clear();

                        activeLine = false;
                    }

                    queueFlush = false;
                }

                StringBuilder sb = new StringBuilder();

                sb.Append(combo.DisplayName);

                string result = sb.ToString();

                StackPanel newImage = ComboImageBuilder.CreateImage(this, combo.ActionEvent, combo.Buttons);
                newImage.VerticalAlignment = VerticalAlignment.Bottom;
                newImage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                bool animate = true; // should it pop in with an animation?
                if (ComboRow.Children.Count > 0)
                {
                    if (previousAction.IsRepeatedAction(nextAction: combo.ActionEvent.Action) && previousAction == combo.ActionEvent.Action)
                    {
                        ComboRow.Children.RemoveAt(ComboRow.Children.Count - 1);
                        animate = false;
                    }
                }

                double childWidth = 0;
                foreach (var child in ComboRow.Children)
                {
                    childWidth += ((FrameworkElement)child)!.DesiredSize.Width;
                }

                // clear the row if it can't fit our new element
                if (childWidth + newImage.DesiredSize.Width >= this.ActualWidth)
                {
                    ComboRow.Children.Clear();
                }

                Grid imageTextGrid = new Grid()
                {
                    Margin = combo.HasContinuation ? new Thickness(0) : new Thickness(0, 0, 10, 0), // squish the continuations together
                    Height = 180,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // some evil fixed layout magic so things don't jump around
                imageTextGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(127, GridUnitType.Pixel)
                });

                imageTextGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(40, GridUnitType.Pixel)
                });

                imageTextGrid.Children.Add(newImage);
                Grid.SetRow(newImage, 0);

                var text = ComboImageBuilder.GetStrokeText(this, combo.DisplayName);
                text.VerticalAlignment = VerticalAlignment.Center;
                imageTextGrid.Children.Add(text);
                Grid.SetRow(text, 1);

                // Add the whole assembled piece to the combo row
                ComboRow.Children.Add(imageTextGrid);

                if (animate)
                {
                    imageTextGrid.PopInOut();
                }

                continuation = combo.HasContinuation;

                // if the combo says clear the row, clear the row.
                // if all we have up is Dash, clear the row. (because we don't want random dashes lingering)
                if (combo.EndsCombo || (combo.ActionEvent.Action == Actions.Dash && ComboRow.Children.Count == 1))
                {
                    queueFlush = true;
                    activeLine = false;
                }
                else
                {
                    activeLine = true;
                }

                s.Restart();
                previousAction = combo.ActionEvent.Action;
            });
        }
    }
}