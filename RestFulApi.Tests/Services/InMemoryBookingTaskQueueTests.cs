using FluentAssertions;
using RestFulApi.Models;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class InMemoryBookingTaskQueueTests
{
    private readonly InMemoryBookingTaskQueue _queue;

    public InMemoryBookingTaskQueueTests()
    {
        _queue = new InMemoryBookingTaskQueue();
    }

    [Fact]
    public void Enqueue_ShouldAddBookingTaskToQueue()
    {
        // Arrange
        var task = new BookingTask
        {
            Id = Guid.NewGuid()
        };

        // Act
        _queue.Enqueue(task);

        // Assert
        var result = _queue.TryDequeue(out var dequeuedTask);
        result.Should().BeTrue();
        dequeuedTask.Should().NotBeNull();
        dequeuedTask.Id.Should().Be(task.Id);
    }

    [Fact]
    public void TryDequeue_ShouldReturnFalse_WhenQueueIsEmpty()
    {
        // Act
        var result = _queue.TryDequeue(out var dequeuedTask);

        // Assert
        result.Should().BeFalse();
        dequeuedTask.Should().BeNull();
    }

    [Fact]
    public void TryDequeue_ShouldReturnTrueAndDequeuedTask_WhenQueueIsNotEmpty()
    {
        // Arrange
        var task = new BookingTask
        {
            Id = Guid.NewGuid()
        };
        _queue.Enqueue(task);

        // Act
        var result = _queue.TryDequeue(out var dequeuedTask);

        // Assert
        result.Should().BeTrue();
        dequeuedTask.Should().NotBeNull();
        dequeuedTask.Id.Should().Be(task.Id);
    }

    [Fact]
    public void Queue_ShouldMaintainFifoOrder()
    {
        // Arrange
        var task1 = new BookingTask
        {
            Id = Guid.NewGuid()
        };
        var task2 = new BookingTask
        {
            Id = Guid.NewGuid()
        };
        var task3 = new BookingTask
        {
            Id = Guid.NewGuid()
        };

        _queue.Enqueue(task1);
        _queue.Enqueue(task2);
        _queue.Enqueue(task3);

        // Act & Assert
        _queue.TryDequeue(out var dequeuedTask1).Should().BeTrue();
        dequeuedTask1.Id.Should().Be(task1.Id);

        _queue.TryDequeue(out var dequeuedTask2).Should().BeTrue();
        dequeuedTask2.Id.Should().Be(task2.Id);

        _queue.TryDequeue(out var dequeuedTask3).Should().BeTrue();
        dequeuedTask3.Id.Should().Be(task3.Id);
    }
}