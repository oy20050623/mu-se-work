using ContactBookAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactBookAPI.Data;

public class ContactBookDbContext : DbContext
{
    public ContactBookDbContext(DbContextOptions<ContactBookDbContext> options) : base(options)
    {
    }

    // 联系人表
    public DbSet<Contact> Contacts { get; set; }

    // 联系方式表
    public DbSet<ContactDetail> ContactDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置一对多关系
        modelBuilder.Entity<ContactDetail>()
            .HasOne(cd => cd.Contact)
            .WithMany(c => c.ContactDetails)
            .HasForeignKey(cd => cd.ContactId)
            .OnDelete(DeleteBehavior.Cascade); // 删除联系人时级联删除联系方式
    }
}