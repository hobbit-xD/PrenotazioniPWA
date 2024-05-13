
namespace prenotazioniBack.Dtos
{
    public class PrenotazioneReadDto
    {
        public int Id { get; set; }
        public string? NomePrenotazione { get; set; } //il punto di domanda vuol dire che e opzionale
        public DateTime InizioPrenotazione { get; set; }
        public DateTime FinePrenotazione { get; set; }
        public string? NumeroPrenotazione { get; set; }
        public bool IsConfermata { get; set; }
    }
}
