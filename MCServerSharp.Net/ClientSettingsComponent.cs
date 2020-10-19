using MCServerSharp.Components;
using MCServerSharp.Entities.Mobs;
using MCServerSharp.Net.Packets;

namespace MCServerSharp.Net
{
    public class ClientSettingsComponent : Component<Player>, ITickable
    {
        public ClientSettings Settings { get; set; }
        public bool SettingsChanged { get; set; }

        public ClientSettingsComponent(Player entity) : base(entity)
        {
        }

        public void Tick()
        {
            if (SettingsChanged)
            {
                SettingsChanged = false;

                Entity.SentChunks.Clear();

                Entity.UpdateChunksToSend(Settings.ViewDistance);
            }
        }
    }
}
