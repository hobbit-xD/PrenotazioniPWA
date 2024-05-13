using prenotazioniBack.Models;

namespace prenotazioniBack.Data
{
    public interface IPrenotazioneRepo
    {
        Task SaveChanges();
        Task<Prenotazione?> GetPrenotazioneById(int id);
        Task<IEnumerable<Prenotazione>> GetAllPrenotazioni();
        Task CreatePrenotazione(Prenotazione cmd);
        void DeletePrenotazione(Prenotazione cmd);

    }
}