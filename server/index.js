require('dotenv').config()
const WebSocket = require('ws');
const {
    createServer
} = require('https');
const {
    readFileSync
} = require('fs');

const server = createServer({
    cert: readFileSync(process.env.CERT),
    key: readFileSync(process.env.KEY)
});

const wss = new WebSocket.Server({
    server
});
const users = new Map();
const wsVer = process.env.WSVER;

const commands = {
    IDENTIFY: 0,
    TYPING: 1,
    NOTYPING: 2,
    VERSION: 3,
    FAILED: 4
}

function PartyBroadcast(ws, command, content) {
    if (ws.party == null) return;
    ws.party.forEach(identity => {
        if (identity == ws.identity) return;
        if (!users.has(identity)) return;
        const member = users.get(identity);
        member.send(JSON.stringify({
            Command: command,
            Content: content
        }));
    });
}

wss.on("connection", (ws) => {
    ws.isAlive = true;
    ws.on("pong", () => {
        ws.isAlive = true;
    });
    
    ws.send(JSON.stringify({
        Command: commands.VERSION
    }));

    ws.verify = setTimeout(() => {
        if(ws.identity != null) return;
        ws.send(JSON.stringify({
            Command: commands.FAILED
        }));
        return ws.terminate();
    }, 30000)

    ws.on("message", (data) => {
        var message = JSON.parse(data);
        console.log(`${data}`)
        switch (message.Command) {
            default:
                return console.log(`Unhandled command: ${message.Command}`);
            case commands.IDENTIFY:
                ws.identity = message.Content;
                users.set(message.Content, ws);
                break;
            case commands.TYPING:
                ws.party = message.Content.split(",");
                PartyBroadcast(ws, commands.TYPING, ws.identity);
                break;
            case commands.NOTYPING:
                ws.party = message.Content.split(",");
                PartyBroadcast(ws, commands.NOTYPING, ws.identity);
                break;
            case commands.VERSION:
                clearTimeout(ws.verify);
                if (message.Content != wsVer){
                    ws.send(JSON.stringify({
                        Command: commands.FAILED
                    }));
                    return ws.terminate();
                }
                return ws.send(JSON.stringify({
                    Command: commands.IDENTIFY
                }));
        }
    });

    ws.on("close", () => {
        if (ws.identity != null) {
            PartyBroadcast(ws, commands.NOTYPING, ws.identity);
            if (users.has(ws.identity)) users.delete(ws.identity);
        } else {}
    });
})

const checkAlive = setInterval(() => {
    wss.clients.forEach((ws) => {
        if (!ws.isAlive) {
            if (ws.identity != null) {
                PartyBroadcast(ws, commands.NOTYPING, ws.identity);
                if (users.has(ws.identity)) users.delete(ws.identity);
            }
            return ws.terminate();
        }
        ws.isAlive = false;
        ws.ping();
    });
}, 30000);

wss.on("close", () => {
    clearInterval(checkAlive);
})

server.listen(8080);