using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using EmmyLua.LanguageServer.Framework.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VModer.Core;
using VModer.Core.Handlers;
using VModer.Core.Services;
using VModer.Core.Services.GameResource;

var settings = new HostApplicationBuilderSettings { Args = args, ApplicationName = "VModer" };

#if DEBUG
settings.EnvironmentName = "Development";
#else
settings.EnvironmentName = "Production";
#endif

var builder = Host.CreateApplicationBuilder(settings);

using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var ipAddress = new IPAddress([127, 0, 0, 1]);
var endPoint = new IPEndPoint(ipAddress, 1231);
socket.Bind(endPoint);
socket.Listen(1);
Debug.WriteLine("等待连接...");
var languageClientSocket = await socket.AcceptAsync().ConfigureAwait(false);
await using var _networkStream = new NetworkStream(languageClientSocket);

var server = LanguageServer.From(_networkStream, _networkStream);

builder.Services.AddSingleton(server);
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<GameFilesService>();
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<AnalyzeService>();
builder.Services.AddSingleton<EditorDiagnosisService>();
builder.Services.AddHostedService<LanguageServerHostedService>();

builder.Logging.ClearProviders();
builder.Logging.AddNLog(builder.Configuration);
NLog.LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));

var host = builder.Build();
App.Services = host.Services;

await host.RunAsync().ConfigureAwait(false);
