using System;
using System.IO;
using System.Text;

public static class Logger
{
    private static readonly string LogPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");

    private static readonly object LockObj = new object();

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    // Felhasználói esemény logolás (a WPF ezt olvassa)
    public static void UserEvent(string userId, string name, string action, string ip, string query = null, string info = null)
    {
        // USER: id=..; name=..; action=..; ip=..; query=..; info=..
        var extras = "";
        if (!string.IsNullOrWhiteSpace(query)) extras += $"; query={query}";
        if (!string.IsNullOrWhiteSpace(info)) extras += $"; info={info}";

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] USER: id={userId}; name={name}; action={action}; ip={ip}{extras}";
        AppendLine(line);
    }

    // kényelmi metódusok
    public static void UserLogin(string userId, string name, string ip) =>
        UserEvent(userId, name, "LOGIN", ip);

    public static void UserSearch(string userId, string name, string ip, string query) =>
        UserEvent(userId, name, "SEARCH", ip, query: query);

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}: {message}";
        AppendLine(line);
    }

    private static void AppendLine(string line)
    {
        lock (LockObj)
        {
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}