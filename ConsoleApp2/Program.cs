using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static HttpListener listener = new HttpListener();
    static readonly object lockObj = new object();

    // Храним уровни в виде строк для удобства логирования
    static string lastLevels = "Нет данных";

    class LevelsData
    {
        public string Symbol { get; set; }
        public List<decimal> Resistance { get; set; }
        public List<decimal> Support { get; set; }
    }

    static async Task Main()
    {
        string url = "http://*:8080/";
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine($"Сервер запущен по адресу {url}");

        Task.Run(UpdateLevelsEveryMinute);

        while (true)
        {
            var context = await listener.GetContextAsync();
            ProcessRequest(context);
        }
    }

    static void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            using var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            string body = reader.ReadToEnd();
            Console.WriteLine($"📥 Получен запрос: {body}");

            if (string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("❌ Ошибка: пустой запрос");
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var data = JsonSerializer.Deserialize<LevelsData>(body);

            if (data != null)
            {
                lock (lockObj)
                {
                    lastLevels = $"📊 Символ: {data.Symbol}, " +
                                 $"Resistance: {string.Join(", ", data.Resistance)}, " +
                                 $"Support: {string.Join(", ", data.Support)}";
                }
                Console.WriteLine(lastLevels);
            }

            string response = "OK";
            byte[] buffer = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки запроса: {ex.Message}");
        }
    }

    static async Task UpdateLevelsEveryMinute()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            lock (lockObj)
            {
                Console.WriteLine($"⏳ Последние уровни: {lastLevels}");
            }
        }
    }
}
