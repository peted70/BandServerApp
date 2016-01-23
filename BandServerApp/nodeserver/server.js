var WebSocketServer = require('ws').Server
  , wss = new WebSocketServer({ port: 54545 });

console.log("Waiting...");

wss.on('connection', function connection(ws) {
    console.log('connection');

    ws.on('message', function incoming(message) {
        console.log('received: %s', message);
        console.log(wss.clients.length);
        wss.clients.forEach(function each(client) {
            client.send(message);
        });
    });

    ws.send('something');
});