using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace SERVER.Protocol
{
    public class ControlMessage
    {

        public string? msg { get; set; } 

        public string? id { get; set; } 
        public string? password { get; set; } 
        public string? userName { get; set; } 
        public string? carModel { get; set; } 
        public string? command { get; set; } 
        public bool? charging { get; set; } 

        public bool? open { get; set; } 
        public bool? on { get; set; } 


        public int? temp { get; set; } 
        public string? reason { get; set; } 
        public bool? registered { get; set; }
        public bool? logined { get; set; }
        public string? doorStatus { get; set; }
        public bool? resulted { get; set; }

        public static async Task<ControlMessage?> DeserializeMessageAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {

            const int MAX_MESSAGE_SIZE = 1024 * 100;
            byte[] lengthBytes = new byte[4];

            int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);
            if (bytesRead != 4) return null;

            int messageLength = BitConverter.ToInt32(lengthBytes, 0);
            if (messageLength <= 0 || messageLength > MAX_MESSAGE_SIZE) return null;

            byte[] messageBytes = new byte[messageLength];
            int totalBytesRead = 0;


            while (totalBytesRead < messageLength)
            {
                bytesRead = await stream.ReadAsync(
                    messageBytes, 
                    totalBytesRead,
                    messageLength - totalBytesRead,
                    cancellationToken);

                if (bytesRead == 0 || cancellationToken.IsCancellationRequested) return null;
                totalBytesRead += bytesRead;
            }

            // 3. JSON 변환
            string json = Encoding.UTF8.GetString(messageBytes); 
            try
            {
                ControlMessage? message = JsonSerializer.Deserialize<ControlMessage>(json);
                return message;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static byte[] SerializeMessage(ControlMessage message)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(message, options);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            byte[] lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
            byte[] result = new byte[4 + jsonBytes.Length];
            Array.Copy(lengthBytes, 0, result, 0, 4);
            Array.Copy(jsonBytes, 0, result, 4, jsonBytes.Length);
            return result;
        }
    }
}