using System;
using System.ComponentModel.DataAnnotations;
using MarathonManager.Web.Interfaces; 

namespace MarathonManager.Web.DTOs
{
    public class RaceDistanceCreateDto : IDistanceFormModel 
    {
        public int RaceId { get; set; }

       
        public string Name { get; set; }
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
    }
}