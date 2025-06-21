using System.Net.Sockets;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class Watcher : IHostedService
{
    private readonly object _lock = new();
    private Timer? _timer;
    private IMessageChannel? _channel;
    private bool? _isServerUp;
    private bool isTestActive;

    private readonly DiscordSocketClient _client;
    private readonly BotConfiguration _config;
    private readonly ILogger _logger;

    public Watcher(DiscordSocketClient client, ILoggerFactory loggerFactory, IOptions<BotConfiguration> config)
    {
        _client = client;
        _config = config.Value;
        _logger = loggerFactory.CreateLogger<Watcher>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Log += Log;
        _client.Ready += ClientReady;

        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        _timer = new Timer(CheckConnection, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        await _client.StopAsync();
    }

    private Task ClientReady()
    {
        _logger.LogInformation($"Setting up Discord client for Channel # {_config.ChannelId}");
        _channel = _client.GetChannel(_config.ChannelId) as IMessageChannel ?? throw new InvalidOperationException("Could not get Channel!");
        return Task.CompletedTask;
    }

    async void CheckConnection(object? state)
    {
        lock (_lock)
        {
            if (isTestActive) return;
            isTestActive = true;
        }
        _logger.LogInformation($"Testing {_config.Server}:{_config.Port}");

        using var udpClient = new UdpClient();
        try
        {
            udpClient.Connect(_config.Server, _config.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed.");
            await TryUpdateStatus(false);
            isTestActive = false;
            return;
        }

        try
        {
            udpClient.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed.");
        }

        await TryUpdateStatus(true);

        isTestActive = false;
    }

    private async Task TryUpdateStatus(bool isServerUp)
    {
        bool isFirstCheck = false;
        lock (_lock)
        {
            if (isServerUp == _isServerUp) return;
            isFirstCheck = _isServerUp == null;
            _isServerUp = isServerUp;
        }

        if (isFirstCheck)
        {
            _logger.LogDebug("First startup, no state change to report.");
            return;
        }

        var status = _isServerUp.Value ? "Online!" : "Offline!";
        await _channel.SendMessageAsync($"Server Update: Server is {status}");
    }

    Task Log(LogMessage message)
    {
        _logger.LogInformation(message.ToString());
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _client?.Dispose();
    }
}
