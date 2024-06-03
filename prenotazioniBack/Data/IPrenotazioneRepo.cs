using prenotazioniBack.Models;

namespace prenotazioniBack.Data
{
    public interface IPrenotazioneRepo
    {
        Task SaveChanges();
        Task<Prenotazione?> GetPrenotazioneById(int id);
        Task<IEnumerable<Prenotazione?>> GetPrenotazioneByUser(long UserId);
        Task<IEnumerable<Prenotazione>> GetAllPrenotazioni();
        Task CreatePrenotazione(Prenotazione cmd);
        void DeletePrenotazione(Prenotazione cmd);
        Task<IEnumerable<Prenotazione>> GetPrenotazioniByDate(string dataInizio);
    }
}