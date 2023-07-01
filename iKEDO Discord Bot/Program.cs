using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using System.Text.Json;

public class Program
{
    private readonly IServiceProvider _services;
    private readonly Tokens _tokens;
    public Program()
    {
        _tokens = SetTokens();
        _services = CreateServices();
    }
    static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();
    //Dependency Injection
    static IServiceProvider CreateServices()
    {
        var config = new DiscordSocketConfig()
        {
            
        };
        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<iKEDOClient>()
            .AddSingleton<SlashCommandHandler>()
            .AddSingleton<UserLoginClient>()
            .AddSingleton<NotificationUpdater>();

        return collection.BuildServiceProvider();
    }
    async Task RunAsync(string[] args)
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        var kedoClient = _services.GetRequiredService<iKEDOClient>();
        var notificationUpdater = _services.GetRequiredService<NotificationUpdater>();
        //проверка подключения
        try
        {
            await kedoClient.TestConnection();
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"Обнаружена ошибка при подключении к серверу:{kedoClient.statusCode}");
            Environment.Exit(0);
        }
        var slashHandler = _services.GetRequiredService<SlashCommandHandler>();
        //логи подключения
        client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };
        //запуск задач после окончательной подготовки бота, таких как билдинг слеш комманд и рекурсивная проверка и высылка уведомлений
        client.Ready += slashHandler.Slash_Ready;
        client.Ready += notificationUpdater.UpdateNotification;
        //установка эксекуторов, выполняющий слеш и меню комманды
        client.SelectMenuExecuted += slashHandler.MenuOptionExecute;
        client.SlashCommandExecuted += slashHandler.SlashCommandExecute;
        //подключение к боту дискорда
        await client.LoginAsync(TokenType.Bot, _tokens.Discord);
        await client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }
    //установка токенов
    //сохраняются в новый файл (гит игнор прописан)
    static Tokens SetTokens()
    {
        Tokens newTokens = new Tokens();
        string path = AppDomain.CurrentDomain.BaseDirectory + "/tokens.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            newTokens = JsonSerializer.Deserialize<Tokens>(json);
        }
        else
        {
            Console.WriteLine("Insert Bearer Token"); newTokens.Bearer = Console.ReadLine();
            Console.WriteLine("Insert Discord Token"); newTokens.Discord = Console.ReadLine();
            string json = JsonSerializer.Serialize(newTokens);
            File.WriteAllText(path, json);
        }
        return newTokens;
    }
}
//токены
public class Tokens
{
    public string Bearer { get; set; }
    public string Discord { get; set; }
}