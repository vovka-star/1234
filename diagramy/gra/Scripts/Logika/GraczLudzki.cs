namespace Duren
{
    public class GraczLudzki : Gracz
    {
        // Pozniej: powiazanie z kontem z bazy (Uzytkownik).
        public GraczLudzki(string nazwa) : base(nazwa) { }

        public override bool CzyAutomatyczny => false;

        // Ruch czlowieka wykonuje interfejs, wywolujac Gra.Atakuj / Obron / Zabierz / Pas,
        // wiec te metody nie podejmuja decyzji.
        public override Karta WybierzAtak(Stol stol, Kolor atut) => null;
        public override Karta WybierzPrzeniesienie(Stol stol, Kolor atut) => null;
        public override Karta WybierzObrone(Karta atak, Kolor atut) => null;
    }
}
