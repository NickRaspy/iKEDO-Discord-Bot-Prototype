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
    public LastEntityNotification ldn;
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
        if (_ulclient.UserDatas != null)
        {
            //получение списка уведомлений
            List<SystemEvents> events = await _kedoclient.GetNotificationRequest();
            //проверка на обновление
            if (ldn.EntityId != events.Last().EntityId || ldn.EntityState != events.Last().SystemEventType)
            {
                ldn.EntityId = events.Last().EntityId;
                ldn.EntityState = events.Last().SystemEventType;
                //сохранение последних данных уведомления в случае отключки бота
                SetLDN(ldn);
                KEDOUser kuser = new KEDOUser();
                EntityDataSet entity =await NewEntity(events.Last().EntityId, events.Last().SystemEventType);
                //проверка на пустой объект
                if (entity != null) 
                {
                    //получение данных пользователя iКЭДО
                    kuser = await _kedoclient.GetEmployeeRequest(entity.CreatorId);
                    //поиск зарегистрированного пользователя по номеру телефона
                    if (_ulclient.UserDatas.Contains(_ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber)) && entity != null)
                    {
                        if (_ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber).GetNotificationPing)
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
                                    .WithDescription($"{events.Last().SystemEventType}")
                                    .WithColor(Color.Blue)
                                    .WithCurrentTimestamp();
                                //отправка уведомления
                                await user.SendMessageAsync(embed: embedBuilder.Build());
                            }
                            //если с пользователем нет связи, в консоль придет сообщение ниже
                            catch (HttpException)
                            {
                                Console.WriteLine($"Пользователю с ID {userID} невозможно отправить уведомление");
                            }
                        }
                        //проверка на отсутствие документа
                        if (_ulclient.UserDatas.Find(x => x.Phone == kuser.Contacts[0].PhoneNumber).Entities.Find(x => x.Id == events.Last().EntityId) == null)
                        {
                            await _ulclient.AddDocument(kuser.Contacts[0].PhoneNumber, entity);
                        }
                    }
                }
            }
        }
        //задержка, можно изменить по своему усмотрению
        await Task.Delay(1);
        //рекурсия
        await UpdateNotification();
    }
    //получение данных последнего зафиксированного до обновления уведомления
    static LastEntityNotification GetLDN()
    {
        LastEntityNotification ldn = new();
        string path = AppDomain.CurrentDomain.BaseDirectory + "/lastdocumentnotification.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ldn = JsonSerializer.Deserialize<LastEntityNotification>(json);
        }
        return ldn;
    }
    //установка новых данных уведомления
    static void SetLDN(LastEntityNotification ldn)
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "/lastdocumentnotification.json";
        string json = JsonSerializer.Serialize(ldn);
        File.WriteAllText(path, json);
    }
    //получение нужных данных исходя из EventType
    private async Task<EntityDataSet> NewEntity(string id, string type)
    {
        switch (type)
        {
            case "DocumentOnRoute":
            case "DocumentSigned":
            case "DocumentRejected":
            case "DocumentWithdrawn":
            case "DocumentMarkedAsCompleted":
                Documents docs = await _kedoclient.GetDocumentRequest(id);
                return new EntityDataSet {Id = id, CreatorId = docs.CreatorId, DateTime = docs.CreationTime, Name = docs.DocumentType, State = type, Type = docs.Name };
            case "EmployeeWorkplaceAdded":
            case "EmployeeWorkplaceUpdated":
                List<EmployeeWorkplaces> ew = await _kedoclient.GetEmployeeWorkplacesRequest();
                return new EntityDataSet {Id = id, CreatorId = ew.Find(x => x.Id == id).CreatorId, DateTime = ew.Find(x => x.Id == id).CreationTime, Name = ew.Find(x => x.Id == id).Subdivision.Name + " " + ew.Find(x => x.Id == id).JobTitle.Name, State = type, Type = "Рабочее место сотрудника"};
            default:
                return null;
        }
    }
    //данные последнего уведомления
    public class LastEntityNotification
    {
        public string EntityId { get; set; }
        public string EntityState { get; set; }
    }
}
public class EntityDataSet
{
    public string Id { get; set; }
    public string CreatorId { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string Type { get; set; }
    public DateTime DateTime { get; set; }
}