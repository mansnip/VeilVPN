using Application.API;
using Domain.DTOs.Session;
using Domain.Entities.VPN;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Manager
{
    public class VPNSessionManager
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VPNSessionManager> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // کلید کش برای هر سرور
        private const string SESSION_CACHE_KEY = "VPN_SESSION_{0}"; // {0} = Server ID
        private const int SESSION_CACHE_MINUTES = 30; // مدت زمان نگهداری در کش

        public VPNSessionManager(
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<VPNSessionManager> logger = null)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// دریافت یا ایجاد نشست برای سرور VPN
        /// </summary>
        public async Task<Domain.DTOs.Session.VPNSession> GetOrCreateSessionAsync(VPNServer server)
        {
            var cacheKey = string.Format(SESSION_CACHE_KEY, server.Id);

            // تلاش برای دریافت از کش
            if (_cache.TryGetValue(cacheKey, out Domain.DTOs.Session.VPNSession session))
            {
                _logger?.LogDebug($"نشست از کش برای سرور {server.Name} دریافت شد");
                return session;
            }

            // اگر نشست در کش نبود، از سمافور برای جلوگیری از ایجاد همزمان چند نشست استفاده می‌کنیم
            await _semaphore.WaitAsync();
            try
            {
                // بررسی مجدد کش (برای جلوگیری از race condition)
                if (_cache.TryGetValue(cacheKey, out session))
                {
                    return session;
                }

                // ایجاد نشست جدید
                session = await CreateNewSessionAsync(server);
                if (session != null)
                {
                    // ذخیره در کش با زمان انقضای مشخص
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(SESSION_CACHE_MINUTES))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                        .RegisterPostEvictionCallback(OnSessionEvicted);

                    _cache.Set(cacheKey, session, cacheOptions);
                    _logger?.LogInformation($"نشست جدید برای سرور {server.Name} ایجاد و در کش ذخیره شد");
                }

                return session;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// تجدید نشست در صورت منقضی شدن
        /// </summary>
        public async Task<Domain.DTOs.Session.VPNSession> RefreshSessionAsync(VPNServer server, VPNSession session)
        {
            await _semaphore.WaitAsync();
            try
            {
                var newSession = await CreateNewSessionAsync(server);
                if (newSession != null)
                {
                    var cacheKey = string.Format(SESSION_CACHE_KEY, server.Id);
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(SESSION_CACHE_MINUTES))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                        .RegisterPostEvictionCallback(OnSessionEvicted);

                    _cache.Set(cacheKey, newSession, cacheOptions);
                    _logger?.LogInformation($"نشست برای سرور {server.Name} تجدید شد");
                }
                return newSession;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<Domain.DTOs.Session.VPNSession> CreateNewSessionAsync(VPNServer server)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("VPNApi");

                // آماده‌سازی اطلاعات لاگین
                var loginData = new
                {
                    username = server.ApiUsername,
                    password = server.ApiPassword
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    Encoding.UTF8,
                    "application/json"
                );

                var loginUrl = $"{server.ApiUrl.TrimEnd('/')}/login";
                var response = await client.PostAsync(loginUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var cookies = response.Headers
                        .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        .SelectMany(h => h.Value)
                        .ToList();

                    if (cookies.Any())
                    {
                        // ذخیره کوکی‌ها در هدرهای HttpClient
                        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));

                        return new Domain.DTOs.Session.VPNSession
                        {
                            Client = client,
                            Server = server,
                            CreatedAt = DateTime.UtcNow,
                            Cookies = cookies
                        };
                    }
                }

                _logger?.LogError($"خطا در ایجاد نشست برای سرور {server.Name}: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"خطا در ایجاد نشست برای سرور {server.Name}");
                return null;
            }
        }

        private void OnSessionEvicted(object key, object value, EvictionReason reason, object state)
        {
            var session = value as VPNSession;
            if (session != null)
            {
                _logger?.LogInformation($"نشست برای سرور {session.Server.Name} از کش حذف شد. دلیل: {reason}");
                session.Client?.Dispose();
            }
        }
    }

}
