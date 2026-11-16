namespace Duren
{
    public class Karta
    {
        public Kolor Kolor { get; }
        public Wartosc Wartosc { get; }

        // Atut pokazany przeciwnikowi przy przenoszeniu ataku (karta zostaje na rece).
        public bool Odslonieta { get; set; }

        public Karta(Kolor kolor, Wartosc wartosc)
        {
            Kolor = kolor;
            Wartosc = wartosc;
        }

        public bool CzyAtut(Kolor atut) => Kolor == atut;

        public bool CzyTaSamaWartosc(Karta k) => Wartosc == k.Wartosc;

        // Czy TA karta (obrona) bije karte atakujaca.
        public bool CzyBije(Karta atak, Kolor atut)
        {
            if (Kolor == atak.Kolor) return Wartosc > atak.Wartosc;
            return Kolor == atut && atak.Kolor != atut;
        }

        public override string ToString() => Wartosc + " " + Kolor;
    }
}
