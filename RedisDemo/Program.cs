using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//�ο���https://q.cnblogs.com/q/115234/ �������Ĳ�������ò�����ú�����Ч
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EFWithDapper", Version = "v1" });
    options.IncludeXmlComments(xmlPath, true);
});

//΢��ٷ��Ƽ��Ļ���ʵ�������˾��ý����ʺ�ֻ��Redis����������һ���õ��ֲ�ʽ���ȵȻ�����Ҫ�Լ���չ���������ݶ�����byte[]ת����hash���ʹ���Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisConStr"];
    options.InstanceName = "";
});

//Redis�ٷ��Ƽ�IOC����
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
