using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/jira")]
[Authorize]
public partial class JiraController : ControllerBase
{
    // Custom fields on jira-ticket.misa.vn holding the customer-support details.
    private const string ProblemNoteField = "customfield_10206";
    private const string OrganizationField = "customfield_10323";
    private const string ContactNameField = "customfield_10328";
    private const string ContactPhoneField = "customfield_10329";
    private const string ContactEmailField = "customfield_10300";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<JiraController> _logger;

    public JiraController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<JiraController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpGet("issues/{key}")]
    public async Task<ActionResult<JiraIssueResponse>> GetIssue(string key)
    {
        key = key.Trim().ToUpperInvariant();
        if (!IssueKeyRegex().IsMatch(key))
            return BadRequest(new { message = "Mã Jira không hợp lệ." });

        var section = _config.GetSection("Jira");
        var baseUrl = section["BaseUrl"]?.TrimEnd('/');
        var token = section["Token"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Chưa cấu hình Jira trên server." });

        var fields = string.Join(',', "summary", "description",
            ProblemNoteField, OrganizationField, ContactNameField, ContactPhoneField, ContactEmailField);
        using var message = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/rest/api/2/issue/{key}?fields={fields}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory.CreateClient().SendAsync(message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Jira request for {Key} failed", key);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Không kết nối được tới Jira." });
        }

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return NotFound(new { message = $"Không tìm thấy ticket {key}." });
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Jira returned {Status} for {Key}: {Body}", (int)response.StatusCode, key, body);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = $"Jira trả về lỗi {(int)response.StatusCode}." });
        }

        var issueFields = JsonDocument.Parse(body).RootElement.GetProperty("fields");
        var summary = GetString(issueFields, "summary") ?? key;
        var url = $"{baseUrl}/browse/{key}";

        var description = new StringBuilder();
        AppendLine(description, "Tổ chức", GetString(issueFields, OrganizationField));
        AppendLine(description, "Liên hệ", GetString(issueFields, ContactNameField));
        AppendLine(description, "SĐT", GetString(issueFields, ContactPhoneField));
        AppendLine(description, "Email", GetString(issueFields, ContactEmailField));
        var note = GetString(issueFields, ProblemNoteField) ?? GetString(issueFields, "description");
        if (!string.IsNullOrWhiteSpace(note))
            description.Append('\n').Append(note.Trim()).Append('\n');
        description.Append('\n').Append("Jira: ").Append(url);

        return Ok(new JiraIssueResponse(key, $"[{key}] {summary.Trim()}", description.ToString().Trim(), url));
    }

    private static string? GetString(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static void AppendLine(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.Append(label).Append(": ").Append(value.Trim()).Append('\n');
    }

    [GeneratedRegex(@"^[A-Z][A-Z0-9_]+-\d+$")]
    private static partial Regex IssueKeyRegex();
}
