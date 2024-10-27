using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TicTacToeClient
{
    static void Main(string[] args)
    {
        Console.WriteLine("Enter your name: ");
        var playerName = Console.ReadLine();

        var client = new TcpClient("192.168.1.7", 27001);
        try
        {
            while (true)
            {
                if (client.Connected)
                {
                    var networkStream = client.GetStream();
                    Console.WriteLine("Connected to server");
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
                            var response = Encoding.UTF8.GetBytes(choice1!);
                            networkStream.Write(response, 0, response.Length);

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
    }

    static void PlayGame(NetworkStream stream)
    {
        while (true)
        {
            var buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            var boardState = Encoding.UTF8.GetString(buffer, 0, bytesRead).Split(',');

            Console.Clear();
            PrintBoard(boardState);

            Console.WriteLine("Enter your move (1-9): ");
            var move = Console.ReadLine();
            var message = Encoding.UTF8.GetBytes(move);
            stream.Write(message, 0, message.Length);
        }
    }

    static void PrintBoard(string[] board)
    {
        Console.WriteLine($"{board[0]} | {board[1]} | {board[2]}");
        Console.WriteLine("--|---|--");
        Console.WriteLine($"{board[3]} | {board[4]} | {board[5]}");
        Console.WriteLine("--|---|--");
        Console.WriteLine($"{board[6]} | {board[7]} | {board[8]}");
    }
}