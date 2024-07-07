namespace Quiz.Api.Models
{
    public class LeaderBoard
    {
        public List<User> Users { get; set; }
        public long LastModifyTimestamp { get; set; }        
    }
}
