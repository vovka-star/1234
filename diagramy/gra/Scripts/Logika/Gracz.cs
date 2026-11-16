using System;
using System.Collections.Generic;
using System.Linq;

namespace Duren
{
    public abstract class Gracz
    {
        public const int KartNaReke = 6;

        protected readonly List<Karta> reka = new List<Karta>();

        public string Nazwa { get; protected set; }
        public IReadOnlyList<Karta> Reka => reka;
        public int LiczbaKart => reka.Count;

        // true = decyzje podejmuje program (bot), false = czeka na klikniecie gracza w interfejsie
        public abstract bool CzyAutomatyczny { get; }

        protected Gracz(string nazwa)
        {
            Nazwa = nazwa;
        }

        public void DobierzDoSzesciu(Talia t)
        {
            while (reka.Count < KartNaReke && !t.CzyPusta)
                reka.Add(t.Dobierz());
        }

        public void DodajKarty(IEnumerable<Karta> karty)
        {
            reka.AddRange(karty);
        }

        public Karta ZagrajKarte(Karta k)
        {
            if (!reka.Remove(k))
                throw new InvalidOperationException("Gracz nie ma tej karty: " + k);

            // Karta wylozona na stol przestaje byc "pokazana". Gdy przeciwnik ja zabierze,
            // moze ja znowu raz pokazac, zeby przeniesc atak.
            k.Odslonieta = false;
            return k;
        }

        public void WyrzucKarty()
        {
            reka.Clear();
        }

        // Najnizszy atut na rece albo null, jesli gracz nie ma atutu.
        public Karta NajmniejszyAtut(Kolor atut)
        {
            return reka.Where(k => k.Kolor == atut).OrderBy(k => k.Wartosc).FirstOrDefault();
        }

        // Zwraca karte do wylozenia albo null (pas / koniec dokladania).
        public abstract Karta WybierzAtak(Stol stol, Kolor atut);

        // Zwraca karte tej samej wartosci co atak, ktora chce uzyc do przeniesienia ataku, albo null.
        public abstract Karta WybierzPrzeniesienie(Stol stol, Kolor atut);

        // Zwraca karte, ktora bije atak, albo null (gracz zabiera karty).
        public abstract Karta WybierzObrone(Karta atak, Kolor atut);

        public override string ToString() => Nazwa;
    }
}
