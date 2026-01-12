using GachaSystem.Services;
using Microsoft.Extensions.Logging.Console;
using System.Text;

// 콘솔 인코딩을 UTF-8로 설정 (한글 출력 지원)
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 콘솔 로거 포맷 설정 (KST 시간대 사용)
builder.Logging.ClearProviders();
builder.Logging.AddConsoleFormatter<KstConsoleFormatter, SimpleConsoleFormatterOptions>();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "kst";
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add HttpContextAccessor for accessing HTTP context in services
builder.Services.AddHttpContextAccessor();

// Register GachaService as Singleton (데이터가 메모리에 유지됨)
builder.Services.AddSingleton<IGachaService, GachaService>();

// CORS 설정 (필요시)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Swagger는 /swagger 경로에서 접근
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gacha System API v1");
    c.RoutePrefix = "swagger"; // Swagger UI를 /swagger에서 접근
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
