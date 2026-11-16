using System.Linq;

namespace Duren
{
    // Prosta strategia: zawsze najslabsza mozliwa karta, atuty oszczedza.
    public class Bot : Gracz
    {
        public Bot(string nazwa = "Bot") : base(nazwa) { }

        public override bool CzyAutomatyczny => true;

        public override Karta WybierzAtak(Stol stol, Kolor atut)
        {
            Karta najlepsza = reka
                .Where(k => stol.CzyMoznaDorzucic(k))
                .OrderBy(k => k.Kolor == atut ? 1 : 0)
                .ThenBy(k => k.Wartosc)
                .FirstOrDefault();

            // Dokladajac do istniejacego ataku bot nie marnuje atutow.
            if (najlepsza != null && stol.LiczbaPar > 0 && najlepsza.Kolor == atut)
                return null;
            return najlepsza;
        }

        // Bot przenosi atak, gdy tylko moze; najpierw zwyklymi kartami, atut zostawia na koniec.
        public override Karta WybierzPrzeniesienie(Stol stol, Kolor atut)
        {
            if (stol.LiczbaPar == 0) return null;
            Wartosc w = stol.Pary[0].Atak.Wartosc;
            return reka
                .Where(k => k.Wartosc == w)
                .OrderBy(k => k.Kolor == atut ? 1 : 0)
                .FirstOrDefault();
        }

        public override Karta WybierzObrone(Karta atak, Kolor atut)
        {
            return reka
                .Where(k => k.CzyBije(atak, atut))
                .OrderBy(k => k.Kolor == atut ? 1 : 0)
                .ThenBy(k => k.Wartosc)
                .FirstOrDefault();
        }
    }
}
