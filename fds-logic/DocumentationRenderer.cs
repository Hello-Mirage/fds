using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FdsLogic;

public static class DocumentationRenderer
{
    private static readonly SKColor Bg = new SKColor(8, 8, 12);
    private static readonly SKColor Cyan = new SKColor(0, 255, 255);
    private static readonly SKColor Green = new SKColor(76, 175, 80);
    private static readonly SKColor Purple = new SKColor(150, 100, 255);
    private static readonly SKColor Orange = new SKColor(255, 165, 0);
    private static readonly SKColor White = SKColors.White;
    private static readonly SKColor Gray = new SKColor(140, 140, 140);
    private static readonly SKColor DarkGray = new SKColor(80, 80, 80);
    private static readonly SKColor CardBg = new SKColor(16, 16, 22);
    private static readonly SKColor BorderColor = new SKColor(40, 40, 50);

    private static string _clickNotification = "";
    private static double _notificationTime = 0;
    private static string _currentPage = "Home"; 
    public static bool IsServer { get; set; } = false;
    private static float _pulse = 0;
    private static readonly List<Particle> _particles = new List<Particle>();
    private static DateTime _lastPhysicsUpdate = DateTime.Now;

    // --- GPU Resident Vertex Buffers ---
    private static readonly SKPoint[] _vPoints = new SKPoint[1024];
    private static readonly SKColor[] _vColors = new SKColor[1024];
    private static int _residentCount = 0;

    private class Particle {
        public float X, Y, VX, VY, Radius;
        public SKColor Color;
        public float Life = 1.0f;
    }

    private struct LayoutContext
    {
        public float Margin, ContentW, Left, HeroH, BadgeY, TitleSize, SubY, TagY, BtnY, BtnW, BtnH, BtnGap;
        public float DocsX, DocsY, StatsY;
        public bool IsMobile;
    }

    private static LayoutContext GetLayout(float width, float height)
    {
        var ctx = new LayoutContext();
        ctx.IsMobile = width < 600;
        ctx.Margin = Math.Max(width * 0.06f, 24);
        ctx.ContentW = Math.Min(width - ctx.Margin * 2, 900);
        ctx.Left = (width - ctx.ContentW) / 2;
        ctx.HeroH = ctx.IsMobile ? height * 0.85f : height * 0.9f;
        ctx.BadgeY = ctx.HeroH * 0.25f;
        ctx.TitleSize = ctx.IsMobile ? 48 : Math.Min(ctx.ContentW * 0.1f, 84);
        ctx.SubY = ctx.BadgeY + 50 + ctx.TitleSize + 10;
        ctx.TagY = ctx.SubY + 30;
        ctx.BtnY = ctx.TagY + 60;
        ctx.BtnW = ctx.IsMobile ? ctx.ContentW : 200;
        ctx.BtnH = 48;
        ctx.BtnGap = 16;
        ctx.DocsX = ctx.IsMobile ? ctx.Left : ctx.Left + ctx.BtnW + ctx.BtnGap;
        ctx.DocsY = ctx.IsMobile ? ctx.BtnY + ctx.BtnH + 12 : ctx.BtnY;
        float btm = ctx.IsMobile ? ctx.DocsY + ctx.BtnH : ctx.BtnY + ctx.BtnH;
        ctx.StatsY = btm + (ctx.IsMobile ? 40 : 50);
        return ctx;
    }

    public static void HandleClick(float x, float y, float width, float height, float scrollOffset)
    {
        var ctx = GetLayout(width, height);
        float worldY = y + scrollOffset;
        float navH = 64;
        if (y < navH)
        {
            float nl = ctx.Left;
            if (x >= nl && x <= nl + 80) { _currentPage = "Home"; return; }
            if (x >= nl + 100 && x <= nl + 160) { _currentPage = "Docs"; return; }
            if (x >= nl + 180 && x <= nl + 280) { _currentPage = "QuickStart"; return; }
            if (x >= nl + 300 && x <= nl + 400) { _currentPage = "Arcade"; return; }
        }

        if (_currentPage == "Home")
        {
            if (x >= ctx.Left && x <= ctx.Left + ctx.BtnW && worldY >= ctx.BtnY && worldY <= ctx.BtnY + ctx.BtnH) _currentPage = "QuickStart";
            else if (x >= ctx.DocsX && x <= ctx.DocsX + ctx.BtnW && worldY >= ctx.DocsY && worldY <= ctx.DocsY + ctx.BtnH) _currentPage = "Docs";
        }
        else if (_currentPage == "Arcade") SpawnParticles(x, worldY);
        else { _clickNotification = "Interaction Recieved!"; _notificationTime = DateTime.Now.TimeOfDay.TotalSeconds; }
    }

    public static float Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        double currentTime = DateTime.Now.TimeOfDay.TotalSeconds;
        float time = (float)currentTime;
        canvas.Clear(Bg);
        var ctx = GetLayout(width, height);
        DrawGridBackground(canvas, width, height, scrollOffset, time);

        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, width, height));
        canvas.Translate(0, -scrollOffset);

        float y = 80;
        switch (_currentPage)
        {
            case "Home": y = RenderHomePage(canvas, ctx, time, y); break;
            case "Docs": y = RenderDocsPage(canvas, ctx, time, y); break;
            case "QuickStart": y = RenderQuickStartPage(canvas, ctx, time, y); break;
            case "Arcade": y = RenderArcadePage(canvas, width, height, time); break;
        }

        if (IsServer)
        {
            _pulse = (float)Math.Abs(Math.Sin(DateTime.Now.Ticks / 5000000.0));
            float lx = width - ctx.Margin - 100;
            float ly = 32;
            canvas.DrawRoundRect(new SKRect(lx, ly-10, lx+80, ly+10), 10, 10, new SKPaint { Color = new SKColor(20, 20, 30), Style = SKPaintStyle.Fill });
            canvas.DrawCircle(lx + 10, ly, 4 * _pulse + 2, new SKPaint { Color = Green, IsAntialias = true });
            canvas.DrawText("LIVE FEED", lx + 22, ly + 5, new SKPaint { Color = Green, TextSize = 10, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) });
        }

        y += 80;
        if (!string.IsNullOrEmpty(_clickNotification) && currentTime - _notificationTime < 3.0)
        {
            float alpha = (float)Math.Min(1.0, (3.0 - (currentTime - _notificationTime)) * 2.0);
            DrawToast(canvas, width, height, _clickNotification, alpha);
        }
        canvas.Restore();
        RenderNavBar(canvas, width, ctx, time);
        return y;
    }

    private static void RenderNavBar(SKCanvas canvas, float width, LayoutContext ctx, float time)
    {
        float h = 64;
        canvas.DrawRect(0, 0, width, h, new SKPaint { Color = Bg.WithAlpha(230), IsAntialias = true });
        canvas.DrawLine(0, h, width, h, new SKPaint { Color = BorderColor, StrokeWidth = 1 });
        float nl = ctx.Left;
        using var lp = new SKPaint { Color = Cyan, TextSize = 22, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        canvas.DrawText("FDS", nl, 40, lp);
        using var lk = new SKPaint { Color = White, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        lk.Color = _currentPage == "Docs" ? Cyan : White; canvas.DrawText("DOCS", nl + 100, 38, lk);
        lk.Color = _currentPage == "QuickStart" ? Cyan : White; canvas.DrawText("QUICK START", nl + 180, 38, lk);
        lk.Color = _currentPage == "Arcade" ? Orange : White; canvas.DrawText("ARCADE", nl + 300, 38, lk);
    }

    private static float RenderHomePage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        DrawGlowOrb(canvas, ctx.Margin + ctx.ContentW * 0.7f, y + ctx.HeroH * 0.3f, ctx.ContentW * 0.4f, time, new SKColor(0, 100, 120, 20));
        DrawPulsingBadge(canvas, ctx.Left, y + ctx.BadgeY, "V3.2.0 GPU RESIDENT", time);
        DrawGradientText(canvas, "FAST DRAWING STREAMER", ctx.Left, y + ctx.BadgeY + 50, ctx.TitleSize * 0.8f, time);
        float ty = DrawWrappedText(canvas, "High-performance remote UI transport with WASM-native logic and GPU-resident vertex buffers.", ctx.Left, y + ctx.TagY, ctx.ContentW, 22, new SKColor(220, 220, 230));
        DrawButton(canvas, ctx.Left, ty + 40, ctx.BtnW, ctx.BtnH, "GET STARTED", Cyan, time, true);
        DrawButton(canvas, ctx.DocsX, ty + 40, ctx.BtnW, ctx.BtnH, "ARCHITECTURE", Purple, time, false);
        return ty + 200;
    }

    private static float RenderDocsPage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        DrawSectionTitle(canvas, "DOCS: ARCHITECTURE", ctx.Left, y, ctx.ContentW, time); y += 60;
        y = DrawWrappedText(canvas, "FDS streams UI logic as binary blobs and executing them locally via Skia.", ctx.Left, y, ctx.ContentW, 16, White);
        return y + 100;
    }

    private static float RenderQuickStartPage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        DrawSectionTitle(canvas, "QUICK START GUIDE", ctx.Left, y, ctx.ContentW, time); y += 60;
        y = DrawWrappedText(canvas, "Compile your logic classlib and stream the DLL.", ctx.Left, y, ctx.ContentW, 16, White);
        return y + 100;
    }

    private static float RenderArcadePage(SKCanvas c, float w, float h, float time)
    {
        if (IsServer)
        {
            var now = DateTime.Now;
            float dt = (float)(now - _lastPhysicsUpdate).TotalSeconds;
            _lastPhysicsUpdate = now;
            if (dt > 0.1f) dt = 0.016f;
            foreach (var p in _particles) {
                p.VY += 25.0f * dt; p.X += p.VX; p.Y += p.VY;
                if (p.X < 0 || p.X > w) p.VX *= -0.8f;
                if (p.Y > h - 100) { p.Y = h - 100; p.VY *= -0.6f; }
                p.Life -= dt * 0.4f;
            }
            _particles.RemoveAll(p => p.Life <= 0);
        }

        using var tp = new SKPaint { Color = Orange, TextSize = 40, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText("FDS ARCADE", 40, 140, tp);
        c.DrawLine(0, h - 100, w, h - 100, new SKPaint { Color = BorderColor, StrokeWidth = 2 });

        if (_residentCount > 0)
        {
            var pts = new SKPoint[_residentCount];
            var cls = new SKColor[_residentCount];
            Array.Copy(_vPoints, pts, _residentCount);
            Array.Copy(_vColors, cls, _residentCount);
            using var vertices = SKVertices.CreateCopy(SKVertexMode.Points, pts, null, cls);
            using var fill = new SKPaint { StrokeWidth = 8, StrokeCap = SKStrokeCap.Round, IsAntialias = true };
            c.DrawVertices(vertices, SKBlendMode.SrcOver, fill);
        }
        else if (IsServer)
        {
            using var fill = new SKPaint { IsAntialias = true, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round };
            foreach (var p in _particles) { fill.Color = p.Color.WithAlpha((byte)(255 * p.Life)); c.DrawPoint(p.X, p.Y, fill); }
        }
        return h;
    }

    public static void ApplyVertexUpdates(byte[] delta)
    {
        if (delta.Length < 4) return;
        int count = BitConverter.ToInt32(delta, 0);
        _residentCount = Math.Min(count, 1024);
        int offset = 4;
        for (int i = 0; i < _residentCount; i++)
        {
            if (offset + 12 > delta.Length) break;
            _vPoints[i] = new SKPoint(BitConverter.ToSingle(delta, offset), BitConverter.ToSingle(delta, offset + 4));
            _vColors[i] = new SKColor(delta[offset + 8], delta[offset + 9], delta[offset + 10], delta[offset + 11]);
            offset += 12;
        }
    }

    public static float[] GetParticlePositions()
    {
        var pos = new float[_particles.Count * 2];
        for (int i = 0; i < _particles.Count; i++) { pos[i * 2] = _particles[i].X; pos[i * 2 + 1] = _particles[i].Y; }
        return pos;
    }

    public static byte[] GetParticleColors()
    {
        var cols = new byte[_particles.Count * 4];
        for (int i = 0; i < _particles.Count; i++) {
            cols[i * 4] = _particles[i].Color.Red; cols[i * 4 + 1] = _particles[i].Color.Green;
            cols[i * 4 + 2] = _particles[i].Color.Blue; cols[i * 4 + 3] = _particles[i].Color.Alpha;
        }
        return cols;
    }

    private static void SpawnParticles(float x, float y)
    {
        var rnd = new Random();
        for (int i = 0; i < 10; i++) _particles.Add(new Particle { X = x, Y = y, VX = (float)(rnd.NextDouble() * 10 - 5), VY = (float)(rnd.NextDouble() * -15 - 5), Color = Cyan });
        if (_particles.Count > 1024) _particles.RemoveRange(0, _particles.Count - 1024);
    }

    private static void DrawGridBackground(SKCanvas c, float w, float h, float s, float t)
    {
        using var p = new SKPaint { Color = new SKColor(30, 30, 40, 15), StrokeWidth = 1 };
        float sp = 60; float oy = -(s % sp);
        for (float gy = oy; gy < h; gy += sp) c.DrawLine(0, gy, w, gy, p);
        for (float gx = 0; gx < w; gx += sp) c.DrawLine(gx, 0, gx, h, p);
    }

    private static void DrawGlowOrb(SKCanvas c, float x, float y, float r, float t, SKColor clr)
    {
        using var p = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r, new[] { clr, SKColors.Transparent }, null, SKShaderTileMode.Clamp) };
        c.DrawCircle(x, y, r, p);
    }

    private static void DrawPulsingBadge(SKCanvas c, float x, float y, string text, float t)
    {
        using var tp = new SKPaint { Color = Cyan, TextSize = 10, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        float tw = tp.MeasureText(text); c.DrawRoundRect(new SKRect(x, y, x + tw + 20, y + 24), 12, 12, new SKPaint { Color = new SKColor(0, 255, 255, 20) });
        c.DrawText(text, x + 10, y + 16, tp);
    }

    private static float DrawWrappedText(SKCanvas c, string txt, float x, float y, float mw, float sz, SKColor clr)
    {
        using var p = new SKPaint { Color = clr, TextSize = sz, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        var ws = txt.Split(' '); string ln = ""; float lh = sz + 6;
        foreach (var w in ws) {
            string t = ln == "" ? w : ln + " " + w;
            if (p.MeasureText(t) > mw) { c.DrawText(ln, x, y + sz, p); y += lh; ln = w; } else ln = t;
        }
        c.DrawText(ln, x, y + sz, p); return y + lh;
    }

    private static void DrawButton(SKCanvas c, float x, float y, float w, float h, string txt, SKColor acc, float t, bool pri)
    {
        var r = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = pri ? acc : SKColors.Transparent, IsAntialias = true };
        if (!pri) bg.Style = SKPaintStyle.Stroke;
        c.DrawRoundRect(r, 8, 8, bg);
        using var tp = new SKPaint { Color = pri ? Bg : acc, TextSize = 14, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(txt, x + w / 2, y + h / 2 + 5, tp);
    }

    private static void DrawGradientText(SKCanvas c, string txt, float x, float y, float sz, float t)
    {
        using var p = new SKPaint { TextSize = sz, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold), Shader = SKShader.CreateLinearGradient(new SKPoint(x, y), new SKPoint(x + 300, y + sz), new[] { Cyan, Purple }, null, SKShaderTileMode.Clamp) };
        c.DrawText(txt, x, y + sz, p);
    }

    private static void DrawSectionTitle(SKCanvas c, string txt, float x, float y, float w, float t)
    {
        using var p = new SKPaint { Color = White, TextSize = 24, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(txt, w / 2, y + 24, p);
    }

    private static void DrawToast(SKCanvas c, float w, float h, string txt, float a)
    {
        using var tp = new SKPaint { Color = White.WithAlpha((byte)(255 * a)), TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        float tw = tp.MeasureText(txt); c.DrawRoundRect(new SKRect((w - tw) / 2 - 20, h - 80, (w + tw) / 2 + 20, h - 40), 20, 20, new SKPaint { Color = CardBg.WithAlpha((byte)(220 * a)) });
        c.DrawText(txt, (w - tw) / 2, h - 54, tp);
    }
}
