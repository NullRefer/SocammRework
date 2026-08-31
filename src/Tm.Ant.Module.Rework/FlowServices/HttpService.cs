using System.Diagnostics;
using System.Net.Http;
using System.Text;

using Newtonsoft.Json;

using Tm.Ant.Framework.Models.Events;

internal class HttpService : IHttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITmLogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IEventBus _eventBus;

    public HttpService(IHttpClientFactory httpClientFactory, ITmLogger logger, IConfiguration configuration, IEventBus eventBus)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _eventBus= eventBus;
    }

    public async Task<TResponse> PostJsonAsync<TResponse>(string url, object request, LogCategory logCategory, CancellationToken token = default) where TResponse : TmResponseBase, new()
    {
        using var httpContent = new StringContent(request.ToJson(), Encoding.UTF8, "application/json");
        return await PostInternalAsync<TResponse>(url, httpContent, logCategory, token);
    }

    public async Task<TResponse> PostFormAsync<TResponse>(string url, Dictionary<string, string> request, LogCategory logCategory, CancellationToken token = default) where TResponse : TmResponseBase, new()
    {
        using var httpContent = new FormUrlEncodedContent(request);
        return await PostInternalAsync<TResponse>(url, httpContent, logCategory, token);
    }

    private async Task<TResponse> PostInternalAsync<TResponse>(string url, HttpContent httpContent, LogCategory logCategory, CancellationToken token) where TResponse : TmResponseBase, new()
    {
        var timeoutMs = int.TryParse(_configuration["DefaultHttpTimeout"], out var timeout) ? timeout : 3000;
        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);
        var requestString = FormatJson(await httpContent.ReadAsStringAsync(default));

        string responseString = "";
        int statusCode = -1;
        bool success = false;
        var sw = Stopwatch.StartNew();
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Charset", "Utf-8");
            httpClient.DefaultRequestHeaders.Add("Connection", "close");
            httpClient.DefaultRequestHeaders.Add("Authorization", "123");
            using var response = await httpClient.PostAsync(url, httpContent, cts.Token);
            statusCode = (int)response.StatusCode;
            var rawResponse = await response.Content.ReadAsStringAsync(default);
            responseString = FormatJson(rawResponse);
            success = true;
            return JsonHelper.DeserializeObject<TResponse>(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when post to {url}, request: {requestString}. {Environment.NewLine}Exception: {ex}", logCategory);
            success = false;
            responseString = $"   Exception: {ex.Message}";
            return new TResponse()
            {
                Rc = "9999",
                Rm = responseString
            };
        }
        finally
        {
            sw.Stop();
            var msg = $"[{logCategory}] HttpPost {url} (StatusCode:{statusCode})  TotalMilliseconds:{sw.ElapsedMilliseconds}ms  IsSuccess:{success} {Environment.NewLine}Request:{Environment.NewLine}{requestString}{Environment.NewLine}Response:{Environment.NewLine}{responseString}";
            _logger.LogInformation(msg, logCategory, LogDisplayMode.TOUI);
        }
    }

    public async Task<TResponse> PostAsync<TResponse>(string url, object request, LogCategory logCategory, CancellationToken token = default)
    {
        var timeoutMs = int.TryParse(_configuration["DefaultHttpTimeout"], out var timeout) ? timeout : 3000;
        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

        var requestJsonString = request.ToJson();
        var requestString =  FormatJson(requestJsonString);
        string responseString = "";
        int statusCode = -1;
        bool success = false;
        var sw = Stopwatch.StartNew();

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var stringContent = new StringContent(requestJsonString, Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync(url, stringContent, cts.Token);
            statusCode = (int)response.StatusCode;
            var rawResponse = await response.Content.ReadAsStringAsync(default);
            responseString = FormatJson(rawResponse);
            success = true;
            return JsonHelper.DeserializeObject<TResponse>(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when post to {url}, request: {requestString}. {Environment.NewLine}Exception: {ex}", logCategory);
            success = false;
            responseString = $"   Exception: {ex.Message}";
            return default;
        }
        finally
        {
            sw.Stop();
            var msg = $"[{logCategory}] HttpPost {url} (StatusCode:{statusCode})  TotalMilliseconds:{sw.ElapsedMilliseconds}ms  IsSuccess:{success} {Environment.NewLine}Request:{Environment.NewLine}{requestString}{Environment.NewLine}Response:{Environment.NewLine}{responseString}";
            _logger.LogInformation(msg, logCategory, LogDisplayMode.TOUI);

            _eventBus.Publish(new HttpLogPubSubEvent() { LogMessage = msg });
        }
    }


    /// <summary>
    /// Formats a JSON string with 2-space indentation. Returns the original string if parsing fails.
    /// </summary>
    private static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;
        try
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }
}
public class HttpLogPubSubEvent: PubSubEvent<HttpLogPubSubEvent>
{
    public string LogMessage { get; set; }
}
