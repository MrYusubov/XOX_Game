using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Models;

class TicTacToeServer
{
    static List<Player> onlinePlayers = new List<Player>();
    static TcpListener listener;

    static void Main(string[] args)
    {
        var ip = IPAddress.Parse("192.168.1.7");
        var port = 27001;
        listener = new TcpListener(ip, port);
        listener.Start();

        Console.WriteLine("Server started...");

        _ = Task.Run(() => AcceptClient());

        while (true)
        {
            var input = Console.ReadLine();
            if (input!.ToLower() == "exit")
            {
                listener.Stop();
                break;
            }
        }
    }

    static async Task AcceptClient()
    {
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];

        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var playerName = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        var player = new Player { Name = playerName, Client = client };
        onlinePlayers.Add(player);

        Console.WriteLine($"New player connected: {playerName}");

        var playerList = string.Join(",", onlinePlayers.Select(p => p.Name));
        var onlineListMessage = Encoding.UTF8.GetBytes(playerList);
        await stream.WriteAsync(onlineListMessage, 0, onlineListMessage.Length);

        while (true)
        {
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                break;

            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (message.StartsWith("Invite:"))
            {
                var targetPlayerName = message.Split(':')[1].Trim();
                var targetPlayer = onlinePlayers.FirstOrDefault(p => p.Name == targetPlayerName);
                if (targetPlayer != null)
                {
                    var targetStream = targetPlayer.Client.GetStream();
                    var inviteResponse = "You have been invited to the game. Yes/No?";
                    var inviteResponseMessage = Encoding.UTF8.GetBytes(inviteResponse);
                    await targetStream.WriteAsync(inviteResponseMessage, 0, inviteResponseMessage.Length);

                    var targetBuffer = new byte[1024];
                    var targetBytesRead = await targetStream.ReadAsync(targetBuffer, 0, targetBuffer.Length);
                    var response = Encoding.UTF8.GetString(targetBuffer, 0, targetBytesRead).Trim();

                    if (response.ToLower() == "yes")
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("yes"), 0, 3);
                        await targetStream.WriteAsync(Encoding.UTF8.GetBytes("yes"), 0, 3);

                        StartGame(player, targetPlayer);
                        break;
                    }
                    else
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("no"), 0, 2);
                    }
                }
                else
                {
                    await stream.WriteAsync(Encoding.UTF8.GetBytes("Player not found"), 0, 15);
                }
            }
        }

        client.Close();
    }

    static void StartGame(Player player1, Player player2)
    {
        string[] board = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        string currentPlayer = "X";

        TcpClient[] clients = { player1.Client, player2.Client };
        NetworkStream[] streams = clients.Select(c => c.GetStream()).ToArray();

        while (true)
        {
            foreach (var stream in streams)
            {
                var boardState = string.Join(",", board);
                var message = Encoding.UTF8.GetBytes(boardState);
                stream.Write(message, 0, message.Length);
            }

            var currentStream = currentPlayer == "X" ? streams[0] : streams[1];
            var buffer = new byte[1024];
            int bytesRead = currentStream.Read(buffer, 0, buffer.Length);
            var move = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            int pos = int.Parse(move);

            if (board[pos - 1] != "X" && board[pos - 1] != "O")
            {
                board[pos - 1] = currentPlayer;
                if (CheckWin(board, currentPlayer))
                {
                    foreach (var stream in streams)
                    {
                        var message = Encoding.UTF8.GetBytes($"{currentPlayer} Won!");
                        stream.Write(message, 0, message.Length);
                    }
                    break;
                }
                currentPlayer = currentPlayer == "X" ? "O" : "X";
            }
        }
    }

    static bool CheckWin(string[] board, string player)
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
}