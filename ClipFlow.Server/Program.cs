using ClipFlow.Server.Filters;
using ClipFlow.Server.Middleware;
using ClipFlow.Server.Models;
using ClipFlow.Server.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);



// 添加 Kestrel 配置 
builder.WebHost.ConfigureKestrel(options =>
{
    // 设置请求体最大值为 500MB
    options.Limits.MaxRequestBodySize = 524288000; // 500MB in bytes

    // 配置 HTTP/2 选项
    options.Limits.Http2.InitialConnectionWindowSize = 262144; // 256 KB
    options.Limits.Http2.InitialStreamWindowSize = 262144; // 256 KB
    options.Limits.Http2.MaxRequestHeaderFieldSize = 32768; // 32 KB
    options.Limits.Http2.MaxFrameSize = 262144; // 256 KB

    // 增加保持活动超时
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});


// 配置控制器选项
builder.Services.AddControllers(options =>
{
    // 配置请求体的大小限制
    options.MaxModelBindingCollectionSize = 524288000;
    options.Filters.Add<GlobalExceptionFilter>();
    // 添加全局认证过滤器
    options.Filters.Add<TokenAuthorizationFilter>();
}).ConfigureApiBehaviorOptions(options =>
{

});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 添加WebSocket支持
builder.Services.AddSingleton<ClipboardWebSocketManager>();

// 添加配置
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// 添加剪贴板数据管理器
builder.Services.AddSingleton<ClipboardDataManager>();

// 添加文件清理服务
builder.Services.AddHostedService<FileCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseGlobalResponse(); // 添加全局响应中间件
app.UseAuthorization();

app.MapControllers();


app.MapGet("/", () => "OK");
// 配置WebSocket
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.Run();