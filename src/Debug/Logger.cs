using System;
using System.Collections.Generic;
using Godot;
using GoDotLog;

namespace MidiWranglerGodot.Debug;

public class Logger : Node
{
    public static LogSeverity SeverityLevel { get; set; } = LogSeverity.Debug;
    public static int MaxMessagesPerFrame { get; set; } = 5;
    
    static readonly Queue<ILogMessage> _msgQueue = new();
    readonly ILog _log = new GDLog(nameof(Logger));

    public static void Info(string message) => Log(LogSeverity.Info, message);
    public static void Debug(string message) => Log(LogSeverity.Debug, message);
    public static void Warning(string message) => Log(LogSeverity.Warning, message);
    public static void Error(string message) => Log(LogSeverity.Error, message);

    static void Log(LogSeverity severity, string message)
    {
        _msgQueue.Enqueue(new LogMessage{Severity = severity, Message = message});
    }

    public override void _Process(float delta)
    {
        var loopMax = Math.Min(_msgQueue.Count, MaxMessagesPerFrame);
        for (var logIdx = 0; logIdx < loopMax; logIdx++)
        {
            if (!_msgQueue.TryDequeue(out var nextMsg) || nextMsg.Severity <= SeverityLevel) return;
            _log.Print($"{nextMsg.Severity}: {nextMsg.Message}");
        }
    }
}