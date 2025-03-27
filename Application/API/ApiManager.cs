using Domain.DTOs.Session;
using Domain.Entities.VPN;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Application.Manager;
using Domain.DTOs.VPN;

namespace Application.API
{
    public class ApiManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VPNSessionManager _sessionManager;
        private readonly ILogger<ApiManager> _logger;

        public ApiManager(
            IHttpClientFactory httpClientFactory,
            VPNSessionManager sessionManager,
            ILogger<ApiManager> logger = null)
        {
            _httpClientFactory = httpClientFactory;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        /// <summary>
        /// ارسال درخواست به API با استفاده از مدیر نشست
        /// </summary>
        private async Task<ApiResponse<T>> SendRequestAsync<T>(VPNServer server, HttpMethod method, string endpoint, object data = null)
        {
            try
            {
                // دریافت یا ایجاد نشست
                var session = await _sessionManager.GetOrCreateSessionAsync(server);
                if (session == null)
                {
                    return new ApiResponse<T>
                    {
                        Success = false,
                        Message = "خطا در ایجاد نشست"
                    };
                }

                // ساخت آدرس کامل
                string fullUrl = $"{server.ApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
                _logger?.LogInformation($"ارسال درخواست {method} به {fullUrl}");

                // ایجاد درخواست
                var request = new HttpRequestMessage(method, fullUrl);

                // افزودن محتوا در صورت نیاز
                if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    var jsonContent = JsonSerializer.Serialize(data);
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }

                // ارسال درخواست
                var response = await session.Client.SendAsync(request);

                // بررسی وضعیت پاسخ
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    T result = default;

                    if (!string.IsNullOrEmpty(content))
                    {
                        result = JsonSerializer.Deserialize<T>(content);
                    }

                    return new ApiResponse<T>
                    {
                        Success = true,
                        Data = result,
                        Message = "عملیات با موفقیت انجام شد"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                         response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning($"نشست منقضی شده است، تلاش برای تجدید نشست: {response.StatusCode}");

                    // تلاش مجدد با نشست جدید
                    session = await _sessionManager.RefreshSessionAsync(server, session);
                    if (session == null)
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = "خطا در تجدید نشست"
                        };
                    }

                    // ارسال مجدد درخواست
                    response = await session.Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        T result = default;

                        if (!string.IsNullOrEmpty(content))
                        {
                            result = JsonSerializer.Deserialize<T>(content);
                        }

                        return new ApiResponse<T>
                        {
                            Success = true,
                            Data = result,
                            Message = "عملیات با تلاش مجدد با موفقیت انجام شد"
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError($"خطا در درخواست: {response.StatusCode} - {errorContent}");

                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"خطا در درخواست: {response.StatusCode} - {errorContent}"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطا در ارسال درخواست");
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = $"خطای سیستمی: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// افزودن کاربر جدید به سرور VPN
        /// </summary>
        public async Task<(bool Success, string ConnectionUrl, string Message)> AddClient(VPNServer server, string email, string idVpn, double trafficLimit = 0, int? expiryDays = null, bool isActive = true, int limitIp = 0)
        {
            try
            {
                _logger?.LogInformation($"شروع افزودن کاربر جدید: {email} به سرور {server?.Name}");

                // بررسی پارامترهای ورودی
                if (server == null)
                    return (false, null, "اطلاعات سرور نامعتبر است");

                if (string.IsNullOrEmpty(email))
                    return (false, null, "نام کاربری/ایمیل نمی‌تواند خالی باشد");

                // ایجاد شناسه‌های منحصر به فرد
                string vpnId = idVpn;
                string subId = GenerateRandomString(16);

                // محاسبه زمان انقضا
                long expiryTimeMillis = 0;
                if (expiryDays.HasValue && expiryDays.Value > 0)
                {
                    expiryTimeMillis = DateTimeOffset.UtcNow.AddDays(expiryDays.Value).ToUnixTimeMilliseconds();
                    _logger?.LogInformation($"زمان انقضا محاسبه شد: {expiryDays.Value} روز ({expiryTimeMillis})");
                }

                // تبدیل گیگابایت به بایت
                long trafficLimitBytes = 0;
                if (trafficLimit > 0)
                {
                    trafficLimitBytes = (long)(trafficLimit * 1073741824L);
                    _logger?.LogInformation($"حجم ترافیک محاسبه شد: {trafficLimit} GB ({trafficLimitBytes} bytes)");
                }

                // آماده‌سازی داده‌های کاربر
                var clientData = new
                {
                    id = vpnId,
                    email = email.ToLower(),
                    limitIp = limitIp,
                    totalGB = trafficLimitBytes,
                    expiryTime = expiryTimeMillis,
                    enable = isActive,
                    tgId = "",
                    subId = subId,
                    flow = "",
                    reset = 0
                };

                var settings = new { clients = new[] { clientData } };
                var requestData = new
                {
                    id = server.InboundID,
                    settings = JsonSerializer.Serialize(settings)
                };

                _logger?.LogInformation($"درخواست افزودن کاربر با ID: {vpnId}, subId: {subId}");

                // ارسال درخواست
                var response = await SendRequestAsync<ApiResponseInternal>(
                    server,
                    HttpMethod.Post,
                    "panel/inbound/addClient",
                    requestData
                );

                if (response.Success && response.Data?.Success == true)
                {
                    // ساخت لینک اشتراک
                    string connectionUrl = ""; // $"{server.SubscriptionUrl}/{subId}";
                    _logger?.LogInformation($"کاربر با موفقیت اضافه شد. لینک اشتراک: {connectionUrl}");
                    return (true, connectionUrl, "کاربر با موفقیت اضافه شد");
                }

                string errorMessage = response.Success ? response.Data?.Msg : response.Message;
                _logger?.LogError($"خطا در افزودن کاربر: {errorMessage}");
                return (false, null, $"خطا در افزودن کاربر: {errorMessage}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطای سیستمی در افزودن کاربر");
                return (false, null, $"خطای سیستمی: {ex.Message}");
            }
        }

        /// <summary>
        /// تولید رشته تصادفی با طول مشخص
        /// </summary>
        private static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// کلاس داخلی برای پاسخ API
        /// </summary>
        private class ApiResponseInternal
        {
            /// <summary>
            /// نتیجه عملیات
            /// </summary>
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            /// <summary>
            /// پیام سیستم
            /// </summary>
            [JsonPropertyName("msg")]
            public string Msg { get; set; }

            /// <summary>
            /// داده‌های بازگشتی (در صورت وجود)
            /// </summary>
            [JsonPropertyName("obj")]
            public object Obj { get; set; }
        }

        /// <summary>
        /// دریافت لیست Inbound ها و کاربران از سرور VPN
        /// </summary>
        /// <param name="server">اطلاعات سرور VPN</param>
        /// <returns>لیست Inbound ها به همراه وضعیت موفقیت عملیات</returns>
        public async Task<ApiResponse<InboundListResponse>> GetInbounds(VPNServer server)
        {
            try
            {
                _logger?.LogInformation($"درخواست دریافت لیست Inbound ها از سرور {server?.Name}");

                // بررسی پارامترهای ورودی
                if (server == null)
                {
                    return new ApiResponse<InboundListResponse>
                    {
                        Success = false,
                        Message = "اطلاعات سرور نامعتبر است",
                        Data = null
                    };
                }

                // دریافت نشست معتبر
                var session = await _sessionManager.GetOrCreateSessionAsync(server);
                if (session == null)
                {
                    return new ApiResponse<InboundListResponse>
                    {
                        Success = false,
                        Message = "خطا در ایجاد نشست",
                        Data = null
                    };
                }

                // ساخت آدرس کامل
                string fullUrl = $"{server.ApiUrl.TrimEnd('/')}/panel/api/inbounds/list";
                _logger?.LogInformation($"ارسال درخواست GET به {fullUrl}");

                // ایجاد درخواست
                var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                request.Headers.Add("Accept", "application/json");

                // ارسال درخواست
                var response = await session.Client.SendAsync(request);

                // بررسی وضعیت پاسخ
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.LogDebug($"پاسخ دریافتی: {content}");

                    // تبدیل به مدل داده
                    var result = JsonSerializer.Deserialize<InboundListResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new ApiResponse<InboundListResponse>
                    {
                        Success = true,
                        Data = result,
                        Message = "دریافت لیست Inbound ها با موفقیت انجام شد"
                    };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                         response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning($"نشست منقضی شده است، تلاش برای تجدید نشست: {response.StatusCode}");

                    // تلاش مجدد با نشست جدید
                    session = await _sessionManager.RefreshSessionAsync(server, session);
                    if (session == null)
                    {
                        return new ApiResponse<InboundListResponse>
                        {
                            Success = false,
                            Message = "خطا در تجدید نشست",
                            Data = null
                        };
                    }

                    // ارسال مجدد درخواست
                    response = await session.Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var result = JsonSerializer.Deserialize<InboundListResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return new ApiResponse<InboundListResponse>
                        {
                            Success = true,
                            Data = result,
                            Message = "دریافت لیست Inbound ها با تلاش مجدد با موفقیت انجام شد"
                        };
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError($"خطا در دریافت لیست Inbound ها: {response.StatusCode} - {errorContent}");

                return new ApiResponse<InboundListResponse>
                {
                    Success = false,
                    Message = $"خطا در دریافت لیست Inbound ها: {response.StatusCode} - {errorContent}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطا در دریافت لیست Inbound ها");
                return new ApiResponse<InboundListResponse>
                {
                    Success = false,
                    Message = $"خطای سیستمی در دریافت لیست Inbound ها: {ex.Message}",
                    Data = null
                };
            }
        }

        /// <summary>
        /// به‌روزرسانی اطلاعات کاربر موجود در سرور VPN
        /// </summary>
        /// <param name="server">اطلاعات سرور VPN</param>
        /// <param name="vpnId">شناسه کاربر VPN (uuid)</param>
        /// <param name="email">نام کاربری/ایمیل کاربر</param>
        /// <param name="trafficLimit">محدودیت ترافیک جدید (گیگابایت)</param>
        /// <param name="expiryDays">مدت اعتبار جدید از امروز (روز)</param>
        /// <param name="isActive">وضعیت فعال بودن</param>
        /// <param name="limitIp">محدودیت تعداد آی‌پی متصل همزمان (0 برای نامحدود)</param>
        /// <param name="subId">شناسه اشتراک (اختیاری)</param>
        /// <returns>نتیجه عملیات به همراه پیام مناسب</returns>
        public async Task<(bool Success, string Message)> UpdateClient(VPNServer server, string vpnId, string email, double trafficLimit = 0, int? expiryDays = null, bool isActive = true, int limitIp = 0, string subId = null)
        {
            try
            {
                _logger?.LogInformation($"شروع به‌روزرسانی کاربر: {email} با شناسه {vpnId} در سرور {server?.Name}");

                // بررسی پارامترهای ورودی
                if (server == null)
                    return (false, "اطلاعات سرور نامعتبر است");

                if (string.IsNullOrEmpty(vpnId))
                    return (false, "شناسه کاربر VPN نمی‌تواند خالی باشد");

                if (string.IsNullOrEmpty(email))
                    return (false, "نام کاربری/ایمیل نمی‌تواند خالی باشد");

                // دریافت نشست معتبر
                var session = await _sessionManager.GetOrCreateSessionAsync(server);
                if (session == null)
                {
                    return (false, "خطا در ایجاد نشست");
                }

                // محاسبه زمان انقضا به میلی‌ثانیه از مبدأ Unix
                long expiryTimeMillis = 0;
                if (expiryDays.HasValue && expiryDays.Value > 0)
                {
                    expiryTimeMillis = DateTimeOffset.UtcNow.AddDays(expiryDays.Value).ToUnixTimeMilliseconds();
                    _logger?.LogInformation($"زمان انقضای جدید محاسبه شد: {expiryDays.Value} روز ({expiryTimeMillis})");
                }

                // تبدیل گیگابایت به بایت برای محدودیت ترافیک
                long trafficLimitBytes = 0;
                if (trafficLimit > 0)
                {
                    trafficLimitBytes = (long)(trafficLimit * 1073741824L); // 1 GB = 1073741824 bytes
                    _logger?.LogInformation($"حجم ترافیک جدید محاسبه شد: {trafficLimit} GB ({trafficLimitBytes} bytes)");
                }

                // اگر subId خالی است، یک مقدار تصادفی ایجاد کنیم
                if (string.IsNullOrEmpty(subId))
                {
                    subId = GenerateRandomString(16);
                }

                // ایجاد داده‌های به‌روزرسانی کاربر
                var clientData = new
                {
                    id = vpnId,
                    flow = "",
                    email = email.ToLower(),
                    limitIp = limitIp,
                    totalGB = trafficLimitBytes,
                    expiryTime = expiryTimeMillis,
                    enable = isActive,
                    tgId = "",
                    subId = subId,
                    reset = 0
                };

                // ایجاد تنظیمات با شامل کردن کاربر به‌روزرسانی شده در آرایه clients
                var settings = new
                {
                    clients = new[] { clientData }
                };

                // ایجاد درخواست نهایی
                var clientSettings = new
                {
                    id = server.InboundID,
                    settings = JsonSerializer.Serialize(settings)
                };

                // ساخت آدرس کامل
                string fullUrl = $"{server.ApiUrl.TrimEnd('/')}/panel/api/inbounds/updateClient/{vpnId}";
                _logger?.LogInformation($"ارسال درخواست به: {fullUrl}");

                // ایجاد درخواست
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Headers.Add("Accept", "application/json");

                // افزودن محتوا
                var jsonContent = JsonSerializer.Serialize(clientSettings);
                _logger?.LogInformation($"درخواست به‌روزرسانی کاربر: {jsonContent}");
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // ارسال درخواست
                var response = await session.Client.SendAsync(request);

                // بررسی وضعیت پاسخ
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogInformation($"پاسخ دریافتی: {responseContent}");

                    // تلاش برای پارس کردن پاسخ
                    try
                    {
                        var result = JsonSerializer.Deserialize<ApiResponseInternal>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result.Success)
                        {
                            _logger?.LogInformation("کاربر با موفقیت به‌روزرسانی شد");
                            return (true, "اطلاعات کاربر با موفقیت به‌روزرسانی شد");
                        }
                        else
                        {
                            _logger?.LogError($"خطا از سمت سرور: {result.Msg}");
                            return (false, $"خطا از سمت سرور: {result.Msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "خطا در پردازش پاسخ");
                        return (false, $"خطا در پردازش پاسخ: {ex.Message}");
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                         response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning($"نشست منقضی شده است، تلاش برای تجدید نشست: {response.StatusCode}");

                    // تلاش مجدد با نشست جدید
                    session = await _sessionManager.RefreshSessionAsync(server, session);
                    if (session == null)
                    {
                        return (false, "خطا در تجدید نشست");
                    }

                    // ارسال مجدد درخواست
                    response = await session.Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation($"پاسخ دریافتی پس از تجدید نشست: {responseContent}");

                        try
                        {
                            var result = JsonSerializer.Deserialize<ApiResponseInternal>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (result.Success)
                            {
                                return (true, "اطلاعات کاربر با تلاش مجدد با موفقیت به‌روزرسانی شد");
                            }
                            else
                            {
                                return (false, $"خطا پس از تلاش مجدد: {result.Msg}");
                            }
                        }
                        catch (Exception ex)
                        {
                            return (false, $"خطا در پردازش پاسخ پس از تلاش مجدد: {ex.Message}");
                        }
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError($"خطا در به‌روزرسانی کاربر: {response.StatusCode} - {errorContent}");
                return (false, $"خطا در به‌روزرسانی کاربر: {response.StatusCode} - {errorContent}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطای سیستمی در به‌روزرسانی کاربر");
                return (false, $"خطای سیستمی: {ex.Message}");
            }
        }
        /// <summary>
        /// ریست کردن ترافیک مصرفی کاربر در سرور VPN
        /// </summary>
        /// <param name="server">اطلاعات سرور VPN</param>
        /// <param name="email">ایمیل/نام کاربری کاربر</param>
        /// <returns>نتیجه عملیات ریست ترافیک</returns>
        public async Task<(bool Success, string Message)> ResetClientTraffic(VPNServer server, string email)
        {
            try
            {
                _logger?.LogInformation($"شروع ریست ترافیک کاربر: {email} در سرور {server?.Name}");

                // بررسی پارامترهای ورودی
                if (server == null)
                    return (false, "اطلاعات سرور نامعتبر است");

                if (string.IsNullOrEmpty(email))
                    return (false, "نام کاربری/ایمیل نمی‌تواند خالی باشد");

                // دریافت نشست معتبر
                var session = await _sessionManager.GetOrCreateSessionAsync(server);
                if (session == null)
                {
                    return (false, "خطا در ایجاد نشست");
                }

                // ساخت آدرس کامل برای ریست ترافیک
                string fullUrl = $"{server.ApiUrl.TrimEnd('/')}/panel/api/inbounds/{server.InboundID}/resetClientTraffic/{email}";
                _logger?.LogInformation($"ارسال درخواست ریست ترافیک به: {fullUrl}");

                // ایجاد درخواست POST
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Headers.Add("Accept", "application/json");

                // ارسال درخواست
                var response = await session.Client.SendAsync(request);

                // بررسی وضعیت پاسخ
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogInformation($"پاسخ دریافتی: {responseContent}");

                    // تلاش برای پارس کردن پاسخ
                    try
                    {
                        var result = JsonSerializer.Deserialize<ApiResponseInternal>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result.Success)
                        {
                            _logger?.LogInformation("ترافیک کاربر با موفقیت ریست شد");
                            return (true, "ترافیک کاربر با موفقیت ریست شد");
                        }
                        else
                        {
                            _logger?.LogError($"خطا از سمت سرور: {result.Msg}");
                            return (false, $"خطا از سمت سرور: {result.Msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "خطا در پردازش پاسخ");
                        return (false, $"خطا در پردازش پاسخ: {ex.Message}");
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                         response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning($"نشست منقضی شده است، تلاش برای تجدید نشست: {response.StatusCode}");

                    // تلاش مجدد با نشست جدید
                    session = await _sessionManager.RefreshSessionAsync(server, session);
                    if (session == null)
                    {
                        return (false, "خطا در تجدید نشست");
                    }

                    // ارسال مجدد درخواست
                    response = await session.Client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger?.LogInformation($"پاسخ دریافتی پس از تجدید نشست: {responseContent}");

                        try
                        {
                            var result = JsonSerializer.Deserialize<ApiResponseInternal>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (result.Success)
                            {
                                return (true, "ترافیک کاربر با تلاش مجدد با موفقیت ریست شد");
                            }
                            else
                            {
                                return (false, $"خطا پس از تلاش مجدد: {result.Msg}");
                            }
                        }
                        catch (Exception ex)
                        {
                            return (false, $"خطا در پردازش پاسخ پس از تلاش مجدد: {ex.Message}");
                        }
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError($"خطا در ریست ترافیک کاربر: {response.StatusCode} - {errorContent}");
                return (false, $"خطا در ریست ترافیک کاربر: {response.StatusCode} - {errorContent}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطای سیستمی در ریست ترافیک کاربر");
                return (false, $"خطای سیستمی: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// کلاس مدل پاسخ لیست Inbound ها
    /// </summary>
    public class InboundListResponse
    {
        /// <summary>
        /// وضعیت موفقیت
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// پیام
        /// </summary>
        [JsonPropertyName("msg")]
        public string Message { get; set; }

        /// <summary>
        /// لیست Inbound ها
        /// </summary>
        [JsonPropertyName("obj")]
        public List<Inbound> Inbounds { get; set; }
    }



    /// <summary>
    /// کلاس پاسخ استاندارد API
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// وضعیت موفقیت عملیات
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// پیام خطا یا موفقیت
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// داده‌های بازگشتی
        /// </summary>
        public T Data { get; set; }
    }
}