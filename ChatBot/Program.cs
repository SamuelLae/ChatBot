using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Threading;

public class ErrorMessage : Exception{
    string message = "Faulty input";
    public override string ToString()
    {
        return message;
    }
}

public class Bot{

    ClientWebSocket client;

    public Bot(){
    client = new();
  }
/// <summary>
/// Connects to the dedicated ws server
/// </summary>
/// <returns></returns>
  public async Task Connect(){
    string ServerUrl = "wss://echo.websocket.in";
    client = new ClientWebSocket();
    await client.ConnectAsync(new Uri(ServerUrl), default);
    Console.ReadKey();
  }
  
  string userOuthToken;
  string userAccountName;
  
/// <summary>
/// Takes the users name and "password" from a seperate file
/// </summary>
/// <returns></returns>
      public async Task GetOuthToken(){
      using (StreamReader w = new("UserData.txt")){
      string[] UserDataString = w.ReadToEnd().Split('\n');
      userOuthToken = UserDataString[1];
      userAccountName = UserDataString[0];
      }

  }
/// <summary>
/// Allows users to send messages, which also logs them
/// </summary>
/// <returns></returns>
      async Task Send(){
      Console.WriteLine("Send message: ");
      string message = Console.ReadLine();
      while (message == ""){
        Console.WriteLine("Send message: ");
      }
      await Log.handleMessage(message, "Samuel", true, '#');
      byte[] buffer = Encoding.UTF8.GetBytes(message);
      await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
/// <summary>
/// Allows the server to revive the message sent to the server
/// </summary>
/// <returns></returns>
      async Task<string> Recive(){
        byte[] receiveBuffer = new byte[1024];
      WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
      if (result.MessageType == WebSocketMessageType.Text){
        string recivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
        await Log.handleMessage(recivedMessage, "Echo", true, '#');
        Console.WriteLine(recivedMessage);
        await Send();
        return recivedMessage;
      }
      return "";
      
    }
/// <summary>
/// Sends the verification information to the server to see if you can pass through
/// </summary>
/// <returns></returns>
/// <exception cref="ErrorMessage"></exception>
    public async Task SendVerification(){
      await Log.handleMessage(userOuthToken, userAccountName, true, '#');
      byte[] buffer = Encoding.UTF8.GetBytes(userOuthToken);
      await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
      buffer = Encoding.UTF8.GetBytes(userAccountName);
      await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
      await Recive();
      if (await Recive() == ":tmi.twitch.tv NOTICE * :Login authentication failed"){
        throw new ErrorMessage();
      } else{
        using (StreamWriter w = File.AppendText("user.log")){
          Log.log("User Found", "Echo", w);
      }
    }
}






   public static class Log{
/// <summary>
/// Handels the messages that are sent to see if they are commands etc.
/// </summary>
/// <param name="msg"></param>
/// <param name="user"></param>
/// <param name="write"></param>
/// <param name="prefix"></param>
/// <returns></returns>
    public static async Task<string> handleMessage(string msg, string user, bool write, char prefix){

      
    if(write){
      using (StreamWriter w = File.AppendText("chat.log")){
        log(msg, user, w);
       
      }
    }
    

    if (Parser.RemoveFromPrefix(prefix, msg) == msg){
      using (StreamWriter w = File.AppendText("chat.log")){
         //log(msg, user, w);
      }
    } else{
      string isCommand = Parser.getCommand(prefix, msg);
      switch(isCommand){
        case "rev":
          string reversedText = Parser.Rev(Parser.getArgs(prefix, msg)[0]);
          Console.WriteLine(reversedText);
          using (StreamWriter w = File.AppendText("chat.log")){
         log(reversedText, user, w);
          }
          break;
        case "joke":
        string jokeRecived = await Parser.Joke();
          Console.WriteLine(jokeRecived);
          using (StreamWriter w = File.AppendText("chat.log")){
         log(jokeRecived, user, w);
          }
        break;
      }
    }

     return $"| {DateTime.Now.ToLongTimeString()} | {user} | {msg} |";
  
}
/// <summary>
/// Logs the message in a readable way
/// </summary>
/// <param name="msg"></param>
/// <param name="user"></param>
/// <param name="w"></param>
public static void log(string msg, string user, StreamWriter w){
    w.WriteLine($"| {DateTime.Now.ToLongTimeString()} | {user} | {msg} |");
  }

}


class Parser(){
/// <summary>
/// Gets the command that is in the sent string
/// </summary>
/// <param name="prefix"></param>
/// <param name="input"></param>
/// <returns></returns>
    public static string getCommand(char prefix,string input){
    var Command = RemoveFromPrefix(prefix, input);

    string[] subs = Command.Split(' ');

    foreach (var sub in subs){
        // Console.Write(sub);
    }
    return subs[0];
  }
/// <summary>
/// Removes unnececary text in a command
/// </summary>
/// <param name="prefix"></param>
/// <param name="input"></param>
/// <returns></returns>
    public static string RemoveFromPrefix(char prefix,string input){
    int prefixIndex = 0;

    prefixIndex = input.IndexOf(prefix);
    // Console.WriteLine("  {0}", input.Substring(found + 1));

    return input.Substring(prefixIndex + 1);
  }
/// <summary>
/// Takes in the arguments that are sent in a command
/// </summary>
/// <param name="prefix"></param>
/// <param name="input"></param>
/// <returns></returns>
  public static string[] getArgs(char prefix,string input){
    input = RemoveFromPrefix(prefix, input);

    return input.Split(" ")[1..];
    }
/// <summary>
/// Reverses the users text that they send
/// </summary>
/// <param name="arg"></param>
/// <returns></returns>
  public static string Rev(string arg){
    char[] charReverseString = arg.ToCharArray();
    Array.Reverse(charReverseString);
    return new string(charReverseString);
  }
/// <summary>
/// Sends the user a funny dad joke
/// </summary>
/// <returns></returns>
  public static async Task<string> Joke(){
    JsonSerializerOptions options = new JsonSerializerOptions{WriteIndented = true};

    var handler = new HttpClientHandler() { 
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator};


    using (HttpClient FetchJoke = new HttpClient(handler)){
      FetchJoke.BaseAddress = new Uri("https://icanhazdadjoke.com");
      FetchJoke.DefaultRequestHeaders.Add("Accept", "text/plain");
      
       try{
      HttpResponseMessage response = await FetchJoke.GetAsync("");
      response.EnsureSuccessStatusCode();
      string responseBody = await response.Content.ReadAsStringAsync();
      string JsonString = JsonSerializer.Serialize(responseBody, options);
      return JsonString;
      } catch (HttpRequestException e){
        return e.InnerException.ToString();
      }
       
    };
  }
}

class Program(){

  static async Task Main(){
    Bot a = new Bot();
    await a.Connect();
    await a.Recive();
    while (a.client.State == WebSocketState.Open){
      var task = a.Recive();
      task.Wait();
    // Console.WriteLine(task.Result);
    // await a.GetOuthToken();
    // await a.SendVerification();
    bool Server = true;
    while (Server){
      await a.Recive();
    }
  }
}
}
}

 


 

