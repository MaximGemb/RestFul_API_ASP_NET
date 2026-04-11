using RestFulApi.Models;

namespace RestFulApi.Interfaces;

/// <summary>
/// Интерфейс очереди задач на генерацию отчетов.
/// </summary>
public interface IBookingTaskQueue
{
    /// <summary>
    /// Добавляет задачу в очередь.
    /// </summary>
    /// <param name="task">Задача на генерацию отчета.</param>
    void Enqueue(BookingTask task);

    /// <summary>
    /// Пытается извлечь задачу из очереди.
    /// </summary>
    /// <param name="task">Извлеченная задача, если очередь не пуста.</param>
    /// <returns>True, если задача успешно извлечена, иначе False.</returns>
    bool TryDequeue(out BookingTask task);
}
