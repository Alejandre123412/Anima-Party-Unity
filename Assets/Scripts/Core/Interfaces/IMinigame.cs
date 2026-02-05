using System.Collections.Generic;

namespace AnimaParty.Core
{
    public interface IMinigame
    {
        string MinigameName { get; }
        string Description { get; }
        int MaxPlayers { get; }
        int MinPlayers { get; }
        float Duration { get; }
        
        void Initialize(List<PlayerController> players);
        void StartMinigame();
        void EndMinigame();
        Dictionary<int, int> GetResults();
        
        event System.Action<Dictionary<int, int>> OnMinigameCompleted;
    }
}