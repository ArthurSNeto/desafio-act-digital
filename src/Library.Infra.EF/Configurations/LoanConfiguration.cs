using Library.Domain.LoanModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infra.EF.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.BookId)
            .IsRequired();

        builder.Property(l => l.UserId)
            .IsRequired();

        builder.Property(l => l.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(l => l.IdempotencyKey)
            .IsUnique();

        builder.Property(l => l.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.LoanDateUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ExpectedReturnDateUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.ReturnedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.CancelledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.CancellationReason)
            .HasMaxLength(250);

        builder.HasOne(l => l.Book)
            .WithMany()
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.BookId);
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => new { l.BookId, l.Status });
    }
}
