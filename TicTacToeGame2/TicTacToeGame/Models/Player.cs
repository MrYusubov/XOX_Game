using System.Net.Sockets;

namespace Server.Models
{
    public class Player
    {
        public string? Name { get; set; }
        public TcpClient? Client { get; set; }
        public string? Role { get; set; }
    }
}
