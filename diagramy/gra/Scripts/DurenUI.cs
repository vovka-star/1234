using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Duren;

// Caly interfejs gry budowany z kodu. Wystarczy dodac ten skrypt do pustego GameObjectu na scenie.
public class DurenUI : MonoBehaviour
{
    const float KartaW = 90f, KartaH = 130f;
    static readonly Color Stol_ = new Color(0.08f, 0.35f, 0.18f);

    Font font;
    RectTransform root, menuPanel, startPanel, trybPanel, liczbaPanel, gamePanel, endPanel;
    RectTransform polePrzeciwnika, poleStolu, poleReki;
    Text tekstStatus, tekstPunkty, tekstKoniec;
    Button btnZabieram, btnBito;
    Text tekstBito;
    RectTransform wyborPanel, wyborKolumna, opisPanel;
    Text opisTytul, opisTekst;
    bool wybranyTryb;
    Karta wybranaKarta; // karta z reki wybrana do odbicia, gdy na stole jest kilka kart ataku

    Gra gra;
    GraczLudzki czlowiek;
    int punkty = 0;
    string komunikat = "";
    bool petlaDziala, koniecPokazany;

    // ---------------------------------------------------------------- start
    void Start()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI Symbol", "DejaVu Sans" }, 24);

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Stol_;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        var cg = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cg.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cg.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)cg.transform;

        Panel("Tlo", root, Stol_, Vector2.zero, Vector2.one);
        ZbudujMenu();
        ZbudujStart();
        ZbudujTryb();
        ZbudujLiczbe();
        ZbudujGre();
        ZbudujWybor();
        ZbudujOpis();
        ZbudujKoniec();

        // punkty w prawym gornym rogu (miejsce na ikonke uzytkownika dodamy z logowaniem)
        tekstPunkty = Label(root, "", 28, Color.white, TextAnchor.UpperRight, new Vector2(0.6f, 0.93f), new Vector2(0.99f, 1f));
        OdswiezPunkty();

        PokazMenu();
    }

    // ---------------------------------------------------------------- ekrany
    void ZbudujMenu()
    {
        menuPanel = Panel("Menu", root, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
        var kol = Kolumna(menuPanel, 500, 500);
        Etykieta(kol, "DUREŃ", 80, 120);
        Przycisk(kol, "Start", new Vector2(320, 70), PokazStart);
        Przycisk(kol, "Exit", new Vector2(320, 70), Wyjdz);
    }

    void ZbudujStart()
    {
        startPanel = Panel("StartMenu", root, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
        var kol = Kolumna(startPanel, 500, 500);
        Etykieta(kol, "Wybierz tryb", 50, 90);
        Przycisk(kol, "Graj z botem", new Vector2(320, 70), PokazTryb);
        var online = Przycisk(kol, "Graj online (wkrótce)", new Vector2(320, 70), () => { });
        online.interactable = false;
        Przycisk(kol, "Powrót do panelu głównego", new Vector2(320, 70), PokazMenu);
    }

    // Drugie okno po „Graj z botem”: wybor trybu gry.
    void ZbudujTryb()
    {
        trybPanel = Panel("WyborTrybu", root, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
        var kol = Kolumna(trybPanel, 700, 560);
        Etykieta(kol, "Wybierz tryb gry", 50, 80);
        Przycisk(kol, "Standardowy", new Vector2(360, 70), () => PokazOpis(false));
        Przycisk(kol, "Przerzucany", new Vector2(360, 70), () => PokazOpis(true));
        Przycisk(kol, "Powrót", new Vector2(360, 70), PokazStart);
    }

    // Trzecie okno: wybor liczby graczy (2-4), pojawia sie po zatwierdzeniu trybu przyciskiem OK.
    void ZbudujLiczbe()
    {
        liczbaPanel = Panel("LiczbaGraczy", root, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
        var kol = Kolumna(liczbaPanel, 700, 560);
        Etykieta(kol, "W ile osób gramy?", 50, 80);
        Przycisk(kol, "2 osoby (Ty + 1 bot)", new Vector2(420, 70), () => StartZBotem(wybranyTryb, 2));
        Przycisk(kol, "3 osoby (Ty + 2 boty)", new Vector2(420, 70), () => StartZBotem(wybranyTryb, 3));
        Przycisk(kol, "4 osoby (Ty + 3 boty)", new Vector2(420, 70), () => StartZBotem(wybranyTryb, 4));
        Przycisk(kol, "Powrót", new Vector2(420, 70), PokazTryb);
    }

    void ZbudujGre()
    {
        gamePanel = Panel("Gra", root, new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);

        tekstStatus = Label(gamePanel, "", 24, Color.white, TextAnchor.UpperLeft, new Vector2(0.01f, 0.86f), new Vector2(0.65f, 1f));

        polePrzeciwnika = Panel("Przeciwnik", gamePanel, new Color(0, 0, 0, 0), new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.86f));
        DodajH(polePrzeciwnika, 20);

        poleStolu = Panel("Stol", gamePanel, new Color(0, 0, 0, 0.15f), new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.72f));
        DodajH(poleStolu, 20);

        var akcje = Panel("Akcje", gamePanel, new Color(0, 0, 0, 0), new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.30f));
        // "Zabieram" i "Bito" nigdy nie sa widoczne naraz, wiec leza w tym samym miejscu (srodek paska).
        btnZabieram = Przycisk(akcje, "Zabieram", new Vector2(260, 50), KliknijZabieram);
        btnBito = Przycisk(akcje, "Bito", new Vector2(260, 50), KliknijBito);
        tekstBito = btnBito.GetComponentInChildren<Text>();

        // "Wyjdź z gry" stoi w stałym miejscu: prawy gorny rog, pod punktami.
        var wyjdz = Przycisk(gamePanel, "Wyjdź z gry", new Vector2(200, 50), WyjdzZGry);
        var wrt = (RectTransform)wyjdz.transform;
        wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(1, 1);
        wrt.anchoredPosition = new Vector2(-20, -60);

        poleReki = Panel("Reka", gamePanel, new Color(0, 0, 0, 0), new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.22f));
        DodajH(poleReki, 6);
    }

    void ZbudujWybor()
    {
        wyborPanel = Panel("Wybor", root, new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.one);
        wyborKolumna = Kolumna(wyborPanel, 700, 480);
        wyborPanel.gameObject.SetActive(false);
    }

    // Okienko z opisem trybu: pojawia sie po kliknieciu trybu, "OK" przechodzi do wyboru liczby graczy.
    void ZbudujOpis()
    {
        opisPanel = Panel("OpisTrybu", root, new Color(0, 0, 0, 0.85f), Vector2.zero, Vector2.one);
        var kol = Kolumna(opisPanel, 900, 560);
        opisTytul = Etykieta(kol, "", 46, 70);
        opisTekst = Etykieta(kol, "", 26, 300);
        ((RectTransform)opisTekst.transform).sizeDelta = new Vector2(860, 300);
        Przycisk(kol, "OK", new Vector2(260, 60), PokazLiczbe);
        Przycisk(kol, "Wstecz", new Vector2(260, 60), () => opisPanel.gameObject.SetActive(false));
        opisPanel.gameObject.SetActive(false);
    }

    void PokazOpis(bool przerzucany)
    {
        wybranyTryb = przerzucany;
        if (przerzucany)
        {
            opisTytul.text = "Tryb przerzucany";
            opisTekst.text =
                "Zasady jak w trybie standardowym, a dodatkowo: gdy jeszcze nic nie odbiłeś, możesz przenieść atak " +
                "na przeciwnika kartą tej samej wartości co karta ataku. Przeciwnik musi wtedy odbić wszystkie karty " +
                "ze stołu (musi mieć ich co najmniej tyle).\n\n" +
                "Atut o tej samej wartości możesz tylko pokazać: zostaje wtedy na Twojej ręce, a przeciwnik go widzi. " +
                "Każdy atut możesz pokazać tylko raz, kolejnym razem musisz go wyłożyć. Atut, który przeciwnik " +
                "zabierze, może on znowu raz pokazać.";
        }
        else
        {
            opisTytul.text = "Tryb standardowy";
            opisTekst.text =
                "Atakujący wykłada kartę, a obrońca musi ją odbić wyższą kartą tego samego koloru albo atutem, " +
                "albo zabrać karty.\n\n" +
                "Po obronie atakujący może dołożyć kartę o wartości, która jest już na stole. Gdy obrońca zabiera karty, " +
                "atakujący może jeszcze dołożyć karty o tych wartościach. Gra trwa, aż skończy się talia. " +
                "Przegrywa gracz, który zostanie z kartami na ręce (dureń).";
        }
        opisPanel.gameObject.SetActive(true);
    }

    void PokazWybor(Karta k, bool odbij, bool wyloz, bool pokaz)
    {
        Wyczysc(wyborKolumna);
        Etykieta(wyborKolumna, "Co zrobić kartą " + Nominal(k.Wartosc) + Symbol(k.Kolor) + "?", 36, 80);
        if (odbij)
            Przycisk(wyborKolumna, "Odbij kartę", new Vector2(480, 60), () => { wyborPanel.gameObject.SetActive(false); ZacznijOdbijanie(k); });
        if (wyloz)
            Przycisk(wyborKolumna, "Przenieś atak (wyłóż kartę)", new Vector2(480, 60), () => WykonajWybor(() => gra.Przenies(k, false)));
        if (pokaz)
            Przycisk(wyborKolumna, "Przenieś atak (pokaż atut)", new Vector2(480, 60), () => WykonajWybor(() => gra.Przenies(k, true)));
        Przycisk(wyborKolumna, "Anuluj", new Vector2(480, 60), () => wyborPanel.gameObject.SetActive(false));
        wyborPanel.gameObject.SetActive(true);
    }

    void WykonajWybor(System.Func<bool> akcja)
    {
        wyborPanel.gameObject.SetActive(false);
        if (akcja()) komunikat = "";
        PoRuchu();
    }

    void ZbudujKoniec()
    {
        endPanel = Panel("Koniec", root, new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one);
        var kol = Kolumna(endPanel, 700, 450);
        tekstKoniec = Etykieta(kol, "", 40, 160);
        Przycisk(kol, "Zagraj ponownie", new Vector2(320, 70), ZagrajPonownie);
        Przycisk(kol, "Wyjdź z gry", new Vector2(320, 70), WyjdzZGry);
    }

    void Pokaz(RectTransform panel)
    {
        menuPanel.gameObject.SetActive(panel == menuPanel);
        startPanel.gameObject.SetActive(panel == startPanel);
        trybPanel.gameObject.SetActive(panel == trybPanel);
        liczbaPanel.gameObject.SetActive(panel == liczbaPanel);
        gamePanel.gameObject.SetActive(panel == gamePanel);
        endPanel.gameObject.SetActive(false);
        wyborPanel.gameObject.SetActive(false);
        opisPanel.gameObject.SetActive(false);
    }

    void PokazMenu() { StopAllCoroutines(); petlaDziala = false; Pokaz(menuPanel); }
    void PokazStart() { Pokaz(startPanel); }
    void PokazTryb() { Pokaz(trybPanel); }
    void PokazLiczbe() { Pokaz(liczbaPanel); }

    void Wyjdz()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- przebieg gry
    void StartZBotem(bool przerzucany, int liczbaGraczy)
    {
        czlowiek = new GraczLudzki("Ty");
        var uczestnicy = new List<Gracz> { czlowiek }; // kolejnosc przy stole: Ty, potem boty
        for (int i = 1; i < liczbaGraczy; i++) uczestnicy.Add(new Bot("Bot " + i));
        gra = new Gra(uczestnicy, null, przerzucany);
        gra.Start();
        koniecPokazany = false;
        komunikat = "";
        Pokaz(gamePanel);
        PoRuchu();
    }

    void ZagrajPonownie()
    {
        gra.ZagrajPonownie(); // zaczyna ten, kto wygral
        koniecPokazany = false;
        komunikat = "";
        endPanel.gameObject.SetActive(false);
        PoRuchu();
    }

    void WyjdzZGry() { PokazMenu(); }

    bool CzyKolejBota()
    {
        if (gra.Stan == StanGry.Atak || gra.Stan == StanGry.Dorzucanie) return gra.Atakujacy.CzyAutomatyczny;
        if (gra.Stan == StanGry.Obrona) return gra.Obronca.CzyAutomatyczny;
        return false;
    }

    void PoRuchu()
    {
        Odswiez();
        if (gra.Stan != StanGry.Koniec && !petlaDziala && CzyKolejBota())
            StartCoroutine(PetlaBota());
    }

    IEnumerator PetlaBota()
    {
        petlaDziala = true;
        while (gra.Stan != StanGry.Koniec && CzyKolejBota())
        {
            // gdy czlowiek juz wyszedl z gry, boty graja szybciej
            yield return new WaitForSeconds(gra.CzyWyszedl(czlowiek) ? 0.15f : 0.9f);
            if (!gra.WykonajRuchAutomatyczny()) break;
            Odswiez();
        }
        petlaDziala = false;
        Odswiez();
    }

    void KliknijKarte(Karta k)
    {
        bool ok;
        if ((gra.Stan == StanGry.Atak || gra.Stan == StanGry.Dorzucanie) && gra.Atakujacy == czlowiek)
        {
            ok = gra.Atakuj(k);
            if (!ok) komunikat = "Tą kartą nie możesz teraz zaatakować (wartość musi być już na stole).";
        }
        else if (gra.Stan == StanGry.Obrona && gra.Obronca == czlowiek)
        {
            // ponowne klikniecie wybranej karty anuluje wybor
            if (wybranaKarta == k) { wybranaKarta = null; komunikat = ""; Odswiez(); return; }
            wybranaKarta = null;

            bool odbij = false;
            foreach (ParaKart p in gra.Stol.Pary)
                if (!p.CzyOdbita && k.CzyBije(p.Atak, gra.Atut)) odbij = true;
            bool wyloz = gra.CzyMoznaPrzeniesc(k, false);
            bool pokaz = gra.CzyMoznaPrzeniesc(k, true);
            int opcji = (odbij ? 1 : 0) + (wyloz ? 1 : 0) + (pokaz ? 1 : 0);

            if (opcji > 1) { PokazWybor(k, odbij, wyloz, pokaz); return; } // gracz wybiera: odbij czy przenies

            if (odbij) { ZacznijOdbijanie(k); return; }
            else if (wyloz) ok = gra.Przenies(k, false);
            else if (pokaz) ok = gra.Przenies(k, true);
            else
            {
                ok = false;
                komunikat = "Ta karta nie bije karty ze stołu i nie możesz nią przenieść ataku.";
            }
        }
        else return;

        if (ok) komunikat = "";
        PoRuchu();
    }

    // Odbijanie: jesli karta bije tylko jedna karte ze stolu, odbija ja od razu.
    // Jesli bije kilka, gracz klika potem karte ze stolu, ktora chce odbic.
    void ZacznijOdbijanie(Karta k)
    {
        var cele = new List<ParaKart>();
        foreach (ParaKart p in gra.Stol.Pary)
            if (!p.CzyOdbita && k.CzyBije(p.Atak, gra.Atut)) cele.Add(p);

        if (cele.Count == 1)
        {
            if (gra.Obron(k, cele[0])) komunikat = "";
            PoRuchu();
            return;
        }

        wybranaKarta = k;
        komunikat = "Wybierz kartę ze stołu, którą chcesz odbić (podświetlone na zielono).";
        Odswiez();
    }

    void KliknijAtak(ParaKart para)
    {
        if (wybranaKarta == null || gra.Stan != StanGry.Obrona || gra.Obronca != czlowiek) return;
        Karta k = wybranaKarta;
        wybranaKarta = null;
        komunikat = gra.Obron(k, para) ? "" : "Ta karta nie bije wybranej karty.";
        PoRuchu();
    }

    void KliknijZabieram() { if (gra.Zabierz()) komunikat = ""; PoRuchu(); }
    void KliknijBito() { if (gra.Pas()) komunikat = ""; PoRuchu(); }

    // ---------------------------------------------------------------- rysowanie stanu gry
    void Odswiez()
    {
        if (gra == null) return;
        if (gra.Stan != StanGry.Obrona || gra.Obronca != czlowiek) wybranaKarta = null;

        Wyczysc(polePrzeciwnika);
        Wyczysc(poleStolu);
        Wyczysc(poleReki);

        // kazdy przeciwnik: podpis (imie, liczba kart, rola) i rzad kart (rewersy albo pokazane atuty)
        foreach (Gracz g in gra.Gracze)
        {
            if (g == czlowiek) continue;

            var blok = new GameObject("Przeciwnik", typeof(RectTransform));
            blok.transform.SetParent(polePrzeciwnika, false);
            var brt = (RectTransform)blok.transform;
            brt.sizeDelta = new Vector2(340, 92);
            DodajV(brt, 2);

            string rola = "";
            if (gra.CzyWyszedl(g)) rola = " - wyszedł";
            else if (gra.Stan != StanGry.Koniec && g == gra.Obronca) rola = " [broni się]";
            else if (gra.Stan != StanGry.Koniec && g == gra.Atakujacy) rola = " [atakuje]";
            var napis = Etykieta(brt, g.Nazwa + " (" + g.LiczbaKart + ")" + rola, 20, 24);
            ((RectTransform)napis.transform).sizeDelta = new Vector2(340, 24);

            var rzad = new GameObject("Rzad", typeof(RectTransform));
            rzad.transform.SetParent(brt, false);
            var rrt = (RectTransform)rzad.transform;
            rrt.sizeDelta = new Vector2(340, 66);
            DodajH(rrt, g.LiczbaKart > 8 ? -22 : 4);

            foreach (Karta bk in g.Reka)
            {
                if (bk.Odslonieta)
                {
                    RysujKarte(rrt, bk, null, new Vector2(44, 64)); // pokazany atut
                    continue;
                }
                var rub = Panel("Rewers", rrt, new Color(0.15f, 0.2f, 0.55f), Vector2.zero, Vector2.one);
                rub.anchorMin = rub.anchorMax = new Vector2(0.5f, 0.5f);
                rub.sizeDelta = new Vector2(44, 64);
            }
        }

        foreach (ParaKart p in gra.Stol.Pary)
        {
            var para = new GameObject("Para", typeof(RectTransform));
            para.transform.SetParent(poleStolu, false);
            var prt = (RectTransform)para.transform;
            prt.sizeDelta = new Vector2(KartaW, 2 * KartaH + 8);
            DodajV(prt, 8);

            ParaKart pk = p;
            bool cel = wybranaKarta != null && !p.CzyOdbita && wybranaKarta.CzyBije(p.Atak, gra.Atut);
            RysujKarte(prt, p.Atak, cel ? (UnityAction)(() => KliknijAtak(pk)) : null,
                       default(Vector2), cel ? new Color(0.2f, 0.9f, 0.2f) : default(Color));
            if (p.Obrona != null) RysujKarte(prt, p.Obrona, null);
            else
            {
                var puste = Panel("Puste", prt, new Color(1, 1, 1, 0.12f), Vector2.zero, Vector2.one);
                puste.anchorMin = puste.anchorMax = new Vector2(0.5f, 0.5f);
                puste.sizeDelta = new Vector2(KartaW, KartaH);
            }
        }

        var reka = new List<Karta>(czlowiek.Reka);
        reka.Sort((a, b) => a.Kolor != b.Kolor ? a.Kolor.CompareTo(b.Kolor) : a.Wartosc.CompareTo(b.Wartosc));
        poleReki.GetComponent<HorizontalLayoutGroup>().spacing = reka.Count > 10 ? -30 : 6;
        foreach (Karta k in reka)
        {
            Karta kopia = k;
            RysujKarte(poleReki, k, () => KliknijKarte(kopia), default(Vector2),
                       k == wybranaKarta ? new Color(1f, 0.6f, 0f) : default(Color));
        }

        btnZabieram.gameObject.SetActive(gra.Stan == StanGry.Obrona && gra.Obronca == czlowiek);
        bool bitoAtak = gra.Stan == StanGry.Atak && gra.Atakujacy == czlowiek && gra.Stol.CzyWszystkieOdbite;
        bool bitoDorz = gra.Stan == StanGry.Dorzucanie && gra.Atakujacy == czlowiek;
        btnBito.gameObject.SetActive(bitoAtak || bitoDorz);
        tekstBito.text = bitoDorz ? "Koniec dokładania" : "Bito";

        string kto;
        if (gra.Stan == StanGry.Atak)
            kto = gra.Atakujacy == czlowiek
                ? (gra.Stol.LiczbaPar == 0 ? "Twój atak: wyłóż kartę." : "Możesz dołożyć kartę o tej samej wartości albo kliknąć „Bito”.")
                : gra.Atakujacy.Nazwa + " myśli nad atakiem...";
        else if (gra.Stan == StanGry.Obrona)
            kto = gra.Obronca != czlowiek
                ? gra.Obronca.Nazwa + " się broni..."
                : (gra.Przerzucany && gra.Stol.LiczbaOdbitych == 0
                    ? "Bronisz się: odbij kartę (gdy kart ataku jest kilka, wybierzesz którą), przenieś atak kartą tej samej wartości albo „Zabieram”."
                    : "Bronisz się: odbij kartę (możesz wybrać którą) albo „Zabieram”.");
        else if (gra.Stan == StanGry.Dorzucanie)
            kto = gra.Atakujacy == czlowiek
                ? "Obrońca zabiera karty. Możesz dołożyć karty o wartościach ze stołu, potem kliknij „Koniec dokładania”."
                : (gra.Obronca == czlowiek
                    ? "Zabierasz karty. " + gra.Atakujacy.Nazwa + " dokłada..."
                    : gra.Obronca.Nazwa + " zabiera karty. " + gra.Atakujacy.Nazwa + " dokłada...");
        else kto = "Koniec gry";

        tekstStatus.text = "Tryb: " + (gra.Przerzucany ? "przerzucany" : "standardowy") + "    Atut: " + Symbol(gra.Atut) + "    Talia: " + gra.Talia.LiczbaKart +
                           "    Graczy: " + gra.Gracze.Count + "\n" + kto +
                           (gra.OstatnieZdarzenie != "" ? "\n" + gra.OstatnieZdarzenie : "") +
                           (komunikat != "" ? "\n" + komunikat : "");

        if (gra.Stan == StanGry.Koniec && !koniecPokazany) PokazKoniec();
    }

    void PokazKoniec()
    {
        koniecPokazany = true;
        if (gra.Remis)
            tekstKoniec.text = "Remis!";
        else if (gra.Przegrany == czlowiek)
        {
            punkty -= 10;
            tekstKoniec.text = "Przegrałeś (zostałeś durniem).\nStraciłeś 10 punktów";
        }
        else
        {
            punkty += 10;
            string durnie = gra.Gracze.Count > 2 ? "\nDurniem został: " + gra.Przegrany.Nazwa : "";
            tekstKoniec.text = "Wygrałeś!" + durnie + "\nZdobyłeś 10 punktów";
        }
        OdswiezPunkty();
        endPanel.gameObject.SetActive(true);
    }

    void OdswiezPunkty() { tekstPunkty.text = "Punkty: " + punkty; }

    void RysujKarte(Transform rodzic, Karta k, UnityAction klik, Vector2 rozmiar = default(Vector2), Color ramka = default(Color))
    {
        if (rozmiar == Vector2.zero) rozmiar = new Vector2(KartaW, KartaH);
        bool czerwona = k.Kolor == Kolor.Kier || k.Kolor == Kolor.Karo;
        Color tlo = (gra != null && k.Kolor == gra.Atut) ? new Color(1f, 0.95f, 0.7f) : Color.white;
        var go = new GameObject("Karta", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(rodzic, false);
        ((RectTransform)go.transform).sizeDelta = rozmiar;
        go.GetComponent<Image>().color = tlo;
        var btn = go.GetComponent<Button>();
        if (klik != null) btn.onClick.AddListener(klik); else btn.interactable = false;
        // nieklikalne karty nie maja byc przyciemnione
        var cb = btn.colors; cb.disabledColor = Color.white; btn.colors = cb;
        var t = Label(go.transform, Nominal(k.Wartosc) + "\n" + Symbol(k.Kolor), rozmiar.x < 50 ? 16 : (rozmiar.x < 70 ? 22 : 34),
                      czerwona ? new Color(0.8f, 0.1f, 0.1f) : Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        t.fontStyle = FontStyle.Bold;

        if (ramka.a > 0f || k.Odslonieta) // ramka: wybor/cel albo pokazany atut (niebieska)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = ramka.a > 0f ? ramka : new Color(0.1f, 0.4f, 1f);
            o.effectDistance = new Vector2(4, -4);
        }
    }

    static string Symbol(Kolor k)
    {
        switch (k)
        {
            case Kolor.Pik: return "♠";
            case Kolor.Kier: return "♥";
            case Kolor.Karo: return "♦";
            default: return "♣";
        }
    }

    static string Nominal(Wartosc w)
    {
        int v = (int)w;
        if (v <= 10) return v.ToString();
        switch (w)
        {
            case Wartosc.Walet: return "J";
            case Wartosc.Dama: return "Q";
            case Wartosc.Krol: return "K";
            default: return "A";
        }
    }

    // ---------------------------------------------------------------- pomocnicy UI
    static void Wyczysc(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            t.GetChild(i).gameObject.SetActive(false);
            Destroy(t.GetChild(i).gameObject);
        }
    }

    RectTransform Panel(string nazwa, Transform rodzic, Color c, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject(nazwa, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(rodzic, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = c.a > 0.5f; // przezroczyste panele nie blokuja klikniec
        return rt;
    }

    Text Label(Transform rodzic, string s, int rozmiar, Color c, TextAnchor a, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject("Tekst", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(rodzic, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font; t.text = s; t.fontSize = rozmiar; t.color = c; t.alignment = a;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // tekst o stalym rozmiarze, do ukladania w kolumnie
    Text Etykieta(Transform rodzic, string s, int rozmiar, float wysokosc)
    {
        var t = Label(rodzic, s, rozmiar, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        ((RectTransform)t.transform).sizeDelta = new Vector2(650, wysokosc);
        return t;
    }

    Button Przycisk(Transform rodzic, string s, Vector2 rozmiar, UnityAction akcja)
    {
        var go = new GameObject("Przycisk", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(rodzic, false);
        ((RectTransform)go.transform).sizeDelta = rozmiar;
        go.GetComponent<Image>().color = new Color(0.95f, 0.85f, 0.45f);
        var b = go.GetComponent<Button>();
        b.onClick.AddListener(akcja);
        Label(go.transform, s, 26, Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
        return b;
    }

    RectTransform Kolumna(Transform rodzic, float w, float h)
    {
        var go = new GameObject("Kolumna", typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(rodzic, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = false; v.childControlHeight = false;
        v.childForceExpandWidth = false; v.childForceExpandHeight = false;
        v.spacing = 20;
        return rt;
    }

    static void DodajH(RectTransform rt, float odstep)
    {
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false; h.childControlHeight = false;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        h.spacing = odstep;
    }

    static void DodajV(RectTransform rt, float odstep)
    {
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = false; v.childControlHeight = false;
        v.childForceExpandWidth = false; v.childForceExpandHeight = false;
        v.spacing = odstep;
    }
}
