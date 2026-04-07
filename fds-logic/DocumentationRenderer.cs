using SkiaSharp;
using System;

namespace FdsLogic;

public static class DocumentationRenderer
{
    private static readonly SKColor BackgroundColor = new SKColor(15, 15, 15);
    private static readonly SKColor AccentColor = SKColors.Cyan;
    private static readonly SKColor TextColor = SKColors.White;
    private static readonly SKColor SubtextColor = SKColors.Gray;

    public static void Render(SKCanvas canvas, float width, float height, float scrollOffset)
    {
        float time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
        canvas.Clear(BackgroundColor);

        bool isMobile = width < 580;
        float margin = width * 0.04f;
        float contentWidth = width - margin * 2;

        DrawAnimatedBackground(canvas, width, height, time);

        canvas.Save();
        canvas.Translate(0, -scrollOffset);

        // Header
        float headerHeight = isMobile ? height * 0.15f : height * 0.22f;
        DrawHeader(canvas, margin, 0, contentWidth, headerHeight, time);

        // Content
        float currentY = headerHeight + margin;
        
        DrawSectionHeader(canvas, "WASM NATIVE ENGINE", margin, currentY, Math.Clamp(width * 0.022f, 10, 16));
        currentY += 40;
        
        DrawGlassCard(canvas, margin, currentY, contentWidth, 120, "LOCAL EXECUTION", "This UI is now running locally on your device via a server-provided binary module. Zero network latency for rendering.");
        
        canvas.Restore();
        
        // Footer
        DrawFooter(canvas, width, height, height * 0.08f, time);
    }

    private static void DrawAnimatedBackground(SKCanvas canvas, float width, float height, float time)
    {
        float x1 = width * 0.5f + (float)Math.Sin(time * 0.5f) * width * 0.12f;
        float y1 = height * 0.3f + (float)Math.Cos(time * 0.3f) * height * 0.1f;
        using var paint1 = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(x1, y1), width * 0.6f, new SKColor[] { new SKColor(0, 80, 100, 30), SKColors.Transparent }, null, SKShaderTileMode.Clamp) };
        canvas.DrawRect(0, 0, width, height, paint1);
    }

    private static void DrawHeader(SKCanvas canvas, float x, float y, float w, float h, float time)
    {
        using var paint = new SKPaint { Color = AccentColor, TextSize = h * 0.5f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
        canvas.DrawText("FDS WASM", x, y + h * 0.7f, paint);
    }

    private static void DrawSectionHeader(SKCanvas canvas, string title, float x, float y, float textSize)
    {
        using var paint = new SKPaint { Color = AccentColor, TextSize = textSize, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
        canvas.DrawText(title, x, y, paint);
    }

    private static void DrawGlassCard(SKCanvas canvas, float x, float y, float w, float h, string title, string description)
    {
        var rect = new SKRect(x, y, x + w, y + h);
        using var paint = new SKPaint { Color = new SKColor(40, 40, 40, 180), IsAntialias = true };
        canvas.DrawRoundRect(rect, 10, 10, paint);
        
        paint.Color = AccentColor;
        paint.TextSize = 18;
        canvas.DrawText(title, x + 15, y + 30, paint);
        
        paint.Color = SKColors.White;
        paint.TextSize = 14;
        canvas.DrawText(description, x + 15, y + 60, paint);
    }

    private static void DrawFooter(SKCanvas canvas, float width, float height, float footerHeight, float time)
    {
        using var paint = new SKPaint { Color = SubtextColor, TextSize = 12, IsAntialias = true, TextAlign = SKTextAlign.Center };
        canvas.DrawText($"WASM RUNTIME ACTIVE | {time:F1}s", width / 2, height - 20, paint);
    }
}
