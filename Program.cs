using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器
builder.Services.AddControllers();

// 添加CORS（解决跨域）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加数据库上下文（SQL Server，可替换为SQLite/MySQL）
builder.Services.AddDbContext<ContactContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置EPPlus许可证
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 添加Swagger（可选，方便调试）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 开发环境启用Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用HTTPS（本地调试可注释）
app.UseHttpsRedirection();

// 启用CORS
app.UseCors("AllowAll");

// 启用授权（简单示例可注释）
app.UseAuthorization();

// 映射控制器路由
app.MapControllers();

app.Run();

// 数据库上下文
public class ContactContext : DbContext
{
    public ContactContext(DbContextOptions<ContactContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactDetail> ContactDetails => Set<ContactDetail>();
}

// 联系人实体
public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsBookmarked { get; set; }
    public List<ContactDetail> ContactDetails { get; set; } = new List<ContactDetail>();
}

// 联系方式实体
public class ContactDetail
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string Type { get; set; } = string.Empty; // 电话、微信、邮箱等
    public string Value { get; set; } = string.Empty; // 具体值
    public Contact Contact { get; set; } = null!;
}
