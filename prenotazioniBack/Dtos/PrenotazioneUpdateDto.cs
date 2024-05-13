using System.ComponentModel.DataAnnotations;

namespace prenotazioniBack.Dtos
{
    public class PrenotazioneUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string? NomePrenotazione { get; set; } //il punto di domanda vuol dire che e opzionale
        [Required]
        public DateTime InizioPrenotazione { get; set; }
        [Required]
        public DateTime FinePrenotazione { get; set; }
        public string? NumeroPrenotazione { get; set; }
        [Required]
        public bool IsConfermata { get; set; }
        [Required]
        public DateTime DataModifica { get; set; }
    }
}
