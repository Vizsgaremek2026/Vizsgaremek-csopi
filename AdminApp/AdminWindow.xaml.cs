using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AdminApp
{
    public partial class AdminWindow : Window
    {
        private readonly string logFilePath;
        private FileSystemWatcher watcher;

        private List<string> allLines = new List<string>();
        private List<UserLogEntry> userEntries = new List<UserLogEntry>();

        // USER sor formátum:
        // [time] USER: id=42; name=Balazs; action=LOGIN; ip=1.2.3.4; query=Eminem
        private static readonly Regex UserRegex = new Regex(
            @"^\[(?<time>[^\]]+)\]\s*USER:\s*(?<kv>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AdminWindow()
        {
            InitializeComponent();

            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
            EnsureLogFileExists();

            SafeReload();
            StartWatching();
        }

        private void EnsureLogFileExists()
        {
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "[INFO] Log fájl létrehozva\n", Encoding.UTF8);
            }
        }

        private void LoadLogs()
        {
            allLines = File.ReadAllLines(logFilePath, Encoding.UTF8)
                           .Reverse()
                           .ToList();

            userEntries = allLines
                .Select(ParseUserLine)
                .Where(x => x != null)
                .ToList();
        }

        private void StartWatching()
        {
            watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(logFilePath),
                Filter = Path.GetFileName(logFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += (s, e) =>
            {
                Dispatcher.Invoke(SafeReload);
            };

            watcher.EnableRaisingEvents = true;
        }

        private void SafeReload()
        {
            try
            {
                LoadLogs();

                ApplyFiltersAndRenderLogs();
                UpdateSearchStats();

                ApplyUserFiltersAndRender();
                UpdateUserStats();
            }
            catch
            {
                // ha írás közben olvasnánk, a következő change úgyis frissít
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => SafeReload();

        private void Filters_Changed(object sender, RoutedEventArgs e) => ApplyFiltersAndRenderLogs();
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFiltersAndRenderLogs();

        private void UserFilters_Changed(object sender, TextChangedEventArgs e) => ApplyUserFiltersAndRender();

        // -----------------------
        // LOGOK TAB
        // -----------------------
        private void ApplyFiltersAndRenderLogs()
        {
            if (ChkInfo == null || ChkWarn == null || ChkError == null || SearchBox == null || LogList == null)
                return;

            bool showInfo = ChkInfo.IsChecked == true;
            bool showWarn = ChkWarn.IsChecked == true;
            bool showError = ChkError.IsChecked == true;

            string q = (SearchBox.Text ?? "").Trim();
            bool hasQuery = q.Length > 0;

            var filtered = allLines.Where(line =>
            {
                bool levelOk =
                    (showInfo && HasLevel(line, "INFO")) ||
                    (showWarn && HasLevel(line, "WARN")) ||
                    (showError && HasLevel(line, "ERROR")) ||
                    // USER sorokat is mutassuk INFO-ban (opcionális)
                    (showInfo && line.IndexOf(" USER:", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!levelOk) return false;

                if (hasQuery)
                    return line.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

                return true;
            }).ToList();

            RenderColoredLogLines(filtered);
        }

        private bool HasLevel(string line, string level)
        {
            if (line.IndexOf($"[{level}]", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (line.IndexOf($" {level}:", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void RenderColoredLogLines(List<string> lines)
        {
            LogList.Items.Clear();

            foreach (var line in lines)
            {
                var tb = new TextBlock { Text = line };

                if (line.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
                    tb.Foreground = Brushes.Red;
                else if (line.IndexOf("WARN", StringComparison.OrdinalIgnoreCase) >= 0)
                    tb.Foreground = Brushes.DarkOrange;
                else if (line.IndexOf(" USER:", StringComparison.OrdinalIgnoreCase) >= 0)
                    tb.Foreground = Brushes.DarkSlateBlue; // user sorok
                else
                    tb.Foreground = Brushes.Black;

                LogList.Items.Add(tb);
            }
        }

        // -----------------------
        // STAT TAB (SEARCH)
        // -----------------------
        private void UpdateSearchStats()
        {
            var searches = allLines
                .Select(ExtractSearchQuery)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(g => new StatRow { Query = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Query)
                .Take(25)
                .ToList();

            if (StatsList != null)
                StatsList.ItemsSource = searches;
        }

        private string ExtractSearchQuery(string line)
        {
            var idx = line.IndexOf("SEARCH:", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return line.Substring(idx + "SEARCH:".Length).Trim();

            idx = line.IndexOf("SEARCH=", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return line.Substring(idx + "SEARCH=".Length).Trim();

            return null;
        }

        // -----------------------
        // FELHASZNÁLÓK TAB
        // -----------------------
        private void ApplyUserFiltersAndRender()
        {
            if (UserEventsList == null || UserSearchBox == null) return;

            string q = (UserSearchBox.Text ?? "").Trim();
            bool hasQuery = q.Length > 0;

            var filtered = userEntries.Where(u =>
            {
                if (!hasQuery) return true;

                var hay = $"{u.Time} {u.UserId} {u.Name} {u.Action} {u.Ip} {u.QueryOrInfo}";
                return hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            }).ToList();

            UserEventsList.ItemsSource = filtered;
        }

        private void UpdateUserStats()
        {
            if (UserStatsList == null) return;

            // Top felhasználók: "id - name" alapján
            var topUsers = userEntries
                .GroupBy(u => $"{u.UserId} - {u.Name}", StringComparer.OrdinalIgnoreCase)
                .Select(g => new CountRow { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Key)
                .Take(25)
                .ToList();

            UserStatsList.ItemsSource = topUsers;
        }

        private UserLogEntry ParseUserLine(string line)
        {
            // User sor az általunk definiált formátumban?
            var m = UserRegex.Match(line);
            if (!m.Success) return null;

            var time = m.Groups["time"].Value.Trim();
            var kv = m.Groups["kv"].Value.Trim();

            var dict = ParseKeyValues(kv);

            dict.TryGetValue("id", out var id);
            dict.TryGetValue("name", out var name);
            dict.TryGetValue("action", out var action);
            dict.TryGetValue("ip", out var ip);

            // query vagy bármi extra:
            dict.TryGetValue("query", out var query);
            dict.TryGetValue("info", out var info);

            var qoi = !string.IsNullOrWhiteSpace(query) ? query : info;

            return new UserLogEntry
            {
                Time = time,
                UserId = id ?? "",
                Name = name ?? "",
                Action = action ?? "",
                Ip = ip ?? "",
                QueryOrInfo = qoi ?? ""
            };
        }

        private Dictionary<string, string> ParseKeyValues(string kv)
        {
            // "id=42; name=Balazs; action=LOGIN; ip=1.2.3.4; query=Eminem"
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var parts = kv.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var s = p.Trim();
                var eq = s.IndexOf('=');
                if (eq <= 0) continue;

                var key = s.Substring(0, eq).Trim();
                var val = s.Substring(eq + 1).Trim();

                dict[key] = val;
            }
            return dict;
        }

        private class StatRow
        {
            public string Query { get; set; }
            public int Count { get; set; }
        }

        private class CountRow
        {
            public string Key { get; set; }
            public int Count { get; set; }
        }

        private class UserLogEntry
        {
            public string Time { get; set; }
            public string UserId { get; set; }
            public string Name { get; set; }
            public string Action { get; set; }
            public string Ip { get; set; }
            public string QueryOrInfo { get; set; }
        }
    }
}