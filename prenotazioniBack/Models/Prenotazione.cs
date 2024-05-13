using System.ComponentModel.DataAnnotations;

namespace prenotazioniBack.Models
{
    public class Prenotazione
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string? NomePrenotazione { get; set; } //il punto di domanda vuol dire che e opzionale
        [Required]
        public DateTime InizioPrenotazione { get; set; }
        [Required]
        public DateTime FinePrenotazione { get; set; }
        public string NumeroPrenotazione { get; set; } = Guid.NewGuid().ToString("N").ToUpper();
        [Required]
        public bool IsConfermata { get; set; }
        [Required]
        public DateTime DataInserimento { get; set; }
        [Required]
        public DateTime DataModifica { get; set; }
    }
}