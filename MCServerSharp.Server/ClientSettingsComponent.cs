using MCServerSharp.Components;
using MCServerSharp.Entity.Mob;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Server
{
    public class ClientSettingsComponent : Component<Player>
    {
        public ClientSettings Settings { get; set; }
        public bool SettingsChanged { get; set; }

        public ClientSettingsComponent(Player entity) : base(entity)
        {
        }

        public override void Tick()
        {

        }
    }
}
