public class BotConfiguration {
    public const string DiscordSettings = nameof(DiscordSettings);

    public string Token { get; set; }
    public string Server { get; set; }
    public int Port {get;set;}
    public ulong ChannelId { get; set; }
}
