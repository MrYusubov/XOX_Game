using System.Net.Sockets;
using System.Net;
using System.Text;

var ip = IPAddress.Parse("192.168.1.7");
var port = 27001;

var ep = new IPEndPoint(ip, port);
var client = new TcpClient();

try
{
    client.Connect(ep);
    if (client.Connected)
    {
        var networkStream = client.GetStream();
        Console.WriteLine("Connected to server");

        Console.WriteLine("Enter your name: ");
        var playerName = Console.ReadLine();

        var nameMessage = Encoding.UTF8.GetBytes(playerName!);
        networkStream.Write(nameMessage, 0, nameMessage.Length);

        var roleBuffer = new byte[1024];
        var roleBytesRead = networkStream.Read(roleBuffer, 0, roleBuffer.Length);
        var roleMessage = Encoding.UTF8.GetString(roleBuffer, 0, roleBytesRead).Trim();

        if (roleMessage.StartsWith("Role:"))
        {
            var playerRole = roleMessage.Split(':')[1].Trim();
            Console.WriteLine($"Your role is {playerRole}");

            string[] board = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            while (true)
            {
                var buffer = new byte[1024];
                var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                var gameMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (gameMessage.StartsWith("Board:"))
                {
                    board = gameMessage.Split(':')[1].Split(',');
                    Console.Clear();
                    PrintBoard(board);

                    Console.WriteLine("Your move, choose (1-9): ");
                    var move = Console.ReadLine();

                    if (int.TryParse(move, out int pos) && pos >= 1 && pos <= 9 && board[pos - 1] != "X" && board[pos - 1] != "O")
                    {
                        board[pos - 1] = playerRole;

                        var gameMessageToSend = $"Move:{string.Join(",", board)}";
                        var moveMessage = Encoding.UTF8.GetBytes(gameMessageToSend);
                        networkStream.Write(moveMessage, 0, moveMessage.Length);
                    }
                }
                else if (gameMessage == "Not your turn")
                {
                    Console.WriteLine("It's not your turn.");
                }
                else if (gameMessage.Contains("won"))
                {
                    Console.WriteLine(gameMessage);
                    break;
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

void PrintBoard(string[] board)
{
    Console.WriteLine($"{board[0]} | {board[1]} | {board[2]}");
    Console.WriteLine("--|---|--");
    Console.WriteLine($"{board[3]} | {board[4]} | {board[5]}");
    Console.WriteLine("--|---|--");
    Console.WriteLine($"{board[6]} | {board[7]} | {board[8]}");
}
