namespace ModernKuleSavunma.Siniflar
{
    // PDF Gereksinimi: IYukseltilebilir Interface'i
    public interface IYukseltilebilir
    {
        int Seviye { get; set; }
        void Yukselt(); // Seviye atlatma metodu
    }
}