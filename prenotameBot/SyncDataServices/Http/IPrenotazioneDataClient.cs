using prenotameBot.Models;

namespace prenotameBot.SyncDataServices.Http
{
    public interface IPrenotazioneDataClient{
        Task SendPrenotazione(PrenotazioneCreate prenotazione);
        Task<string> GetPrenotazioniByDate(string data);
        Task<string> GetPrenotazioni();
    }
    
}