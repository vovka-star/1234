using System;
using System.Collections.Generic;
using System.Linq;

namespace Duren
{
    // Dureń podrzucany dla 2-4 graczy: 36 kart, po 6 na reke, atut = kolor ostatniej karty talii.
    //
    // Role w turze:
    //  - Glowny    - gracz, ktory zaczal ture (kladzie pierwsza karte),
    //  - Obronca   - gracz po lewej od Glownego (nastepny aktywny),
    //  - Atakujacy - gracz, ktory ma teraz prawo polozyc/dolozyc karte (na zmiane: wszyscy poza obronca).
    //
    // Stan Dorzucanie: obronca zabiera karty, ale wszyscy pozostali gracze moga jeszcze dolozyc karty
    // o wartosciach ze stolu. Gracze, ktorzy nie moga nic dolozyc, sa pomijani automatycznie.
    //
    // Tryb przerzucany (Przerzucany = true): obronca, ktory jeszcze nic nie odbil, moze przeniesc atak
    // na nastepnego gracza karta tej samej wartosci (Przenies). Atut o tej wartosci mozna tylko POKAZAC
    // (zostaje na rece), ale kazdy atut mozna pokazac tylko raz; przy kolejnym przeniesieniu trzeba go wylozyc.
    // Wylozona karta, ktora ktos zabierze na reke, moze byc przez niego znowu raz pokazana.
    //
    // Koniec: gdy talia jest pusta, gracze bez kart "wychodza". Ostatni gracz z kartami to dureń.
    public class Gra
    {
        public const int MaxPar = 6;

        private readonly List<Gracz> gracze;
        private readonly Random rnd;
        private readonly HashSet<Gracz> wyszli = new HashSet<Gracz>();
        private readonly List<Gracz> kolejnoscWyjscia = new List<Gracz>();
        private int pasy; // ilu dokladajacych z rzedu spasowalo od ostatniej polozonej karty

        public Talia Talia { get; private set; }
        public Stol Stol { get; } = new Stol();
        public IReadOnlyList<Gracz> Gracze => gracze;
        public Gracz Glowny { get; private set; }
        public Gracz Atakujacy { get; private set; }
        public Gracz Obronca { get; private set; }
        public StanGry Stan { get; private set; } = StanGry.Przygotowanie;
        public Kolor Atut => Talia.Atut;

        // Wynik: Zwyciezca to pierwszy gracz, ktory pozbyl sie kart; Przegrany to "dureń".
        public Gracz Zwyciezca { get; private set; }
        public Gracz Przegrany { get; private set; }
        public bool Remis { get; private set; }

        // Krotki opis ostatniego przeniesienia ataku (do wyswietlenia w interfejsie).
        public string OstatnieZdarzenie { get; private set; } = "";

        public event Action<Gra> KoniecGry;

        // false = tryb standardowy, true = Dureń przerzucany (mozna przenosic atak).
        public bool Przerzucany { get; }

        public Gra(Gracz a, Gracz b, Random rnd = null, bool przerzucany = false)
            : this(new List<Gracz> { a, b }, rnd, przerzucany)
        {
        }

        public Gra(IEnumerable<Gracz> uczestnicy, Random rnd = null, bool przerzucany = false)
        {
            gracze = uczestnicy.ToList();
            if (gracze.Count < 2 || gracze.Count > 4)
                throw new ArgumentException("W grze moze brac udzial od 2 do 4 graczy.");
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
            wyszli.Clear();
            kolejnoscWyjscia.Clear();

            foreach (Gracz g in gracze)
            {
                g.WyrzucKarty();
                g.DobierzDoSzesciu(Talia);
            }

            Gracz start = zaczyna
                ?? gracze.Where(g => g.NajmniejszyAtut(Atut) != null)
                         .OrderBy(g => g.NajmniejszyAtut(Atut).Wartosc)
                         .FirstOrDefault()
                ?? gracze[rnd.Next(gracze.Count)];

            UstawTure(start);
            Stan = StanGry.Atak;
        }

        // "Zagraj ponownie": zaczyna ten, kto pierwszy pozbyl sie kart w poprzedniej grze.
        public void ZagrajPonownie()
        {
            Start(Zwyciezca);
        }

        public bool CzyWyszedl(Gracz g)
        {
            return wyszli.Contains(g);
        }

        // ------------------------------------------------------------ ruchy graczy

        // Atakujacy (aktualny dokladajacy) kladzie karte.
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
            pasy = 0;
            if (Stan == StanGry.Atak) Stan = StanGry.Obrona;
            return true;
        }

        // Obrona pierwszej nieodbitej karty (uzywane m.in. przez bota).
        public bool Obron(Karta k)
        {
            return Obron(k, Stol.PierwszaNieodbita);
        }

        // Obrona wybranej karty ze stolu: obronca sam decyduje, ktora karte ataku odbija.
        public bool Obron(Karta k, ParaKart para)
        {
            if (Stan != StanGry.Obrona) return false;
            if (para == null || para.CzyOdbita || !Stol.Pary.Contains(para)) return false;
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

            Gracz nowy = NastepnyAktywny(Obronca);
            if (nowy == null || nowy == Obronca) return false;

            int naStole = Stol.LiczbaPar + (pokaz ? 0 : 1);
            return naStole <= MaxPar && naStole <= nowy.LiczbaKart;
        }

        public bool Przenies(Karta k, bool pokaz)
        {
            if (!CzyMoznaPrzeniesc(k, pokaz)) return false;

            Gracz przenoszacy = Obronca;
            Gracz nowy = NastepnyAktywny(przenoszacy);

            if (pokaz)
            {
                k.Odslonieta = true;
                OstatnieZdarzenie = przenoszacy.Nazwa + " przenosi atak i pokazuje atut: " + Symbol(k);
            }
            else
            {
                przenoszacy.ZagrajKarte(k);
                Stol.DodajAtak(k);
                OstatnieZdarzenie = przenoszacy.Nazwa + " przenosi atak kartą: " + Symbol(k);
            }

            // przenoszacy staje po stronie ataku, atak idzie na kolejnego gracza
            Glowny = przenoszacy;
            Atakujacy = przenoszacy;
            Obronca = nowy;
            pasy = 0;
            Stan = StanGry.Obrona;
            return true;
        }

        // Obronca nie moze albo nie chce sie bronic: pozostali gracze moga jeszcze dokladac karty.
        public bool Zabierz()
        {
            if (Stan != StanGry.Obrona) return false;
            Stan = StanGry.Dorzucanie;
            pasy = 0;
            // jesli aktualny dokladajacy nic nie moze dolozyc, kolej przechodzi dalej (a gdy nikt - obronca bierze karty)
            if (!MozeDolozyc(Atakujacy)) return PrzekazDokladanie(true);
            return true;
        }

        // Aktualny dokladajacy rezygnuje z dokladania.
        // - Stan Dorzucanie: kolej przechodzi na nastepnego; gdy wszyscy spasuja, obronca zabiera karty.
        // - Stan Atak i wszystko odbite: kolej na nastepnego; gdy wszyscy spasuja, tura konczy sie "bito".
        public bool Pas()
        {
            if (Stan == StanGry.Dorzucanie) return PrzekazDokladanie(true);
            if (Stan == StanGry.Atak && Stol.CzyWszystkieOdbite) return PrzekazDokladanie(false);
            return false;
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

        // ------------------------------------------------------------ koniec tury

        private bool PrzekazDokladanie(bool obroncaZabiera)
        {
            int ilu = Dokladajacy().Count;
            pasy++; // aktualny dokladajacy spasowal
            while (pasy < ilu)
            {
                Atakujacy = NastepnyDokladajacy(Atakujacy);
                if (MozeDolozyc(Atakujacy)) return true; // ten gracz ma jeszcze mozliwosc dolozenia
                pasy++;                                   // nie moze, wiec pasuje automatycznie
            }
            return obroncaZabiera ? ZakonczBranieKart() : ZakonczBito();
        }

        private bool ZakonczBranieKart()
        {
            OstatnieZdarzenie = "";
            Obronca.DodajKarty(Stol.ZabierzKarty());
            DobierzKartyWszyscy();
            if (SprawdzKoniec()) return true;

            // obronca stracil kolejke: atakuje gracz po nim
            UstawTure(NastepnyAktywny(Obronca));
            Stan = StanGry.Atak;
            return true;
        }

        private bool ZakonczBito()
        {
            OstatnieZdarzenie = "";
            Gracz staryObronca = Obronca;
            Stol.Wyczysc();
            DobierzKartyWszyscy();
            if (SprawdzKoniec()) return true;

            // obronca, ktory sie obronil, atakuje w nastepnej turze (chyba ze juz wyszedl z gry)
            Gracz nowyGlowny = wyszli.Contains(staryObronca) ? NastepnyAktywny(staryObronca) : staryObronca;
            UstawTure(nowyGlowny);
            Stan = StanGry.Atak;
            return true;
        }

        // Dobieranie do 6 kart: najpierw glowny atakujacy i kolejni gracze, obronca na koncu.
        private void DobierzKartyWszyscy()
        {
            foreach (Gracz g in AktywniOd(Glowny))
                if (g != Obronca) g.DobierzDoSzesciu(Talia);
            Obronca.DobierzDoSzesciu(Talia);
        }

        // Gra konczy sie, gdy talia jest pusta i zostaje najwyzej jeden gracz z kartami.
        private bool SprawdzKoniec()
        {
            if (!Talia.CzyPusta) return false;

            foreach (Gracz g in gracze)
            {
                if (!wyszli.Contains(g) && g.LiczbaKart == 0)
                {
                    wyszli.Add(g);
                    kolejnoscWyjscia.Add(g);
                }
            }

            List<Gracz> pozostali = gracze.Where(g => !wyszli.Contains(g)).ToList();
            if (pozostali.Count > 1) return false;

            Stan = StanGry.Koniec;
            if (pozostali.Count == 1)
            {
                Przegrany = pozostali[0];
                Zwyciezca = kolejnoscWyjscia[0];
            }
            else
            {
                Remis = true;
            }
            KoniecGry?.Invoke(this);
            return true;
        }

        // ------------------------------------------------------------ pomocnicze

        private void UstawTure(Gracz glowny)
        {
            Glowny = glowny;
            Atakujacy = glowny;
            Obronca = NastepnyAktywny(glowny);
            pasy = 0;
        }

        // Gracze, ktorzy jeszcze nie wyszli, w kolejnosci przy stole, zaczynajac od g.
        private List<Gracz> AktywniOd(Gracz g)
        {
            int start = gracze.IndexOf(g);
            var wynik = new List<Gracz>();
            for (int i = 0; i < gracze.Count; i++)
            {
                Gracz c = gracze[(start + i) % gracze.Count];
                if (!wyszli.Contains(c)) wynik.Add(c);
            }
            return wynik;
        }

        private Gracz NastepnyAktywny(Gracz g)
        {
            int idx = gracze.IndexOf(g);
            for (int krok = 1; krok < gracze.Count; krok++)
            {
                Gracz c = gracze[(idx + krok) % gracze.Count];
                if (!wyszli.Contains(c)) return c;
            }
            return null;
        }

        // Gracze, ktorzy moga dokladac karty: wszyscy aktywni poza obronca, zaczynajac od glownego atakujacego.
        private List<Gracz> Dokladajacy()
        {
            return AktywniOd(Glowny).Where(g => g != Obronca).ToList();
        }

        private Gracz NastepnyDokladajacy(Gracz g)
        {
            List<Gracz> d = Dokladajacy();
            int i = d.IndexOf(g);
            return d[(i + 1) % d.Count];
        }

        private bool MozeDolozyc(Gracz g)
        {
            if (Stol.LiczbaPar >= MaxPar) return false;
            if (Stol.LiczbaNieodbitych + 1 > Obronca.LiczbaKart) return false;
            return g.Reka.Any(k => Stol.CzyMoznaDorzucic(k));
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
    }
}
