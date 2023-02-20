namespace MidiWranglerGodot.Debug;

public interface ILogMessage
{
    public LogSeverity Severity { get; set; }
    public string Message { get; set; }
}

public struct LogMessage : ILogMessage
{
    public LogSeverity Severity { get; set; }
    public string Message { get; set; }
}