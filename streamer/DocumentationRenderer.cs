using SkiaSharp;
using System;

namespace Streamer;

public static class DocumentationRenderer
{
    private static readonly SKColor BackgroundColor = new SKColor(15, 15, 15);
    private static readonly SKColor AccentColor = SKColors.Cyan;
    private static readonly SKColor TextColor = SKColors.White;
    private static readonly SKColor SubtextColor = SKColors.Gray;

    // Button interactive state
    private static int _clickCount = 0;
    private static DateTime _lastClickTime = DateTime.MinValue;

    // Button bounds in canvas space — set during render, read during hit-test
    private static SKRect _buttonBounds = SKRect.Empty;

    public static void HandleClick(float cx, float cy, float scrollOffset)
    {
        // Adjust screen coordinates to content coordinates
        float adjustedCy = cy + scrollOffset;
        
        if (_buttonBounds.Contains(cx, adjustedCy))
        {
            Interlocked.Increment(ref _clickCount);
            _lastClickTime = DateTime.Now;
            Console.WriteLine($"Button clicked! Total: {_clickCount}");
        }
    }

    public static void Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
        canvas.Clear(BackgroundColor);

        bool isMobile = width < 580;

        float margin = width * 0.04f;
        float contentWidth = width - margin * 2;

        // 1. Animated Background (FIXED)
        DrawAnimatedBackground(canvas, width, height, time);

        // 2. Content with Scroll
        canvas.Save();
        canvas.Translate(0, -scrollOffset);

        // 2a. Header
        float headerHeight = isMobile ? height * 0.15f : height * 0.22f;
        DrawHeader(canvas, margin, 0, contentWidth, headerHeight, time);

        // 2b. Separator line
        float separatorY = headerHeight;
        using (var linePaint = new SKPaint { Color = new SKColor(255, 255, 255, 25), StrokeWidth = 1, IsAntialias = true })
        {
            canvas.DrawLine(margin, separatorY, width - margin, separatorY, linePaint);
        }

        // 2c. Content area
        float contentTop = separatorY + margin * 0.5f;
        float currentY = contentTop;

        float sectionHeaderSize = Math.Clamp(width * 0.022f, 10, 16);
        float cardGap = isMobile ? 10 : margin * 0.4f;

        // --- Section 1: Vision ---
        DrawSectionHeader(canvas, "PLATFORM VISION", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.06f;

        if (!isMobile)
        {
            float rowH = height * 0.22f;
            DrawGlassCard(canvas, margin, currentY, contentWidth, rowH, "NO CHROMIUM", "FDS eliminates the heavy browser overhead. No V8, no Blink, no WebViews.");
            currentY += rowH + cardGap;
            
            float halfW = (contentWidth - cardGap) / 2;
            DrawGlassCard(canvas, margin, currentY, halfW, rowH, "WASM NATIVE", "Logic runs in WASM containers, producing direct Skia drawing calls.");
            DrawGlassCard(canvas, margin + halfW + cardGap, currentY, halfW, rowH, "PACKET STREAMING", "Serialized drawing packets are streamed via TCP efficiently.");
            currentY += rowH + cardGap;
        }
        else
        {
            float cardH = height * 0.12f;
            DrawGlassCard(canvas, margin, currentY, contentWidth, cardH, "NO CHROMIUM", "No heavy browser overhead.");
            currentY += cardH + cardGap;
            DrawGlassCard(canvas, margin, currentY, contentWidth, cardH, "WASM NATIVE", "Near-native logic performance.");
            currentY += cardH + cardGap;
            DrawGlassCard(canvas, margin, currentY, contentWidth, cardH, "PACKET STREAMING", "Pixel-perfect streaming.");
            currentY += cardH + cardGap;
        }

        // --- Section 2: Architecture ---
        DrawSectionHeader(canvas, "SYSTEM ARCHITECTURE", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.04f;
        float archH = height * 0.25f;
        DrawArchitecture(canvas, margin, currentY, contentWidth, archH, time);
        currentY += archH + cardGap * 2;

        // --- Section 3: Performance (NEW) ---
        DrawSectionHeader(canvas, "CORE PERFORMANCE", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.04f;
        
        float perfRowH = isMobile ? height * 0.15f : height * 0.18f;
        if (!isMobile)
        {
            float thirdW = (contentWidth - cardGap * 2) / 3;
            DrawGlassCard(canvas, margin, currentY, thirdW, perfRowH, "0ms LATENCY", "Direct memory access for drawing commands.");
            DrawGlassCard(canvas, margin + thirdW + cardGap, currentY, thirdW, perfRowH, "60FPS SYNC", "Hardware-accelerated remote frame synchronization.");
            DrawGlassCard(canvas, margin + (thirdW + cardGap) * 2, currentY, thirdW, perfRowH, "ULTRA LIGHT", "Client binary is less than 5MB on most platforms.");
            currentY += perfRowH + cardGap;
        }
        else
        {
            DrawGlassCard(canvas, margin, currentY, contentWidth, perfRowH, "60FPS SYNC", "Hardware-accelerated frames.");
            currentY += perfRowH + cardGap;
        }

        // --- Section 4: Security (NEW) ---
        DrawSectionHeader(canvas, "NATIVE INTEGRATIONS", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.04f;
        DrawGlassCard(canvas, margin, currentY, contentWidth, perfRowH, "OS NATIVE BRIDGE", "Direct access to local file systems and hardware without browser sandbox limitations.");
        currentY += perfRowH + cardGap * 2;

        // --- Section 5: Ecosystem (NEW) ---
        DrawSectionHeader(canvas, "COMMUNITY ECOSYSTEM", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.04f;
        float ecoH = height * 0.2f;
        DrawGlassCard(canvas, margin, currentY, contentWidth, ecoH, "FDS PACKAGE MANAGER", "Deploy UI components as lightweight WASM bundles via our global registry.");
        currentY += ecoH + cardGap * 2;

        // --- Section 6: Dev Tools (NEW) ---
        DrawSectionHeader(canvas, "DEVELOPER TOOLS", margin, currentY + sectionHeaderSize, sectionHeaderSize);
        currentY += height * 0.04f;
        DrawGlassCard(canvas, margin, currentY, contentWidth, ecoH, "FDS STUDIO", "Visual debugger and hot-reload environment for remote-rendered applications.");
        currentY += ecoH + cardGap * 3;

        // 5. Button
        float btnW = isMobile ? contentWidth * 0.4f : contentWidth * 0.2f;
        float btnH = isMobile ? headerHeight * 0.35f : headerHeight * 0.28f;
        float btnX = margin + contentWidth - btnW;
        float btnY = isMobile ? headerHeight * 0.2f : headerHeight * 0.55f;
        DrawButton(canvas, btnX, btnY, btnW, btnH, time);

        // EXTRA SPACE FOR SCROLL
        currentY += height * 0.3f;

        canvas.Restore();

        // 6. Scrollbar Indicator (NEW)
        float scrollableHeight = 3000; // Static limit for now
        float barH = (height / (scrollableHeight + height)) * height;
        float barY = (scrollOffset / scrollableHeight) * (height - barH);
        using (var barPaint = new SKPaint { Color = new SKColor(255, 255, 255, 40), IsAntialias = true })
        {
            canvas.DrawRoundRect(width - 8, barY + 4, 4, barH - 8, 2, 2, barPaint);
        }

        // 7. Footer (FIXED)
        float footerHeight = height * 0.08f;
        DrawFooter(canvas, width, height, footerHeight, time);
    }

    private static void DrawAnimatedBackground(SKCanvas canvas, float width, float height, float time)
    {
        float x1 = width * 0.5f + (float)Math.Sin(time * 0.5f) * width * 0.12f;
        float y1 = height * 0.3f + (float)Math.Cos(time * 0.3f) * height * 0.1f;

        using var paint1 = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(x1, y1), width * 0.6f,
                new SKColor[] { new SKColor(0, 80, 100, 30), SKColors.Transparent },
                null, SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, 0, width, height, paint1);

        float x2 = width * 0.7f + (float)Math.Cos(time * 0.4f) * width * 0.18f;
        float y2 = height * 0.6f + (float)Math.Sin(time * 0.6f) * height * 0.16f;

        using var paint2 = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(x2, y2), width * 0.5f,
                new SKColor[] { new SKColor(100, 0, 100, 20), SKColors.Transparent },
                null, SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, 0, width, height, paint2);
    }

    private static void DrawHeader(SKCanvas canvas, float x, float y, float w, float h, float time)
    {
        float logoSize = Math.Min(h * 0.65f, w * 0.12f);

        using var paint = new SKPaint
        {
            Color = AccentColor,
            TextSize = logoSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        float logoY = y + h * 0.65f;

        paint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
        paint.Color = new SKColor(0, 255, 255, 50);
        canvas.DrawText("FDS", x, logoY, paint);

        paint.ImageFilter = null;
        paint.Color = AccentColor;
        canvas.DrawText("FDS", x, logoY, paint);

        paint.TextSize = logoSize * 0.25f;
        paint.Color = SubtextColor;
        paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        canvas.DrawText("THE BROWSER-LESS REMOTE UI PLATFORM", x, logoY + logoSize * 0.35f, paint);

        float pulse = (float)Math.Abs(Math.Sin(time * 3f));
        float dotX = x + w - logoSize * 0.3f;
        float dotY = logoY - logoSize * 0.4f;

        paint.Color = new SKColor(0, 255, 100, (byte)(100 + pulse * 155));
        canvas.DrawCircle(dotX, dotY, 4 + pulse * 3, paint);

        paint.Color = new SKColor(0, 255, 100);
        canvas.DrawCircle(dotX, dotY, 4, paint);

        paint.TextSize = logoSize * 0.2f;
        paint.Color = TextColor;
        canvas.DrawText("ACTIVE", dotX + 12, dotY + 5, paint);
    }

    // FIX 1: textSize is now a parameter — caller derives it from width
    private static void DrawSectionHeader(SKCanvas canvas, string title, float x, float y, float textSize)
    {
        using var paint = new SKPaint
        {
            Color = AccentColor,
            TextSize = textSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            FakeBoldText = true
        };
        canvas.DrawText(title, x, y, paint);

        float lineW = paint.MeasureText(title);
        paint.StrokeWidth = 2;
        canvas.DrawLine(x, y + 6, x + lineW, y + 6, paint);
    }

    private static void DrawGlassCard(SKCanvas canvas, float x, float y, float w, float h, string title, string description)
    {
        var rect = new SKRect(x, y, x + w, y + h);

        using var paint = new SKPaint { Color = new SKColor(40, 40, 40, 180), IsAntialias = true };
        canvas.DrawRoundRect(rect, 10, 10, paint);

        paint.Style = SKPaintStyle.Stroke;
        paint.Color = new SKColor(255, 255, 255, 30);
        paint.StrokeWidth = 1;
        canvas.DrawRoundRect(rect, 10, 10, paint);

        paint.Color = AccentColor;
        paint.StrokeWidth = 3;
        canvas.DrawLine(x + 1.5f, y + 10, x + 1.5f, y + h - 10, paint);

        paint.Style = SKPaintStyle.Fill;
        paint.Color = AccentColor;
        float titleSize = Math.Min(h * 0.2f, 18);
        paint.TextSize = titleSize;
        paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);

        float pad = w * 0.03f + 8;
        canvas.DrawText(title, x + pad, y + titleSize + pad * 0.5f, paint);

        paint.Color = new SKColor(200, 200, 200);
        float descSize = Math.Min(h * 0.14f, 14);
        paint.TextSize = descSize;
        paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);

        float lineHeight = descSize * 1.5f;
        float maxTextW = w - pad * 2;
        float currentY = y + titleSize + pad * 0.5f + lineHeight + 4;

        var words = description.Split(' ');
        var line = "";
        foreach (var word in words)
        {
            if (paint.MeasureText(line + word) > maxTextW)
            {
                canvas.DrawText(line.TrimEnd(), x + pad, currentY, paint);
                line = word + " ";
                currentY += lineHeight;
            }
            else
            {
                line += word + " ";
            }
        }
        if (!string.IsNullOrWhiteSpace(line))
            canvas.DrawText(line.TrimEnd(), x + pad, currentY, paint);
    }

    private static void DrawArchitecture(SKCanvas canvas, float x, float y, float w, float h, float time)
    {
        // FIX 3: Clamp box size, derive spacing from remaining space — no more overlap or overflow
        float boxW = Math.Clamp(w * 0.18f, 60, 120);
        float boxH = Math.Clamp(h * 0.7f, 30, 60);
        float centerY = y + h * 0.5f;

        float remainingSpace = w - boxW * 3;
        float spacing = remainingSpace / 4; // equal gaps: [sp][box][sp][box][sp][box][sp]

        float box1X = x + spacing + boxW * 0.5f;
        float box2X = x + spacing * 2 + boxW * 1.5f;
        float box3X = x + spacing * 3 + boxW * 2.5f;

        DrawProcessBox(canvas, box1X, centerY, boxW, boxH, "WASM CORE", time);
        DrawProcessBox(canvas, box2X, centerY, boxW, boxH, "SKIA STREAM", time);
        DrawProcessBox(canvas, box3X, centerY, boxW, boxH, "FDS CLIENT", time);

        using var paint = new SKPaint
        {
            Color = new SKColor(0, 255, 255, 60),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };

        float lineStart1 = box1X + boxW / 2;
        float lineEnd1   = box2X - boxW / 2;
        float lineStart2 = box2X + boxW / 2;
        float lineEnd2   = box3X - boxW / 2;

        canvas.DrawLine(lineStart1, centerY, lineEnd1, centerY, paint);
        canvas.DrawLine(lineStart2, centerY, lineEnd2, centerY, paint);

        float gap1 = lineEnd1 - lineStart1;
        float gap2 = lineEnd2 - lineStart2;

        if (gap1 > 0 && gap2 > 0)
        {
            float progress1 = (time * 80) % gap1;
            float progress2 = (time * 80) % gap2;

            paint.Style = SKPaintStyle.Fill;
            paint.Color = AccentColor;
            canvas.DrawCircle(lineStart1 + progress1, centerY, 3, paint);
            canvas.DrawCircle(lineStart2 + progress2, centerY, 3, paint);
        }

        paint.Color = AccentColor;
        paint.Style = SKPaintStyle.Fill;
        DrawArrowHead(canvas, lineEnd1, centerY, 6, paint);
        DrawArrowHead(canvas, lineEnd2, centerY, 6, paint);
    }

    private static void DrawArrowHead(SKCanvas canvas, float tipX, float tipY, float size, SKPaint paint)
    {
        using var path = new SKPath();
        path.MoveTo(tipX, tipY);
        path.LineTo(tipX - size, tipY - size * 0.5f);
        path.LineTo(tipX - size, tipY + size * 0.5f);
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static void DrawProcessBox(SKCanvas canvas, float cx, float cy, float w, float h, string text, float time)
    {
        var rect = new SKRect(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2);

        using var paint = new SKPaint { Color = new SKColor(50, 50, 50, 150), IsAntialias = true };
        canvas.DrawRoundRect(rect, 6, 6, paint);

        paint.Color = AccentColor;
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1 + (float)Math.Abs(Math.Sin(time * 2f)) * 0.8f;
        canvas.DrawRoundRect(rect, 6, 6, paint);

        paint.Color = SKColors.White;
        paint.Style = SKPaintStyle.Fill;
        float textSize = Math.Min(h * 0.35f, 13);
        paint.TextSize = textSize;
        paint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        paint.TextAlign = SKTextAlign.Center;
        canvas.DrawText(text, cx, cy + textSize * 0.35f, paint);
    }

    // FIX 2: Footer text size scales with width — was hardcoded 11px
    private static void DrawFooter(SKCanvas canvas, float width, float height, float footerHeight, float time)
    {
        float footerY = height - footerHeight;
        using var linePaint = new SKPaint { Color = new SKColor(255, 255, 255, 20), StrokeWidth = 1, IsAntialias = true };
        canvas.DrawLine(width * 0.04f, footerY, width * 0.96f, footerY, linePaint);

        using var paint = new SKPaint
        {
            Color = SubtextColor,
            TextSize = Math.Clamp(width * 0.018f, 9, 13),
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
        var footerText = $"FDS CORE ENGINE V2.0  |  ENGINE TIME: {time:F1}s  |  STREAMING";
        canvas.DrawText(footerText, width / 2, height - footerHeight * 0.3f, paint);
    }

    private static void DrawButton(SKCanvas canvas, float x, float y, float w, float h, float time)
    {
        _buttonBounds = new SKRect(x, y, x + w, y + h);

        bool wasRecentlyClicked = (DateTime.Now - _lastClickTime).TotalSeconds < 0.4;
        float pulse = wasRecentlyClicked ? 1f : (float)Math.Abs(Math.Sin(time * 2f)) * 0.4f;

        if (wasRecentlyClicked || pulse > 0.1f)
        {
            using var glowPaint = new SKPaint
            {
                Shader = SKShader.CreateRadialGradient(
                    new SKPoint(x + w / 2, y + h / 2), w * 0.8f,
                    new SKColor[] { new SKColor(0, 255, 255, (byte)(pulse * 60)), SKColors.Transparent },
                    null, SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(x - w * 0.3f, y - h, x + w * 1.3f, y + h * 2f, glowPaint);
        }

        var rect = new SKRect(x, y, x + w, y + h);

        using var bgPaint = new SKPaint { IsAntialias = true };
        bgPaint.Color = wasRecentlyClicked
            ? new SKColor(0, 200, 200, 240)
            : new SKColor(20, 20, 20, 230);
        canvas.DrawRoundRect(rect, 6, 6, bgPaint);

        bgPaint.Style = SKPaintStyle.Stroke;
        bgPaint.Color = AccentColor;
        bgPaint.StrokeWidth = 1.5f + pulse * 1f;
        canvas.DrawRoundRect(rect, 6, 6, bgPaint);

        string label = _clickCount > 0 ? $"CLICKED × {_clickCount}" : "GET STARTED →";
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = Math.Min(h * 0.38f, 14),
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            Color = wasRecentlyClicked ? SKColors.Black : AccentColor
        };
        canvas.DrawText(label, x + w / 2, y + h * 0.62f, textPaint);
    }
}
