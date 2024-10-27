using Server.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;

var ip = IPAddress.Parse("192.168.1.7");
var port = 27001;
var ep = new IPEndPoint(ip, port);
var listener = new TcpListener(ep);
var onlinePlayers = new List<Player>();
string[] board = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
string currentPlayer = "X";

string[] availableRoles = { "X", "O" };

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

        var playerRole = availableRoles[onlinePlayers.Count % 2];

        onlinePlayers.Add(new Player { Name = playerName, Client = client, Role = playerRole });
        Console.WriteLine($"{playerName} joined as {playerRole}");

        var roleMessage = Encoding.UTF8.GetBytes($"Role:{playerRole}");
        networkStream.Write(roleMessage, 0, roleMessage.Length);

        if (onlinePlayers.Count == 2)
        {
            Console.WriteLine("Starting game...");
            SendBoardToAllPlayers();
        }

        while (true)
        {
            try
            {
                var buffer = new byte[1024];
                var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (message.StartsWith("Move:"))
                {
                    var currentPlayerName = onlinePlayers.First(p => p.Client == client).Role;
                    if (currentPlayerName != currentPlayer)
                    {
                        networkStream.Write(Encoding.UTF8.GetBytes("Not your turn"), 0, 13);
                        continue;
                    }

                    var move = message.Split(':')[1].Split(',');
                    board = move;

                    if (CheckWin(board, currentPlayerName))
                    {
                        SendMessageToAllPlayers($"{currentPlayerName} won the game!");
                        break;
                    }

                    currentPlayer = currentPlayer == "X" ? "O" : "X";
                    SendBoardToAllPlayers();
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

void SendBoardToAllPlayers()
{
    foreach (var player in onlinePlayers)
    {
        var playerStream = player.Client!.GetStream();
        var gameMessage = $"Board:{string.Join(",", board)}";
        var boardMessage = Encoding.UTF8.GetBytes(gameMessage);
        playerStream.Write(boardMessage, 0, boardMessage.Length);
    }
}

void SendMessageToAllPlayers(string message)
{
    foreach (var player in onlinePlayers)
    {
        var playerStream = player.Client!.GetStream();
        var gameMessage = Encoding.UTF8.GetBytes(message);
        playerStream.Write(gameMessage, 0, gameMessage.Length);
    }
}

bool CheckWin(string[] board, string player)
{
    string[] winningCombos = {
        "012", "345", "678",
        "036", "147", "258",
        "048", "246"
    };

    foreach (var combo in winningCombos)
    {
        if (board[combo[0] - '0'] == player && board[combo[1] - '0'] == player && board[combo[2] - '0'] == player)
        {
            return true;
        }
    }
    return false;
}

