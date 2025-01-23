using System.Diagnostics;
using System.Text.Json;
using EmmyLua.LanguageServer.Framework.Protocol.JsonRpc;
using EmmyLua.LanguageServer.Framework.Server;
using Microsoft.Extensions.Hosting;
using NLog;
using VModer.Core.Handlers;
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
        _currentProcess.Refresh();

        long memoryUsedBytes = _currentProcess.PrivateMemorySize64;
        var document = JsonDocument.Parse($"{{\"memoryUsedBytes\": {memoryUsedBytes}}}");
        return Task.FromResult<JsonDocument?>(document);
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
                string gameRootPath =
                    c.InitializationOptions?.RootElement.GetProperty("GameRootFolderPath").GetString()
                    ?? string.Empty;

                _settings.GameRootFolderPath = gameRootPath;
                _settings.ModRootFolderPath = c.RootUri?.FileSystemPath ?? string.Empty;
                s.Name = "VModer";
                s.Version = "1.0.0";

                foreach (var handler in handlers)
                {
                    handler.Initialize();
                }
                return Task.CompletedTask;
            }
        );

        _server.OnInitialized(
            (c) =>
            {
                Log.Info("Game root path: {Path}", _settings.GameRootFolderPath);
                Log.Info("Workspace root path: {Path}", _settings.ModRootFolderPath);
                _logger.Log("Language server initialized.");
                return Task.CompletedTask;
            }
        );

        _ = Task.Run(
            async () =>
            {
                await _server.Run().ConfigureAwait(false);
                // 连接中断后关闭应用
                _lifetime.StopApplication();
            },
            cancellationToken
        );
        Log.Info("Language server started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _currentProcess.Dispose();
        return Task.CompletedTask;
    }
}
