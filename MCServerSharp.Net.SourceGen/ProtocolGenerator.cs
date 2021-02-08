using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCServerSharp.Net.SourceGen
{
    [Generator]
    public class ProtocolGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
#endif 
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                string fileName = Path.GetFileName(file.Path);
                if (fileName != "protocol_mapping.json")
                    continue;

                using (Stream fs = File.OpenRead(file.Path))
                {
                    using var sr = new StreamReader(fs);
                    using var jr = new JsonTextReader(sr);

                    JObject doc = JObject.Load(jr);

                    context.ReportDiagnostic(Diagnostic.Create("69", "96", "hello dude", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 1));
                }
                break;
            }

            context.AddSource("ProtocolState", ProtocolStateCode);
            context.AddSource("MinecraftPacketAttribute", MinecraftPacketAttributeCode);
            context.AddSource("ServerboundPacketAttribute", ServerboundPacketAttributeCode);
            context.AddSource("ClientboundPacketAttribute", ClientboundPacketAttributeCode);
        }

        private const string ProtocolStateCode = @"
namespace MCServerSharp.Net.Packets
{
    public enum ProtocolState
    {
        Undefined = 0,
        
        Status = 1,
        Login = 2,

        Handshaking,
        Play,
        
        // TODO: remove these and implement something else
        Closing,
        Disconnected
    }
}";

        private const string MinecraftPacketAttributeCode = @"using System;

namespace MCServerSharp.Net.Packets
{
    /// <summary>
    /// Defines metadata for a Minecraft packet struct.
    /// </summary>
    public abstract class MinecraftPacketAttribute : Attribute
    {
        public int PacketId { get; }

        public PacketStructAttribute(int packetId)
        {
            PacketId = packetId;
        }
    }
}";

        private const string ServerboundPacketAttributeCode = @"using System;

namespace MCServerSharp.Net.Packets
{
    /// <summary>
    /// Defines metadata for a serverbound packet struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class ServerboundPacketAttribute : MinecraftPacketAttribute
    {
        public PacketStructAttribute(ServerPacketId packetId) : this((int)packetId)
        {
        }
    }
}";

        private const string ClientboundPacketAttributeCode = @"using System;

namespace MCServerSharp.Net.Packets
{
    /// <summary>
    /// Defines metadata for a clientbound packet struct.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class ClientboundPacketAttribute : MinecraftPacketAttribute
    {
        public PacketStructAttribute(ClientPacketId packetId) : this((int)packetId)
        {
        }
    }
}";

    }
}
