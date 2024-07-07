using Quiz.Api.Models;

namespace Quiz.Api.Repositories
{
    public interface IGameScoreRepository
    {
        Task SaveAsync(GameScore gameScore);
    }
}
