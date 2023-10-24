require('dotenv').config()
const {
    readFileSync
} = require("fs");
const {
    createServer
} = require("https");
const {
    Server
} = require("socket.io");

const server = createServer({
    cert: readFileSync(process.env.CERT),
    key: readFileSync(process.env.KEY)
});

const io = new Server(server, {});

io.on("connection", (socket) => {
    console.log(`Socket ${socket.id} connected.`)
    if (socket.handshake.query.version != process.env.WSVER || !socket.handshake.query.ContentID) {
        socket.emit("mismatch");
        socket.disconnect();
        console.log(`Socket ${socket.id} disconnected. Using an old version or lacking ContentID.`);
        return;
    }

    socket.party = [];
    socket.ContentID = socket.handshake.query.ContentID;

    console.log(`Socket ${socket.id} identified as ContentID ${socket.ContentID}`);
    socket.join(socket.ContentID);

    socket.on("startTyping", (service, party) => {
        party.splice(8, Infinity);
        socket.party = party;
        socket.to(party).emit("startTyping", service, socket.ContentID);
    });

    socket.on("stopTyping", (service, party) => {
        party.splice(8, Infinity);
        socket.party = party;
        socket.to(party).emit("stopTyping", service, socket.ContentID);
    });

    socket.on("disconnecting", (reason) => {
        if (socket.party.length < 1) return;
        socket.to(socket.party).emit("stopTyping", "all", socket.ContentID);
    });

    socket.on("disconnect", (reason) => {
        console.log(`Socket ${socket.id} disconnected. Reason: ${reason}.`);
        console.log(`Total connected clients: ${io.engine.clientsCount}`);
    });

});

server.listen(process.env.PORT);
console.log(`RTyping Server started on port ${process.env.PORT}.`);