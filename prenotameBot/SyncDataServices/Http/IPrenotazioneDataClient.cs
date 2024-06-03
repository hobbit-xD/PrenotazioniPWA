using prenotameBot.Models;

namespace prenotameBot.SyncDataServices.Http
{
    public interface IPrenotazioneDataClient{
        Task<bool> SendPrenotazione(PrenotazioneCreate prenotazione);
        Task<string> GetPrenotazioniByDate(string data);
        Task<string> GetPrenotazioniByUser(long UserId);
        Task<string> GetPrenotazioni();
        Task<bool>  DeletePrenotazioni(string id);
    }
    
}