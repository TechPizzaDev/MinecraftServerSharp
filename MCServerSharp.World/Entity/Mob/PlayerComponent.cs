using MCServerSharp.Components;

namespace MCServerSharp.Entity.Mob
{
    public class PlayerComponent : Component<Player>
    {
        public PlayerComponent(Player entity) : base(entity)
        {
        }

        public override void Tick()
        {

        }
    }
}
