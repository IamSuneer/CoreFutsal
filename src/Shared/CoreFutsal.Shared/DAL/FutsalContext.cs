using CoreFutsal.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Shared.DAL;

public class FutsalContext : DbContext
{
    public FutsalContext(DbContextOptions<FutsalContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PlayerProfile> Players => Set<PlayerProfile>();
    public DbSet<StaffProfile> Staff => Set<StaffProfile>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamStaff> TeamStaff => Set<TeamStaff>();
    public DbSet<PlayerTeamRequest> PlayerTeamRequests => Set<PlayerTeamRequest>();
    public DbSet<StaffTeamRequest> StaffTeamRequests => Set<StaffTeamRequest>();
    public DbSet<Stadium> Stadiums => Set<Stadium>();
    public DbSet<StadiumSlot> StadiumSlots => Set<StadiumSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchRequest> MatchRequests => Set<MatchRequest>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<MatchResultRequest> MatchResultRequests => Set<MatchResultRequest>();
    public DbSet<PlayerMatchStat> PlayerMatchStats => Set<PlayerMatchStat>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StadiumMatchProposal> StadiumMatchProposals => Set<StadiumMatchProposal>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Enums stored as strings ──────────────────────────────────────────
        mb.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        mb.Entity<PlayerTeamRequest>()
            .Property(r => r.Direction).HasConversion<string>();
        mb.Entity<PlayerTeamRequest>()
            .Property(r => r.Status).HasConversion<string>();

        mb.Entity<StaffTeamRequest>()
            .Property(r => r.Direction).HasConversion<string>();
        mb.Entity<StaffTeamRequest>()
            .Property(r => r.Status).HasConversion<string>();

        mb.Entity<Booking>()
            .Property(b => b.PaymentStatus).HasConversion<string>();

        mb.Entity<Match>()
            .Property(m => m.Status).HasConversion<string>();

        mb.Entity<MatchRequest>()
            .Property(r => r.Status).HasConversion<string>();

        mb.Entity<MatchEvent>()
            .Property(e => e.EventType).HasConversion<string>();

        mb.Entity<MatchResultRequest>()
            .Property(r => r.Status).HasConversion<string>();

        // ── User ─────────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ── PlayerProfile ─────────────────────────────────────────────────────
        mb.Entity<PlayerProfile>(e =>
        {
            e.HasKey(p => p.PlayerId);
            e.HasOne(p => p.User)
             .WithOne(u => u.PlayerProfile)
             .HasForeignKey<PlayerProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StaffProfile ──────────────────────────────────────────────────────
        mb.Entity<StaffProfile>(e =>
        {
            e.HasKey(s => s.StaffId);
            e.HasOne(s => s.User)
             .WithOne(u => u.StaffProfile)
             .HasForeignKey<StaffProfile>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Team ──────────────────────────────────────────────────────────────
        mb.Entity<Team>(e =>
        {
            e.HasKey(t => t.TeamId);
            e.HasOne(t => t.Owner)
             .WithMany()
             .HasForeignKey(t => t.OwnerUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(t => t.TeamName).HasMaxLength(30);
            e.Property(t => t.Abbreviation).HasMaxLength(3);
        });

        // ── TeamMember ────────────────────────────────────────────────────────
        mb.Entity<TeamMember>(e =>
        {
            e.HasKey(tm => tm.TeamMemberId);
            e.HasOne(tm => tm.Team)
             .WithMany(t => t.Members)
             .HasForeignKey(tm => tm.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(tm => tm.Player)
             .WithOne(p => p.ActiveMembership)
             .HasForeignKey<TeamMember>(tm => tm.PlayerId)
             .OnDelete(DeleteBehavior.Restrict);
            // Enforce one active team per player at the DB level
            e.HasIndex(tm => tm.PlayerId)
             .IsUnique()
             .HasFilter("[LeftAt] IS NULL");
        });

        // ── TeamStaff ─────────────────────────────────────────────────────────
        mb.Entity<TeamStaff>(e =>
        {
            e.HasKey(ts => ts.TeamStaffId);
            e.HasOne(ts => ts.Team)
             .WithMany(t => t.Staff)
             .HasForeignKey(ts => ts.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ts => ts.Staff)
             .WithOne(s => s.ActiveAssignment)
             .HasForeignKey<TeamStaff>(ts => ts.StaffId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(ts => ts.StaffId)
             .IsUnique()
             .HasFilter("[LeftAt] IS NULL");
        });

        // ── PlayerTeamRequest ─────────────────────────────────────────────────
        mb.Entity<PlayerTeamRequest>(e =>
        {
            e.HasKey(r => r.RequestId);
            e.HasOne(r => r.Team)
             .WithMany()
             .HasForeignKey(r => r.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Player)
             .WithMany(p => p.TeamRequests)
             .HasForeignKey(r => r.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StaffTeamRequest ──────────────────────────────────────────────────
        mb.Entity<StaffTeamRequest>(e =>
        {
            e.HasKey(r => r.RequestId);
            e.HasOne(r => r.Team)
             .WithMany()
             .HasForeignKey(r => r.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Staff)
             .WithMany(s => s.TeamRequests)
             .HasForeignKey(r => r.StaffId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Stadium ───────────────────────────────────────────────────────────
        mb.Entity<Stadium>(e =>
        {
            e.HasKey(s => s.StadiumId);
            e.HasOne(s => s.Owner)
             .WithMany()
             .HasForeignKey(s => s.OwnerUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.PricePerHour).HasColumnType("decimal(10,2)");
        });

        // ── StadiumSlot ───────────────────────────────────────────────────────
        mb.Entity<StadiumSlot>(e =>
        {
            e.HasKey(s => s.SlotId);
            e.HasOne(s => s.Stadium)
             .WithMany(st => st.Slots)
             .HasForeignKey(s => s.StadiumId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.PriceOverride).HasColumnType("decimal(10,2)");
        });

        // ── Booking ───────────────────────────────────────────────────────────
        mb.Entity<Booking>(e =>
        {
            e.HasKey(b => b.BookingId);
            e.HasOne(b => b.Slot)
             .WithOne(s => s.Booking)
             .HasForeignKey<Booking>(b => b.SlotId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Stadium)
             .WithMany()
             .HasForeignKey(b => b.StadiumId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.BookedByTeam)
             .WithMany()
             .HasForeignKey(b => b.BookedByTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)");
        });

        // ── Match ─────────────────────────────────────────────────────────────
        mb.Entity<Match>(e =>
        {
            e.HasKey(m => m.MatchId);
            e.HasOne(m => m.Booking)
             .WithOne(b => b.Match)
             .HasForeignKey<Match>(m => m.BookingId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Stadium)
             .WithMany()
             .HasForeignKey(m => m.StadiumId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.HomeTeam)
             .WithMany()
             .HasForeignKey(m => m.HomeTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AwayTeam)
             .WithMany()
             .HasForeignKey(m => m.AwayTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.InitiatedBy)
             .WithMany()
             .HasForeignKey(m => m.InitiatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchRequest ──────────────────────────────────────────────────────
        mb.Entity<MatchRequest>(e =>
        {
            e.HasKey(r => r.MatchRequestId);
            e.HasOne(r => r.RequestingTeam)
             .WithMany()
             .HasForeignKey(r => r.RequestingTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.OpponentTeam)
             .WithMany()
             .HasForeignKey(r => r.OpponentTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Stadium)
             .WithMany()
             .HasForeignKey(r => r.StadiumId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Slot)
             .WithMany()
             .HasForeignKey(r => r.SlotId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchEvent ────────────────────────────────────────────────────────
        mb.Entity<MatchEvent>(e =>
        {
            e.HasKey(ev => ev.EventId);
            e.HasOne(ev => ev.Match)
             .WithMany(m => m.Events)
             .HasForeignKey(ev => ev.MatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ev => ev.Team)
             .WithMany()
             .HasForeignKey(ev => ev.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.Player)
             .WithMany()
             .HasForeignKey(ev => ev.PlayerId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
            e.HasOne(ev => ev.SubstitutedForPlayer)
             .WithMany()
             .HasForeignKey(ev => ev.SubstitutedForPlayerId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
            e.HasOne(ev => ev.RecordedBy)
             .WithMany()
             .HasForeignKey(ev => ev.RecordedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MatchResultRequest ────────────────────────────────────────────────
        mb.Entity<MatchResultRequest>(e =>
        {
            e.HasKey(r => r.ResultRequestId);
            e.HasOne(r => r.Match)
             .WithMany(m => m.ResultRequests)
             .HasForeignKey(r => r.MatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.SubmittedByTeam)
             .WithMany()
             .HasForeignKey(r => r.SubmittedByTeamId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.SubmittedByUser)
             .WithMany()
             .HasForeignKey(r => r.SubmittedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PlayerMatchStat ───────────────────────────────────────────────────
        mb.Entity<PlayerMatchStat>(e =>
        {
            e.HasKey(s => s.StatId);
            e.HasIndex(s => new { s.MatchId, s.PlayerId }).IsUnique();
            e.HasOne(s => s.Match)
             .WithMany(m => m.PlayerStats)
             .HasForeignKey(s => s.MatchId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Player)
             .WithMany(p => p.MatchStats)
             .HasForeignKey(s => s.PlayerId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Team)
             .WithMany()
             .HasForeignKey(s => s.TeamId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RefreshToken ──────────────────────────────────────────────────────
        mb.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Token).IsUnique();
            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── StadiumMatchProposal ──────────────────────────────────────────────
        mb.Entity<StadiumMatchProposal>(e =>
        {
            e.HasKey(p => p.ProposalId);
            e.Property(p => p.HomeTeamStatus).HasConversion<string>();
            e.Property(p => p.AwayTeamStatus).HasConversion<string>();
            e.HasOne(p => p.Stadium).WithMany().HasForeignKey(p => p.StadiumId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Slot).WithMany().HasForeignKey(p => p.SlotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.HomeTeam).WithMany().HasForeignKey(p => p.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.AwayTeam).WithMany().HasForeignKey(p => p.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
