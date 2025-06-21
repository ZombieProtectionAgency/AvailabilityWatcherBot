using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSimpleConsole(options => {
    options.TimestampFormat = "yyyy-MM-dd hh:mm:ss\t";
});

builder.Services.AddHostedService<Watcher>();
builder.Services.Configure<BotConfiguration>(
    builder.Configuration.GetSection(BotConfiguration.DiscordSettings));
builder.Services.AddTransient<DiscordSocketClient>();

IHost host = builder.Build();
host.Run();
