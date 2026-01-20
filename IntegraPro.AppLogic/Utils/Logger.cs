namespace IntegraPro.AppLogic.Utils;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");

    public static void WriteLog(string modulo, string accion, string detalle)
    {
        try
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] | {modulo} | {accion} | {detalle}{Environment.NewLine}";
            File.AppendAllText(LogPath, logLine);
        }
        catch { /* Fallback silencioso si el archivo esta bloqueado */ }
    }
}
