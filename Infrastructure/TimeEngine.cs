using System;
using System.Diagnostics;
using System.Net;
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
                long elapsedTicks = Stopwatch.GetTimestamp() - _syncedStopwatchTicks;
                double elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;
                return _syncedUtcTime.AddSeconds(elapsedSeconds);
            }
        }

        public async Task SynchronizeAsync()
        {
            string[] servers = { "time.google.com", "time.cloudflare.com", "pool.ntp.org", "time.windows.com" };
            
            foreach (var server in servers)
            {
                if (await TryQueryNtp(server))
                {
                    break; 
                }
            }
        }

        private async Task<bool> TryQueryNtp(string server)
        {
            try
            {
                // رفع خطای CS1929: دریافت IP سرور به جای اتصال مستقیم با نام دامنه
                var addresses = await Dns.GetHostAddressesAsync(server);
                if (addresses.Length == 0) return false;
                
                var endPoint = new IPEndPoint(addresses[0], 123);

                using var client = new UdpClient();
                client.Client.ReceiveTimeout = 2000; 
                
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; 
                
                var pingStopwatch = Stopwatch.StartNew();
                
                // ارسال مستقیم به EndPoint
                await client.SendAsync(ntpData, ntpData.Length, endPoint);
                var result = await client.ReceiveAsync();
                pingStopwatch.Stop();

                ulong intPart = ((ulong)result.Buffer[40] << 24) | ((ulong)result.Buffer[41] << 16) | ((ulong)result.Buffer[42] << 8) | result.Buffer[43];
                ulong fractPart = ((ulong)result.Buffer[44] << 24) | ((ulong)result.Buffer[45] << 16) | ((ulong)result.Buffer[46] << 8) | result.Buffer[47];

                var millis = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(millis);
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
                return false; 
            }
        }
    }
}
