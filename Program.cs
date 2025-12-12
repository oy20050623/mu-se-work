using ContactBookAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器
builder.Services.AddControllers();

// 添加数据库上下文（SQLite）
builder.Services.AddDbContext<ContactBookDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ContactBookDb")));

// 允许跨域（开发环境）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger（接口文档）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 迁移数据库（自动创建表）
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ContactBookDbContext>();
    dbContext.Database.Migrate(); // 自动执行迁移，无迁移则创建表
}

// 开发环境启用Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 启用跨域
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();