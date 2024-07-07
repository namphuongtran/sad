namespace Quiz.Api.Models
{
    public class GameScore
    {
        public int UserId { get; set; }
        public double Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
