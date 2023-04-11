public class FNet_Player
{
    private Net_Player _player;

    private int _id;

    public Net_Player Player { get => _player; set => _player = value; }
    public int Id { get => _id; set => _id = value; }

    public FNet_Player(Net_Player pPlayer, int pId)
    {
        _player = pPlayer;
        _id = pId;
    }
}
