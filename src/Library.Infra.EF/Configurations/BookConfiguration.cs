using Library.Domain.BookModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infra.EF.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Author)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(b => b.Isbn)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(b => b.Isbn)
            .IsUnique();

        builder.Property(b => b.TotalCopies)
            .IsRequired();

        builder.Property(b => b.AvailableCopies)
            .IsRequired();

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(b => b.ConcurrencyToken)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(b => b.IsActive);
    }
}