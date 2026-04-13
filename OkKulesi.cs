using System;
using System.Collections.Generic;
using System.Drawing;

namespace ModernKuleSavunma.Siniflar
{
    // (1) ABSTRACT CLASS: Kule sınıfı abstract yapıldı
    // (5) INTERFACE: ISaldirabilir ve IYukseltilebilir implemente edildi
    public abstract class Kule : ISaldirabilir, IYukseltilebilir
    {
        // (4) ENCAPSULATION (KAPSÜLLEME)
        // Kural: Private Field (Gizli Değişken) -> Public Property (Açık Özellik)

        private int _hasar;  // Private field
        public int Hasar     // Public property
        {
            get { return _hasar; }
            set { _hasar = value; }
        }

        private int _menzil;
        public int Menzil
        {
            get { return _menzil; }
            set { _menzil = value; }
        }

        private int _fiyat;
        public int Fiyat
        {
            get { return _fiyat; }
            set { _fiyat = value; }
        }

        // Diğer özellikler (Auto-Property olarak kalabilir veya istenirse bunlar da açılabilir)
        public PointF Konum { get; set; }
        public int AtisHiziMs { get; set; }
        public DateTime SonAtisZamani { get; set; }
        public Color Renk { get; set; }
        public string Isim { get; set; }
        public int Seviye { get; set; } = 1;

        // Satış Fiyatı (Read-Only Property)
        public int SatisFiyati
        {
            get { return _fiyat / 2; }
        }

        public Kule()
        {
            SonAtisZamani = DateTime.Now;
        }

        // (1) ORTAK METOD: Mesafe hesaplama her kulede aynıdır
        protected float MesafeHesapla(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        // (3) POLYMORPHISM: Abstract metod (Gövdesi yok, alt sınıflar dolduracak)
        public abstract void Saldir(List<Dusman> dusmanlar);

        // (3) POLYMORPHISM: Virtual metod (İstenirse ezilebilir)
        public virtual void Yukselt()
        {
            Seviye++;
            // Private field'ları property üzerinden güncelliyoruz
            Hasar += (int)(Hasar * 0.2);
            Menzil += 10;
            Fiyat += 50;
        }
    }
}