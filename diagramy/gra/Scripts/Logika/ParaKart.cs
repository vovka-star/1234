namespace Duren
{
    public class ParaKart
    {
        public Karta Atak { get; }
        public Karta Obrona { get; set; }
        public bool CzyOdbita => Obrona != null;

        public ParaKart(Karta atak)
        {
            Atak = atak;
        }
    }
}
