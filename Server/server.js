'use strict';

const io = require('./init.js');
const Client = require('./client.js');

var updateInterval = 100; 
var numEnemies = 5;

var connectedClients = {};
var enemies = {};

io.on('connection', socket => {
    socket.on('ping', (data) => {
        socket.emit('pong', data);
    });
 });

io.on('ping', (data) => {
    console.log('Game started');
    io.emit('start', data);
});

io.on('start', (data) => {
    console.log('Game started');
    io.emit('start', data);
});

io.on('register', (data) => {
    console.log("Adding " + data.id);

    if (connectedClients.length() == 0){
        initEnemies();
    }

    let client = new Client(data.id);
    connectedClients[data.id] = client;

    let init = {
        playerList: Object.keys(connectedClients),
        enemyPositions: enemies
    }
    io.emit('initialize', init);
});

io.on('leave', (data) => {
    console.log("Removing " + data.id);
    delete connectedClients[data.id];

    let leave = {
        playerList: Object.keys(connectedClients)
    }
    io.emit('leave', leave);
});

io.on('enemyAttack', (data) => {
    console.log("Enemy attacking " + data.id);
    connectedClients[data.id].decrementHealth();
});

io.on('enemyShot', (data) => {
    console.log("Enemy " + data.enemyId + " shot by " + data.id);
    if (data.enemyId in enemies) {
        console.log("Enemy " + data.enemyId + " killed by " + data.id);
        io.emit('enemyKilled', data);
        connectedClients[data.id].registerKill();
    }
});

io.on('state', (data) => {
    connectedClients[data.id].updateRotation(data.rotation);
})

function initEnemies() {
    for (let i = 0; i < numEnemies; i++) {
        enemies[i] = "" + ((Math.floor(Math.random() * 11)-5)) * 4 + "," + (Math.floor(Math.random() * 6) * 4);
    }
}

setInterval(function(){
    for (const name in connectedClients) {
        io.emit('remoteState', connectedClients[name].state); 
    }
}, updateInterval);