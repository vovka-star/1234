using System;
using System.Collections.Generic;
using System.Linq;

namespace Duren
{
    // Dwuosobowy "Dureń podrzucany": 36 kart, po 6 na reke, atut = kolor ostatniej karty talii.
    // Tryb przerzucany (Przerzucany = true): obronca, ktory jeszcze nic nie odbil, moze przeniesc atak na przeciwnika
    // kartą tej samej wartosci (Przenies). Atut o tej wartosci mozna tylko POKAZAC (zostaje na rece),
    // ale kazdy atut mozna pokazac tylko raz; przy kolejnym przeniesieniu trzeba go wylozyc.
    // Wylozona karta, ktora przeciwnik zabierze na reke, moze byc przez niego znowu raz pokazana.
    // Atakujacy kladzie karty pojedynczo. Gdy obronca zabiera karty (Stan = Dorzucanie),
    // atakujacy moze jeszcze dolozyc karty o wartosciach ze stolu, a potem konczy ("Pas").
    public class Gra
    {
        public const int MaxPar = 6;

        private readonly List<Gracz> gracze;
        private readonly Random rnd;

        public Talia Talia { get; private set; }
        public Stol Stol { get; } = new Stol();
        public IReadOnlyList<Gracz> Gracze => gracze;
        public Gracz Atakujacy { get; private set; }
        public Gracz Obronca { get; private set; }
        public StanGry Stan { get; private set; } = StanGry.Przygotowanie;
        public Kolor Atut => Talia.Atut;

        // Wynik: Zwyciezca to gracz, ktory pozbyl sie kart; Przegrany to "dureń".
        public Gracz Zwyciezca { get; private set; }
        public Gracz Przegrany { get; private set; }
        public bool Remis { get; private set; }

        // Krotki opis ostatniego przeniesienia ataku (do wyswietlenia w interfejsie).
        public string OstatnieZdarzenie { get; private set; } = "";

        public event Action<Gra> KoniecGry;

        // false = tryb standardowy, true = Dureń przerzucany (mozna przenosic atak).
        public bool Przerzucany { get; }

        public Gra(Gracz a, Gracz b, Random rnd = null, bool przerzucany = false)
        {
            gracze = new List<Gracz> { a, b };
            this.rnd = rnd ?? new Random();
            Przerzucany = przerzucany;
        }

        // zaczyna == null: zaczyna gracz z najmniejszym atutem.
        public void Start(Gracz zaczyna = null)
        {
            Talia = new Talia();
            Talia.Tasuj(rnd);
            Stol.Wyczysc();
            Zwyciezca = null;
            Przegrany = null;
            Remis = false;
            OstatnieZdarzenie = "";

            foreach (Gracz g in gracze)
            {
                g.WyrzucKarty();
                g.DobierzDoSzesciu(Talia);
            }

            Gracz start = zaczyna
                ?? gracze.Where(g => g.NajmniejszyAtut(Atut) != null)
                         .OrderBy(g => g.NajmniejszyAtut(Atut).Wartosc)
                         .FirstOrDefault()
                ?? gracze[rnd.Next(2)];

            UstawRole(start);
            Stan = StanGry.Atak;
        }

        // "Zagraj ponownie": zaczyna ten, kto wygral poprzednia gre.
        public void ZagrajPonownie()
        {
            Start(Zwyciezca);
        }

        public bool Atakuj(Karta k)
        {
            if (Stan != StanGry.Atak && Stan != StanGry.Dorzucanie) return false;
            if (!Atakujacy.Reka.Contains(k)) return false;
            if (Stol.LiczbaPar >= MaxPar) return false;
            // nieodbitych kart nie moze byc wiecej niz kart na rece obroncy
            if (Stol.LiczbaNieodbitych + 1 > Obronca.LiczbaKart) return false;
            if (!Stol.CzyMoznaDorzucic(k)) return false;

            Atakujacy.ZagrajKarte(k);
            Stol.DodajAtak(k);
            if (Stan == StanGry.Atak) Stan = StanGry.Obrona;
            return true;
        }

        public bool Obron(Karta k)
        {
            if (Stan != StanGry.Obrona) return false;
            ParaKart para = Stol.PierwszaNieodbita;
            if (para == null) return false;
            if (!Obronca.Reka.Contains(k)) return false;
            if (!k.CzyBije(para.Atak, Atut)) return false;

            Obronca.ZagrajKarte(k);
            para.Obrona = k;
            // gdy po przeniesieniu na stole jest kilka kart, obronca musi odbic wszystkie
            Stan = Stol.LiczbaNieodbitych > 0 ? StanGry.Obrona : StanGry.Atak;
            return true;
        }

        // Czy obronca moze przeniesc atak karta k (pokaz = true: tylko pokazuje atut, karta zostaje na rece).
        public bool CzyMoznaPrzeniesc(Karta k, bool pokaz)
        {
            if (!Przerzucany) return false; // w trybie standardowym nie ma przenoszenia
            if (Stan != StanGry.Obrona) return false;
            if (Stol.LiczbaPar == 0 || Stol.LiczbaOdbitych > 0) return false;
            if (!Obronca.Reka.Contains(k)) return false;
            if (k.Wartosc != Stol.Pary[0].Atak.Wartosc) return false;
            if (pokaz && (k.Kolor != Atut || k.Odslonieta)) return false;

            int naStole = Stol.LiczbaPar + (pokaz ? 0 : 1);
            return naStole <= MaxPar && naStole <= Atakujacy.LiczbaKart;
        }

        public bool Przenies(Karta k, bool pokaz)
        {
            if (!CzyMoznaPrzeniesc(k, pokaz)) return false;

            if (pokaz)
            {
                k.Odslonieta = true;
                OstatnieZdarzenie = Obronca.Nazwa + " przenosi atak i pokazuje atut: " + Symbol(k);
            }
            else
            {
                Obronca.ZagrajKarte(k);
                Stol.DodajAtak(k);
                OstatnieZdarzenie = Obronca.Nazwa + " przenosi atak kartą: " + Symbol(k);
            }

            UstawRole(Obronca); // przenoszacy staje po stronie ataku, przeciwnik musi sie bronic
            Stan = StanGry.Obrona;
            return true;
        }

        private static string Symbol(Karta k)
        {
            string s;
            switch (k.Kolor)
            {
                case Kolor.Pik: s = "♠"; break;
                case Kolor.Kier: s = "♥"; break;
                case Kolor.Karo: s = "♦"; break;
                default: s = "♣"; break;
            }
            return ((int)k.Wartosc <= 10 ? ((int)k.Wartosc).ToString() : k.Wartosc.ToString()) + s;
        }

        // Obronca nie moze albo nie chce sie bronic: atakujacy moze jeszcze dokladac karty.
        public bool Zabierz()
        {
            if (Stan != StanGry.Obrona) return false;
            Stan = StanGry.Dorzucanie;
            return true;
        }

        // Atakujacy konczy dokladanie.
        // - Stan Dorzucanie: obronca zabiera wszystkie karty, atakujacy zostaje atakujacym.
        // - Stan Atak i wszystko odbite: "bito", role sie zamieniaja.
        public bool Pas()
        {
            if (Stan == StanGry.Dorzucanie)
            {
                OstatnieZdarzenie = "";
                Obronca.DodajKarty(Stol.ZabierzKarty());
                DobierzKarty(Atakujacy, Obronca);
                if (SprawdzKoniec()) return true;

                Stan = StanGry.Atak;
                return true;
            }

            if (Stan != StanGry.Atak || !Stol.CzyWszystkieOdbite) return false;

            OstatnieZdarzenie = "";
            Gracz staryAtakujacy = Atakujacy;
            Gracz staryObronca = Obronca;

            Stol.Wyczysc();
            DobierzKarty(staryAtakujacy, staryObronca);
            if (SprawdzKoniec()) return true;

            UstawRole(staryObronca); // obronca, ktory sie obronil, atakuje w nastepnej turze
            Stan = StanGry.Atak;
            return true;
        }

        // Wykonuje jeden ruch bota. Zwraca false, gdy ruch nalezy do czlowieka albo gra sie skonczyla.
        public bool WykonajRuchAutomatyczny()
        {
            if (Stan == StanGry.Atak || Stan == StanGry.Dorzucanie)
            {
                if (!Atakujacy.CzyAutomatyczny) return false;
                Karta k = Atakujacy.WybierzAtak(Stol, Atut);
                if (k != null && Atakuj(k)) return true;
                return Pas();
            }
            if (Stan == StanGry.Obrona)
            {
                if (!Obronca.CzyAutomatyczny) return false;

                Karta p = Przerzucany ? Obronca.WybierzPrzeniesienie(Stol, Atut) : null;
                if (p != null)
                {
                    bool pokaz = p.Kolor == Atut && !p.Odslonieta;
                    if (Przenies(p, pokaz)) return true;
                }

                Karta k = Obronca.WybierzObrone(Stol.PierwszaNieodbita.Atak, Atut);
                if (k != null && Obron(k)) return true;
                return Zabierz();
            }
            return false;
        }

        private void UstawRole(Gracz atakujacy)
        {
            Atakujacy = atakujacy;
            Obronca = gracze[0] == atakujacy ? gracze[1] : gracze[0];
        }

        private void DobierzKarty(Gracz pierwszy, Gracz drugi)
        {
            pierwszy.DobierzDoSzesciu(Talia);
            drugi.DobierzDoSzesciu(Talia);
        }

        // Gra konczy sie, gdy talia jest pusta i ktorys gracz nie ma kart.
        private bool SprawdzKoniec()
        {
            if (!Talia.CzyPusta) return false;

            bool pusty0 = gracze[0].LiczbaKart == 0;
            bool pusty1 = gracze[1].LiczbaKart == 0;
            if (!pusty0 && !pusty1) return false;

            Stan = StanGry.Koniec;
            if (pusty0 && pusty1)
            {
                Remis = true;
            }
            else
            {
                Zwyciezca = pusty0 ? gracze[0] : gracze[1];
                Przegrany = pusty0 ? gracze[1] : gracze[0];
            }
            KoniecGry?.Invoke(this);
            return true;
        }
    }
}
