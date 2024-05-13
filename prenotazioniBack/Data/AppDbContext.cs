using Microsoft.EntityFrameworkCore;
using prenotazioniBack.Models;

namespace prenotazioniBack.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Prenotazione> Prenotazioni => Set<Prenotazione>();

    }
}