using System;
using System.Net.Http;

namespace SBot.Projects.DumbRequestManager;

public class WebClient
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    
    public void Init()
    {
        // Ensure we are working with a clean slate
        _httpClient.DefaultRequestHeaders.Clear();
    }

    public void Dispose()
    {
        // Free up allocations
        _httpClient.Dispose();
    }

    public HttpResponseMessage GetRequestSync(string url)
    {
       return _httpClient.GetAsync(url).GetAwaiter().GetResult(); 
    }
}