using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Cafe101
{
    // ═══════════════════════════════════════════════════════════
    //  SplashScreen.cs  —  Café 101  Loading Screen
    //  Shows while the app initialises and DB connection is made.
    //  Call SplashScreen.ShowSplash(ms) to display for a set time.
    // ═══════════════════════════════════════════════════════════
    public class SplashScreen : Form
    {
        // ── Palette (matches app brand) ──────────────────────────
        static readonly Color C_Bg        = Color.FromArgb(12,   7,   3);
        static readonly Color C_BgMid     = Color.FromArgb(30,  18,   8);
        static readonly Color C_Gold      = Color.FromArgb(196, 154,  82);
        static readonly Color C_GoldPale  = Color.FromArgb(230, 190, 120);
        static readonly Color C_Brown     = Color.FromArgb( 88,  48,  26);
        static readonly Color C_Sub       = Color.FromArgb(148, 118,  88);

        // ── State ────────────────────────────────────────────────
        Timer  _animTimer;
        int    _tick          = 0;
        float  _barProgress   = 0f;   // 0..1
        float  _barTarget     = 0f;
        string _statusText    = "Initialising...";
        float  _fadeAlpha     = 0f;   // 0..255 for fade-in
        bool   _fadingIn      = true;
        bool   _fadingOut     = false;
        float  _particleAngle = 0f;

        // Steam particles
        readonly (float x, float y, float speed, float alpha)[] _particles;
        readonly Random _rng = new Random();

        public SplashScreen()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            Size            = new Size(1280, 800);
            BackColor       = C_Bg;
            DoubleBuffered  = true;
            TopMost         = true;
            ShowInTaskbar   = false;
            Opacity         = 0;

            // Pre-build steam particles
            _particles = new (float, float, float, float)[12];
            for (int i = 0; i < _particles.Length; i++)
                _particles[i] = (
                    0.45f + (float)(_rng.NextDouble() * 0.10f),
                    0.38f + (float)(_rng.NextDouble() * 0.06f),
                    0.004f + (float)(_rng.NextDouble() * 0.003f),
                    (float)(_rng.NextDouble())
                );

            Paint += OnPaint;
        }

        // ═══════════════════════════════════════════════════════
        //  PUBLIC ENTRY POINT
        // ═══════════════════════════════════════════════════════
        // dbReady is set to true externally once the DB thread completes
        public static bool DbReady = false;

        public static void ShowSplash(int minMs = 3200)
        {
            DbReady = false;
            var splash = new SplashScreen();
            splash.Show();

            int elapsed = 0;
            bool waitingForDb = false;
            string[] steps = {
                "Connecting to database...",
                "Loading menu items...",
                "Preparing workspace...",
                "Almost ready..."
            };

            splash._animTimer = new Timer { Interval = 16 };
            splash._animTimer.Tick += (s, e) =>
            {
                elapsed += 16;
                splash._tick++;
                splash._particleAngle += 0.8f;

                // Animate steam particles upward
                for (int i = 0; i < splash._particles.Length; i++) {
                    var p = splash._particles[i];
                    p.y  -= p.speed;
                    p.alpha -= 0.008f;
                    if (p.y < 0.08f || p.alpha <= 0) {
                        p.x     = 0.42f + (float)(splash._rng.NextDouble() * 0.16f);
                        p.y     = 0.35f + (float)(splash._rng.NextDouble() * 0.04f);
                        p.speed = 0.004f + (float)(splash._rng.NextDouble() * 0.003f);
                        p.alpha = 0.3f + (float)(splash._rng.NextDouble() * 0.5f);
                    }
                    splash._particles[i] = p;
                }

                // Phase 1: Fade in (0 → 400ms)
                if (elapsed < 400) {
                    splash._fadeAlpha = elapsed / 400f * 255f;
                    splash.Opacity    = splash._fadeAlpha / 255.0;
                    splash._barTarget = 0.02f;
                    splash._statusText = steps[0];
                }
                // Phase 2: Progress (400ms → minMs-600ms) — fill bar to 0.85
                else if (elapsed < minMs - 600) {
                    splash.Opacity = 1.0;
                    float prog = (float)(elapsed - 400) / (minMs - 1000);
                    splash._barTarget  = Math.Min(0.85f, 0.02f + prog * 0.83f);
                    int step = Math.Min(steps.Length - 2, (int)(prog * (steps.Length - 1)));
                    splash._statusText = steps[step];
                }
                // Phase 3: Hold at 85% until DB is ready
                else if (!DbReady) {
                    waitingForDb = true;
                    splash._barTarget  = 0.87f;
                    splash._statusText = "Connecting to database...";
                    // pulse opacity slightly to show activity
                    splash.Opacity = 0.92 + 0.08 * Math.Sin(elapsed * 0.005);
                }
                // Phase 4: DB ready — rush to 100%
                else {
                    if (waitingForDb) { waitingForDb = false; }
                    splash._barTarget  = 1f;
                    splash._statusText = "Welcome!";
                    splash.Opacity     = 1.0;

                    // Only start fade-out once bar visually reaches ~98%
                    if (splash._barProgress >= 0.97f) {
                        float fo = (float)(elapsed - (minMs - 600)) / 500f;
                        splash.Opacity = Math.Max(0, 1.0 - fo * 0.5);
                        if (splash._barProgress >= 0.999f && fo >= 1f) {
                            splash._animTimer.Stop();
                            splash.Close();
                        }
                    }
                }

                // Smooth ease toward target
                splash._barProgress += (splash._barTarget - splash._barProgress) * 0.06f;
                splash.Invalidate();
            };
            splash._animTimer.Start();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!splash.IsDisposed && sw.ElapsedMilliseconds < minMs + 5000)
                Application.DoEvents();
        }

        // ═══════════════════════════════════════════════════════
        //  PAINT
        // ═══════════════════════════════════════════════════════
        void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;

            int W = Width, H = Height;

            // ── Background gradient ──────────────────────────────
            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(W, H),
                Color.FromArgb(8, 5, 2), Color.FromArgb(38, 24, 10)))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Radial glow behind cup ────────────────────────────
            float cx = W * 0.5f, cy = H * 0.42f;
            try {
                using (var gp = new GraphicsPath()) {
                    gp.AddEllipse(cx - 160, cy - 130, 320, 260);
                    using (var pg = new PathGradientBrush(gp)) {
                        pg.CenterColor    = Color.FromArgb(28, C_Gold);
                        pg.SurroundColors = new[] { Color.FromArgb(0, C_Gold) };
                        g.FillPath(pg, gp);
                    }
                }
            } catch { }

            // ── Decorative thin arc rings ─────────────────────────
            using (var p = new Pen(Color.FromArgb(18, C_Gold), 1f)) {
                g.DrawEllipse(p, cx - 110, cy - 88,  220, 176);
                g.DrawEllipse(p, cx - 140, cy - 112, 280, 224);
            }

            // ── Steam particles ───────────────────────────────────
            foreach (var pt in _particles) {
                int alpha = (int)(Math.Min(1f, pt.alpha) * 160);
                if (alpha <= 0) continue;
                float px = pt.x * W, py = pt.y * H;
                float r  = 3f + pt.alpha * 6f;
                using (var sb = new SolidBrush(Color.FromArgb(alpha, C_GoldPale)))
                    g.FillEllipse(sb, px - r / 2, py - r / 2, r, r);
            }

            // ── Coffee cup (GDI+) ────────────────────────────────
            DrawCup(g, cx, cy, 78);

            // ── CAFÉ 101 ─────────────────────────────────────────
            using (var f = new Font("Segoe UI", 34f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center }) {
                // Shadow
                using (var sb = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    g.DrawString("CAFÉ 101", f, sb, cx + 2, H * 0.64f + 2, sf);
                // Gold gradient text via clip trick
                var textRect = new RectangleF(cx - 200, H * 0.64f, 400, 48);
                using (var lgb = new LinearGradientBrush(
                    new PointF(cx - 200, H * 0.64f), new PointF(cx + 200, H * 0.64f + 48),
                    C_GoldPale, C_Gold))
                    g.DrawString("CAFÉ 101", f, lgb, cx, H * 0.64f, sf);
            }

            // ── Sub-title ─────────────────────────────────────────
            using (var f = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            using (var sb = new SolidBrush(Color.FromArgb(160, C_Sub)))
                g.DrawString("ORDER & DELIVERY MANAGEMENT SYSTEM", f, sb, cx, H * 0.78f, sf);

            // ── Progress bar track ────────────────────────────────
            float bx = 60, by = H * 0.87f, bw = W - 120, bh = 5f;
            using (var sb = new SolidBrush(Color.FromArgb(35, C_Gold)))
                g.FillRectangle(sb, bx, by, bw, bh);

            // Progress bar fill with gradient
            float fillW = bw * _barProgress;
            if (fillW > 2) {
                using (var lgb = new LinearGradientBrush(
                    new PointF(bx, by), new PointF(bx + fillW, by + bh),
                    C_Brown, C_GoldPale))
                    g.FillRectangle(lgb, bx, by, fillW, bh);

                // Glow dot at end of bar
                float dotX = bx + fillW;
                using (var gp = new GraphicsPath()) {
                    gp.AddEllipse(dotX - 6, by - 4, 12, 12);
                    using (var pg = new PathGradientBrush(gp)) {
                        pg.CenterColor    = Color.White;
                        pg.SurroundColors = new[] { Color.FromArgb(0, C_Gold) };
                        g.FillPath(pg, gp);
                    }
                }
            }

            // ── Status text ───────────────────────────────────────
            using (var f = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            using (var sb = new SolidBrush(Color.FromArgb(120, C_GoldPale)))
                g.DrawString(_statusText, f, sb, cx, H * 0.93f, sf);

            // ── Thin gold top border ──────────────────────────────
            using (var p = new Pen(Color.FromArgb(80, C_Gold), 1.5f))
                g.DrawLine(p, 0, 0, W, 0);
            using (var p = new Pen(Color.FromArgb(30, C_Gold), 1f))
                g.DrawLine(p, 0, H - 1, W, H - 1);

            // ── Version / credit (bottom right) ──────────────────
            using (var f = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var sb = new SolidBrush(Color.FromArgb(55, C_Sub)))
                g.DrawString("v2.0  •  ISTN3AS  •  2026", f, sb, W - 148, H - 18);
        }

        // ─────────────────────────────────────────────────────────
        //  DRAW CUP
        // ─────────────────────────────────────────────────────────
        void DrawCup(Graphics g, float cx, float cy, float r)
        {
            // Saucer
            using (var sb = new SolidBrush(Color.FromArgb(50, C_Gold)))
                g.FillEllipse(sb, cx - r * 0.72f, cy + r * 0.52f, r * 1.44f, r * 0.38f);
            using (var p = new Pen(C_Gold, 1.8f))
                g.DrawEllipse(p, cx - r * 0.72f, cy + r * 0.52f, r * 1.44f, r * 0.38f);

            // Cup body — trapezoid with rounded bottom
            float tw = r * 1.0f, bw2 = r * 1.3f, ch = r * 1.1f;
            float ty = cy - ch * 0.5f;
            var cupPath = new GraphicsPath();
            cupPath.AddLine(cx - tw / 2, ty, cx + tw / 2, ty);
            cupPath.AddLine(cx + tw / 2, ty, cx + bw2 / 2, ty + ch);
            cupPath.AddArc(cx - bw2 / 2, ty + ch - 16, bw2, 32, 0, 180);
            cupPath.AddLine(cx - bw2 / 2, ty + ch, cx - tw / 2, ty);
            cupPath.CloseFigure();

            using (var lgb = new LinearGradientBrush(
                new PointF(cx - bw2 / 2, ty), new PointF(cx + bw2 / 2, ty + ch),
                Color.FromArgb(90, C_Gold), Color.FromArgb(35, C_Brown)))
                g.FillPath(lgb, cupPath);

            using (var p = new Pen(C_Gold, 2f))
                g.DrawPath(p, cupPath);

            // Handle
            var handleRect = new RectangleF(cx + bw2 / 2 - 6, ty + ch * 0.15f, r * 0.46f, ch * 0.6f);
            using (var p = new Pen(C_Gold, 2.5f))
                g.DrawArc(p, handleRect, -80, 160);

            // Coffee surface inside cup
            float surfY = ty + ch * 0.18f;
            float surfW  = tw * 0.78f;
            using (var sb = new SolidBrush(Color.FromArgb(70, C_Brown)))
                g.FillEllipse(sb, cx - surfW / 2, surfY - 6, surfW, 14);
            using (var p = new Pen(Color.FromArgb(120, C_Gold), 1f))
                g.DrawEllipse(p, cx - surfW / 2, surfY - 6, surfW, 14);

            // Latte art swirl on surface
            using (var p = new Pen(Color.FromArgb(90, C_GoldPale), 1.2f)) {
                float sa = _particleAngle * (float)(Math.PI / 180.0);
                for (int i = 0; i < 3; i++) {
                    float a = sa + i * (float)(Math.PI * 2 / 3);
                    float ex = cx + (float)Math.Cos(a) * surfW * 0.22f;
                    float ey = surfY - 2 + (float)Math.Sin(a) * 3.5f;
                    g.DrawLine(p, cx, surfY - 2, ex, ey);
                }
            }

            cupPath.Dispose();
        }
    }
}
