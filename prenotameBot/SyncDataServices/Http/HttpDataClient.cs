using System.Text;
using System.Text.Json;
using prenotameBot.Models;

namespace prenotameBot.SyncDataServices.Http
{
    public class HttpDataClient : IPrenotazioneDataClient
    {
        private readonly HttpClient _client;

        public HttpDataClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> GetPrenotazioniByDate(string data)
        {
            HttpResponseMessage response = await _client.GetAsync($"commands/search?DataInizio={data}");
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }

        public async Task<string> GetPrenotazioni()
        {
            HttpResponseMessage response = await _client.GetAsync("commands");
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }

        public async Task<string> GetPrenotazioniByUser(long UserId)
        {
            HttpResponseMessage response = await _client.GetAsync($"commands/{UserId}");
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }

        public async Task<bool> SendPrenotazione(PrenotazioneCreate prenotazione)
        {
            StringContent httpContent = new StringContent(
             JsonSerializer.Serialize(prenotazione), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync("commands", httpContent);

            Console.WriteLine(response);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePrenotazioni(string id)
        {
            HttpResponseMessage response = await _client.DeleteAsync($"commands/{id}");
            Console.WriteLine(response);

            return response.IsSuccessStatusCode;
        }
    }
}