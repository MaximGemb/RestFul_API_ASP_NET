using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.DataAccess.Configurations;

/// <summary>
/// Конфигурация сущности <see cref="InboxMessage"/> для Entity Framework Core.
/// </summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <summary>
    /// Настраивает отображение сущности <see cref="InboxMessage"/> в базе данных.
    /// </summary>
    /// <param name="builder">Строитель конфигурации типа сущности.</param>
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();

        builder.Property(m => m.MessageType)
            .HasColumnName("message_type")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
