using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ModernKuleSavunma.Siniflar;

namespace ModernKuleSavunma
{
    public partial class Form1 : Form
    {
        // --- YARDIMCI SINIF 1: Kayan Yazı Efekti ---
        public class KayanYazi
        {
            public PointF Konum { get; set; }
            public string Metin { get; set; }
            public Color Renk { get; set; }
            public int Omur { get; set; } = 255;

            public KayanYazi(string metin, PointF baslangic, Color renk)
            {
                Metin = metin; Konum = baslangic; Renk = renk;
            }

            public bool Guncelle() { Konum = new PointF(Konum.X, Konum.Y - 1.5f); Omur -= 5; return Omur > 0; }
        }

        // --- YENİ YARDIMCI SINIF 2: MERMİ (PROJEKTİL) ---
        public enum MermiTipi { Ok, TopGullesi, BuyuYildizi, Bomba }
        public class Mermi
        {
            public PointF Konum { get; set; }
            public Dusman Hedef { get; set; }
            public float Hiz { get; set; }
            public int Hasar { get; set; }
            public MermiTipi Tip { get; set; }
            public float Aci { get; set; } // Büyü döndürme efekti için

            public Mermi(PointF baslangic, Dusman hedef, int hasar, Kule kuleTuru)
            {
                Konum = baslangic; Hedef = hedef; Hasar = hasar;

                // Kule türüne göre mermi tipi ve hızı belirle
                if (kuleTuru is OkKulesi) { Tip = MermiTipi.Ok; Hiz = 12f; }
                else if (kuleTuru is TopKulesi) { Tip = MermiTipi.TopGullesi; Hiz = 8f; }
                else if (kuleTuru is BuyuKulesi) { Tip = MermiTipi.BuyuYildizi; Hiz = 10f; }
                else if (kuleTuru is BombaKulesi) { Tip = MermiTipi.Bomba; Hiz = 7f; }
            }

            public bool HedefeUlastiMi()
            {
                if (!Hedef.Yasiyor) return true; // Hedef öldüyse mermi de yok olsun
                float dx = Hedef.Konum.X - Konum.X;
                float dy = Hedef.Konum.Y - Konum.Y;
                return Math.Sqrt(dx * dx + dy * dy) < Hiz; // Hedefe çok yaklaştıysa vurmuş say
            }

            public void Ilerle()
            {
                if (!Hedef.Yasiyor) return;
                float dx = Hedef.Konum.X - Konum.X;
                float dy = Hedef.Konum.Y - Konum.Y;
                float mesafe = (float)Math.Sqrt(dx * dx + dy * dy);
                // Vektör normalizasyonu ve hızla çarpma
                Konum = new PointF(Konum.X + (dx / mesafe) * Hiz, Konum.Y + (dy / mesafe) * Hiz);
                Aci += 15; // Büyü için dönme açısı
            }
        }

        // OYUN DEĞİŞKENLERİ
        enum OyunDurumu { Menu, NasilOynanir, Oynuyor, Duraklatildi, OyunBitti, Kazandi, BilgiEkrani }
        OyunDurumu durum = OyunDurumu.Menu;

        List<Dusman> dusmanlar = new List<Dusman>();
        List<Kule> kuleler = new List<Kule>();
        List<KayanYazi> efektler = new List<KayanYazi>();
        List<Mermi> mermiler = new List<Mermi>(); // YENİ: Aktif mermiler listesi
        List<PointF> yol = new List<PointF>();

        int altin = 300; int can = 100; int dalga = 1; int skor = 0; int toplamDalgaSayisi = 5;
        int spawnlananDusmanSayisi = 0; int buDalgadakiToplamDusman = 0;

        // RENKLER & UI
        Color renkZemin = Color.FromArgb(44, 62, 80); Color renkYol = Color.FromArgb(236, 240, 241);
        Color renkYolGolge = Color.FromArgb(30, 40, 50); Color renkUI = Color.FromArgb(52, 73, 94);
        Rectangle btnOkRect = new Rectangle(30, 650, 140, 70); Rectangle btnTopRect = new Rectangle(190, 650, 140, 70);
        Rectangle btnBuyuRect = new Rectangle(350, 650, 140, 70); Rectangle btnBombaRect = new Rectangle(510, 650, 140, 70);
        Rectangle btnPauseRect = new Rectangle(1150, 20, 100, 40); Rectangle btnInfoRect = new Rectangle(1040, 20, 100, 40);

        // ETKİLEŞİM
        Kule haritaSeciliKule = null; Rectangle btnYukseltRect; Rectangle btnSatRect;
        Kule hoverKule = null; string seciliButonKule = "";

        // MENÜLER
        Rectangle btnBaslaRect = new Rectangle(450, 300, 300, 80); Rectangle btnNasilRect = new Rectangle(450, 400, 300, 80);
        Rectangle btnCikisRect = new Rectangle(450, 500, 300, 80); Rectangle btnMenuDonRect = new Rectangle(450, 550, 300, 80);

        public Form1()
        {
            InitializeComponent();
            this.Text = "Modern Tower Defense - Mermi Efektli Final";
            this.Size = new Size(1280, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true; this.BackColor = renkZemin;
            yol.Add(new PointF(50, 200)); yol.Add(new PointF(250, 200)); yol.Add(new PointF(250, 500));
            yol.Add(new PointF(650, 500)); yol.Add(new PointF(650, 250)); yol.Add(new PointF(950, 250));
            yol.Add(new PointF(950, 550)); yol.Add(new PointF(1200, 550));
            oyunTimer.Interval = 16; oyunTimer.Enabled = true; oyunTimer.Tick += OyunTimer_Tick;
            this.Paint += Form1_Paint; this.MouseClick += Form1_MouseClick; this.MouseMove += Form1_MouseMove;
        }

        // YARDIMCI METOT: Mesafe Hesaplama
        private float Mesafe(PointF p1, PointF p2) { return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)); }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (durum != OyunDurumu.Oynuyor) return;
            hoverKule = null;
            foreach (var k in kuleler) { if (Math.Abs(k.Konum.X - e.X) < 30 && Math.Abs(k.Konum.Y - e.Y) < 30) { hoverKule = k; break; } }
            this.Invalidate();
        }

        private void OyunTimer_Tick(object sender, EventArgs e)
        {
            if (durum != OyunDurumu.Oynuyor) return;

            // 1. YAZI EFEKTLERİNİ GÜNCELLE
            for (int i = efektler.Count - 1; i >= 0; i--) { if (!efektler[i].Guncelle()) efektler.RemoveAt(i); }

            // 2. MERMİLERİ GÜNCELLE (Hareket ve Vuruş Kontrolü)
            for (int i = mermiler.Count - 1; i >= 0; i--)
            {
                Mermi m = mermiler[i];
                m.Ilerle();
                if (m.HedefeUlastiMi() && m.Hedef.Yasiyor)
                {
                    // VURUŞ ANI! Hasar ver.
                    if (m.Tip == MermiTipi.TopGullesi || m.Tip == MermiTipi.Bomba)
                    {
                        // Alan Hasarı (Patlama merkezine yakın olanlar hasar alır)
                        float patlamaMenzili = m.Tip == MermiTipi.Bomba ? 80f : 50f;
                        foreach (var d in dusmanlar.Where(d => d.Yasiyor && Mesafe(m.Konum, d.Konum) < patlamaMenzili))
                        {
                            d.CanAzalt(m.Hasar);
                        }
                    }
                    else
                    {
                        // Tek Hedef Hasarı (Ok, Büyü)
                        m.Hedef.CanAzalt(m.Hasar);
                    }
                    mermiler.RemoveAt(i); // Mermiyi yok et
                }
                else if (!m.Hedef.Yasiyor) { mermiler.RemoveAt(i); } // Hedef öldüyse mermi boşa gider
            }

            // 3. KULELERİN SALDIRI MANTIĞI (Artık Mermi Fırlatıyorlar)
            foreach (var k in kuleler)
            {
                if ((DateTime.Now - k.SonAtisZamani).TotalMilliseconds < k.AtisHiziMs) continue;

                // Menzildeki düşmanları bul
                var hedefler = dusmanlar.Where(d => d.Yasiyor && Mesafe(k.Konum, d.Konum) <= k.Menzil).OrderBy(d => Mesafe(k.Konum, d.Konum)).ToList();
                if (!hedefler.Any()) continue;

                // Kule tipine göre mermi fırlat
                if (k is OkKulesi || k is TopKulesi || k is BombaKulesi)
                {
                    // Tek hedefe bir mermi (Top ve Bomba vurduğu yerde patlayacak)
                    mermiler.Add(new Mermi(k.Konum, hedefler[0], k.Hasar, k));
                    k.SonAtisZamani = DateTime.Now;
                }
                else if (k is BuyuKulesi)
                {
                    // Büyücü aynı anda 5 kişiye mermi atar
                    int atisSayisi = Math.Min(hedefler.Count, 5);
                    for (int j = 0; j < atisSayisi; j++) mermiler.Add(new Mermi(k.Konum, hedefler[j], k.Hasar, k));
                    k.SonAtisZamani = DateTime.Now;
                }
            }

            // 4. DÜŞMAN HAREKETİ VE KONTROLLERİ
            buDalgadakiToplamDusman = dalga * 10;
            int spawnOlasilik = Math.Max(20, 80 - (dalga * 10));
            if (spawnlananDusmanSayisi < buDalgadakiToplamDusman && new Random().Next(0, spawnOlasilik) == 0)
            { dusmanlar.Add(new Dusman(yol[0], dalga)); spawnlananDusmanSayisi++; }

            for (int i = dusmanlar.Count - 1; i >= 0; i--)
            {
                var d = dusmanlar[i]; d.HareketEt(yol);
                if (d.YolIndeksi >= yol.Count) { can -= 10; dusmanlar.RemoveAt(i); if (can <= 0) { can = 0; durum = OyunDurumu.OyunBitti; } }
                else if (!d.Yasiyor) { altin += d.Odul; skor += 20 * dalga; efektler.Add(new KayanYazi($"+{d.Odul} G", d.Konum, Color.Gold)); dusmanlar.RemoveAt(i); }
            }

            if (spawnlananDusmanSayisi >= buDalgadakiToplamDusman && dusmanlar.Count == 0 && can > 0)
            { if (dalga < toplamDalgaSayisi) { dalga++; spawnlananDusmanSayisi = 0; } else { durum = OyunDurumu.Kazandi; } }

            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            if (durum == OyunDurumu.Menu)
            {
                g.Clear(renkZemin); CizBaslik(g, "KULE SAVUNMA", 150, Color.White);
                ButonCiz(g, btnBaslaRect, "OYUNA BAŞLA", Color.FromArgb(46, 204, 113), false, null);
                ButonCiz(g, btnNasilRect, "NASIL OYNANIR?", Color.FromArgb(241, 196, 15), false, null);
                ButonCiz(g, btnCikisRect, "ÇIKIŞ", Color.FromArgb(231, 76, 60), false, null);
            }
            else if (durum == OyunDurumu.NasilOynanir)
            {
                g.Clear(renkZemin); CizBaslik(g, "NASIL OYNANIR?", 50, Color.White);
                Rectangle bilgiKutusu = new Rectangle(200, 150, 880, 450);
                using (SolidBrush saydamFirca = new SolidBrush(Color.FromArgb(80, 0, 0, 0))) { g.FillRectangle(saydamFirca, bilgiKutusu); }
                g.DrawRectangle(new Pen(Color.White, 2), bilgiKutusu);
                string bilgi = "• SOL TIK: Kule Satın Al ve Yerleştir\n• KULEYE TIKLA: Yükseltme ve Satma menüsünü aç\n• İNFO: Kule özelliklerini gör\n• HEDEF: 5 Dalga boyunca hayatta kal!";
                StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(bilgi, new Font("Segoe UI", 14, FontStyle.Bold), Brushes.White, bilgiKutusu, format);
                ButonCiz(g, new Rectangle(490, 630, 300, 80), "MENÜYE DÖN", Color.Orange, false, null);
            }
            else if (durum == OyunDurumu.OyunBitti || durum == OyunDurumu.Kazandi)
            {
                g.Clear(Color.FromArgb(30, 30, 30));
                string mesaj = durum == OyunDurumu.Kazandi ? "ZAFER!" : "OYUN BİTTİ";
                Color renk = durum == OyunDurumu.Kazandi ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);
                CizBaslik(g, mesaj, 200, renk);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString($"TOPLAM SKOR: {skor}", new Font("Segoe UI", 25, FontStyle.Bold), Brushes.Gold, Width / 2, 350, sf);
                ButonCiz(g, btnMenuDonRect, "MENÜYE DÖN", Color.Gray, false, null);
            }
            else if (durum == OyunDurumu.BilgiEkrani)
            {
                g.Clear(Color.FromArgb(40, 44, 52)); CizBaslik(g, "KULE BİLGİLERİ", 50, Color.White);
                Font baslikF = new Font("Segoe UI", 14, FontStyle.Bold); Font normalF = new Font("Segoe UI", 12); int y = 150;
                string[] basliklar = { "KULE TİPİ", "HASAR", "MENZİL", "HIZ", "FİYAT", "ÖZELLİK" };
                for (int i = 0; i < basliklar.Length; i++) g.DrawString(basliklar[i], baslikF, Brushes.Gold, 100 + (i * 180), y);
                y += 50;
                string[][] veriler = { new string[] { "🏹 OKÇU", "15", "150", "Hızlı", "100", "Tek Hedef" }, new string[] { "💣 TOPÇU", "50", "120", "Yavaş", "250", "Alan Hasarı" }, new string[] { "✨ BÜYÜCÜ", "25", "130", "Orta", "200", "5 Düşman" }, new string[] { "☢️ BOMBA", "40", "110", "Orta", "200", "3 Düşman" } };
                foreach (var satir in veriler) { for (int i = 0; i < satir.Length; i++) g.DrawString(satir[i], normalF, Brushes.White, 100 + (i * 180), y); y += 60; }
                ButonCiz(g, new Rectangle(490, 600, 300, 80), "OYUNA DÖN", Color.Orange, false, null);
            }
            else { CizOyunEkrani(g); if (durum == OyunDurumu.Duraklatildi) { using (SolidBrush saydamMavi = new SolidBrush(Color.FromArgb(200, 44, 62, 80))) { g.FillRectangle(saydamMavi, 0, 0, Width, Height); } StringFormat sf = new StringFormat { Alignment = StringAlignment.Center }; g.DrawString("DURAKLATILDI", new Font("Segoe UI", 40, FontStyle.Bold), Brushes.White, Width / 2, 300, sf); } }
        }

        private void CizOyunEkrani(Graphics g)
        {
            using (SolidBrush uiFirca = new SolidBrush(renkUI)) { g.FillRectangle(uiFirca, 0, 0, this.Width, 80); }
            Font etiketFont = new Font("Segoe UI", 12, FontStyle.Bold); Font degerFont = new Font("Segoe UI", 15, FontStyle.Bold);
            g.FillEllipse(Brushes.Gold, 50, 28, 24, 24); g.DrawEllipse(Pens.Orange, 50, 28, 24, 24); g.DrawString("ALTIN:", etiketFont, Brushes.Gold, 80, 30); g.DrawString(altin.ToString(), degerFont, Brushes.White, 140, 27);
            g.DrawString("CAN:", etiketFont, Brushes.Tomato, 300, 30); Rectangle canBarArka = new Rectangle(350, 32, 200, 20); g.FillRectangle(new SolidBrush(Color.FromArgb(80, 0, 0)), canBarArka); g.DrawRectangle(new Pen(Color.Tomato), canBarArka); if (can > 0) g.FillRectangle(Brushes.Crimson, 351, 33, Math.Min(200, can * 2), 18); g.DrawString(can.ToString(), new Font("Arial", 10, FontStyle.Bold), Brushes.White, 435, 33);
            g.DrawString($"DALGA: {dalga}/{toplamDalgaSayisi}", degerFont, Brushes.Cyan, 670, 27); g.DrawString($"SKOR: {skor}", degerFont, Brushes.White, 860, 27);
            ButonCiz(g, btnPauseRect, durum == OyunDurumu.Duraklatildi ? "DEVAM" : "PAUSE", Color.White, false, null); ButonCiz(g, btnInfoRect, "INFO", Color.DeepSkyBlue, false, null);

            if (yol.Count > 1) { using (Pen pGolge = new Pen(renkYolGolge, 60) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLines(pGolge, yol.ToArray()); using (Pen pYol = new Pen(renkYol, 50) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLines(pYol, yol.ToArray()); }
            g.FillEllipse(Brushes.Green, yol[0].X - 30, yol[0].Y - 30, 60, 60); g.DrawString("START", new Font("Arial", 10, FontStyle.Bold), Brushes.White, yol[0].X - 25, yol[0].Y - 8);
            PointF bitis = yol[yol.Count - 1]; g.FillEllipse(Brushes.Red, bitis.X - 30, bitis.Y - 30, 60, 60); g.DrawString("END", new Font("Arial", 10, FontStyle.Bold), Brushes.White, bitis.X - 15, bitis.Y - 8);

            foreach (var k in kuleler)
            {
                bool menzilGoster = (k == haritaSeciliKule) || (k == hoverKule);
                if (menzilGoster) { using (SolidBrush menzilFirca = new SolidBrush(Color.FromArgb(40, k.Renk))) g.FillEllipse(menzilFirca, k.Konum.X - k.Menzil, k.Konum.Y - k.Menzil, k.Menzil * 2, k.Menzil * 2); using (Pen menzilKenar = new Pen(Color.FromArgb(150, k.Renk), 1) { DashStyle = DashStyle.Dot }) g.DrawEllipse(menzilKenar, k.Konum.X - k.Menzil, k.Konum.Y - k.Menzil, k.Menzil * 2, k.Menzil * 2); }
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, 0, 0, 0)), k.Konum.X - 25, k.Konum.Y - 20, 50, 50); g.FillEllipse(new SolidBrush(k.Renk), k.Konum.X - 22, k.Konum.Y - 22, 44, 44);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; string sembol = k is OkKulesi ? "🏹" : k is TopKulesi ? "💣" : k is BombaKulesi ? "☢️" : "✨"; g.DrawString(sembol, new Font("Segoe UI Emoji", 16), Brushes.White, k.Konum.X, k.Konum.Y, sf); g.DrawString(new string('⭐', k.Seviye), new Font("Segoe UI Emoji", 8), Brushes.Gold, k.Konum.X - 15, k.Konum.Y - 35);
                if (k == hoverKule && k != haritaSeciliKule) { Rectangle balon = new Rectangle((int)k.Konum.X - 40, (int)k.Konum.Y - 70, 80, 30); g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), balon); g.DrawString($"Lv {k.Seviye} {k.Isim}", new Font("Arial", 8, FontStyle.Bold), Brushes.White, balon, sf); }
            }

            foreach (var d in dusmanlar) { RectangleF rect = new RectangleF(d.Konum.X - 14, d.Konum.Y - 14, 28, 28); Color dRenk = dalga == 1 ? Color.FromArgb(231, 76, 60) : dalga == 3 ? Color.FromArgb(192, 57, 43) : Color.DarkRed; using (LinearGradientBrush firca = new LinearGradientBrush(rect, dRenk, Color.Black, 45f)) g.FillEllipse(firca, rect); g.FillRectangle(Brushes.Black, d.Konum.X - 14, d.Konum.Y - 22, 28, 5); using (SolidBrush canBarFirca = new SolidBrush(Color.FromArgb(46, 204, 113))) g.FillRectangle(canBarFirca, d.Konum.X - 14, d.Konum.Y - 22, 28 * (d.Can / d.MaksimumCan), 5); }
            foreach (var e in efektler) { using (SolidBrush b = new SolidBrush(Color.FromArgb(e.Omur, e.Renk))) g.DrawString(e.Metin, new Font("Segoe UI", 12, FontStyle.Bold), b, e.Konum); }

            // --- MERMİLERİ ÇİZ (YENİ KISIM) ---
            foreach (var m in mermiler)
            {
                if (m.Tip == MermiTipi.Ok)
                {
                    // Ok: Hedefe dönük ince çizgi
                    float dx = m.Hedef.Konum.X - m.Konum.X; float dy = m.Hedef.Konum.Y - m.Konum.Y;
                    float aci = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                    GraphicsState state = g.Save();
                    g.TranslateTransform(m.Konum.X, m.Konum.Y); g.RotateTransform(aci);
                    using (Pen okKalemi = new Pen(Color.SaddleBrown, 3)) g.DrawLine(okKalemi, -10, 0, 10, 0); // 20px uzunluk
                    g.Restore(state);
                }
                else if (m.Tip == MermiTipi.TopGullesi)
                {
                    // Top Güllesi: Küçük siyah daire
                    g.FillEllipse(Brushes.Black, m.Konum.X - 5, m.Konum.Y - 5, 10, 10);
                }
                else if (m.Tip == MermiTipi.BuyuYildizi)
                {
                    // Büyü: Dönerek giden mor kare (elmas)
                    GraphicsState state = g.Save();
                    g.TranslateTransform(m.Konum.X, m.Konum.Y); g.RotateTransform(m.Aci);
                    g.FillRectangle(Brushes.BlueViolet, -6, -6, 12, 12);
                    g.Restore(state);
                }
                else if (m.Tip == MermiTipi.Bomba)
                {
                    // Bomba: Siyah daire, kırmızı merkezli
                    g.FillEllipse(Brushes.Black, m.Konum.X - 8, m.Konum.Y - 8, 16, 16);
                    g.FillEllipse(Brushes.Red, m.Konum.X - 3, m.Konum.Y - 3, 6, 6);
                }
            }

            if (haritaSeciliKule != null) { int panelX = (int)haritaSeciliKule.Konum.X + 30; int panelY = (int)haritaSeciliKule.Konum.Y - 50; if (panelX > Width - 150) panelX = (int)haritaSeciliKule.Konum.X - 160; Rectangle panelRect = new Rectangle(panelX, panelY, 140, 100); g.FillRectangle(new SolidBrush(Color.FromArgb(220, 40, 40, 40)), panelRect); g.DrawRectangle(Pens.White, panelRect); g.DrawString($"{haritaSeciliKule.Isim} (Lv{haritaSeciliKule.Seviye})", new Font("Arial", 9, FontStyle.Bold), Brushes.White, panelX + 5, panelY + 5); btnYukseltRect = new Rectangle(panelX + 10, panelY + 25, 120, 30); btnSatRect = new Rectangle(panelX + 10, panelY + 60, 120, 30); ButonCiz(g, btnYukseltRect, $"YÜKSELT (50)", Color.SeaGreen, false, null); ButonCiz(g, btnSatRect, $"SAT ({haritaSeciliKule.SatisFiyati})", Color.Crimson, false, null); }
            using (SolidBrush uiFirca = new SolidBrush(renkUI)) { g.FillRectangle(uiFirca, 0, 620, this.Width, 160); }
            ButonCiz(g, btnOkRect, "OKÇU\n100", Color.FromArgb(46, 204, 113), seciliButonKule == "Ok", new OkKulesi(new PointF(0, 0))); ButonCiz(g, btnTopRect, "TOPÇU\n250", Color.FromArgb(230, 126, 34), seciliButonKule == "Top", new TopKulesi(new PointF(0, 0))); ButonCiz(g, btnBuyuRect, "BÜYÜCÜ\n200", Color.FromArgb(155, 89, 182), seciliButonKule == "Buyu", new BuyuKulesi(new PointF(0, 0))); ButonCiz(g, btnBombaRect, "BOMBA\n200", Color.FromArgb(192, 57, 43), seciliButonKule == "Bomba", new BombaKulesi(new PointF(0, 0))); g.DrawString("Kuleyi Satmak için: SHIFT + TIK", new Font("Segoe UI", 10), Brushes.LightGray, 700, 670);
        }

        private void ButonCiz(Graphics g, Rectangle r, string metin, Color renk, bool secili, Kule ornekKule)
        {
            if (secili) g.FillRectangle(Brushes.White, r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6); using (SolidBrush butonFirca = new SolidBrush(renk)) g.FillRectangle(butonFirca, r);
            if (ornekKule != null) { float ikonX = r.X + 30; float ikonY = r.Y + 35; g.FillEllipse(Brushes.White, ikonX - 20, ikonY - 20, 40, 40); using (SolidBrush kuleRengi = new SolidBrush(ornekKule.Renk)) g.FillEllipse(kuleRengi, ikonX - 17, ikonY - 17, 34, 34); string sembol = ornekKule is OkKulesi ? "🏹" : ornekKule is TopKulesi ? "💣" : ornekKule is BombaKulesi ? "☢️" : "✨"; StringFormat ikonFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; g.DrawString(sembol, new Font("Segoe UI Emoji", 14), Brushes.White, ikonX, ikonY + 2, ikonFormat); StringFormat yaziFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; Rectangle yaziAlani = new Rectangle(r.X + 50, r.Y, r.Width - 50, r.Height); g.DrawString(metin, new Font("Segoe UI", 11, FontStyle.Bold), Brushes.White, yaziAlani, yaziFormat); } else { StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; g.DrawString(metin, new Font("Segoe UI", 12, FontStyle.Bold), (renk == Color.White ? Brushes.Black : Brushes.White), r, sf); }
        }
        private void CizBaslik(Graphics g, string metin, int y, Color renk) { Font baslikFont = new Font("Segoe UI", 48, FontStyle.Bold); SizeF size = g.MeasureString(metin, baslikFont); using (SolidBrush baslikFirca = new SolidBrush(renk)) g.DrawString(metin, baslikFont, baslikFirca, (Width - size.Width) / 2, y); }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (durum == OyunDurumu.Menu) { if (btnBaslaRect.Contains(e.Location)) { durum = OyunDurumu.Oynuyor; OyunuSifirla(); } else if (btnNasilRect.Contains(e.Location)) durum = OyunDurumu.NasilOynanir; else if (btnCikisRect.Contains(e.Location)) Application.Exit(); }
            else if (durum == OyunDurumu.NasilOynanir) { if (new Rectangle(490, 630, 300, 80).Contains(e.Location)) durum = OyunDurumu.Menu; }
            else if (durum == OyunDurumu.OyunBitti || durum == OyunDurumu.Kazandi) { if (btnMenuDonRect.Contains(e.Location)) durum = OyunDurumu.Menu; }
            else if (durum == OyunDurumu.BilgiEkrani) { if (new Rectangle(490, 600, 300, 80).Contains(e.Location)) durum = OyunDurumu.Oynuyor; }
            else if (durum == OyunDurumu.Oynuyor || durum == OyunDurumu.Duraklatildi)
            {
                if (btnPauseRect.Contains(e.Location)) durum = (durum == OyunDurumu.Oynuyor) ? OyunDurumu.Duraklatildi : OyunDurumu.Oynuyor; else if (btnInfoRect.Contains(e.Location)) durum = OyunDurumu.BilgiEkrani;
                if (durum == OyunDurumu.Duraklatildi) { this.Invalidate(); return; }
                if (haritaSeciliKule != null) { if (btnYukseltRect.Contains(e.Location)) { if (altin >= 50) { altin -= 50; haritaSeciliKule.Yukselt(); } else MessageBox.Show("Yükseltme için 50 Altın lazım!"); haritaSeciliKule = null; this.Invalidate(); return; } if (btnSatRect.Contains(e.Location)) { altin += haritaSeciliKule.SatisFiyati; kuleler.Remove(haritaSeciliKule); haritaSeciliKule = null; this.Invalidate(); return; } }
                bool kuleyeTiklandi = false; foreach (var k in kuleler) { if (Math.Abs(k.Konum.X - e.X) < 30 && Math.Abs(k.Konum.Y - e.Y) < 30) { haritaSeciliKule = k; seciliButonKule = ""; kuleyeTiklandi = true; break; } }
                if (!kuleyeTiklandi && !btnYukseltRect.Contains(e.Location) && !btnSatRect.Contains(e.Location)) haritaSeciliKule = null;
                if (btnOkRect.Contains(e.Location)) { seciliButonKule = "Ok"; haritaSeciliKule = null; }
                else if (btnTopRect.Contains(e.Location)) { seciliButonKule = "Top"; haritaSeciliKule = null; }
                else if (btnBuyuRect.Contains(e.Location)) { seciliButonKule = "Buyu"; haritaSeciliKule = null; }
                else if (btnBombaRect.Contains(e.Location)) { seciliButonKule = "Bomba"; haritaSeciliKule = null; }
                else if (seciliButonKule != "" && e.Y < 620 && !kuleyeTiklandi) { Kule k = null; if (seciliButonKule == "Ok" && altin >= 100) k = new OkKulesi(e.Location); else if (seciliButonKule == "Top" && altin >= 250) k = new TopKulesi(e.Location); else if (seciliButonKule == "Buyu" && altin >= 200) k = new BuyuKulesi(e.Location); else if (seciliButonKule == "Bomba" && altin >= 200) k = new BombaKulesi(e.Location); if (k != null) { kuleler.Add(k); altin -= k.Fiyat; seciliButonKule = ""; } else if (k == null && seciliButonKule != "") MessageBox.Show("Altın Yetersiz!"); }
            }
            this.Invalidate();
        }

        private void OyunuSifirla() { dusmanlar.Clear(); kuleler.Clear(); efektler.Clear(); mermiler.Clear(); altin = 300; can = 100; dalga = 1; skor = 0; seciliButonKule = ""; spawnlananDusmanSayisi = 0; haritaSeciliKule = null; }
    }
}