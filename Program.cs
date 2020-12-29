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

         Console.WriteLine("Type in something to get echo stream (Server stream call): ");
         inputKey = Console.ReadLine();
         input = new EchoInput()
         {
            Input = inputKey
         };

         var task = HandleEchoStreamCallAsync(client, input);
         task.Wait();

         Console.ReadKey();

      }

      public static async Task HandleEchoStreamCallAsync(TestService.TestServiceClient client, EchoInput input)
      {
         var resStream = client.GetEchoStream(input);

         while (await resStream.ResponseStream.MoveNext())
         {
            Console.WriteLine("Get echo from stream: " + resStream.ResponseStream.Current.Output.Trim());
            Console.WriteLine("Get echo time: " + resStream.ResponseStream.Current.TimeStamp.ToDateTime().ToLocalTime());
         }
      }
   }
}
