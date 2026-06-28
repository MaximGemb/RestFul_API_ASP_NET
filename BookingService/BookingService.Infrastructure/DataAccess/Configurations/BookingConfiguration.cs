using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BookingService.Domain.Entities;

namespace BookingService.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности <see cref="Booking"/> для Entity Framework Core.
/// Навигационные свойства к User и Event убраны — связь только по идентификаторам.
/// </summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>
    /// Настраивает отображение сущности <see cref="Booking"/> в базе данных.
    /// </summary>
    /// <param name="builder">Строитель конфигурации типа сущности.</param>
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(b => b.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.ProcessedAt)
            .HasColumnName("processed_at");

        builder.HasIndex(b => b.EventId);
        builder.HasIndex(b => b.UserId);
    }
}
