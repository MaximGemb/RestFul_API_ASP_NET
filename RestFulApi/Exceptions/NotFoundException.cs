using RestFulApi.Models;

namespace RestFulApi.Exceptions;

/// <summary>
/// Исключение, выбрасываемое когда запрашиваемый ресурс не найден.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Id события
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Базовый конструктор
    /// </summary>
    public NotFoundException() : base(message: "Unknown event error.")
    {
    }

    /// <summary>
    /// Конструктор, принимающий текстовое сообщение
    /// </summary>
    /// <param name="id">Событие в котором произошла ошибка</param>
    /// <param name="message">Принимаемое сообщение</param>
    public NotFoundException(Guid? id, string message) : base(message) => 
        Id = id;


    /// <summary>
    /// Конструктор, принимающий текстовое сообщение и внутреннее исключение
    /// </summary>
    /// <param name="id">Событие в котором произошла ошибка</param>
    /// <param name="message">Принимаемое сообщение</param>
    /// <param name="inner">Внутренне исключение</param>
    public NotFoundException(Guid? id, string message, Exception inner)
        : base(message, inner) =>
        Id = id;
}