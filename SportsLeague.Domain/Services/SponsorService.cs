using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;
using System.Text.RegularExpressions;

namespace SportsLeague.Domain.Services
{
    public class SponsorService : ISponsorService
    {
        private readonly ISponsorRepository _sponsorRepository;
        private readonly ITournamentSponsorRepository _tournamentSponsorRepository;
        private readonly IGenericRepository<Tournament> _tournamentRepository;
        private readonly ILogger<SponsorService> _logger;

        public SponsorService(
            ISponsorRepository sponsorRepository,
            ITournamentSponsorRepository tournamentSponsorRepository,
            IGenericRepository<Tournament> tournamentRepository,
            ILogger<SponsorService> logger)
        {
            _sponsorRepository = sponsorRepository;
            _tournamentSponsorRepository = tournamentSponsorRepository;
            _tournamentRepository = tournamentRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Sponsor>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all sponsors");
            return await _sponsorRepository.GetAllAsync();
        }

        public async Task<Sponsor?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving sponsor with ID: {SponsorId}", id);
            var sponsor = await _sponsorRepository.GetByIdAsync(id);
            if (sponsor == null)
                _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
            return sponsor;
        }

        public async Task<Sponsor> CreateAsync(Sponsor sponsor)
        {
            // Validar nombre duplicado
            if (await _sponsorRepository.ExistsByNameAsync(sponsor.Name))
            {
                _logger.LogWarning("Sponsor with name '{Name}' already exists", sponsor.Name);
                throw new InvalidOperationException(
                    $"Ya existe un sponsor con el nombre '{sponsor.Name}'");
            }

            // Validar formato de email
            if (!IsValidEmail(sponsor.ContactEmail))
            {
                throw new InvalidOperationException(
                    $"El email '{sponsor.ContactEmail}' no tiene un formato válido");
            }

            _logger.LogInformation("Creating sponsor: {Name}", sponsor.Name);
            return await _sponsorRepository.CreateAsync(sponsor);
        }

        public async Task UpdateAsync(int id, Sponsor sponsor)
        {
            var existing = await _sponsorRepository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Sponsor with ID {SponsorId} not found for update", id);
                throw new KeyNotFoundException($"No se encontró el sponsor con ID {id}");
            }

            // Validar nombre duplicado si cambió
            if (existing.Name != sponsor.Name && await _sponsorRepository.ExistsByNameAsync(sponsor.Name))
                throw new InvalidOperationException(
                    $"Ya existe un sponsor con el nombre '{sponsor.Name}'");

            // Validar email
            if (!IsValidEmail(sponsor.ContactEmail))
                throw new InvalidOperationException(
                    $"El email '{sponsor.ContactEmail}' no tiene un formato válido");

            existing.Name = sponsor.Name;
            existing.ContactEmail = sponsor.ContactEmail;
            existing.Phone = sponsor.Phone;
            existing.WebsiteUrl = sponsor.WebsiteUrl;
            existing.Category = sponsor.Category;

            _logger.LogInformation("Updating sponsor with ID: {SponsorId}", id);
            await _sponsorRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            var exists = await _sponsorRepository.ExistsAsync(id);
            if (!exists)
            {
                _logger.LogWarning("Sponsor with ID {SponsorId} not found for deletion", id);
                throw new KeyNotFoundException($"No se encontró el sponsor con ID {id}");
            }

            _logger.LogInformation("Deleting sponsor with ID: {SponsorId}", id);
            await _sponsorRepository.DeleteAsync(id);
        }

        public async Task<TournamentSponsor> AddToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount)
        {
            // Validar que el sponsor existe
            var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
            if (sponsor == null)
                throw new KeyNotFoundException($"No se encontró el sponsor con ID {sponsorId}");

            // Validar que el torneo existe
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
            if (tournament == null)
                throw new KeyNotFoundException($"No se encontró el torneo con ID {tournamentId}");

            // Validar que no esté duplicado
            var existing = await _tournamentSponsorRepository
                .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
            if (existing != null)
                throw new InvalidOperationException(
                    $"El sponsor '{sponsor.Name}' ya está vinculado al torneo '{tournament.Name}'");

            // Validar ContractAmount > 0
            if (contractAmount <= 0)
                throw new InvalidOperationException(
                    "El monto del contrato debe ser mayor a 0");

            var tournamentSponsor = new TournamentSponsor
            {
                SponsorId = sponsorId,
                TournamentId = tournamentId,
                ContractAmount = contractAmount,
                JoinedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Linking sponsor {SponsorId} to tournament {TournamentId}", sponsorId, tournamentId);
            return await _tournamentSponsorRepository.CreateAsync(tournamentSponsor);
        }

        public async Task<IEnumerable<TournamentSponsor>> GetTournamentsBySponsorsAsync(int sponsorId)
        {
            var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
            if (sponsor == null)
                throw new KeyNotFoundException($"No se encontró el sponsor con ID {sponsorId}");

            return await _tournamentSponsorRepository.GetBySponsorIdAsync(sponsorId);
        }

        public async Task RemoveFromTournamentAsync(int sponsorId, int tournamentId)
        {
            var link = await _tournamentSponsorRepository
                .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
            if (link == null)
                throw new KeyNotFoundException(
                    $"No se encontró la vinculación entre el sponsor {sponsorId} y el torneo {tournamentId}");

            _logger.LogInformation("Removing sponsor {SponsorId} from tournament {TournamentId}", sponsorId, tournamentId);
            await _tournamentSponsorRepository.DeleteAsync(link.Id);
        }

        // Método privado para validar email
        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }
    }
}