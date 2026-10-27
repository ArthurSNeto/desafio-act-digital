using Library.Domain.AuditModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infra.EF.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Actor)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.TimestampUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.Details)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => a.CorrelationId);
    }
}