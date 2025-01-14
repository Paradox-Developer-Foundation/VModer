using System.Diagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Initialize;
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

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LanguageServerHostedService(
        IHostApplicationLifetime lifetime,
        SettingsService settings,
        LanguageServer server
    )
    {
        _lifetime = lifetime;
        _settings = settings;
        _server = server;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var handlers = new List<IHandler> { new TextDocumentHandler(), new HoverHandler() };
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

                ResetCurrentDirectory(c);

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

    [Conditional("RELEASE")]
    private static void ResetCurrentDirectory(InitializeParams c)
    {
        string extensionPath =
            c.InitializationOptions?.RootElement.GetProperty("ExtensionPath").GetString()
            ?? string.Empty;

        if (string.IsNullOrEmpty(extensionPath))
        {
            Log.Error("扩展路径为 null.");
        }
        else
        {
            // 将当前目录重定向为扩展路径
            Environment.CurrentDirectory = extensionPath;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
