namespace prenotameBot.Models
{
    public class PrenotazioneCreate
    {
        public string? NomePrenotazione { get; set; } //il punto di domanda vuol dire che e opzionale
        public DateTime InizioPrenotazione { get; set; }
        public DateTime FinePrenotazione { get; set; }


    }
}