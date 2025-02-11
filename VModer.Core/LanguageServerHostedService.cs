using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using EmmyLua.LanguageServer.Framework.Protocol.JsonRpc;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Initialize;
using EmmyLua.LanguageServer.Framework.Server;
using EnumsNET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using VModer.Core.Handlers;
using VModer.Core.Models;
using VModer.Core.Services;

namespace VModer.Core;

public sealed class LanguageServerHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly SettingsService _settings;
    private readonly LanguageServer _server;
    private readonly ServerLoggerService _logger;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LanguageServerHostedService(
        IHostApplicationLifetime lifetime,
        SettingsService settings,
        LanguageServer server,
        ServerLoggerService logger
    )
    {
        _lifetime = lifetime;
        _settings = settings;
        _server = server;
        _logger = logger;

        _server.AddRequestHandler("getRuntimeInfo", GetRuntimeInfoAsync);
    }

    private Task<JsonDocument?> GetRuntimeInfoAsync(
        RequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return Task.Run<JsonDocument?>(
            () =>
            {
                _currentProcess.Refresh();

                long memoryUsedBytes = _currentProcess.PrivateMemorySize64;
                var document = JsonDocument.Parse($"{{\"memoryUsedBytes\": {memoryUsedBytes}}}");
                return document;
            },
            cancellationToken
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var handlers = new List<IHandler>
        {
            new TextDocumentHandler(),
            new HoverHandler(),
            new DocumentColorHandler()
        };
        foreach (var handler in handlers)
        {
            _server.AddHandler(handler);
        }
        _server.OnInitialize(
            (c, s) =>
            {
                Log.Info("Language server initializing...");

                InitialSettings(c);
                s.Name = "VModer";
                s.Version = "1.0.0";

                foreach (var handler in handlers)
                {
                    handler.Initialize();
                }

                Log.Info("Game root path: {Path}", _settings.GameRootFolderPath);
                Log.Info("Workspace root path: {Path}", _settings.ModRootFolderPath);
                return Task.CompletedTask;
            }
        );

        _server.OnInitialized(_ =>
        {
            var analyzersService = App.Services.GetRequiredService<AnalyzeService>();

            _server.SendNotification(new NotificationMessage("analyzeAllFilesStart", null));
            analyzersService
                .AnalyzeAllFilesAsync(cancellationToken)
                .ContinueWith(
                    _ =>
                    {
                        _server.SendNotification(new NotificationMessage("analyzeAllFilesEnd", null));
                        Log.Info("Language server initialized.");
                        _logger.Log("Language server initialized.");
                    },
                    cancellationToken
                );

            return Task.CompletedTask;
        });

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _server.Run().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Language server error.");
                }
                // 连接中断后关闭应用
                _lifetime.StopApplication();
            },
            cancellationToken
        );
        Log.Info("Language server started.");
        return Task.CompletedTask;
    }

    private void InitialSettings(InitializeParams param)
    {
        string gameRootPath =
            param.InitializationOptions?.RootElement.GetProperty("GameRootFolderPath").GetString()
            ?? string.Empty;
        var blackList =
            param
                .InitializationOptions?.RootElement.GetProperty("Blacklist")
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty) ?? [];
        // 传来的是 MB, 要转成 byte
        long parseFileMaxBytesSize = (long)(
            (param.InitializationOptions?.RootElement.GetProperty("ParseFileMaxSize").GetDouble() ?? 0)
            * 1024
            * 1024
        );
        string gameLanguage =
            param.InitializationOptions?.RootElement.GetProperty("GameLanguage").GetString() ?? string.Empty;

        _settings.GameRootFolderPath = gameRootPath;
        _settings.ModRootFolderPath = param.RootUri?.FileSystemPath ?? string.Empty;
        _settings.AnalysisBlackList = blackList.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _settings.ParseFileMaxBytesSize = parseFileMaxBytesSize;
        _settings.GameLanguage = Enums.TryParse<GameLanguage>(gameLanguage, true, out var gameLanguageEnum)
            ? gameLanguageEnum
            : GameLanguage.Default;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _currentProcess.Dispose();
        return Task.CompletedTask;
    }
}
