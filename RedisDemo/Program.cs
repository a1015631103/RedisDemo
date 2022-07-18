using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//参考：https://q.cnblogs.com/q/115234/ 设置中文不被编码貌似设置后还是无效
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EFWithDapper", Version = "v1" });
    options.IncludeXmlComments(xmlPath, true);
});

//微软官方推荐的缓存实践，个人觉得仅仅适合只用Redis缓存的情况，一旦用到分布式锁等等还是需要自己扩展，并且数据都是以byte[]转换成hash类型存入Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisConStr"];
    options.InstanceName = "";
});

//Redis官方推荐IOC做法
var multiplexer = ConnectionMultiplexer.Connect(builder.Configuration["RedisConStr"]);
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
