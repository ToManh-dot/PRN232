using System;

namespace MarathonManager.API.DTOs
{
   
    public class RunnerDashboardDto
    {
        public RunnerStatisticsDto Statistics { get; set; } = new();
        public List<AvailableRaceDto> AvailableRaces { get; set; } = new();
        public List<MyRegistrationDto> MyRegistrations { get; set; } = new();
        public List<MyResultDto> MyResults { get; set; } = new();
    }

  
    public class RunnerStatisticsDto
    {
        public int TotalRegistrations { get; set; }
        public int CompletedRaces { get; set; }
        public int UpcomingRaces { get; set; }
        public int PendingRegistrations { get; set; }
    }


    public class AvailableRaceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;

        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string? OrganizerEmail { get; set; }

        public List<RaceDistanceSummaryDto> Distances { get; set; } = new();

        public bool IsAlreadyRegistered { get; set; }

        public int TotalParticipants { get; set; }
        public int AvailableSlots { get; set; }
    }

  
    public class RaceDistanceSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsFull { get; set; }
    }

   
    public class MyRegistrationDto
    {
        public int Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? BibNumber { get; set; }

        public int RaceId { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? RaceImageUrl { get; set; }

        public int RaceDistanceId { get; set; }
        public string DistanceName { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public DateTime StartTime { get; set; }

        public bool CanCancel { get; set; }
        public bool HasResult { get; set; }
        public string DisplayStatus { get; set; } = string.Empty;
    }

   
    public class RegistrationDetailDto
    {
        public int Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? BibNumber { get; set; }

        public RaceDetailsDto Race { get; set; } = new();

        public RaceDistanceDetailDto RaceDistance { get; set; } = new();

        public RunnerInfoDto Runner { get; set; } = new();

        public MyResultDto? Result { get; set; }
    }

   
    public class MyResultDto
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public int? OverallRank { get; set; }
        public int? GenderRank { get; set; }
        public int? AgeCategoryRank { get; set; }
        public string Status { get; set; } = string.Empty;

        public int RaceId { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }

        public string DistanceName { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }

        public string? FormattedTime { get; set; }
        public string? AveragePace { get; set; } 
        public bool IsTopThree => OverallRank.HasValue && OverallRank <= 3;
        public string MedalIcon => OverallRank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => string.Empty
        };
    }

    
    public class ResultDetailDto
    {
        public MyResultDto MyResult { get; set; } = new();
        public List<LeaderboardEntryDto> Leaderboard { get; set; } = new();
        public RaceDetailsDto Race { get; set; } = new();
    }

   
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string RunnerName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public string? FormattedTime { get; set; }
        public bool IsCurrentUser { get; set; }
    }

   
    public class RaceDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

       
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string? OrganizerEmail { get; set; }
        public string? OrganizerPhone { get; set; }

        public List<RaceDistanceDetailDto> Distances { get; set; } = new();

        public List<BlogPostSummaryDto> BlogPosts { get; set; } = new();
    }


    public class RaceDistanceDetailDto
    {
        public int Id { get; set; }
        public int RaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
        public int CurrentParticipants { get; set; }
        public int AvailableSlots => MaxParticipants - CurrentParticipants;
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public decimal PercentageFilled => MaxParticipants > 0
            ? (decimal)CurrentParticipants / MaxParticipants * 100
            : 0;
    }

    
    public class RunnerInfoDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
    }

    public class BlogPostSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FeaturedImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

  
    public class RegisterForRaceRequest
    {
        public int RaceId { get; set; }
        public int RaceDistanceId { get; set; }
    }

  
    public class CancelRegistrationRequest
    {
        public int RegistrationId { get; set; }
        public string? Reason { get; set; }
    }

  
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

  
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
