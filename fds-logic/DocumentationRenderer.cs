using SkiaSharp;
using System;

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
    private static string _currentPage = "Home"; // Routing state: Home, Docs, QuickStart
    public static bool IsServer { get; set; } = false;
    private static float _pulse = 0;

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

        float effectiveBtnBottom = ctx.IsMobile ? ctx.DocsY + ctx.BtnH : ctx.BtnY + ctx.BtnH;
        ctx.StatsY = ctx.IsMobile ? effectiveBtnBottom + 40 : effectiveBtnBottom + 50;

        return ctx;
    }

    public static void HandleClick(float x, float y, float width, float height, float scrollOffset)
    {
        var ctx = GetLayout(width, height);
        float worldY = y + scrollOffset;

        // --- Persistent Navigation Bar Click Handling ---
        float navH = 64;
        if (y < navH)
        {
            float navLeft = ctx.Left;
            if (x >= navLeft && x <= navLeft + 80)
            {
                _currentPage = "Home";
                return;
            }
            if (x >= navLeft + 100 && x <= navLeft + 160)
            {
                _currentPage = "Docs";
                return;
            }
            if (x >= navLeft + 180 && x <= navLeft + 300)
            {
                _currentPage = "QuickStart";
                return;
            }
        }

        // --- Page Specific Click Handling ---
        if (_currentPage == "Home")
        {
            // Hit test GET STARTED -> QuickStart
            if (x >= ctx.Left && x <= ctx.Left + ctx.BtnW && worldY >= ctx.BtnY && worldY <= ctx.BtnY + ctx.BtnH)
            {
                _currentPage = "QuickStart";
            }
            // Hit test VIEW DOCS -> Docs
            else if (x >= ctx.DocsX && x <= ctx.DocsX + ctx.BtnW && worldY >= ctx.DocsY && worldY <= ctx.DocsY + ctx.BtnH)
            {
                _currentPage = "Docs";
            }
        }
        else
        {
            // Basic notification for other pages to confirm interactivity
            _clickNotification = "Interaction Recieved!";
            _notificationTime = DateTime.Now.TimeOfDay.TotalSeconds;
        }
    }

    public static float Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        double currentTime = DateTime.Now.TimeOfDay.TotalSeconds;
        float time = (float)currentTime;
        canvas.Clear(Bg);

        var ctx = GetLayout(width, height);

        // 1. Persistent Background Layer
        DrawGridBackground(canvas, width, height, scrollOffset, time);

        // 2. Main content rendering based on state
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, width, height));
        canvas.Translate(0, -scrollOffset);

        float y = 0;
        
        // Add top padding for Navigation Bar
        y += 80;

        switch (_currentPage)
        {
            case "Home":
                y = RenderHomePage(canvas, ctx, time, y);
                break;
            case "Docs":
                y = RenderDocsPage(canvas, ctx, time, y);
                break;
            case "QuickStart":
                y = RenderQuickStartPage(canvas, ctx, time, y);
                break;
            // --- Hybrid Mode Indicator (Server-Only Overlay) ---
        if (IsServer)
        {
            _pulse = (float)Math.Abs(Math.Sin(DateTime.Now.Ticks / 5000000.0));
            float lx = width - ctx.Margin - 100;
            float ly = 32;
            
            canvas.DrawRoundRect(new SKRect(lx, ly-10, lx+80, ly+10), 10, 10, new SKPaint { Color = new SKColor(20, 20, 30), Style = SKPaintStyle.Fill });
            canvas.DrawCircle(lx + 10, ly, 4 * _pulse + 2, new SKPaint { Color = Green, IsAntialias = true });
            canvas.DrawText("LIVE FEED", lx + 22, ly + 5, new SKPaint { Color = Green, TextSize = 10, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) });
        }
    }

        y += 80; // Bottom spacing

        // 3. Persistent UI Overlay (Toast)
        if (!string.IsNullOrEmpty(_clickNotification) && currentTime - _notificationTime < 3.0)
        {
            float alpha = (float)Math.Min(1.0, (3.0 - (currentTime - _notificationTime)) * 2.0);
            DrawToast(canvas, width, height, _clickNotification, alpha);
        }

        canvas.Restore();

        // 4. Persistent Nav Bar (Static on Screen)
        RenderNavBar(canvas, width, ctx, time);

        return y;
    }

    private static void RenderNavBar(SKCanvas canvas, float width, LayoutContext ctx, float time)
    {
        float navH = 64;
        var rect = new SKRect(0, 0, width, navH);
        
        using var bg = new SKPaint { Color = Bg.WithAlpha(230), IsAntialias = true };
        canvas.DrawRect(rect, bg);
        
        using var border = new SKPaint { Color = BorderColor, StrokeWidth = 1 };
        canvas.DrawLine(0, navH, width, navH, border);

        float navLeft = ctx.Left;
        
        // Logo
        using var logoP = new SKPaint { Color = Cyan, TextSize = 22, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        canvas.DrawText("FDS", navLeft, 40, logoP);

        // Nav Links
        using var linkP = new SKPaint { Color = White, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        
        linkP.Color = _currentPage == "Docs" ? Cyan : White;
        canvas.DrawText("DOCS", navLeft + 100, 38, linkP);
        
        linkP.Color = _currentPage == "QuickStart" ? Cyan : White;
        canvas.DrawText("QUICK START", navLeft + 180, 38, linkP);

        if (_currentPage != "Home")
        {
            using var homeP = new SKPaint { Color = DarkGray, TextSize = 10, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
            canvas.DrawText("<< Back Home", navLeft, 55, homeP);
        }
    }

    private static float RenderHomePage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        // Hero Decorations
        float heroStart = y;
        DrawGlowOrb(canvas, ctx.Margin + ctx.ContentW * 0.7f, heroStart + ctx.HeroH * 0.3f, ctx.ContentW * 0.4f, time, new SKColor(0, 100, 120, 20));
        
        DrawPulsingBadge(canvas, ctx.Left, heroStart + ctx.BadgeY, "V3.0.0 HYBRID STABLE", time);
        DrawGradientText(canvas, "FAST DRAWING STREAMER", ctx.Left, heroStart + ctx.BadgeY + 50, ctx.TitleSize * 0.8f, time);
        
        float taglineFinalY = DrawWrappedText(canvas, "The next generation of remote UI transport. FDS layers reliable WASM-native logic with high-frequency UDP vector overlays for pixel-perfect performance at zero-latency.", ctx.Left, heroStart + ctx.TagY, ctx.ContentW, ctx.IsMobile ? 16 : 22, new SKColor(220, 220, 230));

        // CTA Buttons (Dynamic Padding)
        float ctaY = taglineFinalY + 40;
        DrawButton(canvas, ctx.Left, ctaY, ctx.BtnW, ctx.BtnH, "GET STARTED", Cyan, time, true);
        DrawButton(canvas, ctx.DocsX, ctaY, ctx.BtnW, ctx.BtnH, "ARCHITECTURE", Purple, time, false);

        y = ctaY + 120; // Set next section start relative to CTAs

        // --- SECTION 1: CORE CAPABILITIES ---
        DrawSectionTitle(canvas, "CORE CAPABILITIES", ctx.Left, y, ctx.ContentW, time);
        y += 60;

        float columnW = ctx.IsMobile ? ctx.ContentW : (ctx.ContentW - 32) / 3;
        float columnH = 220;
        
        RenderFeatureColumn(canvas, ctx.Left, y, columnW, columnH, "01", "WASM LOGIC", "Logic modules are streamed as binary chunks and executed at native edge speeds.", Cyan);
        RenderFeatureColumn(canvas, ctx.Left + columnW + 16, y, columnW, columnH, "02", "UDP OVERLAYS", "Real-time vector streams provide buttery-smooth 60fps dynamic overlays.", Purple);
        RenderFeatureColumn(canvas, ctx.Left + (columnW + 16) * 2, y, columnW, columnH, "03", "ZERO LATENCY", "Hit-testing occurs locally on the client host for immediate 0ms engagement.", Green);
        
        y += columnH + 80;

        // --- SECTION 2: TECHNICAL DEPTH (LOREM IPSUM) ---
        DrawSectionTitle(canvas, "TECHNICAL DEEP DIVE", ctx.Left, y, ctx.ContentW, time);
        y += 60;

        y = DrawWrappedText(canvas, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", ctx.Left, y, ctx.ContentW, 16, White);
        y += 30;
        y = DrawWrappedText(canvas, "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.", ctx.Left, y, ctx.ContentW, 16, Gray);
        
        y += 80;

        return y;
    }

    private static void RenderFeatureColumn(SKCanvas canvas, float x, float y, float w, float h, string num, string title, string desc, SKColor color)
    {
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = CardBg, IsAntialias = true };
        canvas.DrawRoundRect(rect, 12, 12, bg);
        
        using var border = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawRoundRect(rect, 12, 12, border);

        using var numP = new SKPaint { Color = color.WithAlpha(50), TextSize = 48, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold), IsAntialias = true };
        canvas.DrawText(num, x + 20, y + 60, numP);

        using var titleP = new SKPaint { Color = White, TextSize = 18, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), IsAntialias = true };
        canvas.DrawText(title, x + 20, y + 90, titleP);

        using var bar = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
        canvas.DrawRect(new SKRect(x + 20, y + 100, x + 50, y + 104), bar);

        DrawWrappedText(canvas, desc, x + 20, y + 130, w - 40, 14, Gray);
    }

    private static float RenderDocsPage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        DrawSectionTitle(canvas, "DOCS: ARCHITECTURE", ctx.Left, y, ctx.ContentW + ctx.Left * 2, time);
        y += 60;

        y = DrawWrappedText(canvas, "FDS (Fast Drawing Streamer) is a distributed architecture for remote user interfaces. Instead of streaming heavy video frames, FDS streams UI logic modules and executes them locally on the client using a native Skia engine.", ctx.Left, y, ctx.ContentW, 16, Gray);
        y += 40;

        using var sectionP = new SKPaint { Color = Cyan, TextSize = 18, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        canvas.DrawText("[ The Architecture ]", ctx.Left, y, sectionP);
        y += 30;

        y = DrawWrappedText(canvas, "FDS uses a V3 Logic Distribution Protocol. The Streamer acts as the authoritative state provider and binary module distributor, while the Client serves as the high-fidelity host.", ctx.Left, y, ctx.ContentW, 14, White);
        y += 40;

        canvas.DrawText("[ Performance Benefits ]", ctx.Left, y, sectionP);
        y += 30;
        y = DrawWrappedText(canvas, "Because the Render logic resides on the client, resizing the window or scrolling has 0ms latency, even on high-latency connections. This eliminates browser overhead and network-induced jitter.", ctx.Left, y, ctx.ContentW, 14, White);

        return y;
    }

    private static float RenderQuickStartPage(SKCanvas canvas, LayoutContext ctx, float time, float y)
    {
        DrawSectionTitle(canvas, "QUICK START GUIDE", ctx.Left, y, ctx.ContentW + ctx.Left * 2, time);
        y += 60;

        y = DrawWrappedText(canvas, "Developing apps for FDS involves defining a Skia drawing loop in a class library and compiling it for distribution.", ctx.Left, y, ctx.ContentW, 16, Gray);
        y += 40;

        using var h3 = new SKPaint { Color = Green, TextSize = 18, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        canvas.DrawText("1. Create logic module", ctx.Left, y, h3);
        y += 30;
        
        y = DrawTerminalBlock(canvas, ctx.Left, y, ctx.ContentW, new[] {
            "$ dotnet new classlib -n MyFdsApp",
            "$ dotnet add package SkiaSharp --version 2.88.9"
        }, time);
        y += 40;

        canvas.DrawText("2. Compile & Stream", ctx.Left, y, h3);
        y += 30;
        y = DrawTerminalBlock(canvas, ctx.Left, y, ctx.ContentW, new[] {
            "$ dotnet build -c Release",
            "$ python run.py"
        }, time);

        return y;
    }

    // --- Drawing Primitives (Shared) ---

    private static void DrawTextCentered(SKCanvas c, string text, float w, float y, float size, SKColor color)
    {
        using var p = new SKPaint { Color = color, TextSize = size, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas") };
        c.DrawText(text, w / 2, y + size, p);
    }

    private static void DrawGridBackground(SKCanvas c, float w, float h, float scroll, float time)
    {
        using var p = new SKPaint { Color = new SKColor(30, 30, 40, 15), StrokeWidth = 1 };
        float spacing = 60;
        float offsetY = -(scroll % spacing);
        for (float gy = offsetY; gy < h; gy += spacing) c.DrawLine(0, gy, w, gy, p);
        for (float gx = 0; gx < w; gx += spacing) c.DrawLine(gx, 0, gx, h, p);
    }

    private static void DrawGlowOrb(SKCanvas c, float x, float y, float r, float time, SKColor color)
    {
        float pulse = 1.0f + (float)Math.Sin(time * 0.5f) * 0.15f;
        using var p = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(x, y), r * pulse, new[] { color, SKColors.Transparent }, null, SKShaderTileMode.Clamp) };
        c.DrawCircle(x, y, r * pulse, p);
    }

    private static void DrawPulsingBadge(SKCanvas c, float x, float y, string text, float time)
    {
        using var tp = new SKPaint { Color = Cyan, TextSize = 10, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        float tw = tp.MeasureText(text);
        var rect = new SKRect(x, y, x + tw + 20, y + 24);
        float alpha = 180 + (float)Math.Sin(time * 3) * 75;
        using var borderP = new SKPaint { Color = Cyan.WithAlpha((byte)alpha), Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
        c.DrawRoundRect(rect, 12, 12, borderP);
        using var bgP = new SKPaint { Color = new SKColor(0, 255, 255, 20) };
        c.DrawRoundRect(rect, 12, 12, bgP);
        c.DrawText(text, x + 10, y + 16, tp);
    }

    private static void DrawGradientText(SKCanvas c, string text, float x, float y, float size, float time)
    {
        float shift = time * 50;
        using var p = new SKPaint
        {
            TextSize = size, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold),
            Shader = SKShader.CreateLinearGradient(new SKPoint(x + shift % 200, y), new SKPoint(x + 300 + shift % 200, y + size), new[] { Cyan, Purple, Cyan }, null, SKShaderTileMode.Mirror)
        };
        c.DrawText(text, x, y + size, p);
    }

    private static void DrawButton(SKCanvas c, float x, float y, float w, float h, string text, SKColor accent, float time, bool primary)
    {
        var rect = new SKRect(x, y, x + w, y + h);
        if (primary)
        {
            using var glow = new SKPaint { Color = accent.WithAlpha(30), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 12) };
            c.DrawRoundRect(rect, 8, 8, glow);
            using var bg = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(x, y), new SKPoint(x + w, y + h), new[] { accent, accent.WithAlpha(180) }, null, SKShaderTileMode.Clamp), IsAntialias = true };
            c.DrawRoundRect(rect, 8, 8, bg);
            using var tp = new SKPaint { Color = Bg, TextSize = 14, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
            c.DrawText(text, x + w / 2, y + h / 2 + 5, tp);
        }
        else
        {
            using var border = new SKPaint { Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
            c.DrawRoundRect(rect, 8, 8, border);
            using var tp = new SKPaint { Color = accent, TextSize = 14, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
            c.DrawText(text, x + w / 2, y + h / 2 + 5, tp);
        }
    }

    private static void DrawAnimatedStat(SKCanvas c, float x, float y, float w, string value, string label, float time, float delay)
    {
        float p = Math.Clamp((time - delay) * 0.5f, 0, 1);
        byte alpha = (byte)((float)Math.Pow(p, 0.5) * 255);
        using var vp = new SKPaint { Color = White.WithAlpha(alpha), TextSize = 28, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(value, x + 8, y + 28, vp);
        using var lp = new SKPaint { Color = DarkGray.WithAlpha(alpha), TextSize = 12, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        c.DrawText(label, x + 8, y + 48, lp);
    }

    private static void DrawScrollIndicator(SKCanvas c, float w, float y, float time)
    {
        float bounce = (float)Math.Sin(time * 2.5) * 6;
        float cx = w / 2;
        using var p = new SKPaint { Color = DarkGray, StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke };
        c.DrawLine(cx - 8, y + bounce, cx, y + 8 + bounce, p);
        c.DrawLine(cx, y + 8 + bounce, cx + 8, y + bounce, p);
    }

    private static void DrawSectionTitle(SKCanvas c, string text, float x, float y, float w, float time)
    {
        using var p = new SKPaint { Color = White, TextSize = 24, IsAntialias = true, TextAlign = SKTextAlign.Center, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(text, w / 2, y + 24, p);
        float tw = p.MeasureText(text);
        using var lp = new SKPaint { Color = Cyan.WithAlpha(80), StrokeWidth = 2, IsAntialias = true };
        c.DrawLine(w / 2 - tw / 2, y + 34, w / 2 + tw / 2, y + 34, lp);
    }

    private static float DrawStepCard(SKCanvas c, float x, float y, float w, string num, string title, string desc, SKColor accent, float time)
    {
        float h = 140;
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = CardBg, IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var np = new SKPaint { Color = accent.WithAlpha(40), TextSize = 50, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(num, x + w - 60, y + 55, np);
        using var tp = new SKPaint { Color = accent, TextSize = 18, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(title, x + 16, y + 30, tp);
        DrawWrappedText(c, desc, x + 16, y + 50, w - 32, 13, Gray);
        return y + h;
    }

    private static void DrawStepCardFixed(SKCanvas c, float x, float y, float w, float h, string num, string title, string desc, SKColor accent, float time)
    {
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = CardBg, IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var np = new SKPaint { Color = accent.WithAlpha(40), TextSize = 40, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(num, x + w - 50, y + 45, np);
        using var tp = new SKPaint { Color = accent, TextSize = 16, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(title, x + 12, y + 25, tp);
        DrawWrappedText(c, desc, x + 12, y + 42, w - 24, 12, Gray);
    }

    private static float DrawFeatureCardFixed(SKCanvas c, float x, float y, float w, float h, string title, string desc, SKColor accent)
    {
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = CardBg, IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var tp = new SKPaint { Color = accent, TextSize = 16, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(title, x + 12, y + 25, tp);
        DrawWrappedText(c, desc, x + 12, y + 42, w - 24, 12, Gray);
        return y + h;
    }

    private static float DrawFeatureCard(SKCanvas c, float x, float y, float w, string title, string desc, SKColor accent)
    {
        float h = 100;
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = CardBg, IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var tp = new SKPaint { Color = accent, TextSize = 18, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        c.DrawText(title, x + 16, y + 30, tp);
        DrawWrappedText(c, desc, x + 16, y + 50, w - 32, 13, Gray);
        return y + h;
    }

    private static float DrawLiveCodeBlock(SKCanvas c, float x, float y, float w, float time)
    {
        string[] lines = { "namespace FdsApp;", "public static class UI {", "  public static float Render(SKCanvas c) {", "    c.Clear(SKColors.Black);", "    return 200;", "  }", "}" };
        float h = 36 + 20 + lines.Length * 18 + 20;
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = new SKColor(12, 12, 18), IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var tp = new SKPaint { Color = Gray, TextSize = 13, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        for (int i = 0; i < lines.Length; i++) c.DrawText(lines[i], x + 16, y + 60 + i * 18, tp);
        return y + h;
    }

    private static float DrawComparisonRow(SKCanvas c, float x, float y, float w, string c1, string c2, string c3, bool header = false)
    {
        float rowH = 36;
        if (header) c.DrawRect(x, y, w, rowH, new SKPaint { Color = new SKColor(20, 20, 28) });
        using var p = new SKPaint { Color = header ? Cyan : Gray, TextSize = 13, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", header ? SKFontStyle.Bold : SKFontStyle.Normal) };
        c.DrawText(c1, x + 16, y + 23, p);
        c.DrawText(c2, x + w * 0.35f, y + 23, p);
        c.DrawText(c3, x + w * 0.7f, y + 23, p);
        return y + rowH;
    }

    private static float DrawTerminalBlock(SKCanvas c, float x, float y, float w, string[] lines, float time)
    {
        float h = 36 + 20 + lines.Length * 20 + 20;
        var rect = new SKRect(x, y, x + w, y + h);
        using var bg = new SKPaint { Color = new SKColor(5, 5, 8), IsAntialias = true };
        c.DrawRoundRect(rect, 10, 10, bg);
        using var tp = new SKPaint { Color = Gray, TextSize = 13, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        for (int i = 0; i < lines.Length; i++) c.DrawText(lines[i], x + 16, y + 60 + i * 20, tp);
        return y + h;
    }

    private static float DrawWrappedText(SKCanvas c, string text, float x, float y, float maxW, float size, SKColor color)
    {
        using var p = new SKPaint { Color = color, TextSize = size, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas") };
        float lineH = size + 6;
        var words = text.Split(' '); string line = "";
        float startY = y;
        foreach (var word in words) {
            string test = line == "" ? word : line + " " + word;
            if (p.MeasureText(test) > maxW) { c.DrawText(line, x, y + size, p); y += lineH; line = word; }
            else line = test;
        c.DrawText(line, x, y + size, p); 
        return y + lineH; // Return absolute Y again to avoid breaking other calls
    }

    private static void DrawToast(SKCanvas c, float w, float h, string text, float alpha)
    {
        using var p = new SKPaint { Color = Cyan, TextSize = 14, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold) };
        float tw = p.MeasureText(text);
        var rect = new SKRect((w - tw) / 2 - 20, h - 80, (w + tw) / 2 + 20, h - 40);
        using var bg = new SKPaint { Color = CardBg.WithAlpha((byte)(220 * alpha)), IsAntialias = true };
        c.DrawRoundRect(rect, 20, 20, bg);
        p.Color = White.WithAlpha((byte)(255 * alpha));
        c.DrawText(text, (w - tw) / 2, h - 54, p);
    }

    private static void DrawLine(SKCanvas c, float x, float y, float w, SKColor color)
    {
        using var p = new SKPaint { Color = color, StrokeWidth = 1 };
        c.DrawLine(x, y, x + w, y, p);
    }
}
