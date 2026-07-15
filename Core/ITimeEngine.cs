using System;
using System.Threading.Tasks;

namespace ChronoDesk.Core
{
    public interface ITimeEngine
    {
        /// <summary>
        /// زمان دقیق محاسبه شده بر اساس انحراف از NTP را برمی‌گرداند.
        /// </summary>
        DateTime GetCurrentTime();

        /// <summary>
        /// همگام‌سازی اولیه با سرورهای NTP. در صورت قطعی اینترنت، از ساعت سیستم استفاده می‌کند.
        /// </summary>
        Task SynchronizeAsync();
    }
}
