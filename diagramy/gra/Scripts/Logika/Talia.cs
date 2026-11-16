using System;
using System.Collections.Generic;

namespace Duren
{
    public class Talia
    {
        private readonly List<Karta> karty = new List<Karta>();

        // Atut to kolor ostatniej karty w talii (tej, ktora zostanie dobrana na koncu).
        public Kolor Atut { get; private set; }

        public int LiczbaKart => karty.Count;
        public bool CzyPusta => karty.Count == 0;

        public Talia()
        {
            foreach (Kolor k in Enum.GetValues(typeof(Kolor)))
                foreach (Wartosc w in Enum.GetValues(typeof(Wartosc)))
                    karty.Add(new Karta(k, w));
        }

        public void Tasuj(Random rnd)
        {
            for (int i = karty.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                Karta tmp = karty[i];
                karty[i] = karty[j];
                karty[j] = tmp;
            }
            Atut = karty[0].Kolor; // karty[0] jest dobierana jako ostatnia
        }

        public Karta Dobierz()
        {
            if (karty.Count == 0) return null;
            Karta k = karty[karty.Count - 1];
            karty.RemoveAt(karty.Count - 1);
            return k;
        }
    }
}
