using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

public class NotificationUpdater
{
    private readonly UserLoginClient _ulclient;
    private readonly iKEDOClient _kedoclient;
    private readonly DiscordSocketClient _client;
    public LastDocumentNotification ldn;
    public NotificationUpdater(UserLoginClient ulclient, iKEDOClient kedoclient, DiscordSocketClient client)
    {
        _ulclient = ulclient;
        _kedoclient = kedoclient;
        _client = client;
        ldn = GetLDN();
    }

    public async Task UpdateNotification()
    {
        //проверка на наличие пользователей
        if(_ulclient.UserDatas != null)
        {
            //получение списка уведомлений
            await _kedoclient.GetNotificationRequest();
            //проверка на обновление
            if (ldn.DocumentID != _kedoclient.events.Last().EntityId || ldn.DocumentType != _kedoclient.events.Last().SystemEventType)
            {
                ldn.DocumentID = _kedoclient.events.Last().EntityId;
                ldn.DocumentType = _kedoclient.events.Last().SystemEventType;
                //сохранение последних данных уведомления в случае отключки бота
                SetLDN(ldn);
                //получение данных документа
                Documents docs = await _kedoclient.GetDocumentRequest(ldn.DocumentID);
                //получение данных пользователя iКЭДО
                KEDOUser kuser = await _kedoclient.GetEmployeeRequest(docs.CreatorId);
                //поиск зарегистрированного пользователя по номеру телефона
                if (_ulclient.UserDatas.Contains(_ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber)))
                {
                    ulong userID = _ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber).UserID;
                    //попытка отправить пользователю сообщение
                    try
                    {
                        //связь с пользователем дискорда
                        IUser user = await _client.GetUserAsync(userID);
                        //эмбед-уведомление
                        var embedBuilder = new EmbedBuilder()
                            .WithAuthor("iКЭДО")
                            .WithTitle("Вы получили уведомление!")
                            .WithDescription($"{ldn.DocumentType}")
                            .WithColor(Color.Blue)
                            .WithCurrentTimestamp();
                        //отправка уведомления
                        await user.SendMessageAsync(embed: embedBuilder.Build());
                        //проверка на отсутствие документа
                        if(_ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber).Docs.Find(x => x.Id == ldn.DocumentID) == null)
                        {
                            await _ulclient.AddDocument(kuser.Contacts[0].PhoneNumber, docs);
                        }
                        //если документ будет обнаружен - сменится его состояние
                        else
                        {
                           _ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber).Docs.Find(x => x.Id == ldn.DocumentID).DocumentType = ldn.DocumentType;
                        }
                    }
                    //если с пользователем нет связи, в консоль придет сообщение ниже
                    catch (HttpException)
                    {
                        Console.WriteLine($"Пользователю с ID {userID} невозможно отправить уведомление");
                    }

                }
            }
        }
        //задержка, можно изменить по своему усмотрению
        await Task.Delay(1000);
        //рекурсия
        await UpdateNotification();
    }
    //получение данных последнего зафиксированного до обновления уведомления
    static LastDocumentNotification GetLDN()
    {
        LastDocumentNotification ldn = new();
        string path = AppDomain.CurrentDomain.BaseDirectory + "/lastdocumentnotification.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ldn = JsonSerializer.Deserialize<LastDocumentNotification>(json);
        }
        return ldn;
    }
    //установка новых данных уведомления
    static void SetLDN(LastDocumentNotification ldn)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "/lastdocumentnotification.json";
        string json = JsonSerializer.Serialize(ldn);
        File.WriteAllText(path, json);
    }
    //данные последнего уведомления
    public class LastDocumentNotification
    {
        public string DocumentID { get; set; }
        public string DocumentType { get; set; }
    }
}