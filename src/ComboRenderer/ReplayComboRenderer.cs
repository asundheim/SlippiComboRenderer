using ComboInterpreter;
using ComboInterpreter.ComboInterpreters;
using OBSWebsocketDotNet;
using Slippi.NET.Console;
using Slippi.NET.Console.Types;
using Slippi.NET.Types;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ComboRenderer;

internal class ReplayComboRenderer : BaseComboRenderer
{
    private readonly Window _window;
    private DolphinLauncher? _dolphinLauncher;
    private BaseComboInterpreter? _comboBot;

    private readonly string? _replayPath = null;
    private int? _startFrame = null;
    private QueueItem? _currentQueueItem = null;

    private IList<QueueItem>? _queue = null;
    private IList<QueueItem> _rerecordQueue = [];

    public ReplayComboRenderer(Window window, string replayPath, int startFrame = (int)Frames.FIRST) : base()
    { 
        _window = window;
        _replayPath = replayPath;
        _startFrame = startFrame;
    }

    public ReplayComboRenderer(Window window, IList<QueueItem> replays) : base()
    {
        _window = window;
        _queue = replays;
    }

    public override void Begin(OBSWebsocket? obs = null)
    {
        _dolphinLauncher = string.IsNullOrEmpty(SettingsManager.Instance.Settings.ReplayDolphinPath) ?
            new DolphinLauncher(SettingsManager.Instance.Settings.ReplayIsoPath) :
            new DolphinLauncher(SettingsManager.Instance.Settings.ReplayIsoPath, SettingsManager.Instance.Settings.ReplayDolphinPath);

        _dolphinLauncher.OnPlaybackStartFrameAndFilePath += (object? sender, PlaybackEventArgs args) =>
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;

            _startFrame = args.StartFrame;
            _currentQueueItem = args.QueueItem;
            obs?.SetProfileParameter("Output", "FilenameFormatting", Path.GetFileNameWithoutExtension(args.FilePath));

            if (_comboBot is not null)
            {
                _comboBot.OnDI -= HandleDI;
            }

            _comboBot = Utils.GetComboInterpreterForSettings(args.FilePath, isLive: false, args.StartFrame);
            _comboBot.OnDI += HandleDI;

            InvokeNewGame(_comboBot);
        };

        _dolphinLauncher.OnReplayedFrame += (object? sender, int frame) =>
        {
            try
            {
                if (_startFrame + 2 /* avoid recording the white frames if it's seeking */ == frame)
                {
                    obs?.StartRecord();
                }
            }
            catch
            {
                // OBS was still finishing the previous recording - we'll try again once we're done
                if (_currentQueueItem is not null)
                {
                    _rerecordQueue.Add(_currentQueueItem);
                }
            }

            _comboBot?.ProcessFrame(frame);
        };

        _dolphinLauncher.OnPlaybackComplete += (_, _) =>
        {
            _cts?.Cancel();

            try
            {
                obs?.StopRecord();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.Message}");
                Debug.WriteLine("Failed to stop recording");

                if (_currentQueueItem is not null && (_rerecordQueue.Count == 0 || (_rerecordQueue[^1].Path != _currentQueueItem.Path)))
                {
                    _rerecordQueue.Add(_currentQueueItem);
                }
            }
        };

        _dolphinLauncher.OnDolphinClosed += (_, _) =>
        {
            _cts?.Cancel();

            if (_rerecordQueue.Count > 0)
            {
                _window.Dispatcher.BeginInvoke(() =>
                {
                    this.Dispose();

                    _queue = [.._rerecordQueue];
                    _rerecordQueue = [];

                    this.Begin(obs);
                });
            }
            else
            {
                _window.Dispatcher.BeginInvoke(() => _window.Close());
            }
        };

        DolphinLaunchArgs launchArgs;
        if (_replayPath is not null && _startFrame is not null)
        {
            launchArgs = new DolphinLaunchArgs()
            {
                Replay = _replayPath,
                StartFrame = _startFrame.Value,
                EndFrame = int.MaxValue
            };
        }
        else if (_queue is not null)
        {
            launchArgs = new DolphinLaunchArgs()
            {
                Mode = DolphinLaunchModes.Queue,
                Queue = _queue,
                ShouldResync = false
            };
        }
        else 
        {
            throw new Exception("?");
        }

        _dolphinLauncher.LaunchDolphin(launchArgs);
    }

    private void HandleDI(object? sender, DIEventArgs e)
    {
        InvokeDI(e);
    }

    public override void Dispose()
    {
        base.Dispose();

        _dolphinLauncher?.Dispose();
    }
}
