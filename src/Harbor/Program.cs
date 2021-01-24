using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;

Console.WriteLine("Scan some ports!");

static async Task Worker(ChannelReader<int> ports, ChannelWriter<int> results) {
    var address = "scanme.nmap.org";

    using (var client = new TcpClient()) {
        while (await ports.WaitToReadAsync()) {
            try {
                ports.TryRead(out var i);
                //Console.WriteLine($"trying port {i}");
                await client.ConnectAsync(address, i);

                results.TryWrite(i);
                //await Task.Delay(100);
            } catch (SocketException) {
                results.TryWrite(0);
                //await Task.Delay(100);
            } catch (Exception ex) {
                Console.WriteLine($"ex: {ex.ToString()}");
            }
        }
    }
}

var portsChan = Channel.CreateUnbounded<int>();
var resultsChan = Channel.CreateUnbounded<int>();

// spawn workers
//foreach (var i in Enumerable.Range(1, 100)) {
//    _ = Task.Run(async () => await Worker(portsChan.Reader, resultsChan.Writer));
//}

_ = Task.Run(async () => {
    var tasks =
        Enumerable
            .Range(1, 100)
            .Select(_ => Worker(portsChan.Reader, resultsChan.Writer))
            .ToArray();

    await Task.WhenAll(tasks);
});


// start
_ = Task.Run(() => {
    foreach (var i in Enumerable.Range(1, 1024)) {
        portsChan.Writer.TryWrite(i);
    }
});


// wait for completion
var openPorts = new List<int>();
foreach (var i in Enumerable.Range(1, 1024)) {
    var result = await resultsChan.Reader.ReadAsync();
    if (result != 0) {
        openPorts.Add(result);
    }
}


Console.WriteLine($"open ports: {string.Join(", ", openPorts)}");


Console.WriteLine("Bye Bye");
