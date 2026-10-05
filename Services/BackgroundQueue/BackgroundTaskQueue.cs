using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WebApplication1.Services.BackgroundQueue
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<TicketEmailTask> _queue;

        public BackgroundTaskQueue(int capacity = 500)
        {
            // Sử dụng BoundedChannel để quản lý tải, tránh tràn RAM
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true, // Worker BackgroundService đọc tuần tự
                SingleWriter = false // Nhiều request có thể đẩy task vào cùng lúc
            };
            _queue = Channel.CreateBounded<TicketEmailTask>(options);
        }

        public async ValueTask QueueTicketEmailAsync(TicketEmailTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            await _queue.Writer.WriteAsync(task);
        }

        public async ValueTask<TicketEmailTask> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
