using Quiz.Api.Models;

namespace Quiz.Api.Repositories
{
    public class GameScoreRepository: IGameScoreRepository
    {
        public GameScoreRepository() { }

        public Task SaveAsync(GameScore gameScore)
        {
            return Task.CompletedTask;
        }
    }
}
