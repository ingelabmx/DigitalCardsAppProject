using System;

namespace DigitalCardsApp.Models
{
    public class CardsDetails
    {
        public int CardID { get; set; }
        public int CheckQTY { get; set; }
        public int HistoricCheckQTY { get; set; }
        public int UsID { get; set; }
        public int BusinessID { get; set; }
        public string CardIDGoogle { get; set; }
        public string CardIDApple { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastCheck { get; set; }
    }
}