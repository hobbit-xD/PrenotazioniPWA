using AutoMapper;
using prenotazioniBack.Dtos;
using prenotazioniBack.Models;

namespace prenotazioniBack.Profiles
{
    public class PrenotazioniProfile : Profile
    {
        public PrenotazioniProfile()
        {
            //Source -> target
            CreateMap<Prenotazione, PrenotazioneReadDto>();
            CreateMap<PrenotazioneCreateDto, Prenotazione>();//.ForMember(dest => dest.NumeroPrenotazione, opt => opt.MapFrom(src => src.NumeroPrenotazione * 1000));
            CreateMap<PrenotazioneUpdateDto, Prenotazione>();
            
        }

    }
}