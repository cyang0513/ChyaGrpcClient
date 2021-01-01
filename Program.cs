using ChyaGrpcClient.Protos;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ChyaGrpcClient
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("Hello Grpc!");

         using var channel = GrpcChannel.ForAddress(@"https://localhost:5001",
            new GrpcChannelOptions()
            {
               MaxReceiveMessageSize = null,
               MaxSendMessageSize = null
            });
         var client = new TestService.TestServiceClient(channel);

         var ts = client.GetDateTime(new Google.Protobuf.WellKnownTypes.Empty());
         Console.WriteLine("Google Protobuf Time: " + ts.TimeStamp.ToDateTime().ToLocalTime());

         Console.WriteLine("Type in something to echo (Unary call): ");
         var inputKey = Console.ReadLine();
         var input = new EchoInput()
         {
            Input = inputKey
         };
         var res = client.GetEcho(input);
         Console.WriteLine("Get echo: " + res.Output.Trim());

         Console.WriteLine("Type in something to get echo stream (Server streaming call): ");
         var task = HandleServerStreamingCallAsync(client);
         task.Wait();

         Console.WriteLine("Type 3 line to get a echo return (Client streaming call): ");
         HandleClientStreamingCall(client);

         Console.WriteLine("Type 3 line to get echo return in stream (Client/Server streaming call): ");
         var task2 = HandleClientServerStreamingCall(client);
         task2.Wait();

         Console.WriteLine("Type any key to close demo...");
         Console.ReadKey();

      }

      private static async Task HandleClientServerStreamingCall(TestService.TestServiceClient client)
      {
         var call = client.GetInputStreamAsServerStream();

         for (int i = 0; i < 3; i++)
         {
            var inputLine = Console.ReadLine();

            await call.RequestStream.WriteAsync(new EchoInput()
            {
               Input = inputLine
            });
         }
         await call.RequestStream.CompleteAsync();

         var responses = call.ResponseStream.ReadAllAsync();
         await foreach (var res in responses)
         {
            Console.WriteLine("Get echo from stream: " + res.Output.Trim());
            Console.WriteLine("Get echo time: " + res.TimeStamp.ToDateTime().ToLocalTime());
         }
      }

      private static void HandleClientStreamingCall(TestService.TestServiceClient client)
      {
         var call = client.GetInputStream();

         for (int i = 0; i < 3; i++)
         {
            var inputLine = Console.ReadLine();

            call.RequestStream.WriteAsync(new EchoInput()
            {
               Input = inputLine
            });
         }

         call.RequestStream.CompleteAsync();
         var response = call.ResponseAsync;
         response.Wait();

         Console.WriteLine("Get input from stream: " + Environment.NewLine + response.Result.Output);
         Console.WriteLine("Get time: " + response.Result.TimeStamp.ToDateTime().ToLocalTime());
      }

      public static async Task HandleServerStreamingCallAsync(TestService.TestServiceClient client)
      {
         var inputKey = Console.ReadLine();
         var input = new EchoInput()
         {
            Input = inputKey
         };
         var resStream = client.GetEchoStream(input);

         while (await resStream.ResponseStream.MoveNext())
         {
            Console.WriteLine("Get echo from stream: " + resStream.ResponseStream.Current.Output.Trim());
            Console.WriteLine("Get echo time: " + resStream.ResponseStream.Current.TimeStamp.ToDateTime().ToLocalTime());
         }
      }
   }
}
