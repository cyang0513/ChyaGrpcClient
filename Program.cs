using ChyaGrpcClient.Protos;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChyaGrpcClient
{
   class Program
   {
      static void Main(string[] args)
      {
         Console.WriteLine("Hello Grpc!");

         using var channel = GrpcChannel.ForAddress(@"https://chyagrpc.northeurope.azurecontainer.io:5001",
            new GrpcChannelOptions()
            {
               MaxReceiveMessageSize = null,
               MaxSendMessageSize = null
            });
         var client = new TestService.TestServiceClient(channel);

         var sysInfo = client.GetSysInfo(new Google.Protobuf.WellKnownTypes.Empty());
         Console.WriteLine(sysInfo.Output);

         var ts = client.GetDateTime(new Google.Protobuf.WellKnownTypes.Empty());
         Console.WriteLine("Google Protobuf Time: " + ts.TimeStamp.ToDateTime().ToLocalTime());

         Console.WriteLine("Type in something to echo (Unary call, type Q to cancel): ");
         HandleUnaryCall(client);

         Console.WriteLine("Type in something to get echo stream (Server streaming call, type Q to cancel): ");
         var task = HandleServerStreamingCallAsync(client);
         try
         {
            task.Wait();
         }
         catch (AggregateException ex)
         {
            foreach (var ie in ex.InnerExceptions)
            {
               if (ie is RpcException)
               {
                  switch (((RpcException)ie).StatusCode)
                  {
                     case StatusCode.DeadlineExceeded:
                        Console.WriteLine("Exceed deadline.");
                        break;
                     case StatusCode.Cancelled:
                        Console.WriteLine("Operation cancelled.");
                        break;
                     default:
                        break;
                  }
               }
            }
         }

         Console.WriteLine("Type 3 line to get a echo return (Client streaming call, type Q to cancel): ");
         HandleClientStreamingCall(client);

         Console.WriteLine("Type 3 line to get echo return in stream (Client/Server streaming call, type Q to cancel): ");
         var task2 = HandleClientServerStreamingCall(client);
         try
         {
            task2.Wait();
         }
         catch (AggregateException ex)
         {
            foreach (var ie in ex.InnerExceptions)
            {
               if (ie is RpcException)
               {
                  switch (((RpcException)ie).StatusCode)
                  {
                     case StatusCode.DeadlineExceeded:
                        Console.WriteLine("Exceed deadline.");
                        break;
                     case StatusCode.Cancelled:
                        Console.WriteLine("Operation cancelled.");
                        break;
                     default:
                        break;
                  }
               }
            }
         }

         Console.WriteLine("Type any key to close demo...");
         Console.ReadKey();

      }

      private static void HandleUnaryCall(TestService.TestServiceClient client)
      {
         var inputKey = Console.ReadLine();
         var input = new EchoInput()
         {
            Input = inputKey
         };
         var tokensource = new CancellationTokenSource();

         if (inputKey == "Q")
         {
            tokensource.Cancel();
         }

         try
         {
            var res = client.GetEcho(input, cancellationToken: tokensource.Token);
            Console.WriteLine("Get echo: " + res.Output.Trim());
         }
         catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
         {
            Console.WriteLine("Operation cancelled.");
         }
         finally
         {
            tokensource.Dispose();
         }
      }

      private static async Task HandleClientServerStreamingCall(TestService.TestServiceClient client)
      {
         var tokenSource = new CancellationTokenSource();
         var call = client.GetInputStreamAsServerStream(cancellationToken: tokenSource.Token, deadline: DateTime.UtcNow.AddSeconds(10));

         for (int i = 0; i < 3; i++)
         {
            var inputLine = Console.ReadLine();
            if (inputLine == "Q")
            {
               tokenSource.Cancel();
               break;
            }
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
         var tokenSource = new CancellationTokenSource();
         var call = client.GetInputStream(deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: tokenSource.Token);
         
         try
         {
            for (int i = 0; i < 3; i++)
            {
               var inputLine = Console.ReadLine();

               if (inputLine == "Q")
               {
                  tokenSource.Cancel();
                  break;
               }
               
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
         catch (AggregateException ex) 
         {
            foreach (var ie in ex.InnerExceptions)
            { 
               if (ie is RpcException)
               {
                  switch (((RpcException)ie).StatusCode)
                  {
                     case StatusCode.DeadlineExceeded:
                        Console.WriteLine("Exceed deadline.");
                        break;
                     case StatusCode.Cancelled:
                        Console.WriteLine("Operation cancelled.");
                        break;
                     default:
                        break;
                  }
               }
            }           
         }
         finally
         {
            tokenSource.Dispose();
         }
      }

      public static async Task HandleServerStreamingCallAsync(TestService.TestServiceClient client)
      {
         var inputKey = Console.ReadLine();
         var input = new EchoInput()
         {
            Input = inputKey
         };

         var tokenSource = new CancellationTokenSource();
         if (inputKey == "Q")
         {
            tokenSource.Cancel();
         }

         var resStream = client.GetEchoStream(input, deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: tokenSource.Token);

         while (await resStream.ResponseStream.MoveNext())
         {
            Console.WriteLine("Get echo from stream: " + resStream.ResponseStream.Current.Output.Trim());
            Console.WriteLine("Get echo time: " + resStream.ResponseStream.Current.TimeStamp.ToDateTime().ToLocalTime());
         }

      }
      
   }
}