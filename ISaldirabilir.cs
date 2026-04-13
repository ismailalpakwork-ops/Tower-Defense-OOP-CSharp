using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernKuleSavunma.Siniflar
{
    public class Dusman
    {
        public PointF Konum { get; set; }
        public float Can { get; set; }
        public float MaksimumCan { get; set; }
        public float Hiz { get; set; }
        public int YolIndeksi { get; set; } = 0;
        public bool Yasiyor => Can > 0;
        public int Odul { get; set; }

        public Dusman(PointF baslangic, int dalga)
        {
            Konum = baslangic;

            // İPTAL EDİLDİ: Can değerleri eski (Zor) haline döndü
            MaksimumCan = 100 + ((dalga - 1) * 50);
            Can = MaksimumCan;

            // İPTAL EDİLDİ: Hız eski (Hızlı) haline döndü
            Hiz = 2.5f + ((dalga - 1) * 0.5f);

            // KABUL EDİLDİ: Sadece burayı artırdık
            // Normalde 15 altındı, şimdi 25 altın veriyor.
            Odul = 25 + (dalga * 2);
        }

        public void CanAzalt(int hasar) { Can -= hasar; }

        public void HareketEt(List<PointF> yol)
        {
            if (YolIndeksi >= yol.Count) return;
            PointF hedef = yol[YolIndeksi];

            float dx = hedef.X - Konum.X;
            float dy = hedef.Y - Konum.Y;
            float mesafe = (float)Math.Sqrt(dx * dx + dy * dy);

            if (mesafe < Hiz) { Konum = hedef; YolIndeksi++; }
            else { Konum = new PointF(Konum.X + (dx / mesafe) * Hiz, Konum.Y + (dy / mesafe) * Hiz); }
        }
    }
}