using EmmyLua.LanguageServer.Framework.Protocol.Message.Configuration;
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
        _server.AddHandler(new HoverHandler());
        _server.AddHandler(new TextDocumentHandler());
        _server.OnInitialize(
            (c, s) =>
            {
                s.Name = "VModer";
                s.Version = "1.0.0";
                return Task.CompletedTask;
            }
        );
        _server.OnInitialized(
            async (c) =>
            {
                var gameRootPath = await _server
                    .Client.GetConfiguration(
                        new ConfigurationParams
                        {
                            Items = [new ConfigurationItem { Section = "VModer.GameRootPath" }]
                        },
                        default
                    )
                    .ConfigureAwait(false);
                _settings.GameRootFolderPath = gameRootPath[0].Value as string ?? string.Empty;
                Log.Debug("Game root path: {@}", _settings.GameRootFolderPath);
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
        return Task.CompletedTask;
    }
}
