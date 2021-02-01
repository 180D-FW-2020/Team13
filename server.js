'use strict';

const wss = require('./init.js');
const WebSocket = require('ws');
const Client = require('./client.js');
const Enemy = require('./enemy.js');

var Promise = require('promise');

// connect to the redis database

if (process.env.REDISTOGO_URL) {
    var rtg = require("url").parse(process.env.REDISTOGO_URL);
    var redis = require("redis").createClient(rtg.port, rtg.hostname);
    redis.auth(rtg.auth.split(":")[1]);
} else {
    var redis = require("redis").createClient();
}

// report connection status

redis.on('connect', function () {
    console.log('Redis database connected');
});

// declare some variables

const storageLength = 5;
var messageLength = 0;
var currentPosition = 0;

// for debug purpose

function report() {
    console.log('storageLength: ', storageLength, '; messageLength: ', messageLength, '; currentPosition: ', currentPosition);
}

var numEnemies = 5;
var readyCount = 0;

var connectedClients = {};
var enemies = {};

wss.on('connection', socket => {
    console.log("Client connected");

    socket.on('message', data => {

        // determine if the message is asking to replay

        const tempMess = JSON.parse(data);
        if (tempMess.type == "replay") {
            replayFunction();
            console.log('Replay done.');
            clearStatus();
        }
        else {
            processMessage(socket, data);
        }
    });

});

function broadcastRange(m, n) {

    var keyArray = [];

    for (let i = m; i <= n; i++) {
        keyArray.push(JSON.stringify(i));
    }

    keyArray.forEach(
        function (element, index) {
            redis.get(element, function (err, reply) {
                reply = JSON.parse(reply);
                var newData = reply.content;
                sleep(reply.time);
                // console.log('element: ', element, '; broadcastRange ', newData);
                // report();

                // avoid storing the message again, keep the async feature of the database

                broadcastReplay(reply.content);


            })
        }
    );


    /*
   
        for (let i = m; i <= n; i++) {
            redis.get(JSON.stringify(i), function (err, reply) {
                reply = JSON.parse(reply);
                var newData = reply.content;
                sleep(reply.time);
                console.log('broadcastRange ', newData);
                broadcast(reply.content);
            });
        }
    */

}

function clearStatus() {
    // clear the state
    messageLength = 0;
    currentPosition = 0;
}

function replayFunction() {
    console.log('Start to replay the game!');

    if (messageLength < storageLength) {
        broadcastRange(0, currentPosition - 1);
    }
    else {
        broadcastRange(currentPosition, storageLength - 1);
        broadcastRange(0, currentPosition - 1);
    }
}

// Additional variables
var prevTime = 0;
var interval = 0;

function storeMessage(data) {
    var timeNow = Date.now();
    if (messageLength == 0 && currentPosition == 0) {
        interval = 0;
        prevTime = timeNow;
    }
    else {
        interval = timeNow - prevTime;
        prevTime = timeNow;
    }

    if (interval > 1000) {
        interval = 1000;
    }

    var myObject = { 'content': data, 'time': interval };
    myObject = JSON.stringify(myObject);
    if (messageLength < storageLength) {
        redis.set(JSON.stringify(currentPosition), myObject);
        currentPosition++;
        messageLength++;
        // console.log('store position 1');
        // report();

    }
    else { // if the storage if entirely filled

        if (currentPosition == storageLength) {
            currentPosition = 0;
            redis.set(JSON.stringify(currentPosition), myObject);
            currentPosition++;
            // console.log('store position 2');
            // report();

        }
        else { // if not in the end of the storage

            redis.set(JSON.stringify(currentPosition), myObject);
            currentPosition++;
            // console.log('store position 3');
            // report();
        }
    }
}

function sleep(milliseconds) {
    const date = Date.now();
    let currentDate = null;
    do {
        currentDate = Date.now();
    } while (currentDate - date < milliseconds);
}

function initEnemies() {
    for (let i = 0; i < numEnemies; i++) {
        const pos = ((Math.floor(Math.random() * 11)-5)) * 4 + "," + (Math.floor(Math.random() * 6) * 4);
        enemies[i] = new Enemy(i, pos, Object.keys(connectedClients).length);
    }
    console.log('Enemies initialized to: ' + JSON.stringify(enemies));
}

function broadcast(data) {
    // store the message before broadcast each time

    storeMessage(data);

    // broadcast the messages
    // enable the actual broadcast function please

    /*
    wss.clients.forEach(function each(client) {
        if (client.readyState === WebSocket.OPEN) {
            client.send(data);
        }
    });
    */
}


function broadcastReplay(data) {
    // For test purposes

    console.log('broadcast data: ', data, '; JSON.parse result: ', JSON.parse(data));

    // enable the actual broadcast function please
 
/*
wss.clients.forEach(function each(client) {
    if (client.readyState === WebSocket.OPEN) {
        client.send(data);
    }
});
*/

}

function processMessage(socket, message) {
    const data = JSON.parse(message);
    const name = data.id;

    switch (data.type) {
        case "ping":
            socket.send(message);
            break;
        case "ready":
            readyCount++;
            console.log(name + " is ready");
            if (readyCount == Object.keys(connectedClients).length) {
                console.log('Game started');
                readyCount = 0;
                broadcast(JSON.stringify({type: "start"}));
            }
            break;
        case "register":
            console.log("Adding " + name);
    
            if (Object.keys(connectedClients).length == 0){
                initEnemies();
            }
        
            let client = new Client(name);
            connectedClients[name] = client;
        
            let playerList = {
                type: "playerList",
                playerList: Object.keys(connectedClients)
            }
            broadcast(JSON.stringify(playerList));
            break;
        case "requestEnemies":
            if (Object.keys(enemies).length == 0) {
                initEnemies();
            }
            let states = {
                type: "enemyStates",
                enemies: enemies
            }
            socket.send(JSON.stringify(states));
            break;
        case "leave":
            console.log("Removing " + name);
            delete connectedClients[name];
        
            let leave = {
                type: "leave",
                id: name,
                playerList: Object.keys(connectedClients)
            }
            broadcast(JSON.stringify(leave));
            break;
        case "enemyAttack":
            console.log("Enemy attacking " + name);
            connectedClients[name].decrementHealth();
            broadcast(JSON.stringify(connectedClients[name]));
            break;
        case "enemyShot":
            console.log("Enemy " + data.enemyId + " shot by " + name);
            if (data.enemyId in enemies) {
                connectedClients[name].registerShot();
                enemies[data.enemyId].decrementHealth(data.damage);
                if (enemies[data.enemyId].health <= 0) {
                    console.log("Enemy " + data.enemyId + " killed by " + name);
                    broadcast(JSON.stringify({type: "enemyKilled", enemyId: data.enemyId, id: name}));
                    delete enemies[data.enemyId];
                }
                broadcast(JSON.stringify(connectedClients[name]));
            }
            break;
        case "state":
            connectedClients[name].updatePlayerState(data);
            broadcast(JSON.stringify(connectedClients[name]));
            break;
        case "test":
            broadcast(message);
            break;
    }
}