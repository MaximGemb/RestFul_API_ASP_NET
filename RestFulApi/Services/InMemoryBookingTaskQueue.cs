using System.Collections.Concurrent;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Реализация очереди задач на генерацию отчетов в памяти.
/// </summary>
public class InMemoryBookingTaskQueue : IBookingTaskQueue
{
    private readonly ConcurrentQueue<BookingTask> _queue = new();

    /// <summary>
    /// Добавляет задачу в очередь.
    /// </summary>
    /// <param name="task">Задача на генерацию отчета.</param>
    public void Enqueue(BookingTask task)
    {
        _queue.Enqueue(task);
    }

    /// <summary>
    /// Пытается извлечь задачу из очереди.
    /// </summary>
    /// <param name="task">Извлеченная задача, если очередь не пуста.</param>
    /// <returns>True, если задача успешно извлечена, иначе False.</returns>
    public bool TryDequeue(out BookingTask task)
    {
        return _queue.TryDequeue(out task!);
    }
}
