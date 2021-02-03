
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
}
