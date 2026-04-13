using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ModernKuleSavunma.Siniflar
{
    // 4. KULE: BOMBA KULESİ (Turuncu, Orta Menzil, Çoklu Hedef)
    public class BombaKulesi : Kule
    {
        public BombaKulesi(PointF konum)
        {
            Konum = konum;
            Isim = "BOMBA";
            Hasar = 40;        // İstediğin Değer
            Menzil = 110;      // İstediğin Değer
            AtisHiziMs = 2000; // İstediğin Değer (2 saniye)
            Fiyat = 200;       // İstediğin Değer
            Renk = Color.DarkOrange; // Bomba rengi
        }

        public override void Saldir(List<Dusman> dusmanlar)
        {
            if ((DateTime.Now - SonAtisZamani).TotalMilliseconds < AtisHiziMs) return;

            // İSTEK: "En yakın 3 düşmana"
            var hedefler = dusmanlar
                .Where(d => d.Yasiyor && MesafeHesapla(Konum, d.Konum) <= Menzil)
                .OrderBy(d => MesafeHesapla(Konum, d.Konum)) // En yakındakileri sırala
                .Take(3) // Sadece ilk 3 tanesini al
                .ToList();

            if (hedefler.Count > 0)
            {
                foreach (var h in hedefler)
                {
                    h.CanAzalt(Hasar);
                }
                SonAtisZamani = DateTime.Now;
            }
        }
    }
}