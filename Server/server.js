'use strict';

const io = require('./init.js');
const Client = require('./client.js');

var updateInterval = 100; 
var numEnemies = 5;

var connectedClients = {};
var enemies = {};

io.on('connection', socket => {
    console.log("Client connected");
    socket.on('ping', (data) => {
        socket.emit('pong', data);
    });

    socket.on('start', (data) => {
        console.log('Game started');
        io.emit('start', data);
    });
    
    socket.on('register', (j) => {
        const data = JSON.parse(j);
        console.log("Adding " + data.id);
    
        if (Object.keys(connectedClients).length == 0){
            initEnemies();
        }
    
        let client = new Client(data.id);
        connectedClients[data.id] = client;
    
        let init = {
            playerList: Object.keys(connectedClients),
            enemyPositions: enemies
        }
        io.emit('initialize', JSON.stringify(init));
    });
    
    socket.on('leave', (j) => {
        const data = JSON.parse(j);
        console.log("Removing " + data.id);
        delete connectedClients[data.id];
    
        let leave = {
            playerList: Object.keys(connectedClients)
        }
        io.emit('leave', JSON.stringify(leave));
    });
    
    socket.on('enemyAttack', (j) => {
        const data = JSON.parse(j);
        console.log("Enemy attacking " + data.id);
        connectedClients[data.id].decrementHealth();
    });
    
    socket.on('enemyShot', (j) => {
        const data = JSON.parse(j);
        console.log("Enemy " + data.enemyId + " shot by " + data.id);
        if (data.enemyId in enemies) {
            console.log("Enemy " + data.enemyId + " killed by " + data.id);
            io.emit('enemyKilled', j);
            connectedClients[data.id].registerKill();
        }
    });
    
    socket.on('state', (j) => {
        const data = JSON.parse(j);
        connectedClients[data.id].updatePlayerState(data);
    })
});

function initEnemies() {
    for (let i = 0; i < numEnemies; i++) {
        enemies[i] = "" + ((Math.floor(Math.random() * 11)-5)) * 4 + "," + (Math.floor(Math.random() * 6) * 4);
    }
    console.log('Enemies initialized to: ' + JSON.stringify(enemies));
}

setInterval(function(){
    for (const name in connectedClients) {
        io.emit('remoteState', JSON.stringify(connectedClients[name].state)); 
    }
}, updateInterval);