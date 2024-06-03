using System.Globalization;
using Microsoft.EntityFrameworkCore;
using prenotazioniBack.Models;

namespace prenotazioniBack.Data
{
    public class PrenotazioneRepo : IPrenotazioneRepo
    {
        private readonly AppDbContext _context;

        public PrenotazioneRepo(AppDbContext context)
        {
            _context = context;

        }
        public async Task CreatePrenotazione(Prenotazione cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException(nameof(cmd));
            }

            await _context.AddAsync(cmd);
        }

        public void DeletePrenotazione(Prenotazione cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException(nameof(cmd));
            }

            _context.Remove(cmd);
        }

        public async Task<IEnumerable<Prenotazione>> GetAllPrenotazioni()
        {
            return await _context.Prenotazioni.ToListAsync();
        }

        public async Task<Prenotazione?> GetPrenotazioneById(int id)
        {
            return await _context.Prenotazioni.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Prenotazione?>> GetPrenotazioneByUser(long UserId)
        {
            return await _context.Prenotazioni.Where(c => c.TelegramUserId == UserId).ToListAsync();
        }

        public async Task<IEnumerable<Prenotazione>> GetPrenotazioniByDate(string data)
        {
            Console.WriteLine(DateTime.ParseExact(data, "d", new CultureInfo("it-IT")));
            return await _context.Prenotazioni.Where(p => p.InizioPrenotazione.Date == DateTime.Parse(data).Date).ToListAsync();
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}