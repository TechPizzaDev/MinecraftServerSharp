using MCServerSharp.Components;

namespace MCServerSharp.Entities.Mobs
{
    public class PlayerComponent : Component<Player>, ITickable
    {
        public Player Player => Entity;

        public PlayerComponent(Player entity) : base(entity)
        {
        }

        public void Tick()
        {
            Player.Components.Tick();
        }
    }
}
