// ============================================================================
// NillyHub — Minimal Square-Tile Nilly Suite Launcher & Live GitHub Updater
// created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// ~58 KB Native EXE · 0 MB background RAM · Direct PNG-Frame ICO Renderer
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("NillyHub")]
[assembly: AssemblyDescription("Ultra-Light Minimal Nilly Suite Launcher & Live GitHub Auto-Updater — created by Nilly graphics (Nillygraphics · Sotiris Christos)")]
[assembly: AssemblyCompany("Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyHub")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyTrademark("Nilly graphics / Nillygraphics — Sotiris Christos")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace NillyHub
{
    internal static class Native
    {
        public const int SW_RESTORE = 9;
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);

        public static void SetDarkTitlebar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int v = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref v, 4);
            }
            catch { }
        }
    }

    // ========================================================================
    // CRISP PNG-FRAME ICO DECODER (100% fixes .NET Icon.ToBitmap() noise bug!)
    // ========================================================================
    internal static class IconLoader
    {
        public static Bitmap LoadCrispIcon(string icoPath, int targetPx)
        {
            if (string.IsNullOrEmpty(icoPath) || !File.Exists(icoPath)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                if (data.Length < 22) return null;
                int count = BitConverter.ToUInt16(data, 4);
                int bestOff = -1, bestLen = 0, bestDim = -1;
                for (int i = 0; i < count; i++)
                {
                    int e = 6 + i * 16;
                    if (e + 16 > data.Length) break;
                    int w = data[e] == 0 ? 256 : data[e];
                    int len = BitConverter.ToInt32(data, e + 8);
                    int off = BitConverter.ToInt32(data, e + 12);
                    if (off < 0 || len <= 8 || off + len > data.Length) continue;
                    // Check PNG magic header (89 50 4E 47)
                    bool isPng = (data[off] == 0x89 && data[off + 1] == 0x50 && data[off + 2] == 0x4E && data[off + 3] == 0x47);
                    if (isPng && w > bestDim)
                    {
                        bestDim = w; bestOff = off; bestLen = len;
                    }
                }
                if (bestOff >= 0)
                {
                    using (MemoryStream ms = new MemoryStream(data, bestOff, bestLen, false))
                    using (Image src = Image.FromStream(ms))
                    {
                        Bitmap dst = new Bitmap(targetPx, targetPx, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(dst))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(src, 0, 0, targetPx, targetPx);
                        }
                        return dst;
                    }
                }
            }
            catch { }
            return null;
        }

        public static Bitmap DrawPlaceholderIcon(string title, Color accent, int sz)
        {
            Bitmap bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float stroke = Math.Max(1.8f, sz * 0.048f);
                float pad = sz * 0.045f + stroke / 2f;
                float w = sz - 2f * pad;
                using (GraphicsPath gp = RoundRectF(pad, pad, w, w, w * 0.22f))
                using (LinearGradientBrush bg = new LinearGradientBrush(new RectangleF(pad, pad, w, w),
                    Color.FromArgb(44, 46, 39), Color.FromArgb(28, 30, 26), 135f))
                using (Pen bp = new Pen(Color.FromArgb(252, 150, 31), stroke))
                {
                    g.FillPath(bg, gp);
                    g.DrawPath(bp, gp);
                }
                string mono = title.StartsWith("Nilly") && title.Length > 5 ? title.Substring(5, 1).ToUpper() : title.Substring(0, 1).ToUpper();
                using (Font f = new Font("Segoe UI", sz * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush br = new SolidBrush(accent))
                {
                    SizeF m = g.MeasureString(mono, f);
                    g.DrawString(mono, f, br, (sz - m.Width) / 2f, (sz - m.Height) / 2f + 1f);
                }
            }
            return bmp;
        }

        public static GraphicsPath RoundRectF(float x, float y, float w, float h, float r)
        {
            GraphicsPath p = new GraphicsPath();
            float d = r * 2f;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal sealed class SuiteApp
    {
        public string Id;
        public string DisplayName;
        public string ShortTag;
        public string Description;
        public string FolderName;
        public string LaunchFile;
        public string ProcessName;
        public string IconFile;
        public string RepoName;
        public string SiteUrl;
        public Color AccentColor;

        public string LocalDir;
        public string LocalLaunchPath;
        public bool IsInstalled;
        public long LocalExeBytes;
        public DateTime LocalBuildUtc;
        public string InstalledPushedStamp;

        public bool FoundOnGitHub;
        public string DefaultBranch = "main";
        public DateTime RemotePushedUtc;
        public string RemotePushedRaw;
        public string RemoteHomepage;
        public bool HasUpdate;
        public bool Busy;
    }

    internal sealed class GitHubRepoInfo
    {
        public string Name;
        public string Description;
        public string Homepage;
        public string DefaultBranch;
        public string PushedAtRaw;
        public DateTime PushedAtUtc;
    }

    internal static class GitHubClient
    {
        public const string OWNER = "nillynp";

        static GitHubClient()
        {
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls12; } catch { }
        }

        public static string HttpGet(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "NillyHub/1.1 (https://nillygraphics.com/)";
            req.Accept = "application/vnd.github+json";
            req.Timeout = 12000;
            req.ReadWriteTimeout = 15000;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        public static List<GitHubRepoInfo> FetchAllOwnerRepos()
        {
            List<GitHubRepoInfo> list = new List<GitHubRepoInfo>();
            string json = HttpGet("https://api.github.com/users/" + OWNER + "/repos?per_page=60&sort=updated");
            if (string.IsNullOrEmpty(json)) return list;
            List<string> objs = SplitTopLevelJsonObjects(json);
            for (int i = 0; i < objs.Count; i++)
            {
                string o = objs[i];
                string name = ExtractJsonString(o, "name");
                if (string.IsNullOrEmpty(name)) continue;
                GitHubRepoInfo info = new GitHubRepoInfo();
                info.Name = name;
                info.Description = ExtractJsonString(o, "description") ?? "";
                info.Homepage = ExtractJsonString(o, "homepage") ?? "";
                info.DefaultBranch = ExtractJsonString(o, "default_branch") ?? "main";
                info.PushedAtRaw = ExtractJsonString(o, "pushed_at") ?? ExtractJsonString(o, "updated_at") ?? "";
                DateTime dt;
                if (DateTime.TryParse(info.PushedAtRaw, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out dt))
                    info.PushedAtUtc = dt;
                list.Add(info);
            }
            return list;
        }

        private static readonly Dictionary<string, string> PackagePrefixes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "nillyview-pro", "NillyView-Pro" },
            { "nillydoc-pro", "NillyDoc-Pro" },
            { "nillynote-pro", "NillyNote-Pro" },
            { "nillycode-pro", "NillyCode-Pro" },
            { "nillycalc-pro", "NillyCalc-Pro" },
            { "nillybatch-pro", "NillyBatch-Pro" },
            { "nillymedia-lite", "NillyMedia-Lite" },
            { "nillyquicksnap", "NillyQuickSnap" },
            { "nillydisk-pro", "NillyDisk-Pro" },
            { "nilly-pc-manager-tools", "Nilly-PC-Manager-Tools" },
            { "nillyclip-pro", "NillyClip-Pro" }
        };

        public static string ResolvePortableZip(string repoName, out string tag, out string digest, out long size)
        {
            string prefix;
            if (!PackagePrefixes.TryGetValue(repoName, out prefix))
                throw new InvalidOperationException("Το repository δεν ανήκει στη λίστα πακέτων Nilly.");
            string json = HttpGet("https://api.github.com/repos/" + OWNER + "/" + repoName + "/releases/latest");
            return ReadPortableAsset(json, repoName, prefix, out tag, out digest, out size);
        }

        internal static string ReadPortableAsset(string json, string repoName, string prefix, out string tag, out string digest, out long size)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 128 * 1024, RecursionLimit = 24 };
            Dictionary<string, object> release = serializer.Deserialize<Dictionary<string, object>>(json);
            if (release == null || !object.Equals(release["draft"], false) || !object.Equals(release["prerelease"], false))
                throw new InvalidDataException("Χρειάζεται δημόσια σταθερή έκδοση.");
            tag = release["tag_name"] as string;
            if (tag == null || !Regex.IsMatch(tag, @"^v[0-9]+(\.[0-9]+){1,2}$"))
                throw new InvalidDataException("Μη αναγνωρισμένο tag έκδοσης.");
            string name = prefix + "-" + tag + "-Portable.zip";
            string url = "https://github.com/" + OWNER + "/" + repoName + "/releases/download/" + tag + "/" + name;
            digest = null;
            size = 0;
            foreach (object value in (System.Collections.IEnumerable)release["assets"])
            {
                Dictionary<string, object> asset = value as Dictionary<string, object>;
                if (asset == null || !object.Equals(asset["name"], name)) continue;
                if (digest != null || !object.Equals(asset["state"], "uploaded") || !object.Equals(asset["browser_download_url"], url))
                    throw new InvalidDataException("Το portable ZIP δεν αντιστοιχεί στη σταθερή έκδοση.");
                string rawDigest = asset["digest"] as string;
                if (rawDigest == null || !Regex.IsMatch(rawDigest, @"^sha256:[0-9a-f]{64}$"))
                    throw new InvalidDataException("Απουσιάζει το SHA-256 του portable ZIP.");
                digest = rawDigest.Substring(7);
                size = Convert.ToInt64(asset["size"], System.Globalization.CultureInfo.InvariantCulture);
                if (size <= 0 || size > 32L * 1024L * 1024L)
                    throw new InvalidDataException("Μη αποδεκτό μέγεθος ZIP.");
            }
            if (digest == null) throw new FileNotFoundException("Δεν βρέθηκε επαληθεύσιμο portable ZIP.");
            return url;
        }

        public static void DownloadVerifiedZip(string url, string destination, string expectedHash, long expectedBytes)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "NillyHub/1.1 (https://nillygraphics.com/)";
            request.Timeout = 18000;
            request.ReadWriteTimeout = 18000;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream incoming = response.GetResponseStream())
            using (FileStream output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = new byte[32768];
                long count = 0;
                int read;
                while ((read = incoming.Read(buffer, 0, buffer.Length)) > 0)
                {
                    count += read;
                    if (count > expectedBytes) throw new InvalidDataException("Το ZIP είναι μεγαλύτερο από το δηλωμένο.");
                    output.Write(buffer, 0, read);
                }
                if (count != expectedBytes) throw new InvalidDataException("Το ZIP έχει διαφορετικό μέγεθος.");
            }
            using (SHA256 sha = SHA256.Create())
            using (FileStream file = File.OpenRead(destination))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                if (!actual.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Το SHA-256 του ZIP δεν συμφωνεί.");
            }
        }

        private static List<string> SplitTopLevelJsonObjects(string arrJson)
        {
            List<string> res = new List<string>();
            int depth = 0, objStart = -1;
            bool inStr = false, esc = false;
            for (int i = 0; i < arrJson.Length; i++)
            {
                char ch = arrJson[i];
                if (inStr)
                {
                    if (esc) esc = false;
                    else if (ch == '\\') esc = true;
                    else if (ch == '"') inStr = false;
                    continue;
                }
                if (ch == '"') inStr = true;
                else if (ch == '{') { if (depth == 0) objStart = i; depth++; }
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0 && objStart >= 0) { res.Add(arrJson.Substring(objStart, i - objStart + 1)); objStart = -1; }
                }
            }
            return res;
        }

        private static string ExtractJsonString(string objJson, string key)
        {
            Match m = Regex.Match(objJson, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            return m.Success ? UnescapeJson(m.Groups[1].Value) : null;
        }

        private static string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    if (n == 'n') sb.Append('\n');
                    else if (n == 'r') sb.Append('\r');
                    else if (n == 't') sb.Append('\t');
                    else if (n == 'u' && i + 4 < s.Length)
                    {
                        int code;
                        if (int.TryParse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber, null, out code))
                        { sb.Append((char)code); i += 4; }
                    }
                    else sb.Append(n);
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }
    }

    internal static class SuiteCatalog
    {
        public static string DetectSuiteRoot()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NillySuite");
            Directory.CreateDirectory(path);
            return path;
        }

        public static List<SuiteApp> BuildDefaultCatalog()
        {
            List<SuiteApp> list = new List<SuiteApp>();
            list.Add(Make("nillyview", "NillyView Pro", "Image & RAW", "Επαγγελματικός Προβολέας Εικόνων & RAW (65+ formats)", "NillyViewPro", "NillyViewPro.exe", "NillyViewPro", "NillyViewPro.ico", "nillyview-pro", "https://nillygraphics.com/product/nillyview-pro/el/", Color.FromArgb(102, 217, 239)));
            list.Add(Make("nillydoc", "NillyDoc Pro", "PDF Studio", "Πανάλαφρος Acrobat-Style PDF Viewer & Editor", "NillyDocPro", "NillyDocPro.exe", "NillyDocPro", "NillyDocPro.ico", "nillydoc-pro", "https://nillygraphics.com/product/nillydoc-pro-windows/el/", Color.FromArgb(249, 38, 114)));
            list.Add(Make("nillynote", "NillyNote Pro", "Pro Notepad", "Pro Σημειωματάριο Windows 11 με καρτέλες & Auto-Session", "NillyNotePro", "NillyNotePro.exe", "NillyNotePro", "NillyNotePro.ico", "nillynote-pro", "https://nillygraphics.com/product/nillynote-pro/el/", Color.FromArgb(166, 226, 46)));
            list.Add(Make("nillycode", "NillyCode Pro", "Code Editor", "Sublime-Style Code Editor (Monokai, Minimap, Diff)", "NillyCodePro", "NillyCodePro.exe", "NillyCodePro", "NillyCodePro.ico", "nillycode-pro", "https://nillygraphics.com/product/nillycode-pro/el/", Color.FromArgb(252, 150, 31)));
            list.Add(Make("nillycalc", "NillyCalc Pro", "DPI & CMYK", "Υπολογιστής Γραφίστα (DPI, CMYK, Πιπέτα, ΦΠΑ & m²)", "NillyCalcPro", "NillyCalcPro.exe", "NillyCalcPro", "NillyCalcPro.ico", "nillycalc-pro", "https://nillygraphics.com/product/nillycalc-pro/el/", Color.FromArgb(166, 226, 46)));
            list.Add(Make("nillybatch", "NillyBatch Pro", "Batch & SEO", "Μαζική Αλλαγή Εικόνων, Watermark & Greeklish Renamer", "NillyBatchPro", "NillyBatchPro.exe", "NillyBatchPro", "NillyBatchPro.ico", "nillybatch-pro", "https://nillygraphics.com/product/nillybatch-pro/el/", Color.FromArgb(249, 38, 114)));
            list.Add(Make("nillymedia", "NillyMedia Lite", "Media & WAV", "Πανάλαφρος Video/Audio Player & 1-Click WAV Recorder", "NillyMediaLite", "NillyMediaLite.exe", "NillyMediaLite", "NillyMediaLite.ico", "nillymedia-lite", "https://nillygraphics.com/product/nillymedia-lite/el/", Color.FromArgb(166, 226, 46)));
            list.Add(Make("nillyquicksnap", "NillyQuickSnap", "Screen & Ruler", "Snipping Tool + Live Pixel Ruler + Blur/Σχεδίαση", "NillyQuickSnap", "NillyQuickSnap.exe", "NillyQuickSnap", "NillyQuickSnap.ico", "nillyquicksnap", "https://nillygraphics.com/product/nillyquicksnap/el/", Color.FromArgb(102, 217, 239)));
            list.Add(Make("nillydisk", "NillyDisk Pro", "Disk Treemap", "Αναλυτής Δίσκου, Treemap, SHA-256 Διπλότυπα & Cleaner", "NillyDiskPro", "NillyDiskPro.exe", "NillyDiskPro", "NillyDiskPro.ico", "nillydisk-pro", "https://nillygraphics.com/product/nillydisk-pro/el/", Color.FromArgb(252, 150, 31)));
            list.Add(Make("pcmanager", "PC Manager", "System Suite", "All-in-One Studio & Windows Maintenance Suite", "PCManagerLite", "Launch_PCManagerLite.vbs", "", "PCManagerLite.ico", "nilly-pc-manager-tools", "https://nillygraphics.com/product/nilly-pc-manager-tools/el/", Color.FromArgb(102, 217, 239)));
            list.Add(Make("nillyclip", "NillyClip Pro", "Clipboard & Vault", "Ιστορικό προχείρου και τοπική κλειδοθήκη", "NillyClipPro", "NillyClipPro.exe", "NillyClipPro", "NillyClipPro.ico", "nillyclip-pro", "https://nillygraphics.com/product/nillyclip-pro/el/", Color.FromArgb(252, 150, 31)));
            return list;
        }

        private static SuiteApp Make(string id, string name, string tag, string desc, string folder, string launch, string proc, string ico, string repo, string site, Color accent)
        {
            SuiteApp a = new SuiteApp();
            a.Id = id; a.DisplayName = name; a.ShortTag = tag; a.Description = desc;
            a.FolderName = folder; a.LaunchFile = launch; a.ProcessName = proc;
            a.IconFile = ico; a.RepoName = repo; a.SiteUrl = site; a.AccentColor = accent;
            return a;
        }

        public static void RefreshLocalState(SuiteApp app, string suiteRoot)
        {
            app.LocalDir = Path.Combine(suiteRoot, app.FolderName);
            app.LocalLaunchPath = Path.Combine(app.LocalDir, app.LaunchFile);
            app.IsInstalled = File.Exists(app.LocalLaunchPath);
            if (!app.IsInstalled && !string.IsNullOrEmpty(app.LaunchFile))
            {
                string alt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), app.FolderName, app.LaunchFile);
                if (File.Exists(alt)) { app.LocalDir = Path.GetDirectoryName(alt); app.LocalLaunchPath = alt; app.IsInstalled = true; }
            }
            if (app.IsInstalled)
            {
                try
                {
                    FileInfo fi = new FileInfo(app.LocalLaunchPath);
                    app.LocalExeBytes = fi.Length;
                    app.LocalBuildUtc = fi.LastWriteTimeUtc;
                    string stampPath = Path.Combine(app.LocalDir, ".nilly_pushed");
                    app.InstalledPushedStamp = File.Exists(stampPath) ? File.ReadAllText(stampPath, Encoding.UTF8).Trim() : null;
                }
                catch { }
            }
            else
            {
                app.LocalExeBytes = 0; app.LocalBuildUtc = DateTime.MinValue; app.InstalledPushedStamp = null;
            }
            ComputeUpdateFlag(app);
        }

        public static void ComputeUpdateFlag(SuiteApp app)
        {
            if (!app.IsInstalled || !app.FoundOnGitHub || app.RemotePushedUtc == DateTime.MinValue) { app.HasUpdate = false; return; }
            if (!string.IsNullOrEmpty(app.InstalledPushedStamp))
            {
                app.HasUpdate = !app.InstalledPushedStamp.Equals(app.RemotePushedRaw, StringComparison.OrdinalIgnoreCase);
                return;
            }
            app.HasUpdate = (app.RemotePushedUtc > app.LocalBuildUtc.AddMinutes(5));
        }

        public static void InstallOrUpdate(SuiteApp app, Action<string> onProgress)
        {
            string temp = Path.Combine(Path.GetTempPath(), "NillyHub-" + Guid.NewGuid().ToString("N"));
            string zip = Path.Combine(temp, "portable.zip");
            string files = Path.Combine(temp, "package");
            Directory.CreateDirectory(files);
            try
            {
                if (onProgress != null) onProgress("Έλεγχος Release για " + app.DisplayName + "...");
                string tag, hash;
                long bytes;
                string url = GitHubClient.ResolvePortableZip(app.RepoName, out tag, out hash, out bytes);
                if (onProgress != null) onProgress("Λήψη και επαλήθευση " + app.DisplayName + " (" + tag + ")...");
                GitHubClient.DownloadVerifiedZip(url, zip, hash, bytes);
                ExtractZipSmart(zip, files);

                string installerName = "Install_" + Path.GetFileNameWithoutExtension(app.LaunchFile) + ".ps1";
                bool portableLauncher = app.RepoName == "nilly-pc-manager-tools";
                string entryPoint = portableLauncher ? app.LaunchFile : installerName;
                if (!File.Exists(Path.Combine(files, entryPoint)))
                    throw new InvalidDataException("Λείπει το αναμενόμενο αρχείο εκκίνησης από το ZIP.");

                if (!string.IsNullOrEmpty(app.ProcessName))
                {
                    Process[] processes = Process.GetProcessesByName(app.ProcessName);
                    foreach (Process process in processes) process.Dispose();
                    if (processes.Length != 0)
                        throw new InvalidOperationException("Κλείσε πρώτα το " + app.DisplayName + " για να κρατήσεις τις αλλαγές σου.");
                }

                string target = Path.Combine(DetectSuiteRoot(), app.FolderName);
                string backup = target + ".previous-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                if (Directory.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Ο φάκελος εγκατάστασης παραπέμπει αλλού.");
                bool previousMoved = false;
                bool installing = false;
                try
                {
                    if (Directory.Exists(target))
                    {
                        Directory.Move(target, backup);
                        previousMoved = true;
                    }
                    Directory.CreateDirectory(target);
                    installing = true;
                    ExtractZipSmart(zip, target);
                    if (!portableLauncher)
                    {
                        if (onProgress != null) onProgress("Μεταγλώττιση " + app.DisplayName + "...");
                        string installer = Path.Combine(target, installerName);
                        ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + installer + "\" -Silent");
                        psi.WorkingDirectory = target;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        using (Process process = Process.Start(psi))
                        {
                            if (process == null) throw new InvalidOperationException("Ο installer δεν ξεκίνησε.");
                            if (!process.WaitForExit(90000))
                            {
                                process.Kill();
                                process.WaitForExit();
                                throw new TimeoutException("Ο installer δεν ολοκληρώθηκε έγκαιρα.");
                            }
                            if (process.ExitCode != 0) throw new InvalidOperationException("Ο installer δεν επιβεβαίωσε επιτυχία.");
                        }
                    }
                    if (!File.Exists(Path.Combine(target, app.LaunchFile)))
                        throw new FileNotFoundException("Ο installer δεν δημιούργησε το αναμενόμενο αρχείο εκκίνησης.");
                    if (!string.IsNullOrEmpty(app.RemotePushedRaw))
                        File.WriteAllText(Path.Combine(target, ".nilly_pushed"), app.RemotePushedRaw, Encoding.UTF8);
                }
                catch
                {
                    if (installing && Directory.Exists(target)) Directory.Delete(target, true);
                    if (previousMoved) Directory.Move(backup, target);
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        private static void ExtractZipSmart(string zipPath, string targetDir)
        {
            using (ZipArchive arch = ZipFile.OpenRead(zipPath))
            {
                string singlePrefix = null;
                bool allSharePrefix = true;
                for (int i = 0; i < arch.Entries.Count; i++)
                {
                    string fn = arch.Entries[i].FullName.Replace('\\', '/');
                    int slash = fn.IndexOf('/');
                    if (slash <= 0) { allSharePrefix = false; break; }
                    string pref = fn.Substring(0, slash + 1);
                    if (singlePrefix == null) singlePrefix = pref;
                    else if (!singlePrefix.Equals(pref, StringComparison.OrdinalIgnoreCase)) { allSharePrefix = false; break; }
                }
                string fullRoot = Path.GetFullPath(targetDir).TrimEnd('\\') + "\\";
                for (int i = 0; i < arch.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = arch.Entries[i];
                    string rel = entry.FullName.Replace('\\', '/');
                    if (allSharePrefix && singlePrefix != null && rel.StartsWith(singlePrefix, StringComparison.OrdinalIgnoreCase))
                        rel = rel.Substring(singlePrefix.Length);
                    if (string.IsNullOrEmpty(rel) || rel.EndsWith("/")) continue;
                    if (rel.IndexOf(':') >= 0) throw new InvalidDataException("Μη έγκυρο όνομα αρχείου ZIP.");
                    string dest = Path.GetFullPath(Path.Combine(targetDir, rel.Replace('/', '\\')));
                    if (!dest.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Μη έγκυρη διαδρομή ZIP.");
                    string parent = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    entry.ExtractToFile(dest, true);
                }
            }
        }
    }

    // ========================================================================
    // ULTRA-MINIMAL SQUARE TILE WITH CRISP VECTOR MINI-ICONS UNDERNEATH
    // ========================================================================
    internal sealed class SquareAppTile : Control
    {
        public readonly SuiteApp App;
        public event Action<SuiteApp> LaunchClicked;
        public event Action<SuiteApp> UpdateClicked;
        public event Action<SuiteApp> FolderClicked;
        public event Action<SuiteApp> SiteClicked;

        private bool _dark = true;
        private bool _tileHover = false;
        private int _hoverIconIdx = -1; // 0=Open/Install, 1=Update, 2=Folder, 3=Web
        private Bitmap _iconBmp;
        private readonly Rectangle[] _btnRects = new Rectangle[4];
        private readonly ToolTip _tip;
        private int _lastTipZone = -99;

        private static readonly Font FontTitle = new Font("Segoe UI", 9.6f, FontStyle.Bold);
        private static readonly Font FontSub = new Font("Segoe UI", 7.8f);
        private static readonly Font FontBadge = new Font("Segoe UI", 7f, FontStyle.Bold);

        public SquareAppTile(SuiteApp app, ToolTip sharedTip)
        {
            App = app;
            _tip = sharedTip;
            Width = 172;
            Height = 176;
            Margin = new Padding(8);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            // 4 minimal vector icon buttons centered at the bottom
            int btnSz = 26;
            int gap = 8;
            int totalW = 4 * btnSz + 3 * gap;
            int bx = (Width - totalW) / 2;
            int by = Height - 38;
            for (int i = 0; i < 4; i++)
                _btnRects[i] = new Rectangle(bx + i * (btnSz + gap), by, btnSz, btnSz);

            ReloadIcon();
        }

        public void RefreshTile(bool dark)
        {
            _dark = dark;
            ReloadIcon();
            Invalidate();
        }

        private void ReloadIcon()
        {
            if (_iconBmp != null) { _iconBmp.Dispose(); _iconBmp = null; }
            if (!string.IsNullOrEmpty(App.LocalDir) && !string.IsNullOrEmpty(App.IconFile))
            {
                string icoPath = Path.Combine(App.LocalDir, App.IconFile);
                _iconBmp = IconLoader.LoadCrispIcon(icoPath, 56);
            }
            if (_iconBmp == null)
                _iconBmp = IconLoader.DrawPlaceholderIcon(App.DisplayName, App.AccentColor, 56);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color outerBg = _dark ? Color.FromArgb(20, 21, 25) : Color.FromArgb(242, 244, 248);
            Color cardBg = _dark
                ? (_tileHover ? Color.FromArgb(35, 37, 44) : Color.FromArgb(29, 31, 36))
                : (_tileHover ? Color.FromArgb(255, 255, 255) : Color.FromArgb(250, 251, 253));
            Color border = _tileHover
                ? Color.FromArgb(252, 150, 31)
                : (App.HasUpdate ? Color.FromArgb(180, 110, 26) : (_dark ? Color.FromArgb(46, 49, 58) : Color.FromArgb(218, 224, 232)));
            Color fgTitle = _dark ? Color.FromArgb(242, 244, 248) : Color.FromArgb(22, 24, 30);
            Color fgMuted = _dark ? Color.FromArgb(142, 150, 165) : Color.FromArgb(100, 110, 124);
            Color divCol = _dark ? Color.FromArgb(42, 45, 54) : Color.FromArgb(230, 234, 240);

            g.Clear(outerBg);

            // Crisp rounded square card
            using (GraphicsPath gp = IconLoader.RoundRectF(1f, 1f, Width - 3f, Height - 3f, 12f))
            using (SolidBrush br = new SolidBrush(cardBg))
            using (Pen bp = new Pen(border, _tileHover ? 1.6f : 1f))
            {
                g.FillPath(br, gp);
                g.DrawPath(bp, gp);
            }

            // Minimal top-right status dot / tiny pill
            if (App.Busy)
            {
                DrawTinyPill(g, "...", Color.FromArgb(66, 46, 18), Color.FromArgb(252, 150, 31));
            }
            else if (App.IsInstalled && App.HasUpdate)
            {
                DrawTinyPill(g, "UPDATE", Color.FromArgb(70, 42, 14), Color.FromArgb(252, 150, 31));
            }
            else if (App.IsInstalled)
            {
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(166, 226, 46)))
                    g.FillEllipse(dot, Width - 19, 12, 7, 7);
            }
            else if (App.FoundOnGitHub)
            {
                DrawTinyPill(g, "0€", Color.FromArgb(22, 48, 60), Color.FromArgb(102, 217, 239));
            }

            // Centered Crisp 56x56 Hero Icon
            if (_iconBmp != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconBmp, (Width - 56) / 2, 18, 56, 56);
            }

            // Centered Clean Title & Short Tag
            Rectangle titleR = new Rectangle(8, 80, Width - 16, 20);
            TextRenderer.DrawText(g, App.DisplayName, FontTitle, titleR, fgTitle,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            Rectangle subR = new Rectangle(8, 100, Width - 16, 17);
            string subTxt = App.IsInstalled ? App.ShortTag : (App.FoundOnGitHub ? (App.ShortTag + " · GitHub") : App.ShortTag);
            TextRenderer.DrawText(g, subTxt, FontSub, subR, fgMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // Subtle divider above minimal bottom icon row
            using (Pen dp = new Pen(divCol, 1f))
                g.DrawLine(dp, 18, Height - 46, Width - 18, Height - 46);

            // Draw 4 Minimal Vector Mini-Icons Underneath!
            DrawMiniIconBtn(g, 0, App.IsInstalled ? "play" : "download",
                App.IsInstalled || App.FoundOnGitHub,
                App.IsInstalled ? Color.FromArgb(252, 150, 31) : Color.FromArgb(102, 217, 239));

            DrawMiniIconBtn(g, 1, "sync",
                App.FoundOnGitHub && !App.Busy,
                App.HasUpdate ? Color.FromArgb(252, 150, 31) : fgMuted);

            DrawMiniIconBtn(g, 2, "folder",
                App.IsInstalled,
                fgMuted);

            DrawMiniIconBtn(g, 3, "link",
                true,
                fgMuted);
        }

        private void DrawTinyPill(Graphics g, string text, Color bg, Color fg)
        {
            Size sz = TextRenderer.MeasureText(text, FontBadge);
            int pw = Math.Max(26, sz.Width + 6);
            Rectangle r = new Rectangle(Width - pw - 10, 9, pw, 16);
            using (GraphicsPath gp = IconLoader.RoundRectF(r.X, r.Y, r.Width, r.Height, 6f))
            using (SolidBrush br = new SolidBrush(bg))
                g.FillPath(br, gp);
            TextRenderer.DrawText(g, text, FontBadge, r, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawMiniIconBtn(Graphics g, int idx, string kind, bool enabled, Color baseStroke)
        {
            Rectangle r = _btnRects[idx];
            bool hov = enabled && (_hoverIconIdx == idx);
            if (hov)
            {
                using (GraphicsPath gp = IconLoader.RoundRectF(r.X, r.Y, r.Width, r.Height, 6f))
                using (SolidBrush hb = new SolidBrush(_dark ? Color.FromArgb(52, 56, 66) : Color.FromArgb(228, 233, 240)))
                    g.FillPath(hb, gp);
            }

            Color col = !enabled
                ? (_dark ? Color.FromArgb(62, 66, 76) : Color.FromArgb(190, 196, 206))
                : (hov ? Color.FromArgb(252, 150, 31) : baseStroke);

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;

            using (Pen p = new Pen(col, 1.6f))
            using (SolidBrush br = new SolidBrush(col))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                if (kind == "play")
                {
                    PointF[] tri = new PointF[] { new PointF(cx - 3.5f, cy - 5f), new PointF(cx + 5f, cy), new PointF(cx - 3.5f, cy + 5f) };
                    g.FillPolygon(br, tri);
                }
                else if (kind == "download")
                {
                    g.DrawLine(p, cx, cy - 5f, cx, cy + 2.5f);
                    g.DrawLine(p, cx - 3.5f, cy - 0.5f, cx, cy + 3f);
                    g.DrawLine(p, cx + 3.5f, cy - 0.5f, cx, cy + 3f);
                    g.DrawLine(p, cx - 4.5f, cy + 5.5f, cx + 4.5f, cy + 5.5f);
                }
                else if (kind == "sync")
                {
                    g.DrawArc(p, cx - 5f, cy - 5f, 10f, 10f, 35, 290);
                    g.DrawLine(p, cx + 2.2f, cy - 5.2f, cx + 5.2f, cy - 2.2f);
                    g.DrawLine(p, cx + 5.2f, cy - 5.2f, cx + 5.2f, cy - 2.2f);
                }
                else if (kind == "folder")
                {
                    g.DrawRectangle(p, cx - 5.5f, cy - 3.2f, 11f, 7.8f);
                    g.DrawLine(p, cx - 5.5f, cy - 3.2f, cx - 4f, cy - 5.2f);
                    g.DrawLine(p, cx - 4f, cy - 5.2f, cx - 0.5f, cy - 5.2f);
                    g.DrawLine(p, cx - 0.5f, cy - 5.2f, cx + 0.8f, cy - 3.2f);
                }
                else if (kind == "link")
                {
                    g.DrawLine(p, cx - 4f, cy + 4f, cx + 4f, cy - 4f);
                    g.DrawLine(p, cx - 0.5f, cy - 4f, cx + 4f, cy - 4f);
                    g.DrawLine(p, cx + 4f, cy - 4f, cx + 4f, cy + 0.5f);
                }
            }
        }

        private int HitTestBtn(Point pt)
        {
            for (int i = 0; i < 4; i++) if (_btnRects[i].Contains(pt)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int bi = HitTestBtn(e.Location);
            if (!_tileHover || bi != _hoverIconIdx)
            {
                _tileHover = true;
                _hoverIconIdx = bi;
                Cursor = Cursors.Hand;
                Invalidate();
            }
            if (bi != _lastTipZone && _tip != null)
            {
                _lastTipZone = bi;
                if (bi == 0) _tip.SetToolTip(this, App.IsInstalled ? ("Άνοιγμα " + App.DisplayName) : ("Εγκατάσταση " + App.DisplayName + " (0€)"));
                else if (bi == 1) _tip.SetToolTip(this, App.HasUpdate ? "Διαθέσιμη ενημέρωση από GitHub — κλικ για Update" : "Έλεγχος / Ενημέρωση από GitHub");
                else if (bi == 2) _tip.SetToolTip(this, "Άνοιγμα τοπικού φακέλου");
                else if (bi == 3) _tip.SetToolTip(this, "Άνοιγμα στο nillygraphics.com / GitHub");
                else
                {
                    string sz = App.IsInstalled ? (Math.Max(1, App.LocalExeBytes / 1024) + " KB") : "Μη εγκατεστημένο";
                    _tip.SetToolTip(this, App.DisplayName + " — " + App.Description + " (" + sz + ")");
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _tileHover = false; _hoverIconIdx = -1; _lastTipZone = -99;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            int bi = HitTestBtn(e.Location);
            if (bi == 1) { if (App.FoundOnGitHub && UpdateClicked != null) UpdateClicked(App); }
            else if (bi == 2) { if (App.IsInstalled && FolderClicked != null) FolderClicked(App); }
            else if (bi == 3) { if (SiteClicked != null) SiteClicked(App); }
            else
            {
                // Clicking Play icon OR anywhere on the square tile launches (or installs if absent)!
                if (App.IsInstalled) { if (LaunchClicked != null) LaunchClicked(App); }
                else if (App.FoundOnGitHub && UpdateClicked != null) UpdateClicked(App);
            }
            base.OnMouseClick(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _iconBmp != null) { _iconBmp.Dispose(); _iconBmp = null; }
            base.Dispose(disposing);
        }
    }

    // ========================================================================
    // WINDOWS DEFAULT APPS AND CLIPBOARD SETTINGS
    // ========================================================================
    internal sealed class WindowsSwitchDialog : Form
    {
        public WindowsSwitchDialog(bool dark)
        {
            Text = "NillyHub — Ρυθμίσεις Windows";
            ClientSize = new Size(490, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);
            BackColor = dark ? Color.FromArgb(24, 25, 30) : Color.White;
            ForeColor = dark ? Color.White : Color.Black;
            Label guide = new Label { Left = 20, Top = 20, Width = 450, Height = 72,
                Text = "Άλλαξε τις προεπιλεγμένες εφαρμογές μέσα από τα Windows. Για το NillyClip Pro, άνοιξε τις ρυθμίσεις Προχείρου και διάλεξε εσύ αν θέλεις ιστορικό ή συγχρονισμό." };
            Button defaults = SettingsButton("Προεπιλογές Windows", "ms-settings:defaultapps", 20);
            Button clipboard = SettingsButton("Πρόχειρο και συγχρονισμός", "ms-settings:clipboard", 227);
            Controls.AddRange(new Control[] { guide, defaults, clipboard });
        }

        private static Button SettingsButton(string label, string page, int left)
        {
            Button button = new Button { Text = label, Left = left, Top = 96, Width = 200, Height = 32, ForeColor = Color.Black, BackColor = Color.White };
            button.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo(page) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "NillyHub"); }
            };
            return button;
        }
    }

    // ========================================================================
    // MAIN HUB WINDOW
    // ========================================================================
    internal sealed class MainForm : Form
    {
        private readonly string _suiteRoot;
        private readonly List<SuiteApp> _apps;
        private readonly List<SquareAppTile> _tiles = new List<SquareAppTile>();
        private readonly ToolTip _sharedTip = new ToolTip();
        private bool _dark = true;
        private string _filterMode = "all";
        private bool _checking = false;
        private Bitmap _hubLogoBmp;

        private Panel _header;
        private Label _lblTitle, _lblCount;
        private TextBox _txtSearch;
        private Button _btnFAll, _btnFInst, _btnFUpd, _btnCheck, _btnSuiteAll, _btnWinSwap, _btnTheme;

        private FlowLayoutPanel _grid;
        private Panel _footer;
        private Label _lblStatus, _lblCredit;

        public MainForm(bool smokeMode)
        {
            _suiteRoot = SuiteCatalog.DetectSuiteRoot();
            _apps = SuiteCatalog.BuildDefaultCatalog();
            for (int i = 0; i < _apps.Count; i++) SuiteCatalog.RefreshLocalState(_apps[i], _suiteRoot);

            Text = "NillyHub — Nilly graphics";
            Size = new Size(1012, 538);
            MinimumSize = new Size(420, 380);
            StartPosition = FormStartPosition.CenterScreen;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyHub.ico");
            try
            {
                if (File.Exists(icoPath)) Icon = new Icon(icoPath);
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            _hubLogoBmp = IconLoader.LoadCrispIcon(icoPath, 32) ?? IconLoader.DrawPlaceholderIcon("NillyHub", Color.FromArgb(252, 150, 31), 32);

            BuildUi();
            RebuildTiles();
            ApplyTheme();

            if (!smokeMode) Shown += delegate { CheckGitHubUpdatesAsync(); };
        }

        private void BuildUi()
        {
            // Single Ultra-Clean Top Header Bar (properly docked -> zero button clipping!)
            _header = new Panel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(14, 10, 14, 10) };

            Panel leftBrand = new Panel { Dock = DockStyle.Left, Width = 175 };
            PictureBox picLogo = new PictureBox { Left = 0, Top = 1, Width = 32, Height = 32, Image = _hubLogoBmp, SizeMode = PictureBoxSizeMode.Zoom };
            _lblTitle = new Label { Left = 40, Top = 0, Width = 130, Height = 20, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Text = "NillyHub", UseMnemonic = false };
            _lblCount = new Label { Left = 41, Top = 20, Width = 130, Height = 14, Font = new Font("Segoe UI", 7.8f), Text = "Nilly graphics Suite", UseMnemonic = false };
            leftBrand.Controls.AddRange(new Control[] { picLogo, _lblTitle, _lblCount });

            FlowLayoutPanel rightBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 768, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 3, 0, 0)
            };

            _btnTheme = MkPill("☀", 30, delegate { _dark = !_dark; ApplyTheme(); });
            _btnWinSwap = MkPill("⇄ Windows", 92, delegate { OpenWindowsSwitchWizard(); });
            _btnSuiteAll = MkPill("↓ Όλη η Σουίτα", 112, delegate { InstallOrUpdateFullSuite(); });
            _btnCheck = MkPill("↻ Sync", 62, delegate { CheckGitHubUpdatesAsync(); });

            _txtSearch = new TextBox { Width = 128, Height = 26, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f), Margin = new Padding(4, 2, 4, 0) };
            _txtSearch.TextChanged += delegate { RebuildTiles(); };

            _btnFUpd = MkPill("Updates", 76, delegate { _filterMode = "updates"; RebuildTiles(); StylePills(); });
            _btnFInst = MkPill("Τοπικά", 74, delegate { _filterMode = "installed"; RebuildTiles(); StylePills(); });
            _btnFAll = MkPill("Όλα", 64, delegate { _filterMode = "all"; RebuildTiles(); StylePills(); });

            _sharedTip.SetToolTip(_btnCheck, "Έλεγχος ενημερώσεων από github.com/nillynp");
            _sharedTip.SetToolTip(_btnSuiteAll, "Λήψη & Ενημέρωση όλων των εργαλείων της Σουίτας μαζί (1 κλικ)");
            _sharedTip.SetToolTip(_btnWinSwap, "Ασφαλής αντικατάσταση εφαρμογών Windows (Notepad, Calc, Photos, Snipping Tool, Media Player)");
            _sharedTip.SetToolTip(_btnTheme, "Εναλλαγή Dark / Light");
            _sharedTip.SetToolTip(_txtSearch, "Αναζήτηση εργαλείου...");

            rightBar.Controls.AddRange(new Control[] { _btnTheme, _btnWinSwap, _btnSuiteAll, _btnCheck, _txtSearch, _btnFUpd, _btnFInst, _btnFAll });
            _header.Controls.Add(rightBar);
            _header.Controls.Add(leftBrand);

            // Minimal Footer
            _footer = new Panel { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(14, 0, 14, 0) };
            _lblStatus = new Label { Dock = DockStyle.Left, Width = 460, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.2f), UseMnemonic = false, Text = "Έτοιμο" };
            _lblCredit = new Label { Dock = DockStyle.Right, Width = 360, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.2f), Cursor = Cursors.Hand, UseMnemonic = false, Text = "created by Nilly graphics (Sotiris Christos) · nillygraphics.com" };
            _lblCredit.Click += delegate { try { Process.Start(new ProcessStartInfo("https://nillygraphics.com/") { UseShellExecute = true }); } catch { } };
            _footer.Controls.Add(_lblStatus);
            _footer.Controls.Add(_lblCredit);

            // Minimal Centered 5x2 Square Grid
            _grid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(18, 14, 14, 14)
            };

            Controls.Add(_grid);
            Controls.Add(_header);
            Label suiteBar = new Label {
                Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(43, 30, 18), ForeColor = Color.FromArgb(255, 185, 95),
                Text = "Προεπιλογές Windows › Nilly Suite · 11 εργαλεία · 1,22 MiB ZIP · 0 € · MIT", AccessibleName = "Σύγκριση σουίτας", AccessibleDescription = "Συνολικό μέγεθος των 11 φορητών ZIP κατά την προετοιμασία της έκδοσης 1.1."
            };
            Controls.Add(suiteBar);
            suiteBar.BringToFront();
            _header.BringToFront();
            Controls.Add(_footer);
        }

        private static Button MkPill(string txt, int w, EventHandler onClick)
        {
            Button b = new Button();
            b.UseMnemonic = false;
            b.Text = txt; b.Width = w; b.Height = 26;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 1;
            b.Font = new Font("Segoe UI", 8.3f, FontStyle.Bold);
            b.Cursor = Cursors.Hand; b.TabStop = false;
            b.Margin = new Padding(3, 1, 3, 0);
            b.Click += onClick;
            return b;
        }

        public void ApplyTheme()
        {
            Native.SetDarkTitlebar(Handle, _dark);
            Color bgMain = _dark ? Color.FromArgb(20, 21, 25) : Color.FromArgb(242, 244, 248);
            Color bgTop = _dark ? Color.FromArgb(26, 28, 33) : Color.White;
            Color fgMain = _dark ? Color.FromArgb(242, 244, 248) : Color.FromArgb(22, 24, 30);
            Color fgMuted = _dark ? Color.FromArgb(142, 150, 165) : Color.FromArgb(95, 105, 118);
            Color btnBg = _dark ? Color.FromArgb(36, 39, 46) : Color.FromArgb(236, 240, 246);
            Color border = _dark ? Color.FromArgb(52, 56, 66) : Color.FromArgb(214, 220, 228);
            Color orange = Color.FromArgb(252, 150, 31);

            BackColor = bgMain;
            _grid.BackColor = bgMain;
            _header.BackColor = bgTop;
            _footer.BackColor = bgTop;
            _lblTitle.ForeColor = fgMain;
            _lblCount.ForeColor = fgMuted;
            _lblStatus.ForeColor = fgMuted;
            _lblCredit.ForeColor = orange;
            _txtSearch.BackColor = _dark ? Color.FromArgb(22, 24, 28) : Color.White;
            _txtSearch.ForeColor = fgMain;
            _btnTheme.Text = _dark ? "☀" : "🌙";

            foreach (Button b in new Button[] { _btnCheck, _btnWinSwap, _btnTheme })
            { b.BackColor = btnBg; b.ForeColor = fgMain; b.FlatAppearance.BorderColor = border; }
            StyleSuiteAllBtn();
            StylePills();
            for (int i = 0; i < _tiles.Count; i++) _tiles[i].RefreshTile(_dark);
        }

        private void StyleSuiteAllBtn()
        {
            Color orange = Color.FromArgb(252, 150, 31);
            Color cyan = Color.FromArgb(102, 217, 239);
            Color btnBg = _dark ? Color.FromArgb(36, 39, 46) : Color.FromArgb(236, 240, 246);
            Color fgMuted = _dark ? Color.FromArgb(120, 128, 142) : Color.Gray;
            Color border = _dark ? Color.FromArgb(52, 56, 66) : Color.FromArgb(214, 220, 228);

            int missing = 0, upd = 0;
            for (int i = 0; i < _apps.Count; i++)
            {
                if (!_apps[i].IsInstalled && _apps[i].FoundOnGitHub) missing++;
                else if (_apps[i].HasUpdate) upd++;
            }
            if (upd > 0 && missing > 0)
            {
                _btnSuiteAll.Text = "↓ Σουίτα (" + (missing + upd) + ")";
                _btnSuiteAll.Enabled = true;
                _btnSuiteAll.BackColor = orange; _btnSuiteAll.ForeColor = Color.FromArgb(14, 18, 24); _btnSuiteAll.FlatAppearance.BorderColor = orange;
            }
            else if (missing > 0)
            {
                _btnSuiteAll.Text = "↓ Όλη η Σουίτα";
                _btnSuiteAll.Enabled = true;
                _btnSuiteAll.BackColor = cyan; _btnSuiteAll.ForeColor = Color.FromArgb(14, 18, 24); _btnSuiteAll.FlatAppearance.BorderColor = cyan;
            }
            else if (upd > 0)
            {
                _btnSuiteAll.Text = "↓ Update (" + upd + ")";
                _btnSuiteAll.Enabled = true;
                _btnSuiteAll.BackColor = orange; _btnSuiteAll.ForeColor = Color.FromArgb(14, 18, 24); _btnSuiteAll.FlatAppearance.BorderColor = orange;
            }
            else
            {
                _btnSuiteAll.Text = "↓ Όλη η Σουίτα";
                _btnSuiteAll.Enabled = true;
                _btnSuiteAll.BackColor = btnBg; _btnSuiteAll.ForeColor = _dark ? Color.FromArgb(220, 226, 236) : Color.FromArgb(40, 44, 52); _btnSuiteAll.FlatAppearance.BorderColor = border;
            }
        }

        private void StylePills()
        {
            Color orange = Color.FromArgb(252, 150, 31);
            Color btnBg = _dark ? Color.FromArgb(36, 39, 46) : Color.FromArgb(236, 240, 246);
            Color fg = _dark ? Color.FromArgb(228, 232, 240) : Color.FromArgb(25, 28, 34);
            Color border = _dark ? Color.FromArgb(52, 56, 66) : Color.FromArgb(214, 220, 228);
            SetPill(_btnFAll, _filterMode == "all", orange, btnBg, fg, border);
            SetPill(_btnFInst, _filterMode == "installed", orange, btnBg, fg, border);
            SetPill(_btnFUpd, _filterMode == "updates", orange, btnBg, fg, border);
        }

        private static void SetPill(Button b, bool active, Color accent, Color bg, Color fg, Color border)
        {
            b.BackColor = active ? accent : bg;
            b.ForeColor = active ? Color.FromArgb(14, 18, 24) : fg;
            b.FlatAppearance.BorderColor = active ? accent : border;
        }

        private void RebuildTiles()
        {
            _grid.SuspendLayout();
            for (int i = 0; i < _tiles.Count; i++) _tiles[i].Dispose();
            _tiles.Clear();
            _grid.Controls.Clear();

            string q = (_txtSearch.Text ?? "").Trim();
            int inst = 0, upd = 0;
            for (int i = 0; i < _apps.Count; i++)
            {
                SuiteApp a = _apps[i];
                if (a.IsInstalled) inst++;
                if (a.HasUpdate) upd++;

                if (_filterMode == "installed" && !a.IsInstalled) continue;
                if (_filterMode == "updates" && !a.HasUpdate) continue;
                if (q.Length > 0 &&
                    a.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                    a.ShortTag.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                    a.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                SquareAppTile tile = new SquareAppTile(a, _sharedTip);
                tile.LaunchClicked += OnLaunchApp;
                tile.UpdateClicked += OnInstallOrUpdateApp;
                tile.FolderClicked += delegate(SuiteApp ap)
                {
                    if (!string.IsNullOrEmpty(ap.LocalDir) && Directory.Exists(ap.LocalDir))
                        try { Process.Start("explorer.exe", "\"" + ap.LocalDir + "\""); } catch { }
                };
                tile.SiteClicked += delegate(SuiteApp ap)
                {
                    string u = !string.IsNullOrEmpty(ap.RemoteHomepage) ? ap.RemoteHomepage :
                               (!string.IsNullOrEmpty(ap.SiteUrl) ? ap.SiteUrl : ("https://github.com/" + GitHubClient.OWNER + "/" + ap.RepoName));
                    try { Process.Start(new ProcessStartInfo(u) { UseShellExecute = true }); } catch { }
                };
                tile.RefreshTile(_dark);
                _tiles.Add(tile);
                _grid.Controls.Add(tile);
            }
            _grid.ResumeLayout();

            _lblCount.Text = inst + "/" + _apps.Count + " εγκατεστημένα";
            _btnFAll.Text = "Όλα (" + _apps.Count + ")";
            _btnFInst.Text = "Τοπικά (" + inst + ")";
            _btnFUpd.Text = upd > 0 ? ("Updates (" + upd + ")") : "Updates";
            StyleSuiteAllBtn();
        }

        private void OpenWindowsSwitchWizard()
        {
            using (WindowsSwitchDialog dialog = new WindowsSwitchDialog(_dark))
                dialog.ShowDialog(this);
        }

        private void OnLaunchApp(SuiteApp app)
        {
            if (!app.IsInstalled) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(app.LocalLaunchPath);
                psi.WorkingDirectory = app.LocalDir;
                psi.UseShellExecute = true;
                Process.Start(psi);
                _lblStatus.Text = "Άνοιξε το " + app.DisplayName + ".";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Αδυναμία εκκίνησης " + app.DisplayName + ":\r\n" + ex.Message, "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CheckGitHubUpdatesAsync()
        {
            if (_checking) return;
            _checking = true;
            _btnCheck.Enabled = false;
            _lblStatus.Text = "Συγχρονισμός με github.com/" + GitHubClient.OWNER + "...";

            Thread th = new Thread(delegate()
            {
                List<GitHubRepoInfo> repos = null;
                string err = null;
                try { repos = GitHubClient.FetchAllOwnerRepos(); }
                catch (Exception ex) { err = ex.Message; }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        _checking = false;
                        _btnCheck.Enabled = true;
                        if (err != null) { _lblStatus.Text = "Offline (" + err + ")"; return; }
                        MergeGitHubRepos(repos);
                    });
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }

        private void MergeGitHubRepos(List<GitHubRepoInfo> repos)
        {
            if (repos == null) return;
            Dictionary<string, GitHubRepoInfo> map = new Dictionary<string, GitHubRepoInfo>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < repos.Count; i++) map[repos[i].Name] = repos[i];

            for (int i = 0; i < _apps.Count; i++)
            {
                SuiteApp a = _apps[i];
                GitHubRepoInfo ri;
                if (map.TryGetValue(a.RepoName, out ri))
                {
                    a.FoundOnGitHub = true;
                    a.DefaultBranch = ri.DefaultBranch;
                    a.RemotePushedUtc = ri.PushedAtUtc;
                    a.RemotePushedRaw = ri.PushedAtRaw;
                    if (!string.IsNullOrEmpty(ri.Homepage)) a.RemoteHomepage = ri.Homepage;
                    map.Remove(a.RepoName);
                }
                SuiteCatalog.RefreshLocalState(a, _suiteRoot);
            }

            RebuildTiles();
            int upd = 0, gh = 0;
            for (int i = 0; i < _apps.Count; i++) { if (_apps[i].HasUpdate) upd++; if (_apps[i].FoundOnGitHub) gh++; }
            _lblStatus.Text = string.Format("github.com/{0} ({1} repos) · {2}",
                GitHubClient.OWNER, gh, upd > 0 ? (upd + " ενημερώσεις διαθέσιμες") : "Όλα ενημερωμένα");
        }

        private void OnInstallOrUpdateApp(SuiteApp app)
        {
            if (app.Busy) return;
            app.Busy = true;
            RefreshSingleTile(app);
            Thread th = new Thread(delegate()
            {
                string err = null;
                try
                {
                    SuiteCatalog.InstallOrUpdate(app, delegate(string msg)
                    {
                        try { BeginInvoke((Action)delegate { _lblStatus.Text = msg; }); } catch { }
                    });
                }
                catch (Exception ex) { err = ex.Message; }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        app.Busy = false;
                        SuiteCatalog.RefreshLocalState(app, _suiteRoot);
                        RebuildTiles();
                        if (err != null)
                        {
                            _lblStatus.Text = "Σφάλμα: " + app.DisplayName;
                            MessageBox.Show(this, "Αδυναμία ενημέρωσης " + app.DisplayName + ":\r\n" + err, "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else _lblStatus.Text = "✓ Έτοιμο & ενημερωμένο: " + app.DisplayName;
                    });
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }

        private void InstallOrUpdateFullSuite()
        {
            List<SuiteApp> queue = new List<SuiteApp>();
            for (int i = 0; i < _apps.Count; i++)
            {
                SuiteApp a = _apps[i];
                if (!a.Busy && a.FoundOnGitHub && (!a.IsInstalled || a.HasUpdate))
                    queue.Add(a);
            }
            if (queue.Count == 0)
            {
                MessageBox.Show(this, "Όλα τα διαθέσιμα εργαλεία της Nilly Suite είναι ήδη εγκατεστημένα & ενημερωμένα!", "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dr = MessageBox.Show(this,
                "Λήψη & εγκατάσταση/ενημέρωση " + queue.Count + " εργαλείων της Nilly Suite από το github.com/nillynp;\r\n(100% Δωρεάν · Zero Admin · Αυτόματη μεταγλώττιση)",
                "Εγκατάσταση Όλης της Σουίτας — NillyHub", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            _btnSuiteAll.Enabled = false;
            Thread th = new Thread(delegate()
            {
                int ok = 0;
                for (int i = 0; i < queue.Count; i++)
                {
                    SuiteApp a = queue[i];
                    a.Busy = true;
                    try { BeginInvoke((Action)delegate { RefreshSingleTile(a); }); } catch { }
                    try
                    {
                        SuiteCatalog.InstallOrUpdate(a, delegate(string msg)
                        {
                            try { BeginInvoke((Action)delegate { _lblStatus.Text = string.Format("[{0}/{1}] {2}", i + 1, queue.Count, msg); }); } catch { }
                        });
                        ok++;
                    }
                    catch { }
                    a.Busy = false;
                    SuiteCatalog.RefreshLocalState(a, _suiteRoot);
                }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        RebuildTiles();
                        _lblStatus.Text = string.Format("✓ Ολοκληρώθηκε η Σουίτα ({0}/{1} εργαλεία έτοιμα)!", ok, queue.Count);
                    });
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }

        private void RefreshSingleTile(SuiteApp app)
        {
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i].App == app) { _tiles[i].RefreshTile(_dark); break; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hubLogoBmp != null) { _hubLogoBmp.Dispose(); _hubLogoBmp = null; }
                _sharedTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0 && args[0].Equals("--test-smoke", StringComparison.OrdinalIgnoreCase))
                return RunSmokeTest(args.Length > 1 ? args[1] : null, args.Length > 2 ? args[2] : null);

            bool createdNew;
            using (Mutex m = new Mutex(true, "Local\\NillyHub_SingleInstance_v1", out createdNew))
            {
                if (!createdNew)
                {
                    Process[] ps = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                    int myId = Process.GetCurrentProcess().Id;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (ps[i].Id != myId && ps[i].MainWindowHandle != IntPtr.Zero)
                        {
                            Native.ShowWindow(ps[i].MainWindowHandle, Native.SW_RESTORE);
                            Native.SetForegroundWindow(ps[i].MainWindowHandle);
                            break;
                        }
                    }
                    return 0;
                }
                Application.Run(new MainForm(false));
            }
            return 0;
        }

        private static int RunSmokeTest(string logPath, string previewPngPath)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                string root = SuiteCatalog.DetectSuiteRoot();
                List<SuiteApp> apps = SuiteCatalog.BuildDefaultCatalog();
                int localFound = 0, crispIcons = 0;
                for (int i = 0; i < apps.Count; i++)
                {
                    SuiteCatalog.RefreshLocalState(apps[i], root);
                    if (apps[i].IsInstalled) localFound++;
                    using (Bitmap bmp = IconLoader.LoadCrispIcon(Path.Combine(apps[i].LocalDir, apps[i].IconFile), 56))
                        if (bmp != null) crispIcons++;
                }
                sb.AppendLine("LOCAL_INSTALLED=" + localFound + "/" + apps.Count);
                sb.AppendLine("CRISP_PNG_ICONS=" + crispIcons);

                int ghCount = 0;
                try { ghCount = GitHubClient.FetchAllOwnerRepos().Count; } catch { }
                sb.AppendLine("GITHUB_NILLYNP_REPOS=" + ghCount);

                using (MainForm f = new MainForm(true))
                {
                    f.Show();
                    Application.DoEvents();
                    if (!string.IsNullOrEmpty(previewPngPath))
                    {
                        using (Bitmap shot = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot, new Rectangle(0, 0, f.Width, f.Height));
                            shot.Save(previewPngPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    long ramMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                    sb.AppendLine("WINDOW_OPENED_OK=" + f.IsHandleCreated);
                    sb.AppendLine("WORKING_SET_MB=" + ramMb);
                    f.Close();
                }
                sb.AppendLine("SMOKE_RESULT=PASS");
            }
            catch (Exception ex)
            {
                sb.AppendLine("SMOKE_RESULT=FAIL");
                sb.AppendLine(ex.ToString());
                if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return 1;
            }
            if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }
}
