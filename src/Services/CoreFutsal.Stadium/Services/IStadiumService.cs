using CoreFutsal.Shared.Models;
using CoreFutsal.Stadium.DTOs;

namespace CoreFutsal.Stadium.Services;

public interface IStadiumService
{
    Task<PagedResult<StadiumDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<StadiumDto> GetByIdAsync(Guid stadiumId, CancellationToken ct = default);
    Task<StadiumDto> CreateAsync(Guid ownerUserId, CreateStadiumDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid ownerUserId, Guid stadiumId, UpdateStadiumDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerUserId, Guid stadiumId, CancellationToken ct = default);

    Task<IEnumerable<StadiumSlotDto>> GetSlotsAsync(Guid stadiumId, DateTime? date, CancellationToken ct = default);
    Task<StadiumSlotDto> AddSlotAsync(Guid ownerUserId, Guid stadiumId, CreateSlotDto dto, CancellationToken ct = default);
    Task DeleteSlotAsync(Guid ownerUserId, Guid stadiumId, Guid slotId, CancellationToken ct = default);

    Task<BookingDto> BookSlotAsync(Guid teamOwnerUserId, Guid stadiumId, BookSlotDto dto, CancellationToken ct = default);
    Task ConfirmPaymentAsync(Guid stadiumOwnerUserId, Guid bookingId, CancellationToken ct = default);
    Task CancelBookingAsync(Guid userId, Guid bookingId, CancellationToken ct = default);
    Task<IEnumerable<BookingDto>> GetBookingsForStadiumAsync(Guid ownerUserId, Guid stadiumId, CancellationToken ct = default);
    Task<IEnumerable<BookingDto>> GetBookingsForTeamAsync(Guid teamOwnerUserId, CancellationToken ct = default);

    // Stadium-initiated match proposals
    Task<StadiumMatchProposalDto> ProposeMatchAsync(Guid stadiumOwnerUserId, Guid stadiumId, ProposeMatchDto dto, CancellationToken ct = default);
    Task RespondToProposalAsync(Guid teamOwnerUserId, Guid proposalId, RespondToProposalDto dto, CancellationToken ct = default);
    Task<IEnumerable<StadiumMatchProposalDto>> GetProposalsForStadiumAsync(Guid stadiumOwnerUserId, Guid stadiumId, CancellationToken ct = default);
    Task<IEnumerable<StadiumMatchProposalDto>> GetProposalsForTeamAsync(Guid teamOwnerUserId, CancellationToken ct = default);
}
