using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Models;

var ip = IPAddress.Parse("192.168.1.7");
var port = 27001;
var ep = new IPEndPoint(ip, port);
var listener = new TcpListener(ep);
var onlinePlayers = new List<Player>();

listener.Start();
Console.WriteLine("Server started...");

while (true)
{
    var client = listener.AcceptTcpClient();
    _ = Task.Run(() =>
    {
        var networkStream = client.GetStream();
        var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
        Console.WriteLine($"New player connected: {remoteEp}");

        var nameBuffer = new byte[1024];
        var nameBytesRead = networkStream.Read(nameBuffer, 0, nameBuffer.Length);
        var playerName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytesRead).Trim();

        onlinePlayers.Add(new Player { Name = playerName, Client = client });

        var playerList = string.Join(",", onlinePlayers.Select(p => p.Name));
        var onlineListMessage = Encoding.UTF8.GetBytes(playerList);
        networkStream.Write(onlineListMessage, 0, onlineListMessage.Length);

        while (true)
        {
            try
            {
                var buffer = new byte[1024];
                var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var inviteMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                if (inviteMessage.StartsWith("Invite:"))
                {
                    var targetPlayerName = inviteMessage.Split(':')[1].Trim();
                    var targetPlayer = onlinePlayers.FirstOrDefault(p => p.Name == targetPlayerName);

                    if (targetPlayer != null)
                    {
                        var targetStream = targetPlayer.Client.GetStream();
                        var inviteResponse = "You have been invited to the game. Yes/No?";
                        var inviteResponseMessage = Encoding.UTF8.GetBytes(inviteResponse);
                        targetStream.Write(inviteResponseMessage, 0, inviteResponseMessage.Length);

                        var targetBuffer = new byte[1024];
                        var targetBytesRead = targetStream.Read(targetBuffer, 0, targetBuffer.Length);
                        var response = Encoding.UTF8.GetString(targetBuffer, 0, targetBytesRead).Trim();

                        if (response.ToLower() == "yes")
                        {
                            networkStream.Write(Encoding.UTF8.GetBytes("yes"), 0, 3);
                            targetStream.Write(Encoding.UTF8.GetBytes("yes"), 0, 3);

                            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                targetStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        else
                        {
                            networkStream.Write(Encoding.UTF8.GetBytes("no"), 0, 2);
                        }
                    }
                    else
                    {
                        networkStream.Write(Encoding.UTF8.GetBytes("Player not found"), 0, 15);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }
    });
}
