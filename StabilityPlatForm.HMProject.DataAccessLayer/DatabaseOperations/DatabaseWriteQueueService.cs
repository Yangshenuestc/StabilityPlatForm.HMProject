using System.Threading.Channels;

namespace StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations
{
    /// <summary>
    /// 数据库异步写入队列服务（生产者-消费者模式）
    /// 用于防止高频测试时数据库连接池耗尽和主线程卡顿
    /// </summary>
    public class DatabaseWriteQueueService
    {
        // 核心：无界通道（传送带），用于存放待执行的数据库写入任务
        private readonly Channel<Func<Task>> _writeQueue;

        public DatabaseWriteQueueService()
        {
            // 初始化通道
            _writeQueue = Channel.CreateUnbounded<Func<Task>>();

            // 启动后台专职消费者线程，独立运行，不阻塞主程序
            Task.Run(ProcessQueueAsync);
        }

        /// <summary>
        /// 生产者入口：将数据库写入任务扔进队列（耗时约 0.001 毫秒）
        /// </summary>
        /// <param name="dbWriteTask">包含数据库操作的异步委托</param>
        public void EnqueueWriteTask(Func<Task> dbWriteTask)
        {
            _writeQueue.Writer.TryWrite(dbWriteTask);
        }

        /// <summary>
        /// 消费者后台循环：从队列中逐个取出任务并串行执行
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            // ReadAllAsync 会一直等待，直到软件关闭/通道被显式完成
            await foreach (var writeTask in _writeQueue.Reader.ReadAllAsync())
            {
                try
                {
                    await writeTask(); // 真正执行 MySQL 的写入
                }
                catch (Exception ex)
                {
                    // 记录失败日志，防止后台线程因为单个异常而崩溃
                    System.Diagnostics.Debug.WriteLine($"后台数据库写入异常: {ex.Message}");
                }
            }
        }
    }
}