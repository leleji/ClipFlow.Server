using ClipFlow.Server.Filters;
using ClipFlow.Server.Middleware;
using ClipFlow.Server.Models;
using ClipFlow.Server.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);



// ��� Kestrel ���� 
builder.WebHost.ConfigureKestrel(options =>
{
    // �������������ֵΪ 500MB
    options.Limits.MaxRequestBodySize = 524288000; // 500MB in bytes

    // ���� HTTP/2 ѡ��
    options.Limits.Http2.InitialConnectionWindowSize = 262144; // 256 KB
    options.Limits.Http2.InitialStreamWindowSize = 262144; // 256 KB
    options.Limits.Http2.MaxRequestHeaderFieldSize = 32768; // 32 KB
    options.Limits.Http2.MaxFrameSize = 262144; // 256 KB

    // ���ӱ��ֻ��ʱ
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});


// ���ÿ�����ѡ��
builder.Services.AddControllers(options =>
{
    // ����������Ĵ�С����
    options.MaxModelBindingCollectionSize = 524288000;
    options.Filters.Add<GlobalExceptionFilter>();
    // ���ȫ����֤������
    options.Filters.Add<TokenAuthorizationFilter>();
}).ConfigureApiBehaviorOptions(options =>
{

});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ���WebSocket֧��
builder.Services.AddSingleton<ClipboardWebSocketManager>();

// �������
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// ��Ӽ��������ݹ�����
builder.Services.AddSingleton<ClipboardDataManager>();

// ����ļ��������
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
app.UseGlobalResponse(); // ���ȫ����Ӧ�м��
app.UseAuthorization();

app.MapControllers();


app.MapGet("/", () => "OK");
// ����WebSocket
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.Run();