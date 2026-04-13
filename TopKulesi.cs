using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ModernKuleSavunma.Siniflar
{
    public class OkKulesi : Kule
    {
        public OkKulesi(PointF konum)
        {
            Konum = konum;
            Isim = "OKÇU";
            Hasar = 15;
            Menzil = 150;
            AtisHiziMs = 1000;
            Fiyat = 100;
            Renk = Color.SeaGreen;
        }

        public override void Saldir(List<Dusman> dusmanlar)
        {
            if ((DateTime.Now - SonAtisZamani).TotalMilliseconds < AtisHiziMs) return;

            var hedef = dusmanlar
                .Where(d => d.Yasiyor && MesafeHesapla(Konum, d.Konum) <= Menzil)
                .OrderBy(d => MesafeHesapla(Konum, d.Konum))
                .FirstOrDefault();

            if (hedef != null) { hedef.CanAzalt(Hasar); SonAtisZamani = DateTime.Now; }
        }
    }
}