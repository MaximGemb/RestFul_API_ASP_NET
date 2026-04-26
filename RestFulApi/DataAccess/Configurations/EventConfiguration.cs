using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestFulApi.Models;

namespace RestFulApi.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности <see cref="Event"/> для Entity Framework Core.
/// Определяет отображение события на таблицу базы данных.
/// </summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <summary>
    /// Настраивает отображение сущности <see cref="Event"/> в базе данных.
    /// </summary>
    /// <param name="builder">Строитель конфигурации типа сущности.</param>
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(e => e.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(e => e.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();

        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
