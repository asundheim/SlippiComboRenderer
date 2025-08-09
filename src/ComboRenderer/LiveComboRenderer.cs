using ComboInterpreter;
using ComboInterpreter.ComboInterpreters;
using OBSWebsocketDotNet;
using Slippi.NET.Console;
using Slippi.NET.Console.Types;
using Slippi.NET.Slp.Reader.File;
using Slippi.NET.Slp.Writer;
using System.Reflection.Metadata;

namespace ComboRenderer;

internal class LiveComboRenderer : BaseComboRenderer
{
    private DolphinConnection? _connection;
    private SlpFileWriter? _fileWriter;
    private BaseComboInterpreter? _comboBot;

    public LiveComboRenderer() : base()
    {
    }

    public override void Begin(OBSWebsocket? obs = null)
    {
        // live
        _connection = new DolphinConnection();
        _fileWriter = new SlpFileWriter(new SlpFileWriterSettings() 
        { 
            FolderPath = System.IO.Path.GetTempPath() 
        });

        _connection.OnStatusChange += (object? sender, ConnectionStatus status) => InvokeOnStatusChange(status);

        _connection.OnData += (object? sender, byte[] data) =>
        {
            _fileWriter.Write(data);
        };

        _fileWriter.OnNewFile += (object? sender, string path) =>
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                _cancellationToken = _cts.Token;
                if (_comboBot is not null)
                {
                    _comboBot.Dispose();
                    _comboBot.OnDI -= HandleDI;
                }

                _comboBot = Utils.GetComboInterpreterForSettings(gamePath: path, isLive: true);
                _comboBot.OnDI += HandleDI;

                InvokeNewGame(_comboBot);
            });
        };

        _fileWriter.OnFileComplete += (_, _) =>
        {
            InvokeGameEnd();
            _cts.Cancel();
        };

        _ = Task.Run(async () =>
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _connection.Connect("127.0.0.1", (int)Ports.Default, true, 10_000);
                    break;
                }
                catch
                {
                    // wait 5s between attempts
                    await Task.Delay(5_000);
                }
            }
        });
    }

    private void HandleDI(object? sender, DIEventArgs e)
    {
        InvokeDI(e);
    }

    public override void Dispose()
    {
        base.Dispose();

        _connection?.Dispose();
        _fileWriter?.Dispose();
        if (_comboBot is not null)
        {
            _comboBot.OnDI -= HandleDI;
            _comboBot.Dispose();
            _comboBot = null;
        }
    }
}
