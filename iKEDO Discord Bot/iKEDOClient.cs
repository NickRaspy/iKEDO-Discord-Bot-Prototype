using System.Text.Json;
using System.Net.Http.Headers;
using System.Reflection.Metadata;

public class iKEDOClient
{
    private readonly HttpClient _httpClient;
    public iKEDOClient()
    {
        _httpClient = httpClient(GetBearerToken());
    }
    //проверка соединения 
    public async Task TestConnection()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("notifications/SystemEvents?Offset=1000&Count=2000");
        response.EnsureSuccessStatusCode();
    }
    //GET-запрос списка уведомлений
    public async Task<List<SystemEvents>> GetNotificationRequest()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("notifications/SystemEvents?Offset=1000&Count=2000");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SystemEvents>>(jsonResponse);
    }
    //GET-запрос рабочих мест
    public async Task<List<EmployeeWorkplaces>> GetEmployeeWorkplacesRequest()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("administrative/Employees/EmployeeWorkplaces?offset=0&count=2000");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<EmployeeWorkplaces>>(jsonResponse);
    }
    //GET-запрос данных о документе
    public async Task<Documents> GetDocumentRequest(string documentID)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"docstorage/Documents/{documentID}");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Documents>(jsonResponse);
    }
    //GET-запрос данных о сотруднике (пользователе iКЭДО)
    public async Task<KEDOUser> GetEmployeeRequest(string kedouserID)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"staff/Employees/{kedouserID}");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KEDOUser>(jsonResponse);
    }
    //поиск пользователя по телефону среди списка всех сотрудников (пользователей iКЭДО)
    public async Task<bool> FindEmployeeByPhoneRequest(string phoneNumber)
    {
        HttpResponseMessage response = await _httpClient.GetAsync("staff/Employees");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        List<KEDOUser> kusers = JsonSerializer.Deserialize<List<KEDOUser>>(jsonResponse);
        if (kusers.Contains(kusers.Find(x => x.PhoneNumber == phoneNumber))) return true;
        else return false;
    }
    //клиент iКЭДО
    static HttpClient httpClient(string token)
    {
        HttpClient httpClient = new();
        var url = @"https://api-gw.kedo-demo.cloud.astral-dev.ru/api/v3/";
        httpClient.BaseAddress = new Uri(url);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Add("kedo-gateway-token-type", "IntegrationApi");
        return httpClient;
    }
    //получение токена из локального файла
    static string GetBearerToken()
    {
        Tokens newTokens;
        string path = AppDomain.CurrentDomain.BaseDirectory + "/tokens.json";
        string json = File.ReadAllText(path);
        newTokens = JsonSerializer.Deserialize<Tokens>(json);
        return newTokens.Bearer;
    }
}
//данные уведомления
public class SystemEvents
{
    public string EntityId { get; set; }
    public DateTime EventTime { get; set; }
    public string SystemEventType { get; set; }
}
//данные о сотруднике (пользователе iКЭДО)
//P.S: 2 PhoneNumber - костыль, ибо staff/Employees и staff/Employees/{id} имели разные способы хранения номера телефона
public class KEDOUser
{
    public string Id { get; set; }
    public string PhoneNumber { get; set; }
    public List<ContactsData> Contacts { get; set; }
    public class ContactsData
    {
        public string PhoneNumber { get; set; }
    }
}
//данные документа
public class Documents
{
    public string Name { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
    public string Id { get; set; }
    public string DocumentType { get; set; }
}
//данные о месте работы сотрудника
public class EmployeeWorkplaces
{
    public string Id { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
    public Subdivisions Subdivision { get; set; }
    public JobTitles JobTitle { get; set; } 
}
//данные о вакансии
public class JobTitles
{
    public string Id { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
    public string Name { get; set; }
}
//данные о подразделении
public class Subdivisions
{
    public string Id { get; set; }
    public string CreatorId { get; set; }
    public DateTime CreationTime { get; set; }
    public string Name { get; set; }
}
//данные о типе документов

