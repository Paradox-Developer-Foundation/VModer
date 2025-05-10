using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using VModer.Core;
using VModer.Core.Analyzers;
using VModer.Core.Services;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Base;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Core.Services.Hovers;

var settings = new HostApplicationBuilderSettings
{
    Args = args,
    ApplicationName = "VModer",
    ContentRootPath = AppContext.BaseDirectory
};
#if DEBUG
settings.EnvironmentName = "Development";
#else
settings.EnvironmentName = "Production";
#endif

var builder = Host.CreateApplicationBuilder(settings);

Stream? inputStream;
Stream? outputStream;

#if DEBUG
using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var ipAddress = new IPAddress([127, 0, 0, 1]);
var endPoint = new IPEndPoint(ipAddress, 1231);
socket.Bind(endPoint);
socket.Listen(1);
Debug.WriteLine("等待连接...");
Console.WriteLine("等待连接...");
var languageClientSocket = await socket.AcceptAsync().ConfigureAwait(false);
await using var _networkStream = new NetworkStream(languageClientSocket);
inputStream = _networkStream;
outputStream = _networkStream;
#else
inputStream = Console.OpenStandardInput();
outputStream = Console.OpenStandardOutput();
#endif

var server = LanguageServer.From(inputStream, outputStream);

builder.Services.AddSingleton(server);
builder.Services.AddSingleton<SettingsService>();

builder.Services.AddSingleton<GameResourcesPathService>();
builder.Services.AddSingleton<GameModDescriptorService>();
builder.Services.AddSingleton<GameFilesService>();
builder.Services.AddSingleton<GameResourcesWatcherService>();
builder.Services.AddSingleton<IImageService, ImageService>();

// 语言服务
builder.Services.AddSingleton<AnalyzeService>();
builder.Services.AddSingleton<StateAnalyzerService>();
builder.Services.AddSingleton<CharacterAnalyzerService>();
builder.Services.AddSingleton<EditorDiagnosisService>();
builder.Services.AddSingleton<DocumentColorService>();
builder.Services.AddSingleton<ServerLoggerService>();

// Hover服务
builder.Services.AddSingleton<HoverService>();
builder.Services.AddSingleton<HoverStrategyManager>();
builder.Services.AddSingleton<IHoverStrategy, CharacterHoverStrategy>();
builder.Services.AddKeyedSingleton<IHoverStrategy, ModifierHoverStrategy>(nameof(ModifierHoverStrategy));
builder.Services.AddSingleton<IHoverStrategy, CountriesDefineHoverStrategy>();
builder.Services.AddSingleton<IHoverStrategy, TechnologyHoverStrategy>();
builder.Services.AddSingleton<IHoverStrategy, DefaultHoverStrategy>();

// 游戏资源服务
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddSingleton<BuildingsService>();
builder.Services.AddSingleton<LocalizationKeyMappingService>();
builder.Services.AddSingleton<LocalizationFormatService>();
builder.Services.AddSingleton<TerrainService>();
builder.Services.AddSingleton<ModifierDisplayService>();
builder.Services.AddSingleton<ModifierService>();
builder.Services.AddSingleton<ILocalizationTextColorsService, LocalizationTextColorsService>();
builder.Services.AddSingleton<CharacterSkillService>();
builder.Services.AddSingleton<ModiferLocalizationFormatService>();
builder.Services.AddSingleton<GeneralTraitsService>();
builder.Services.AddSingleton<LeaderTraitsService>();
builder.Services.AddSingleton<DefinesService>();
builder.Services.AddSingleton<SpriteService>();
builder.Services.AddSingleton<UnitService>();
builder.Services.AddSingleton<OreService>();
builder.Services.AddSingleton<IdeologiesService>();
builder.Services.AddSingleton<OperationsService>();
builder.Services.AddSingleton<SpecialProjectsService>();
builder.Services.AddSingleton<ModifiersMessageService>();

builder.Services.AddHostedService<LanguageServerHostedService>();

// 添加 NLog 日志
builder.Logging.ClearProviders();
builder.Logging.AddNLog(builder.Configuration);
LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));

var logger = LogManager.GetCurrentClassLogger();

var host = builder.Build();
App.Services = host.Services;

TaskScheduler.UnobservedTaskException += async (_, e) =>
{
    await SendServerErrorMessage().ConfigureAwait(false);
    logger.Error(e.Exception, "未观察到的异常");
    e.SetObserved();
};

try
{
    await host.RunAsync().ConfigureAwait(false);
}
catch (Exception e)
{
    await SendServerErrorMessage().ConfigureAwait(false);
    logger.Error(e);
}
finally
{
    LogManager.Flush();
}

return;

async Task SendServerErrorMessage()
{
    await server
        .Client.ShowMessage(new ShowMessageParams { Type = MessageType.Error, Message = "VModer 运行时错误" })
        .ConfigureAwait(false);
}
