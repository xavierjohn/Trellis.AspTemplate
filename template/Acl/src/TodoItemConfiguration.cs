namespace TodoSample.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSample.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core configuration for the TodoItem aggregate.
/// </summary>
internal class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired();
        builder.Property(t => t.DueDate).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.CreatedByActorId).IsRequired().HasMaxLength(200);

        builder.HasTrellisIndex(t => new { t.Status, t.DueDate });
    }
}
