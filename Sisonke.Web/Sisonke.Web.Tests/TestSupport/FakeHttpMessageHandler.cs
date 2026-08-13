using System.Net;
using System.Text;

namespace Sisonke.Web.Tests.TestSupport;

/// <summary>
/// Records every request it sees and returns a canned response looked up by "METHOD path"
/// (path only, no host/query). Used to test PaystackBillingProvider without any live HTTP call.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = new();
    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> RequestBodies { get; } = [];

    public void SetResponse(HttpMethod method, string path, HttpStatusCode statusCode, string jsonBody) =>
        _responses[$"{method.Method} {path}"] = (statusCode, jsonBody);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        var key = $"{request.Method.Method} {request.RequestUri!.AbsolutePath.TrimStart('/')}";
        if (!_responses.TryGetValue(key, out var response))
        {
            throw new InvalidOperationException($"No fake response configured for {key}.");
        }

        return new HttpResponseMessage(response.StatusCode)
        {
            Content = new StringContent(response.Body, Encoding.UTF8, "application/json")
        };
    }
}
