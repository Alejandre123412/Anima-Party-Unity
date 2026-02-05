namespace AnimaParty.Core
{
    public interface IGameMode
    {
        string ModeName { get; }
        string Description { get; }
        int MaxPlayers { get; }
        int MinPlayers { get; }
        
        void Initialize();
        void StartGame();
        void EndGame();
        void AddPlayer(PlayerInfo player);
        void RemovePlayer(int playerId);
        
        GameState GetCurrentState();
        
        event System.Action OnGameStarted;
        event System.Action OnGameEnded;
        event System.Action<int> OnPlayerEliminated; // playerId
    }
    
    public enum GameState
    {
        Lobby,
        Starting,
        Playing,
        Paused,
        Ended
    }
}