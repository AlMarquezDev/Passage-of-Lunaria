public class CombatEntity
{
    public string name;
    public int agility;
    public bool isPlayer;
    public object reference;

    public CombatEntity(string name, int agi, bool player, object refData)
    {
        this.name = name;
        agility = agi;
        isPlayer = player;
        reference = refData;
    }

    public void ExecuteAction()
    {
        if (isPlayer)
        {
        }
        else
        {
        }
    }
}
