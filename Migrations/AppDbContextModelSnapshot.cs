using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ItemProcessingApp.Data;

namespace ItemProcessingApp.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.20")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ItemProcessingApp.Models.Item", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<int?>("ParentId")
                    .HasColumnType("int");

                b.Property<decimal>("Weight")
                    .HasColumnType("decimal(18,3)");

                b.HasKey("Id");
                b.HasIndex("ParentId");
                b.ToTable("Items");
            });

            modelBuilder.Entity("ItemProcessingApp.Models.Item", b =>
            {
                b.HasOne("ItemProcessingApp.Models.Item", "Parent")
                    .WithMany("Children")
                    .HasForeignKey("ParentId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("Children");
                b.Navigation("Parent");
            });
#pragma warning restore 612, 618
        }
    }
}
