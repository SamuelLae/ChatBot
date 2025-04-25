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

  public async Task Connect(){
    string ServerUrl = "wss://echo.websocket.in";
    client = new ClientWebSocket();
    await client.ConnectAsync(new Uri(ServerUrl), default);
    Console.ReadKey();
  }
  
  string UserOuthToken;
  string UserAccountName;
  

      public async Task GetOuthToken(){
      using (StreamReader w = new("UserData.txt")){
      string[] UserDataString = w.ReadToEnd().Split('\n');
      UserOuthToken = UserDataString[1];
      UserAccountName = UserDataString[0];
      }

  }

      async Task Send(){
      Console.WriteLine("Send message: ");
      string message = Console.ReadLine();
      await Log.handleMessage(message, "Samuel", true, '#');
      byte[] buffer = Encoding.UTF8.GetBytes(message);
      await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

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

    public async Task SendVerification(){
      await Log.handleMessage(UserOuthToken, UserAccountName, true, '#');
      byte[] buffer = Encoding.UTF8.GetBytes(UserOuthToken);
      await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
      buffer = Encoding.UTF8.GetBytes(UserAccountName);
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

    public static async Task<string> handleMessage(string msg, string user, bool write, char prefix){

      /*
    if(write){
      using (StreamWriter w = File.AppendText("chat.log")){
        log(msg, user, w);
       
      }
    }
    */

    if (Parser.RemoveFromPrefix(prefix, msg) == msg){
      using (StreamWriter w = File.AppendText("chat.log")){
         log(msg, user, w);
      }
    } else{
      string CommandChack = Parser.getCommand(prefix, msg);
      switch(CommandChack){
        case "rev":
          Console.WriteLine(Parser.Rev(Parser.getArgs(prefix, msg)[0]));
          break;
        case "joke":
          Console.WriteLine(await Parser.Joke());
        break;
      }
    }

     return $"| {DateTime.Now.ToLongTimeString()} | {user} | {msg} |";
  
}

public static void log(string msg, string user, StreamWriter w){
    w.WriteLine($"| {DateTime.Now.ToLongTimeString()} | {user} | {msg} |");
  }

}


class Parser(){

    public static string getCommand(char prefix,string input){
    var Command = RemoveFromPrefix(prefix, input);

    string[] subs = Command.Split(' ');

    foreach (var sub in subs){
        // Console.Write(sub);
    }
    return subs[0];
  }

    public static string RemoveFromPrefix(char prefix,string input){
    int found = 0;

    found = input.IndexOf(prefix);
    // Console.WriteLine("  {0}", input.Substring(found + 1));

    return input.Substring(found + 1);
  }

  public static string[] getArgs(char prefix,string input){
    input = RemoveFromPrefix(prefix, input);

    string[] subs = input.Split(' ');
    return subs[1..];
    }

  public static string Rev(string arg){
    char[] charReverseString = arg.ToCharArray();
    Array.Reverse(charReverseString);
    string charString = new string(charReverseString);
     using (StreamWriter w = File.AppendText("chat.log")){
     Log.log(charString, "samuel", w);
     };
    return new string(charReverseString);
  }

  public static async Task<string> Joke(){
    JsonSerializerOptions options = new JsonSerializerOptions{WriteIndented = true};

    using (HttpClient FetchJoke = new HttpClient()){
      FetchJoke.BaseAddress = new Uri("https://icanhazdadjoke.com");
      FetchJoke.DefaultRequestHeaders.Add("Accept", "text/plain");
       try{
      HttpResponseMessage response = await FetchJoke.GetAsync("");
      response.EnsureSuccessStatusCode();
      string responseBody = await response.Content.ReadAsStringAsync();
      string JsonString = JsonSerializer.Serialize(responseBody, options);
      using (StreamWriter w = File.AppendText("chat.log")){
      Log.log(JsonString, "samuel", w);
      };
      return JsonString;
      } catch (HttpRequestException e){
        return e.Message;
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

 


 

