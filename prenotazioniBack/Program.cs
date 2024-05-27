using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using prenotazioniBack.Data;
using prenotazioniBack.Dtos;
using prenotazioniBack.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var sqlConBuilder = new SqliteConnectionStringBuilder
{
    ConnectionString = builder.Configuration.GetConnectionString("SQLDbConnection")
};


builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqlConBuilder.ConnectionString));
builder.Services.AddScoped<IPrenotazioneRepo, PrenotazioneRepo>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("api/v1/commands", async (IPrenotazioneRepo repo, IMapper mapper) =>
{
    var commands = await repo.GetAllPrenotazioni();
    return Results.Ok(mapper.Map<IEnumerable<PrenotazioneReadDto>>(commands));
});

app.MapGet("api/v1/commands/{id}", async (IPrenotazioneRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetPrenotazioneById(id);
    if (command != null)
        return Results.Ok(mapper.Map<Prenotazione>(command));

    return Results.NotFound();
});

app.MapGet("api/v1/commands/search", async (IPrenotazioneRepo repo, IMapper mapper, string DataInizio) =>
{
    var commands = await repo.GetPrenotazioniByDate(DataInizio);
    if (commands != null)
        return Results.Ok(mapper.Map<IEnumerable<PrenotazioneReadDto>>(commands));

    return Results.NotFound();
});



app.MapPost("api/v1/commands", async (IPrenotazioneRepo repo, IMapper mapper, [FromBody] PrenotazioneCreateDto cmdCreateDto) =>
{
    var commandModel = mapper.Map<Prenotazione>(cmdCreateDto);
    await repo.CreatePrenotazione(commandModel);
    await repo.SaveChanges();

    var cmdReadDto = mapper.Map<PrenotazioneReadDto>(commandModel);
    
    return Results.Created($"api/v1/commands/{cmdReadDto.Id}", cmdReadDto);
});

app.MapPut("api/v1/commands/{id}", async (IPrenotazioneRepo repo, IMapper mapper, [FromRoute] int id, [FromBody] PrenotazioneUpdateDto cmdUpdateDto) =>
{
    var command = await repo.GetPrenotazioneById(id);
    if (command == null)
        return Results.NotFound();

    mapper.Map(cmdUpdateDto, command);
    await repo.SaveChanges();

    return Results.NoContent();
});

app.MapDelete("api/v1/commands/{id}", async (IPrenotazioneRepo repo, IMapper mapper, [FromRoute] int id) =>
{
    var command = await repo.GetPrenotazioneById(id);
    if (command == null)
        return Results.NotFound();

    repo.DeletePrenotazione(command);
    await repo.SaveChanges();

    return Results.NoContent();
});

app.Run();

