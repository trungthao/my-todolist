using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/emis")]
[Authorize]
public class EmisController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<EmisController> _logger;

    public EmisController(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<EmisController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login-setup")]
    public async Task<IActionResult> LoginSetup(EmisLoginSetupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Thiếu tài khoản cần thiết lập." });
        if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
            return BadRequest(new { message = "Thiếu mã xác thực 2 lớp." });

        var section = _config.GetSection("EmisLoginSetup");
        var url = section["Url"];
        var password = section["Password"];
        var supportUsername = section["SupportUsername"];
        if (new[] { url, password, supportUsername }.Any(string.IsNullOrWhiteSpace))
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Chưa cấu hình EmisLoginSetup trên server." });

        using var message = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new
            {
                username = NormalizeUsername(request.Username),
                password,
                supportUsername,
                twoFactorCode = request.TwoFactorCode.Trim()
            })
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClientFactory.CreateClient().SendAsync(message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "EMIS login setup request failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Không kết nối được tới EMIS." });
        }

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("EMIS login setup returned {Status}: {Body}", (int)response.StatusCode, body);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = $"EMIS trả về lỗi {(int)response.StatusCode}.", detail = body });
        }

        return Content(string.IsNullOrWhiteSpace(body) ? "{}" : body, "application/json");
    }

    // Phone numbers come from free text ("0912 345 678", "+84 912-345-678"); EMIS expects "0912345678".
    private static string NormalizeUsername(string raw)
    {
        var value = raw.Trim();
        if (value.Contains('@')) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return value.StartsWith("+84") ? "0" + digits[2..] : digits;
    }
}
