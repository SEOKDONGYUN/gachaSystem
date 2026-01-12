using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.IO;

namespace GachaSystem.Services;

public class KstConsoleFormatter : ConsoleFormatter
{
    private readonly TimeZoneInfo _kstTimeZone;

    public KstConsoleFormatter(IOptions<SimpleConsoleFormatterOptions> options)
        : base("kst")
    {
        // Linux/Docker 환경에서는 "Asia/Seoul", Windows에서는 "Korea Standard Time"
        try
        {
            _kstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
        }
        catch
        {
            try
            {
                _kstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            }
            catch
            {
                // 둘 다 실패하면 UTC+9 오프셋으로 생성
                _kstTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                    "KST",
                    TimeSpan.FromHours(9),
                    "Korea Standard Time",
                    "Korea Standard Time"
                );
            }
        }
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
        {
            return;
        }

        // KST 시간 생성
        var kstTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _kstTimeZone);
        var timestamp = kstTime.ToString("yyyy-MM-dd HH:mm:ss");

        // 로그 레벨
        var logLevel = logEntry.LogLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => "none"
        };

        // 로그 출력: [timestamp] [level] category: message
        textWriter.Write($"{timestamp} {logLevel}: {logEntry.Category} => {message}");

        // 예외 정보가 있으면 추가
        if (logEntry.Exception != null)
        {
            textWriter.Write($"\n{logEntry.Exception}");
        }

        textWriter.WriteLine();
    }
}
