using System.Collections.Generic;
using System.Linq;

namespace Duren
{
    public class Stol
    {
        private readonly List<ParaKart> pary = new List<ParaKart>();

        public IReadOnlyList<ParaKart> Pary => pary;
        public int LiczbaPar => pary.Count;
        public int LiczbaOdbitych => pary.Count(p => p.CzyOdbita);
        public int LiczbaNieodbitych => pary.Count(p => !p.CzyOdbita);
        public bool CzyWszystkieOdbite => pary.Count > 0 && pary.All(p => p.CzyOdbita);
        public ParaKart PierwszaNieodbita => pary.FirstOrDefault(p => !p.CzyOdbita);

        public void DodajAtak(Karta k)
        {
            pary.Add(new ParaKart(k));
        }

        // Mozna dorzucic karte, jesli stol jest pusty albo wartosc wystepuje juz na stole.
        public bool CzyMoznaDorzucic(Karta k)
        {
            if (pary.Count == 0) return true;
            return pary.Any(p => p.Atak.CzyTaSamaWartosc(k) ||
                                 (p.Obrona != null && p.Obrona.CzyTaSamaWartosc(k)));
        }

        public List<Karta> ZabierzKarty()
        {
            var wynik = new List<Karta>();
            foreach (ParaKart p in pary)
            {
                wynik.Add(p.Atak);
                if (p.Obrona != null) wynik.Add(p.Obrona);
            }
            pary.Clear();
            return wynik;
        }

        public void Wyczysc()
        {
            pary.Clear();
        }
    }
}
