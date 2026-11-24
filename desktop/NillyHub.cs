// ============================================================================
// NillyHub v1.2 — Ultra-Light Minimal Nilly Suite Launcher & Live GitHub Updater
// created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// ~58 KB Native EXE · 0 MB background RAM · ETag Cache · Zero-Flicker Grid
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("NillyHub")]
[assembly: AssemblyDescription("Ultra-Light Minimal Nilly Suite Launcher & Live GitHub Auto-Updater — created by Nilly graphics (Nillygraphics · Sotiris Christos)")]
[assembly: AssemblyCompany("Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyHub")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyTrademark("Nilly graphics / Nillygraphics — Sotiris Christos")]
[assembly: AssemblyVersion("1.4.1.0")]
[assembly: AssemblyFileVersion("1.4.1.0")]

namespace NillyHub
{
    internal static class Native
    {
        public const int SW_RESTORE = 9;
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

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

        public static void SetCueBanner(TextBox tb, string text)
        {
            if (tb == null || !tb.IsHandleCreated) return;
            try { SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, text); } catch { }
        }

        public static void TrimWorkingSet()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
    }

    // ========================================================================
    // TINY STRICT RFC-8259 PURE-C# JSON PARSER (Zero System.Web.Extensions!)
    // ========================================================================
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int idx = 0;
            object val = ParseValue(json, ref idx, 0);
            SkipWs(json, ref idx);
            if (idx != json.Length) throw new InvalidDataException("Unexpected trailing JSON data.");
            return val;
        }

        private static object ParseValue(string s, ref int i, int depth)
        {
            if (depth > 24) throw new InvalidDataException("JSON nesting too deep.");
            SkipWs(s, ref i);
            if (i >= s.Length) throw new InvalidDataException("Unexpected end of JSON.");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i, depth + 1);
            if (c == '[') return ParseArray(s, ref i, depth + 1);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't' && MatchLit(s, ref i, "true")) return true;
            if (c == 'f' && MatchLit(s, ref i, "false")) return false;
            if (c == 'n' && MatchLit(s, ref i, "null")) return null;
            if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber(s, ref i);
            throw new InvalidDataException("Invalid JSON token.");
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i, int depth)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>(StringComparer.Ordinal);
            i++; // '{'
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new InvalidDataException("Expected JSON key.");
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new InvalidDataException("Expected ':' in JSON object.");
                i++;
                obj[key] = ParseValue(s, ref i, depth);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return obj; }
                throw new InvalidDataException("Expected ',' or '}' in JSON object.");
            }
        }

        private static List<object> ParseArray(string s, ref int i, int depth)
        {
            List<object> arr = new List<object>();
            i++; // '['
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref i, depth));
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return arr; }
                throw new InvalidDataException("Expected ',' or ']' in JSON array.");
            }
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // '"'
            StringBuilder sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (i >= s.Length) throw new InvalidDataException("Incomplete JSON escape.");
                    char e = s[i++];
                    if (e == '"' || e == '\\' || e == '/') sb.Append(e);
                    else if (e == 'b') sb.Append('\b');
                    else if (e == 'f') sb.Append('\f');
                    else if (e == 'n') sb.Append('\n');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'u' && i + 4 <= s.Length)
                    {
                        int code;
                        if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw new InvalidDataException("Invalid JSON unicode escape.");
                        sb.Append((char)code);
                        i += 4;
                    }
                    else throw new InvalidDataException("Invalid JSON escape.");
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new InvalidDataException("Unterminated JSON string.");
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            if (s[i] == '-') i++;
            while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
            bool isFloat = false;
            if (i < s.Length && s[i] == '.') { isFloat = true; i++; while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++; }
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                isFloat = true; i++;
                if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
            }
            string raw = s.Substring(start, i - start);
            if (!isFloat)
            {
                long lv;
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out lv)) return lv;
            }
            double dv;
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out dv)) return dv;
            throw new InvalidDataException("Invalid JSON number.");
        }

        private static bool MatchLit(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length) return false;
            for (int k = 0; k < lit.Length; k++) if (s[i + k] != lit[k]) return false;
            i += lit.Length;
            return true;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }
    }

    // ========================================================================
    // CRISP PNG-FRAME ICO DECODER + CUSTOM VECTOR GLYPHS (Zero Noise, Low RAM)
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

        public static Bitmap DrawPlaceholderIcon(string appId, string title, Color accent, int sz)
        {
            Bitmap bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float stroke = Math.Max(1.8f, sz * 0.045f);
                float pad = sz * 0.045f + stroke / 2f;
                float w = sz - 2f * pad;
                using (GraphicsPath gp = RoundRectF(pad, pad, w, w, w * 0.22f))
                using (LinearGradientBrush bg = new LinearGradientBrush(new RectangleF(pad, pad, w, w),
                    Color.FromArgb(42, 45, 52), Color.FromArgb(24, 26, 31), 135f))
                using (Pen bp = new Pen(Color.FromArgb(200, accent.R, accent.G, accent.B), stroke))
                {
                    g.FillPath(bg, gp);
                    g.DrawPath(bp, gp);
                }

                float cx = sz * 0.5f, cy = sz * 0.5f, u = sz * 0.22f;
                using (Pen p = new Pen(accent, Math.Max(1.8f, sz * 0.044f)))
                using (SolidBrush br = new SolidBrush(accent))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                    if (appId == "nillyview")
                    {
                        g.DrawRectangle(p, cx - u, cy - u * 0.78f, u * 2f, u * 1.56f);
                        g.FillEllipse(br, cx - u * 0.52f, cy - u * 0.42f, u * 0.36f, u * 0.36f);
                        g.DrawLines(p, new PointF[] {
                            new PointF(cx - u * 0.8f, cy + u * 0.55f),
                            new PointF(cx - u * 0.15f, cy - u * 0.08f),
                            new PointF(cx + u * 0.22f, cy + u * 0.28f),
                            new PointF(cx + u * 0.55f, cy - u * 0.02f),
                            new PointF(cx + u * 0.85f, cy + u * 0.55f)
                        });
                    }
                    else if (appId == "nillydoc")
                    {
                        g.DrawRectangle(p, cx - u * 0.72f, cy - u * 0.92f, u * 1.44f, u * 1.84f);
                        using (Font f = new Font("Segoe UI", sz * 0.20f, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            SizeF m = g.MeasureString("PDF", f);
                            g.DrawString("PDF", f, br, cx - m.Width / 2f, cy - m.Height / 2f + 1f);
                        }
                    }
                    else if (appId == "nillynote")
                    {
                        g.DrawRectangle(p, cx - u * 0.78f, cy - u * 0.88f, u * 1.56f, u * 1.76f);
                        g.DrawLine(p, cx - u * 0.44f, cy - u * 0.36f, cx + u * 0.44f, cy - u * 0.36f);
                        g.DrawLine(p, cx - u * 0.44f, cy + u * 0.02f, cx + u * 0.44f, cy + u * 0.02f);
                        g.DrawLine(p, cx - u * 0.44f, cy + u * 0.40f, cx + u * 0.16f, cy + u * 0.40f);
                    }
                    else if (appId == "nillycode")
                    {
                        g.DrawLines(p, new PointF[] { new PointF(cx - u * 0.42f, cy - u * 0.52f), new PointF(cx - u * 0.92f, cy), new PointF(cx - u * 0.42f, cy + u * 0.52f) });
                        g.DrawLines(p, new PointF[] { new PointF(cx + u * 0.42f, cy - u * 0.52f), new PointF(cx + u * 0.92f, cy), new PointF(cx + u * 0.42f, cy + u * 0.52f) });
                        g.DrawLine(p, cx + u * 0.14f, cy - u * 0.62f, cx - u * 0.14f, cy + u * 0.62f);
                    }
                    else if (appId == "nillycalc")
                    {
                        g.DrawRectangle(p, cx - u * 0.78f, cy - u * 0.88f, u * 1.56f, u * 1.76f);
                        g.DrawLine(p, cx - u * 0.46f, cy - u * 0.42f, cx + u * 0.46f, cy - u * 0.42f);
                        float d = u * 0.24f;
                        g.FillEllipse(br, cx - u * 0.48f, cy - u * 0.04f, d, d);
                        g.FillEllipse(br, cx - d * 0.5f, cy - u * 0.04f, d, d);
                        g.FillEllipse(br, cx + u * 0.24f, cy - u * 0.04f, d, d);
                        g.FillEllipse(br, cx - u * 0.48f, cy + u * 0.36f, d, d);
                        g.FillEllipse(br, cx - d * 0.5f, cy + u * 0.36f, d, d);
                        g.FillEllipse(br, cx + u * 0.24f, cy + u * 0.36f, d, d);
                    }
                    else if (appId == "nillybatch")
                    {
                        g.DrawRectangle(p, cx - u * 0.82f, cy - u * 0.78f, u * 1.24f, u * 1.18f);
                        g.DrawRectangle(p, cx - u * 0.42f, cy - u * 0.38f, u * 1.24f, u * 1.18f);
                    }
                    else if (appId == "nillymedia")
                    {
                        PointF[] tri = new PointF[] { new PointF(cx - u * 0.55f, cy - u * 0.78f), new PointF(cx + u * 0.78f, cy), new PointF(cx - u * 0.55f, cy + u * 0.78f) };
                        g.FillPolygon(br, tri);
                    }
                    else if (appId == "nillyquicksnap")
                    {
                        float k = u * 0.84f, arm = u * 0.46f;
                        g.DrawLines(p, new PointF[] { new PointF(cx - k, cy - k + arm), new PointF(cx - k, cy - k), new PointF(cx - k + arm, cy - k) });
                        g.DrawLines(p, new PointF[] { new PointF(cx + k - arm, cy - k), new PointF(cx + k, cy - k), new PointF(cx + k, cy - k + arm) });
                        g.DrawLines(p, new PointF[] { new PointF(cx - k, cy + k - arm), new PointF(cx - k, cy + k), new PointF(cx - k + arm, cy + k) });
                        g.DrawLines(p, new PointF[] { new PointF(cx + k - arm, cy + k), new PointF(cx + k, cy + k), new PointF(cx + k, cy + k - arm) });
                        g.FillEllipse(br, cx - u * 0.22f, cy - u * 0.22f, u * 0.44f, u * 0.44f);
                    }
                    else if (appId == "nillydisk")
                    {
                        g.DrawEllipse(p, cx - u * 0.85f, cy - u * 0.85f, u * 1.7f, u * 1.7f);
                        g.DrawLine(p, cx, cy, cx, cy - u * 0.85f);
                        g.DrawLine(p, cx, cy, cx + u * 0.72f, cy + u * 0.42f);
                    }
                    else if (appId == "pcmanager")
                    {
                        PointF[] sh = new PointF[] {
                            new PointF(cx, cy - u * 0.95f),
                            new PointF(cx + u * 0.82f, cy - u * 0.56f),
                            new PointF(cx + u * 0.72f, cy + u * 0.28f),
                            new PointF(cx, cy + u * 0.95f),
                            new PointF(cx - u * 0.72f, cy + u * 0.28f),
                            new PointF(cx - u * 0.82f, cy - u * 0.56f)
                        };
                        g.DrawPolygon(p, sh);
                        g.DrawLines(p, new PointF[] { new PointF(cx - u * 0.34f, cy + u * 0.04f), new PointF(cx - u * 0.06f, cy + u * 0.32f), new PointF(cx + u * 0.38f, cy - u * 0.24f) });
                    }
                    else if (appId == "nillyclip")
                    {
                        g.DrawRectangle(p, cx - u * 0.74f, cy - u * 0.72f, u * 1.48f, u * 1.62f);
                        g.DrawRectangle(p, cx - u * 0.38f, cy - u * 0.96f, u * 0.76f, u * 0.42f);
                        g.DrawLine(p, cx - u * 0.38f, cy + u * 0.06f, cx + u * 0.38f, cy + u * 0.06f);
                        g.DrawLine(p, cx - u * 0.38f, cy + u * 0.44f, cx + u * 0.22f, cy + u * 0.44f);
                    }
                    else if (appId == "hub")
                    {
                        // Always-crisp 4-square NillyHub emblem (even when NillyHub.exe runs solo without .ico!)
                        float gap = sz * 0.06f;
                        float cell = (w - sz * 0.30f - gap) / 2f;
                        float x0 = (sz - (cell * 2f + gap)) / 2f;
                        float y0 = x0;
                        float cr = Math.Max(2f, cell * 0.28f);
                        Color[] cols = new Color[]
                        {
                            Color.FromArgb(252, 150, 31),
                            Color.FromArgb(102, 217, 239),
                            Color.FromArgb(249, 38, 114),
                            Color.FromArgb(166, 226, 46)
                        };
                        for (int q = 0; q < 4; q++)
                        {
                            float qx = x0 + (q % 2) * (cell + gap);
                            float qy = y0 + (q / 2) * (cell + gap);
                            using (GraphicsPath qp = RoundRectF(qx, qy, cell, cell, cr))
                            using (SolidBrush qb = new SolidBrush(cols[q]))
                                g.FillPath(qb, qp);
                        }
                    }
                    else
                    {
                        string mono = title.StartsWith("Nilly") && title.Length > 5 ? title.Substring(5, 1).ToUpper() : title.Substring(0, 1).ToUpper();
                        using (Font f = new Font("Segoe UI", sz * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel))
                        {
                            SizeF m = g.MeasureString(mono, f);
                            g.DrawString(mono, f, br, (sz - m.Width) / 2f, (sz - m.Height) / 2f + 1f);
                        }
                    }
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
            string ignored;
            return HttpGetConditional(url, null, out ignored);
        }

        // Supports HTTP ETag (If-None-Match -> 304 Not Modified uses 0 bandwidth & 0 GitHub rate-limit quota!)
        public static string HttpGetConditional(string url, string etag, out string newEtag)
        {
            newEtag = etag;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "NillyHub/1.4 (https://nillygraphics.com/)";
            req.Accept = "application/vnd.github+json";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Timeout = 12000;
            req.ReadWriteTimeout = 15000;
            if (!string.IsNullOrEmpty(etag)) req.Headers[HttpRequestHeader.IfNoneMatch] = etag;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    string hdr = resp.Headers["ETag"];
                    if (!string.IsNullOrEmpty(hdr)) newEtag = hdr;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                HttpWebResponse hr = wex.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.NotModified)
                {
                    hr.Close();
                    return null; // 304 Not Modified -> caller reuses local cache!
                }
                throw;
            }
        }

        public static List<GitHubRepoInfo> LoadCachedRepos(string suiteRoot)
        {
            try
            {
                string cacheFile = Path.Combine(suiteRoot, ".nillyhub_repos_cache.json");
                if (!File.Exists(cacheFile)) return null;
                return ParseRepoListJson(File.ReadAllText(cacheFile, Encoding.UTF8));
            }
            catch { return null; }
        }

        public static List<GitHubRepoInfo> FetchAllOwnerRepos()
        {
            string ignored;
            return FetchAllOwnerReposCached(null, false, out ignored);
        }

        public static List<GitHubRepoInfo> FetchAllOwnerReposCached(string suiteRoot, bool allowConditional, out string sourceLabel)
        {
            sourceLabel = "live";
            string cacheFile = string.IsNullOrEmpty(suiteRoot) ? null : Path.Combine(suiteRoot, ".nillyhub_repos_cache.json");
            string etagFile = string.IsNullOrEmpty(suiteRoot) ? null : Path.Combine(suiteRoot, ".nillyhub_repos.etag");
            string prevEtag = null;
            if (allowConditional && !string.IsNullOrEmpty(etagFile) && File.Exists(etagFile) && File.Exists(cacheFile))
            {
                try { prevEtag = File.ReadAllText(etagFile, Encoding.UTF8).Trim(); } catch { }
            }

            string newEtag;
            string json = HttpGetConditional("https://api.github.com/users/" + OWNER + "/repos?per_page=60&sort=updated", prevEtag, out newEtag);
            if (json == null && !string.IsNullOrEmpty(cacheFile) && File.Exists(cacheFile))
            {
                sourceLabel = "cached";
                return ParseRepoListJson(File.ReadAllText(cacheFile, Encoding.UTF8));
            }
            if (string.IsNullOrEmpty(json)) return new List<GitHubRepoInfo>();

            if (!string.IsNullOrEmpty(cacheFile))
            {
                try
                {
                    File.WriteAllText(cacheFile, json, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(newEtag)) File.WriteAllText(etagFile, newEtag, Encoding.UTF8);
                }
                catch { }
            }
            return ParseRepoListJson(json);
        }

        private static List<GitHubRepoInfo> ParseRepoListJson(string json)
        {
            List<GitHubRepoInfo> list = new List<GitHubRepoInfo>();
            if (string.IsNullOrEmpty(json)) return list;
            List<object> arr = MiniJson.Parse(json) as List<object>;
            if (arr == null) return list;
            for (int i = 0; i < arr.Count; i++)
            {
                Dictionary<string, object> o = arr[i] as Dictionary<string, object>;
                if (o == null) continue;
                string name = GetStr(o, "name");
                if (string.IsNullOrEmpty(name)) continue;
                GitHubRepoInfo info = new GitHubRepoInfo();
                info.Name = name;
                info.Description = GetStr(o, "description") ?? "";
                info.Homepage = GetStr(o, "homepage") ?? "";
                info.DefaultBranch = GetStr(o, "default_branch") ?? "main";
                info.PushedAtRaw = GetStr(o, "pushed_at") ?? GetStr(o, "updated_at") ?? "";
                DateTime dt;
                if (DateTime.TryParse(info.PushedAtRaw, null, DateTimeStyles.AdjustToUniversal, out dt))
                    info.PushedAtUtc = dt;
                list.Add(info);
            }
            return list;
        }

        private static string GetStr(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) ? (v as string) : null;
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
            { "nillyclip-pro", "NillyClip-Pro" },
            { "nillyhub", "NillyHub" }
        };

        public static bool TryCheckHubSelfUpdate(DateTime hubRepoPushedUtc, out string newTag, out string zipUrl, out string sha256, out long sizeBytes)
        {
            newTag = null; zipUrl = null; sha256 = null; sizeBytes = 0;
            try
            {
                Version curVer = Assembly.GetExecutingAssembly().GetName().Version;
                DateTime exeBuilt = File.GetLastWriteTimeUtc(Application.ExecutablePath);
                if (hubRepoPushedUtc != DateTime.MinValue && hubRepoPushedUtc <= exeBuilt.AddMinutes(10))
                    return false;
                string url = ResolvePortableZip("nillyhub", out newTag, out sha256, out sizeBytes);
                Version remVer;
                if (!TryParseTagVersion(newTag, out remVer) || remVer <= curVer)
                    return false;
                zipUrl = url;
                return true;
            }
            catch { return false; }
        }

        private static bool TryParseTagVersion(string tag, out Version ver)
        {
            ver = null;
            if (string.IsNullOrEmpty(tag) || !tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)) return false;
            string[] parts = tag.Substring(1).Split('.');
            if (parts.Length < 2 || parts.Length > 3) return false;
            int maj, min, pat = 0;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out maj)) return false;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out min)) return false;
            if (parts.Length == 3 && !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out pat)) return false;
            ver = new Version(maj, min, pat, 0);
            return true;
        }

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
            if (string.IsNullOrEmpty(json) || json.Length > 256 * 1024)
                throw new InvalidDataException("Μη έγκυρη απόκριση GitHub release.");
            Dictionary<string, object> release = MiniJson.Parse(json) as Dictionary<string, object>;
            object draftVal = null, preVal = null, tagVal = null, assetsVal = null;
            if (release == null ||
                !release.TryGetValue("draft", out draftVal) || !object.Equals(draftVal, false) ||
                !release.TryGetValue("prerelease", out preVal) || !object.Equals(preVal, false))
                throw new InvalidDataException("Χρειάζεται δημόσια σταθερή έκδοση.");
            release.TryGetValue("tag_name", out tagVal);
            tag = tagVal as string;
            if (tag == null || !Regex.IsMatch(tag, @"^v[0-9]+(\.[0-9]+){1,2}$"))
                throw new InvalidDataException("Μη αναγνωρισμένο tag έκδοσης.");
            string name = prefix + "-" + tag + "-Portable.zip";
            string url = "https://github.com/" + OWNER + "/" + repoName + "/releases/download/" + tag + "/" + name;
            digest = null;
            size = 0;
            release.TryGetValue("assets", out assetsVal);
            List<object> assets = assetsVal as List<object>;
            if (assets != null)
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    Dictionary<string, object> asset = assets[i] as Dictionary<string, object>;
                    if (asset == null || !object.Equals(GetStr(asset, "name"), name)) continue;
                    if (digest != null ||
                        !object.Equals(GetStr(asset, "state"), "uploaded") ||
                        !object.Equals(GetStr(asset, "browser_download_url"), url))
                        throw new InvalidDataException("Το portable ZIP δεν αντιστοιχεί στη σταθερή έκδοση.");
                    string rawDigest = GetStr(asset, "digest");
                    if (rawDigest == null || !Regex.IsMatch(rawDigest, @"^sha256:[0-9a-f]{64}$"))
                        throw new InvalidDataException("Απουσιάζει το SHA-256 του portable ZIP.");
                    digest = rawDigest.Substring(7);
                    object szObj;
                    if (!asset.TryGetValue("size", out szObj))
                        throw new InvalidDataException("Απουσιάζει το μέγεθος του portable ZIP.");
                    size = Convert.ToInt64(szObj, CultureInfo.InvariantCulture);
                    if (size <= 0 || size > 32L * 1024L * 1024L)
                        throw new InvalidDataException("Μη αποδεκτό μέγεθος ZIP.");
                }
            }
            if (digest == null) throw new FileNotFoundException("Δεν βρέθηκε επαληθεύσιμο portable ZIP.");
            return url;
        }

        public static void DownloadVerifiedZip(string url, string destination, string expectedHash, long expectedBytes)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "NillyHub/1.4 (https://nillygraphics.com/)";
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

        private static string TryFindInstalledLaunchPath(SuiteApp app, string suiteRoot)
        {
            if (string.IsNullOrEmpty(app.LaunchFile)) return null;

            // 1. Canonical NillySuite install directory (%LOCALAPPDATA%\NillySuite\<Folder>)
            string p1 = Path.Combine(suiteRoot, app.FolderName, app.LaunchFile);
            if (File.Exists(p1)) return p1;

            // 2. Direct %LOCALAPPDATA%\<Folder>
            string p2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), app.FolderName, app.LaunchFile);
            if (File.Exists(p2)) return p2;

            // 3. Windows Registry HKCU\Software\Classes\Applications\<LaunchFile>\shell\open\command
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Applications\" + app.LaunchFile + @"\shell\open\command"))
                {
                    if (rk != null)
                    {
                        string cmd = rk.GetValue("") as string;
                        string extracted = ExtractExeFromCommand(cmd);
                        if (!string.IsNullOrEmpty(extracted) && File.Exists(extracted)) return extracted;
                    }
                }
            }
            catch { }

            // 4. Sibling folder next to NillyHub's directory (portable suite folder layout)
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                DirectoryInfo parent = Directory.GetParent(exeDir);
                if (parent != null)
                {
                    string sib = Path.Combine(parent.FullName, app.FolderName, app.LaunchFile);
                    if (File.Exists(sib)) return sib;
                }
            }
            catch { }

            // 5. Start Menu "Nilly graphics" & Desktop shortcuts (pure-C# .lnk path scan, zero COM!)
            string fromLnk = FindLaunchFromShortcuts(app);
            if (!string.IsNullOrEmpty(fromLnk) && File.Exists(fromLnk)) return fromLnk;

            return null;
        }

        private static string ExtractExeFromCommand(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return null;
            cmd = cmd.Trim();
            if (cmd.StartsWith("\""))
            {
                int end = cmd.IndexOf('"', 1);
                return end > 1 ? cmd.Substring(1, end - 1) : null;
            }
            int sp = cmd.IndexOf(' ');
            return sp > 0 ? cmd.Substring(0, sp) : cmd;
        }

        private static string FindLaunchFromShortcuts(SuiteApp app)
        {
            try
            {
                string[] dirs = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Nilly graphics"),
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                };
                string fileLower = app.LaunchFile.ToLowerInvariant();
                for (int d = 0; d < dirs.Length; d++)
                {
                    if (!Directory.Exists(dirs[d])) continue;
                    string[] lnks = Directory.GetFiles(dirs[d], "*.lnk");
                    for (int i = 0; i < lnks.Length; i++)
                    {
                        string lnkName = Path.GetFileNameWithoutExtension(lnks[i]);
                        if (lnkName.IndexOf(app.DisplayName, StringComparison.OrdinalIgnoreCase) < 0 &&
                            lnkName.IndexOf(app.FolderName, StringComparison.OrdinalIgnoreCase) < 0 &&
                            !(app.Id == "pcmanager" && lnkName.IndexOf("PC Manager", StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                        string target = ScanLnkForPath(lnks[i], fileLower);
                        if (!string.IsNullOrEmpty(target) && File.Exists(target)) return target;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string ScanLnkForPath(string lnkPath, string expectedFileLower)
        {
            try
            {
                byte[] raw = File.ReadAllBytes(lnkPath);
                if (raw.Length < 64 || raw.Length > 32768) return null;
                // Check ASCII + BOTH even (offset 0) & odd (offset 1) UTF-16LE alignments inside Windows .lnk binaries!
                string ascii = Encoding.Default.GetString(raw);
                string hit = MatchFilePath(ascii, expectedFileLower);
                if (hit != null) return hit;
                string utf16Even = Encoding.Unicode.GetString(raw, 0, raw.Length - (raw.Length % 2));
                hit = MatchFilePath(utf16Even, expectedFileLower);
                if (hit != null) return hit;
                if (raw.Length > 3)
                {
                    int oddLen = (raw.Length - 1) - ((raw.Length - 1) % 2);
                    string utf16Odd = Encoding.Unicode.GetString(raw, 1, oddLen);
                    hit = MatchFilePath(utf16Odd, expectedFileLower);
                    if (hit != null) return hit;
                }
                return null;
            }
            catch { return null; }
        }

        private static string MatchFilePath(string text, string expectedFileLower)
        {
            int idx = text.IndexOf(expectedFileLower, StringComparison.OrdinalIgnoreCase);
            while (idx >= 3)
            {
                int end = idx + expectedFileLower.Length;
                int start = idx;
                while (start > 0 && text[start - 1] >= 32 && text[start - 1] != '"' && text[start - 1] != '<' && text[start - 1] != '>' && text[start - 1] != '|')
                    start--;
                for (int s = start; s + 2 < end; s++)
                {
                    if (((text[s] >= 'A' && text[s] <= 'Z') || (text[s] >= 'a' && text[s] <= 'z')) && text[s + 1] == ':' && text[s + 2] == '\\')
                    {
                        string cand = text.Substring(s, end - s).Trim();
                        if (File.Exists(cand)) return cand;
                    }
                }
                idx = text.IndexOf(expectedFileLower, end, StringComparison.OrdinalIgnoreCase);
            }
            return null;
        }

        public static void RefreshLocalState(SuiteApp app, string suiteRoot)
        {
            string found = TryFindInstalledLaunchPath(app, suiteRoot);
            if (!string.IsNullOrEmpty(found))
            {
                app.LocalLaunchPath = found;
                app.LocalDir = Path.GetDirectoryName(found);
                app.IsInstalled = true;
            }
            else
            {
                app.LocalDir = Path.Combine(suiteRoot, app.FolderName);
                app.LocalLaunchPath = Path.Combine(app.LocalDir, app.LaunchFile);
                app.IsInstalled = false;
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
                    int count = processes.Length;
                    for (int p = 0; p < count; p++) processes[p].Dispose();
                    if (count != 0)
                        throw new InvalidOperationException("Κλείσε πρώτα το " + app.DisplayName + " για να κρατήσεις τις αλλαγές σου.");
                }

                string suiteRoot = DetectSuiteRoot();
                string target = Path.Combine(suiteRoot, app.FolderName);
                string backup = target + ".previous-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                if (Directory.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Ο φάκελος εγκατάστασης παραπέμπει αλλού.");

                // Clean up stale backups from any earlier aborted runs
                CleanupOldBackups(suiteRoot, app.FolderName);

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

                    // Verified clean install: remove temporary rollback backup so disk stays spotless!
                    if (previousMoved && Directory.Exists(backup))
                        try { Directory.Delete(backup, true); } catch { }
                }
                catch
                {
                    if (installing && Directory.Exists(target)) Directory.Delete(target, true);
                    if (previousMoved && Directory.Exists(backup)) Directory.Move(backup, target);
                    throw;
                }
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        private static void CleanupOldBackups(string suiteRoot, string folderName)
        {
            try
            {
                string[] oldDirs = Directory.GetDirectories(suiteRoot, folderName + ".previous-*");
                for (int i = 0; i < oldDirs.Length; i++)
                    try { Directory.Delete(oldDirs[i], true); } catch { }
            }
            catch { }
        }

        internal static void ExtractZipSmart(string zipPath, string targetDir)
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
    // FLICKER-FREE DOUBLE-BUFFERED FLOW PANEL
    // ========================================================================
    internal sealed class DoubleBufferedFlowPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    // ========================================================================
    // ULTRA-MINIMAL SQUARE TILE WITH CACHED ICON & CRISP VECTOR MINI-ICONS
    // ========================================================================
    internal sealed class SquareAppTile : Control
    {
        public readonly SuiteApp App;
        public event Action<SuiteApp> LaunchClicked;
        public event Action<SuiteApp> UpdateClicked;
        public event Action<SuiteApp> FolderClicked;
        public event Action<SuiteApp> SiteClicked;
        public event Action<SuiteApp> UninstallClicked;

        private bool _dark = true;
        private bool _tileHover = false;
        private int _hoverIconIdx = -1; // 0=Open/Install, 1=Update, 2=Folder, 3=Web
        private Bitmap _iconBmp;
        private string _cachedIconKey;
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

            int btnSz = 26;
            int gap = 8;
            int totalW = 4 * btnSz + 3 * gap;
            int bx = (Width - totalW) / 2;
            int by = Height - 38;
            for (int i = 0; i < 4; i++)
                _btnRects[i] = new Rectangle(bx + i * (btnSz + gap), by, btnSz, btnSz);

            EnsureIconLoaded();
        }

        public void RefreshTile(bool dark)
        {
            _dark = dark;
            EnsureIconLoaded();
            Invalidate();
        }

        private void EnsureIconLoaded()
        {
            string icoPath = (!string.IsNullOrEmpty(App.LocalDir) && !string.IsNullOrEmpty(App.IconFile))
                ? Path.Combine(App.LocalDir, App.IconFile)
                : "";
            string key = "placeholder:" + App.Id;
            if (!string.IsNullOrEmpty(icoPath) && File.Exists(icoPath))
            {
                try { key = icoPath + "|" + File.GetLastWriteTimeUtc(icoPath).Ticks; }
                catch { key = icoPath; }
            }
            if (_iconBmp != null && string.Equals(_cachedIconKey, key, StringComparison.Ordinal))
                return;

            if (_iconBmp != null) { _iconBmp.Dispose(); _iconBmp = null; }
            if (!key.StartsWith("placeholder:"))
                _iconBmp = IconLoader.LoadCrispIcon(icoPath, 56);
            if (_iconBmp == null)
                _iconBmp = IconLoader.DrawPlaceholderIcon(App.Id, App.DisplayName, App.AccentColor, 56);
            _cachedIconKey = key;
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

            using (GraphicsPath gp = IconLoader.RoundRectF(1f, 1f, Width - 3f, Height - 3f, 12f))
            using (SolidBrush br = new SolidBrush(cardBg))
            using (Pen bp = new Pen(border, _tileHover ? 1.6f : 1f))
            {
                g.FillPath(br, gp);
                g.DrawPath(bp, gp);
            }

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

            if (_iconBmp != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconBmp, (Width - 56) / 2, 18, 56, 56);
            }

            Rectangle titleR = new Rectangle(8, 80, Width - 16, 20);
            TextRenderer.DrawText(g, App.DisplayName, FontTitle, titleR, fgTitle,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            Rectangle subR = new Rectangle(8, 100, Width - 16, 17);
            string subTxt = App.IsInstalled ? App.ShortTag : (App.FoundOnGitHub ? (App.ShortTag + " · GitHub") : App.ShortTag);
            TextRenderer.DrawText(g, subTxt, FontSub, subR, fgMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            using (Pen dp = new Pen(divCol, 1f))
                g.DrawLine(dp, 18, Height - 46, Width - 18, Height - 46);

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
                else if (bi == 2) _tip.SetToolTip(this, App.IsInstalled ? ("Άνοιγμα φακέλου: " + App.LocalDir) : "Άνοιγμα τοπικού φακέλου");
                else if (bi == 3) _tip.SetToolTip(this, "Άνοιγμα στο nillygraphics.com / GitHub");
                else
                {
                    string sz = App.IsInstalled ? (Math.Max(1, App.LocalExeBytes / 1024) + " KB · " + App.LocalDir) : "Μη εγκατεστημένο";
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
            if (e.Button == MouseButtons.Right)
            {
                ShowTileContextMenu(e.Location);
                base.OnMouseClick(e);
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            int bi = HitTestBtn(e.Location);
            if (bi == 1) { if (App.FoundOnGitHub && UpdateClicked != null) UpdateClicked(App); }
            else if (bi == 2) { if (App.IsInstalled && FolderClicked != null) FolderClicked(App); }
            else if (bi == 3) { if (SiteClicked != null) SiteClicked(App); }
            else
            {
                if (App.IsInstalled) { if (LaunchClicked != null) LaunchClicked(App); }
                else if (App.FoundOnGitHub && UpdateClicked != null) UpdateClicked(App);
            }
            base.OnMouseClick(e);
        }

        private void ShowTileContextMenu(Point loc)
        {
            ContextMenuStrip cms = new ContextMenuStrip();
            ToolStripMenuItem miRun = new ToolStripMenuItem(App.IsInstalled ? ("▶  Άνοιγμα " + App.DisplayName) : ("↓  Εγκατάσταση " + App.DisplayName + " (0€)"));
            miRun.Enabled = App.IsInstalled || App.FoundOnGitHub;
            miRun.Click += delegate
            {
                if (App.IsInstalled) { if (LaunchClicked != null) LaunchClicked(App); }
                else if (App.FoundOnGitHub && UpdateClicked != null) UpdateClicked(App);
            };
            cms.Items.Add(miRun);

            ToolStripMenuItem miUpd = new ToolStripMenuItem(App.HasUpdate ? "↻  Ενημέρωση από GitHub (Νέα έκδοση!)" : "↻  Έλεγχος / Επανεγκατάσταση από GitHub");
            miUpd.Enabled = App.FoundOnGitHub && !App.Busy;
            miUpd.Click += delegate { if (UpdateClicked != null) UpdateClicked(App); };
            cms.Items.Add(miUpd);

            ToolStripMenuItem miDir = new ToolStripMenuItem("📁  Άνοιγμα τοπικού φακέλου");
            miDir.Enabled = App.IsInstalled;
            miDir.Click += delegate { if (FolderClicked != null) FolderClicked(App); };
            cms.Items.Add(miDir);

            ToolStripMenuItem miWeb = new ToolStripMenuItem("🌐  Σελίδα nillygraphics.com / GitHub");
            miWeb.Click += delegate { if (SiteClicked != null) SiteClicked(App); };
            cms.Items.Add(miWeb);

            if (App.IsInstalled)
            {
                cms.Items.Add(new ToolStripSeparator());
                ToolStripMenuItem miUn = new ToolStripMenuItem("✕  Απεγκατάσταση " + App.DisplayName + "...");
                miUn.ForeColor = Color.FromArgb(200, 38, 60);
                miUn.Click += delegate { if (UninstallClicked != null) UninstallClicked(App); };
                cms.Items.Add(miUn);
            }
            cms.Closed += delegate { cms.Dispose(); };
            cms.Show(this, loc);
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
        private static readonly Font DlgFont = new Font("Segoe UI", 9f);

        public WindowsSwitchDialog(bool dark)
        {
            Text = "NillyHub — Ρυθμίσεις Windows";
            ClientSize = new Size(490, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = DlgFont;
            BackColor = dark ? Color.FromArgb(24, 25, 30) : Color.White;
            ForeColor = dark ? Color.White : Color.Black;
            HandleCreated += delegate { Native.SetDarkTitlebar(Handle, dark); };

            Label guide = new Label
            {
                Left = 20, Top = 18, Width = 450, Height = 64,
                Text = "Άλλαξε τις προεπιλεγμένες εφαρμογές μέσα από τα Windows. Για το NillyClip Pro, άνοιξε τις ρυθμίσεις Προχείρου και διάλεξε εσύ αν θέλεις ιστορικό ή συγχρονισμό."
            };
            Button defaults = SettingsButton("Προεπιλογές Windows", "ms-settings:defaultapps", 20, dark);
            Button clipboard = SettingsButton("Πρόχειρο & Συγχρονισμός", "ms-settings:clipboard", 232, dark);
            Controls.AddRange(new Control[] { guide, defaults, clipboard });
        }

        private static Button SettingsButton(string label, string page, int left, bool dark)
        {
            Button button = new Button
            {
                Text = label, Left = left, Top = 96, Width = 218, Height = 34,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = dark ? Color.FromArgb(38, 42, 50) : Color.FromArgb(240, 243, 248),
                ForeColor = dark ? Color.FromArgb(240, 244, 250) : Color.FromArgb(22, 24, 30)
            };
            button.FlatAppearance.BorderColor = dark ? Color.FromArgb(62, 68, 80) : Color.FromArgb(204, 210, 220);
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
        private static readonly Font FontBrand = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        private static readonly Font FontSub = new Font("Segoe UI", 7.8f);
        private static readonly Font FontPill = new Font("Segoe UI", 8.3f, FontStyle.Bold);
        private static readonly Font FontInput = new Font("Segoe UI", 9f);
        private static readonly Font FontFooter = new Font("Segoe UI", 8.2f);
        private static readonly Font FontBanner = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        private readonly string _suiteRoot;
        private readonly List<SuiteApp> _apps;
        private readonly List<SquareAppTile> _tiles = new List<SquareAppTile>();
        private readonly ToolTip _sharedTip = new ToolTip();
        private bool _dark = true;
        private string _filterMode = "all";
        private bool _checking = false;
        private Bitmap _hubLogoBmp;

        private Label _suiteBar;
        private Panel _header;
        private Label _lblTitle, _lblCount;
        private TextBox _txtSearch;
        private Button _btnFAll, _btnFInst, _btnFUpd, _btnCheck, _btnSuiteAll, _btnWinSwap, _btnTheme;

        private DoubleBufferedFlowPanel _grid;
        private Panel _footer;
        private Label _lblStatus, _lblCredit;
        private Button _btnHubUpd;
        private string _hubUpdTag, _hubUpdUrl, _hubUpdSha;
        private long _hubUpdSize;
        private bool _hubUpdating = false;

        public MainForm(bool smokeMode)
        {
            _suiteRoot = SuiteCatalog.DetectSuiteRoot();
            _apps = SuiteCatalog.BuildDefaultCatalog();
            for (int i = 0; i < _apps.Count; i++) SuiteCatalog.RefreshLocalState(_apps[i], _suiteRoot);

            Text = "NillyHub — Nilly graphics";
            int maxH = 744;
            try { maxH = Math.Min(744, Math.Max(520, Screen.PrimaryScreen.WorkingArea.Height - 40)); } catch { }
            Size = new Size(1018, maxH);
            MinimumSize = new Size(440, 400);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyHub.ico");
            try
            {
                if (File.Exists(icoPath)) Icon = new Icon(icoPath);
                else Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            _hubLogoBmp = IconLoader.LoadCrispIcon(icoPath, 32) ?? IconLoader.DrawPlaceholderIcon("hub", "NillyHub", Color.FromArgb(252, 150, 31), 32);

            BuildUi();
            CreateTilesOnce();
            LoadInstantCache();
            ApplyFilters();
            ApplyTheme();

            if (!smokeMode)
            {
                Shown += delegate
                {
                    Native.SetCueBanner(_txtSearch, "Αναζήτηση… (Ctrl+F)");
                    CheckGitHubUpdatesAsync(false);
                    Native.TrimWorkingSet();
                };
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                _txtSearch.Focus();
                _txtSearch.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Escape && (!string.IsNullOrEmpty(_txtSearch.Text) || _filterMode != "all"))
            {
                _txtSearch.Text = "";
                _filterMode = "all";
                StylePills();
                ApplyFilters();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void BuildUi()
        {
            // Top Comparison Banner (docked at the very top: y = 0..28)
            _suiteBar = new Label
            {
                Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter,
                Font = FontBanner, UseMnemonic = false,
                Text = "~6 GB βαριά προγράμματα → Μόνο 1,2 MB (99,98% πιο ελαφριά) · 11 δωρεάν εργαλεία (0€)",
                AccessibleName = "Σύγκριση σουίτας",
                AccessibleDescription = "11 δωρεάν εργαλεία Windows σε ~1,2 MB (99,98% πιο ελαφριά από ~6 GB αντίστοιχων εφαρμογών)."
            };
            _sharedTip.SetToolTip(_suiteBar, "Σύγκριση: ~6 GB (Acrobat Pro, Bridge, Photos, Snipping Tool, Media Player, Notepad, Calc, Sublime, TreeSize) vs 1,24 MiB όλη η Nilly Suite (MIT · 0€ · 0 MB RAM στο παρασκήνιο)");

            // Main Header Bar (docked cleanly directly below _suiteBar: y = 28..82)
            _header = new Panel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(14, 10, 14, 10) };

            Panel leftBrand = new Panel { Dock = DockStyle.Left, Width = 175 };
            PictureBox picLogo = new PictureBox { Left = 0, Top = 1, Width = 32, Height = 32, Image = _hubLogoBmp, SizeMode = PictureBoxSizeMode.Zoom };
            _lblTitle = new Label { Left = 40, Top = 0, Width = 130, Height = 20, Font = FontBrand, Text = "NillyHub", UseMnemonic = false };
            _lblCount = new Label { Left = 41, Top = 20, Width = 134, Height = 14, Font = FontSub, Text = "Nilly graphics Suite", UseMnemonic = false };
            leftBrand.Controls.AddRange(new Control[] { picLogo, _lblTitle, _lblCount });

            FlowLayoutPanel rightBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 816, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Padding = new Padding(0, 3, 0, 0)
            };

            _btnTheme = MkPill("Light", 46, delegate { _dark = !_dark; ApplyTheme(); });
            _btnWinSwap = MkPill("⇄ Windows", 92, delegate { OpenWindowsSwitchWizard(); });
            _btnSuiteAll = MkPill("↓ Όλη η Σουίτα", 112, delegate { InstallOrUpdateFullSuite(); });
            _btnCheck = MkPill("↻ Sync", 62, delegate { CheckGitHubUpdatesAsync(true); });

            _txtSearch = new TextBox { Width = 124, Height = 26, BorderStyle = BorderStyle.FixedSingle, Font = FontInput, Margin = new Padding(4, 2, 4, 0) };
            _txtSearch.TextChanged += delegate { ApplyFilters(); };

            _btnFUpd = MkPill("Updates", 82, delegate { _filterMode = "updates"; ApplyFilters(); StylePills(); });
            _btnFInst = MkPill("Τοπικά", 86, delegate { _filterMode = "installed"; ApplyFilters(); StylePills(); });
            _btnFAll = MkPill("Όλα", 74, delegate { _filterMode = "all"; ApplyFilters(); StylePills(); });

            _sharedTip.SetToolTip(_btnCheck, "Έλεγχος ενημερώσεων από github.com/nillynp (ETag cache)");
            _sharedTip.SetToolTip(_btnSuiteAll, "Λήψη & Ενημέρωση όλων των εργαλείων της Σουίτας μαζί (1 κλικ)");
            _sharedTip.SetToolTip(_btnWinSwap, "Ασφαλής αντικατάσταση εφαρμογών Windows (Προεπιλογές & Πρόχειρο)");
            _sharedTip.SetToolTip(_btnTheme, "Εναλλαγή Dark / Light");
            _sharedTip.SetToolTip(_txtSearch, "Αναζήτηση εργαλείου (Ctrl+F)...");

            rightBar.Controls.AddRange(new Control[] { _btnTheme, _btnWinSwap, _btnSuiteAll, _btnCheck, _txtSearch, _btnFUpd, _btnFInst, _btnFAll });
            _header.Controls.Add(rightBar);
            _header.Controls.Add(leftBrand);

            // Minimal Footer (docked at bottom)
            _footer = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(14, 2, 14, 2) };
            _lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = FontFooter, UseMnemonic = false, Text = "Έτοιμο" };
            _btnHubUpd = new Button
            {
                Dock = DockStyle.Right, Width = 148, Visible = false,
                FlatStyle = FlatStyle.Flat, Font = FontPill, Cursor = Cursors.Hand, TabStop = false,
                BackColor = Color.FromArgb(252, 150, 31), ForeColor = Color.FromArgb(14, 18, 24),
                Text = "⬆ Hub Update"
            };
            _btnHubUpd.FlatAppearance.BorderSize = 0;
            _btnHubUpd.Click += delegate { RunHubSelfUpdate(); };
            _sharedTip.SetToolTip(_btnHubUpd, "Αυτόματη SHA-256 ενημέρωση του NillyHub από GitHub");
            _lblCredit = new Label { Dock = DockStyle.Right, Width = 365, TextAlign = ContentAlignment.MiddleRight, Font = FontFooter, Cursor = Cursors.Hand, UseMnemonic = false, Text = "created by Nilly graphics (Sotiris Christos) · nillygraphics.com" };
            _lblCredit.Click += delegate { try { Process.Start(new ProcessStartInfo("https://nillygraphics.com/") { UseShellExecute = true }); } catch { } };
            _footer.Controls.Add(_lblStatus);
            _footer.Controls.Add(_btnHubUpd);
            _footer.Controls.Add(_lblCredit);

            // Flicker-free Double-Buffered Square Grid (docked Fill — MUST be z-index 0 so NOTHING clips Row 1!)
            _grid = new DoubleBufferedFlowPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(18, 14, 14, 14)
            };

            // CRITICAL WinForms Docking Order: back-to-front!
            // _suiteBar (backmost, docked 1st at y=0) -> _header (docked 2nd at y=28) -> _footer (bottom) -> _grid (frontmost z=0, Fill between header & footer!)
            Controls.Add(_grid);
            Controls.Add(_footer);
            Controls.Add(_header);
            Controls.Add(_suiteBar);
        }

        private void CreateTilesOnce()
        {
            _grid.SuspendLayout();
            for (int i = 0; i < _apps.Count; i++)
            {
                SquareAppTile tile = new SquareAppTile(_apps[i], _sharedTip);
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
                tile.UninstallClicked += OnUninstallApp;
                _tiles.Add(tile);
                _grid.Controls.Add(tile);
            }
            _grid.ResumeLayout(true);
        }

        private void LoadInstantCache()
        {
            List<GitHubRepoInfo> cached = GitHubClient.LoadCachedRepos(_suiteRoot);
            if (cached != null && cached.Count > 0)
                ApplyRepoListToApps(cached, false);
        }

        private static Button MkPill(string txt, int w, EventHandler onClick)
        {
            Button b = new Button();
            b.UseMnemonic = false;
            b.Text = txt; b.Width = w; b.Height = 26;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 1;
            b.Font = FontPill;
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
            _suiteBar.BackColor = _dark ? Color.FromArgb(40, 29, 18) : Color.FromArgb(254, 243, 226);
            _suiteBar.ForeColor = _dark ? Color.FromArgb(255, 185, 95) : Color.FromArgb(146, 64, 14);
            _lblTitle.ForeColor = fgMain;
            _lblCount.ForeColor = fgMuted;
            _lblStatus.ForeColor = fgMuted;
            _lblCredit.ForeColor = orange;
            _txtSearch.BackColor = _dark ? Color.FromArgb(22, 24, 28) : Color.White;
            _txtSearch.ForeColor = fgMain;
            _btnTheme.Text = _dark ? "Light" : "Dark";

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

        // Zero-allocation instant visibility filter (no Win32 control churn, no icon reload!)
        private void ApplyFilters()
        {
            string q = (_txtSearch.Text ?? "").Trim();
            int inst = 0, upd = 0;

            _grid.SuspendLayout();
            for (int i = 0; i < _tiles.Count; i++)
            {
                SquareAppTile tile = _tiles[i];
                SuiteApp a = tile.App;
                if (a.IsInstalled) inst++;
                if (a.HasUpdate) upd++;

                bool show = true;
                if (_filterMode == "installed" && !a.IsInstalled) show = false;
                else if (_filterMode == "updates" && !a.HasUpdate) show = false;
                else if (q.Length > 0 &&
                         a.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                         a.ShortTag.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                         a.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                    show = false;

                if (tile.Visible != show) tile.Visible = show;
                tile.RefreshTile(_dark);
            }
            _grid.ResumeLayout(true);

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

        private void OnUninstallApp(SuiteApp app)
        {
            if (!app.IsInstalled || app.Busy || string.IsNullOrEmpty(app.LocalDir)) return;
            if (Directory.Exists(Path.Combine(app.LocalDir, ".git")))
            {
                MessageBox.Show(this,
                    "Το " + app.DisplayName + " βρίσκεται σε φάκελο Git/ανάπτυξης (" + app.LocalDir + ") και δεν διαγράφεται αυτόματα για προστασία του κώδικά σου.",
                    "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult dr = MessageBox.Show(this,
                "Απεγκατάσταση του " + app.DisplayName + ";\r\nΦάκελος: " + app.LocalDir,
                "Απεγκατάσταση — NillyHub", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                if (!string.IsNullOrEmpty(app.ProcessName))
                {
                    Process[] ps = Process.GetProcessesByName(app.ProcessName);
                    int cnt = ps.Length;
                    for (int i = 0; i < cnt; i++) ps[i].Dispose();
                    if (cnt > 0)
                    {
                        MessageBox.Show(this, "Κλείσε πρώτα το " + app.DisplayName + " πριν την απεγκατάσταση.", "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                string unPs1 = Path.Combine(app.LocalDir, "Uninstall_" + Path.GetFileNameWithoutExtension(app.LaunchFile) + ".ps1");
                if (File.Exists(unPs1))
                {
                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + unPs1 + "\" -Silent");
                    psi.WorkingDirectory = app.LocalDir;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    using (Process p = Process.Start(psi)) { if (p != null) p.WaitForExit(30000); }
                }
                string suiteRootFull = Path.GetFullPath(_suiteRoot).TrimEnd('\\') + "\\";
                string appDirFull = Path.GetFullPath(app.LocalDir).TrimEnd('\\') + "\\";
                if (appDirFull.StartsWith(suiteRootFull, StringComparison.OrdinalIgnoreCase) && Directory.Exists(app.LocalDir))
                {
                    Directory.Delete(app.LocalDir, true);
                }
                SuiteCatalog.RefreshLocalState(app, _suiteRoot);
                ApplyFilters();
                _lblStatus.Text = "Απεγκαταστάθηκε το " + app.DisplayName + ".";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Αδυναμία απεγκατάστασης " + app.DisplayName + ":\r\n" + ex.Message, "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

        private void CheckGitHubUpdatesAsync(bool forceManual)
        {
            if (_checking) return;
            _checking = true;
            _btnCheck.Enabled = false;
            _lblStatus.Text = "Συγχρονισμός με github.com/" + GitHubClient.OWNER + "...";

            Thread th = new Thread(delegate()
            {
                List<GitHubRepoInfo> repos = null;
                string err = null;
                string src = "live";
                bool hasHubUpd = false;
                string hTag = null, hUrl = null, hSha = null;
                long hSize = 0;
                try
                {
                    repos = GitHubClient.FetchAllOwnerReposCached(_suiteRoot, !forceManual, out src);
                    if (repos != null)
                    {
                        DateTime hubPushed = DateTime.MinValue;
                        for (int i = 0; i < repos.Count; i++)
                            if (string.Equals(repos[i].Name, "nillyhub", StringComparison.OrdinalIgnoreCase))
                            { hubPushed = repos[i].PushedAtUtc; break; }
                        hasHubUpd = GitHubClient.TryCheckHubSelfUpdate(hubPushed, out hTag, out hUrl, out hSha, out hSize);
                    }
                }
                catch (Exception ex) { err = ex.Message; }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        _checking = false;
                        _btnCheck.Enabled = true;
                        if (err != null)
                        {
                            _lblStatus.Text = "Offline (" + err + ")";
                            return;
                        }
                        ApplyRepoListToApps(repos, true);
                        if (hasHubUpd && !string.IsNullOrEmpty(hTag))
                        {
                            _hubUpdTag = hTag; _hubUpdUrl = hUrl; _hubUpdSha = hSha; _hubUpdSize = hSize;
                            _btnHubUpd.Text = "⬆ NillyHub " + hTag;
                            _btnHubUpd.Visible = true;
                        }
                        else
                        {
                            _btnHubUpd.Visible = false;
                        }
                        int upd = 0, gh = 0;
                        for (int i = 0; i < _apps.Count; i++) { if (_apps[i].HasUpdate) upd++; if (_apps[i].FoundOnGitHub) gh++; }
                        string suffix = src == "cached" ? " · ETag 304" : "";
                        _lblStatus.Text = string.Format("github.com/{0} ({1} repos) · {2}{3}",
                            GitHubClient.OWNER, gh, upd > 0 ? (upd + " ενημερώσεις διαθέσιμες") : "Όλα ενημερωμένα", suffix);
                        Native.TrimWorkingSet();
                    });
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }

        private void RunHubSelfUpdate()
        {
            if (_hubUpdating || string.IsNullOrEmpty(_hubUpdUrl) || string.IsNullOrEmpty(_hubUpdSha)) return;
            DialogResult dr = MessageBox.Show(this,
                "Διαθέσιμη νέα έκδοση NillyHub (" + _hubUpdTag + ")!\r\nΘες αυτόματη επαληθευμένη (SHA-256) ενημέρωση & επανεκκίνηση;",
                "Ενημέρωση NillyHub — Nilly graphics", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            _hubUpdating = true;
            _btnHubUpd.Enabled = false;
            _btnHubUpd.Text = "Ενημέρωση...";
            string url = _hubUpdUrl, sha = _hubUpdSha, tag = _hubUpdTag;
            long size = _hubUpdSize;
            string curExe = Application.ExecutablePath;
            int myPid = Process.GetCurrentProcess().Id;

            Thread th = new Thread(delegate()
            {
                string temp = Path.Combine(Path.GetTempPath(), "NillyHub-SelfUpd-" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(temp);
                    string zip = Path.Combine(temp, "nillyhub.zip");
                    string pkg = Path.Combine(temp, "pkg");
                    Directory.CreateDirectory(pkg);

                    BeginInvoke((Action)delegate { _lblStatus.Text = "Λήψη & SHA-256 έλεγχος NillyHub " + tag + "..."; });
                    GitHubClient.DownloadVerifiedZip(url, zip, sha, size);
                    SuiteCatalog.ExtractZipSmart(zip, pkg);

                    string srcCs = Path.Combine(pkg, @"desktop\NillyHub.cs");
                    if (!File.Exists(srcCs)) throw new FileNotFoundException("Λείπει το desktop\\NillyHub.cs από το πακέτο.");

                    string csc = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";
                    if (!File.Exists(csc)) csc = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe";
                    if (!File.Exists(csc)) throw new FileNotFoundException("Δεν βρέθηκε ο C# compiler (.NET Framework 4.x).");

                    BeginInvoke((Action)delegate { _lblStatus.Text = "Μεταγλώττιση NillyHub " + tag + "..."; });
                    string newExe = Path.Combine(temp, "NillyHub.new.exe");
                    string ico = Path.Combine(pkg, "NillyHub.ico");
                    string args = "/nologo /target:winexe /optimize+ /utf8output /platform:anycpu /out:\"" + newExe + "\" " +
                                  "/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll " +
                                  "/r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll " +
                                  (File.Exists(ico) ? ("/win32icon:\"" + ico + "\" ") : "") +
                                  "\"" + srcCs + "\"";

                    ProcessStartInfo cpsi = new ProcessStartInfo(csc, args);
                    cpsi.UseShellExecute = false;
                    cpsi.CreateNoWindow = true;
                    using (Process cp = Process.Start(cpsi))
                    {
                        if (cp == null || !cp.WaitForExit(60000) || cp.ExitCode != 0 || !File.Exists(newExe))
                            throw new InvalidOperationException("Αποτυχία μεταγλώττισης της νέας έκδοσης NillyHub.");
                    }

                    // Write hidden atomic swapper script that runs right after this process exits cleanly
                    string swapPs1 = Path.Combine(temp, "swap.ps1");
                    string script =
                        "param($p,$src,$dst,$tmp)\r\n" +
                        "for($i=0;$i -lt 40;$i++){ if(-not (Get-Process -Id $p -ErrorAction SilentlyContinue)){break}; Start-Sleep -Milliseconds 150 }\r\n" +
                        "Copy-Item -LiteralPath $src -Destination $dst -Force\r\n" +
                        "Start-Process -FilePath $dst -WorkingDirectory (Split-Path -Parent $dst)\r\n" +
                        "Start-Sleep -Milliseconds 400\r\n" +
                        "Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue\r\n";
                    File.WriteAllText(swapPs1, script, Encoding.UTF8);

                    ProcessStartInfo spsi = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + swapPs1 + "\" " +
                        myPid + " \"" + newExe + "\" \"" + curExe + "\" \"" + temp + "\"");
                    spsi.UseShellExecute = false;
                    spsi.CreateNoWindow = true;
                    Process.Start(spsi);

                    BeginInvoke((Action)delegate { Application.Exit(); });
                }
                catch (Exception ex)
                {
                    try { Directory.Delete(temp, true); } catch { }
                    try
                    {
                        BeginInvoke((Action)delegate
                        {
                            _hubUpdating = false;
                            _btnHubUpd.Enabled = true;
                            _btnHubUpd.Text = "⬆ NillyHub " + tag;
                            _lblStatus.Text = "Αποτυχία ενημέρωσης NillyHub.";
                            MessageBox.Show(this, "Αδυναμία ενημέρωσης NillyHub:\r\n" + ex.Message, "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                    }
                    catch { }
                }
            });
            th.IsBackground = true;
            th.Start();
        }

        private void ApplyRepoListToApps(List<GitHubRepoInfo> repos, bool updateUi)
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
                }
                SuiteCatalog.RefreshLocalState(a, _suiteRoot);
            }

            if (updateUi) ApplyFilters();
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
                        ApplyFilters();
                        if (err != null)
                        {
                            _lblStatus.Text = "Σφάλμα: " + app.DisplayName;
                            MessageBox.Show(this, "Αδυναμία ενημέρωσης " + app.DisplayName + ":\r\n" + err, "NillyHub", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else _lblStatus.Text = "✓ Έτοιμο & ενημερωμένο: " + app.DisplayName;
                        Native.TrimWorkingSet();
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
                "Λήψη & εγκατάσταση/ενημέρωση " + queue.Count + " εργαλείων της Nilly Suite από το github.com/nillynp;\r\n(100% Δωρεάν · Zero Admin · SHA-256 Επαλήθευση)",
                "Εγκατάσταση Όλης της Σουίτας — NillyHub", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            _btnSuiteAll.Enabled = false;
            Thread th = new Thread(delegate()
            {
                int ok = 0;
                List<string> failed = new List<string>();
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
                    catch (Exception ex)
                    {
                        failed.Add(a.DisplayName + ": " + ex.Message);
                    }
                    a.Busy = false;
                    SuiteCatalog.RefreshLocalState(a, _suiteRoot);
                }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        ApplyFilters();
                        _lblStatus.Text = string.Format("✓ Ολοκληρώθηκε η Σουίτα ({0}/{1} εργαλεία έτοιμα)!", ok, queue.Count);
                        if (failed.Count > 0)
                        {
                            MessageBox.Show(this,
                                "Ολοκληρώθηκαν " + ok + "/" + queue.Count + " εργαλεία.\r\n\r\nΑποτυχίες:\r\n• " + string.Join("\r\n• ", failed.ToArray()),
                                "NillyHub — Αναφορά Σουίτας", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        Native.TrimWorkingSet();
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
                    Process self = Process.GetCurrentProcess();
                    Process[] ps = Process.GetProcessesByName(self.ProcessName);
                    int myId = self.Id;
                    try
                    {
                        for (int i = 0; i < ps.Length; i++)
                        {
                            if (ps[i].Id != myId && ps[i].MainWindowHandle != IntPtr.Zero)
                            {
                                Native.ShowWindow(ps[i].MainWindowHandle, Native.SW_RESTORE);
                                Native.SetForegroundWindow(ps[i].MainWindowHandle);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < ps.Length; i++) ps[i].Dispose();
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
                    Native.TrimWorkingSet();
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
