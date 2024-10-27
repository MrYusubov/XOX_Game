using System.Net.Sockets;
using System.Net;
using System.Text;

var ip = IPAddress.Parse("192.168.1.7");
var port = 27001;

var ep = new IPEndPoint(ip, port);
var client = new TcpClient();
try
{
    while (true)
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

            while (true)
            {
                var buffer = new byte[1024];
                var bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                var onlinePlayers = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Invite:
                Console.WriteLine("Would you like to invite other player?(y/n)");
                var choice = Console.ReadLine();
                if (choice!.ToLower().Trim() == "y")
                {
                    Console.WriteLine("Online players: " + onlinePlayers);
                    Console.WriteLine("Who do you want to invite?");
                    var selectedPlayer = Console.ReadLine();
                    var message = $"Invite:{selectedPlayer}";
                    var inviteMessage = Encoding.UTF8.GetBytes(message);
                    networkStream.Write(inviteMessage, 0, inviteMessage.Length);
                    while (true)
                    {
                        buffer = new byte[1024];
                        bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        if (response!.ToLower().Trim() == "yes")
                        {
                            Console.WriteLine("Game Starting...");
                            PlayGame(networkStream);
                            break;
                        }
                        else if (response.ToLower().Trim() == "no")
                        {
                            Console.WriteLine("Invite rejected.");
                        }
                        else
                        {
                            Console.WriteLine("Something went Wrong");
                        }
                    }

                }
                else if (choice!.ToLower().Trim() == "n")
                {
                    Console.WriteLine("Cancelled.");
                    buffer = new byte[1024];
                    bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    var inviteResponseMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine(inviteResponseMessage);
                    var choice1 = Console.ReadLine();
                    var response=Encoding.UTF8.GetBytes(choice1!);
                    networkStream.Write(response,0, response.Length);

                    if (choice1!.ToLower().Trim() == "yes")
                    {
                        Console.WriteLine("Game Starting...");
                        PlayGame(networkStream);
                        break;
                    }
                    else if (choice1.ToLower().Trim() == "no")
                    {
                        Console.WriteLine("Invite rejected.");
                    }
                    else
                    {
                        Console.WriteLine("Something went Wrong"); 
                    }
                }
                else
                {
                    Console.WriteLine("Wrong operation. Try again!");
                    goto Invite;
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

void PlayGame(NetworkStream networkStream)
{
    string[] board = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    string currentPlayer = "X";

    while (true)
    {
        Console.Clear();
        PrintBoard(board);
        Console.WriteLine($"{currentPlayer} choose (1-9): ");
        var move = Console.ReadLine();
        if (int.TryParse(move, out int pos) && pos >= 1 && pos <= 9 && board[pos - 1] != "X" && board[pos - 1] != "O")
        {
            board[pos - 1] = currentPlayer;

            var gameMessage = $"Move:{string.Join(",", board)}";
            var moveMessage = Encoding.UTF8.GetBytes(gameMessage);
            networkStream.Write(moveMessage, 0, moveMessage.Length);

            if (CheckWin(board, currentPlayer))
            {
                Console.WriteLine($"{currentPlayer} Won!");
                break;
            }
            currentPlayer = currentPlayer == "X" ? "O" : "X";
        }
    }
}

void PrintBoard(string[] board)
{
    Console.WriteLine($"{board[0]} | {board[1]} | {board[2]}");
    Console.WriteLine("--|---|--");
    Console.WriteLine($"{board[3]} | {board[4]} | {board[5]}");
    Console.WriteLine("--|---|--");
    Console.WriteLine($"{board[6]} | {board[7]} | {board[8]}");
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