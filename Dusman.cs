using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ModernKuleSavunma.Siniflar
{
    public class BuyuKulesi : Kule
    {
        public BuyuKulesi(PointF konum)
        {
            Konum = konum;
            Isim = "BÜYÜCÜ";
            Hasar = 25;
            Menzil = 130;
            AtisHiziMs = 1500;
            Fiyat = 200;
            Renk = Color.BlueViolet;
        }

        public override void Saldir(List<Dusman> dusmanlar)
        {
            if ((DateTime.Now - SonAtisZamani).TotalMilliseconds < AtisHiziMs) return;

            var hedefler = dusmanlar
                .Where(d => d.Yasiyor && MesafeHesapla(Konum, d.Konum) <= Menzil)
                .OrderBy(d => MesafeHesapla(Konum, d.Konum))
                .Take(5)
                .ToList();

            if (hedefler.Any())
            {
                foreach (var h in hedefler) h.CanAzalt(Hasar);
                SonAtisZamani = DateTime.Now;
            }
        }
    }
}