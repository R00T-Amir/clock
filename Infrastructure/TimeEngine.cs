using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChronoDesk.Core;

namespace ChronoDesk.Infrastructure
{
    public class TimeEngine : ITimeEngine
    {
        private DateTime _syncedUtcTime;
        private long _syncedStopwatchTicks;
        private readonly object _lock = new();

        public TimeEngine()
        {
            // پیش‌فرض: استفاده از ساعت سیستم تا قبل از همگام‌سازی اولیه
            _syncedUtcTime = DateTime.UtcNow;
            _syncedStopwatchTicks = Stopwatch.GetTimestamp();
        }

        public DateTime GetCurrentTime()
        {
            lock (_lock)
            {
                // محاسبه زمان سپری شده با تایمر سخت‌افزاری ویندوز
                long elapsedTicks = Stopwatch.GetTimestamp() - _syncedStopwatchTicks;
                double elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;
                return _syncedUtcTime.AddSeconds(elapsedSeconds);
            }
        }

        public async Task SynchronizeAsync()
        {
            // لیست سرورهای معتبر طبق پرامپت
            string[] servers = { "time.google.com", "time.cloudflare.com", "pool.ntp.org", "time.windows.com" };
            
            foreach (var server in servers)
            {
                if (await TryQueryNtp(server))
                {
                    // اگر همگام‌سازی با یک سرور موفق بود، نیازی به بقیه نیست
                    break; 
                }
            }
        }

        private async Task<bool> TryQueryNtp(string server)
        {
            try
            {
                using var client = new UdpClient();
                client.Client.ReceiveTimeout = 2000; // تایم‌اوت ۲ ثانیه
                await client.ConnectAsync(server, 123);
                
                // بسته استاندارد NTP (48 بایت)
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // لی = 3 (هشدار)، نسخه = 3، حالت = 3 (کلاینت)
                
                var pingStopwatch = Stopwatch.StartNew();
                await client.SendAsync(ntpData, ntpData.Length);
                
                var result = await client.ReceiveAsync();
                pingStopwatch.Stop();

                // استخراج Transmit Timestamp (بایت‌های 40 تا 47)
                ulong intPart = ((ulong)result.Buffer[40] << 24) | ((ulong)result.Buffer[41] << 16) | ((ulong)result.Buffer[42] << 8) | result.Buffer[43];
                ulong fractPart = ((ulong)result.Buffer[44] << 24) | ((ulong)result.Buffer[45] << 16) | ((ulong)result.Buffer[46] << 8) | result.Buffer[47];

                // تبدیل به میلی‌ثانیه
                var millis = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                
                // زمان NTP از 1 ژانویه 1900 شروع می‌شود
                var networkTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(millis);
                
                // تصحیح زمان بر اساس تاخیر شبکه (نصف زمان رفت و برگشت)
                networkTime = networkTime.AddMilliseconds(-pingStopwatch.Elapsed.TotalMilliseconds / 2);

                lock (_lock)
                {
                    _syncedUtcTime = networkTime;
                    _syncedStopwatchTicks = Stopwatch.GetTimestamp();
                }
                
                return true;
            }
            catch
            {
                // در صورت قطعی اینترنت یا خرابی سرور، سرور بعدی امتحان می‌شود
                return false; 
            }
        }
    }
}
