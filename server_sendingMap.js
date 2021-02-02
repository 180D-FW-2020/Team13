'use strict';

const wss = require('./init.js');
const WebSocket = require('ws');
const Client = require('./client.js');
const Enemy = require('./enemy.js');

const { unpack, pack } = require('msgpackr');

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

const storageLength = 15;
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

function broadcastRange(m, n, myMap) {

    var keyArray = [];

    for (let i = m; i <= n; i++) {
        keyArray.push(JSON.stringify(i));
    }

    /*
    var counter = n - m + 1;
    keyArray.forEach(function (key, i) {
        redis.get(key, function (err, data) {
            if (err)
                console.log(err);
            console.log(data);
            data = JSON.parse(data);
            myMap.set(data.time, data.content);
            counter--;
            if (counter == 0)
                return myMap;
        });
    });
        */
    
    keyArray.forEach(
        function (message, index) {
            /*
            redis.get(message, (err, data) => {
                data = JSON.parse(data);
                myMap.set(data.time, data.content);
                console.log(data);
            });
            */

            let pm = new Promise(function (resolve, reject) {
                // "Producing Code" (May take some time)
                redis.get(message, (err, data) => {
                    data = JSON.parse(data);
                    console.log('reach pm1.');
                    resolve(data);
                });
            });

            // "Consuming Code" (Must wait for a fulfilled Promise)
            pm.then(
                function (value) {
                    let time, content = value;
                    myMap.set(time, content);
                    console.log('reach pm2.');
                },
                function (error) { }
            );
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


    var myMap = new Map();

    var keyArray = [];

    if (messageLength < storageLength) {
        for (let i = 0; i <= currentPosition - 1; i++) {
            keyArray.push(JSON.stringify(i));
        }
        var counter = currentPosition;
    }
    else {
        for (let i = 0; i <= storageLength - 1; i++) {
            keyArray.push(JSON.stringify(i));
        }
        var counter = storageLength;
    }

    function broadcastFirst(callback) {
        keyArray.forEach(
            function (message, index) {
                redis.get(message, (err, data) => {
                    if (err)
                        return callback(err);
                    data = JSON.parse(data);
                    myMap.set(data.time, data.content);
                    counter--;
                    if (counter == 0)
                        callback(null, myMap)
                });
            }
        );
        
    }

    broadcastFirst(function (err, myMap) {
        if (err)
            console.log('error!');
        broadcastReplay(pack(myMap));
    });

    /*
    let myPromise = new Promise(function (myResolve, myReject) {
        keyArray.forEach(
            function (message, index) {
                console.log('here 1.')
                redis.get(message, (err, data) => {
                    data = JSON.parse(data);
                    console.log('here 2.');
                    myMap.set(data.time, data.content);
                    console.log(data);
                });
            }
        );
        myResolve(myMap);
    });

    myPromise.then(
        function (value) { broadcastReplay(pack(value)); console.log('success.'); },
        function (error) { console.log(error); }
    );
    */

    /*
    keyArray.forEach(
        function (message, index) {
            
            redis.get(message, (err, data) => {
                data = JSON.parse(data);
                myMap.set(data.time, data.content);
                console.log(data);
            });
            

            let pm = new Promise(function (resolve, reject) {
                // "Producing Code" (May take some time)
                redis.get(message, (err, data) => {
                    data = JSON.parse(data);
                    console.log('reach pm1.');
                    resolve(data);
                });
            });

            // "Consuming Code" (Must wait for a fulfilled Promise)
            pm.then(
                function (value) {
                    let time, content = value;
                    myMap.set(time, content);
                    console.log('reach pm2.');
                },
                function (error) { }
            );
        }
    );

    broadcastReplay(pack(myMap));
    console.log('success.'); 
    */

    /*
    if (messageLength < storageLength) {
        broadcastRange(0, currentPosition - 1, myMap);
    }
    else {
        broadcastRange(currentPosition, storageLength - 1, myMap);
        broadcastRange(0, currentPosition - 1, myMap);
    }

    broadcastReplay(pack(myMap));
    */

    /*
    let myPromise = new Promise(function (myResolve, myReject) {
        if (messageLength < storageLength) {
            broadcastRange(0, currentPosition - 1, myMap);
        }
        else {
            broadcastRange(currentPosition, storageLength - 1, myMap);
            broadcastRange(0, currentPosition - 1, myMap);
            console.log('here (2).')
        }
        myResolve(myMap);
    });

    myPromise.then(
        function (value) { broadcastReplay(pack(value)); console.log('success.'); },
        function (error) { console.log(error); }
    );
    */
    

    
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

    // var myObject = { 'content': data, 'time': interval };

    var myObject = { 'content': data, 'time': timeNow };
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

    console.log('broadcast: ', unpack(data));

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

/*
            var clientGet = (message) => {
                return new Promise((fullfill, reject) => {
                    Redis.get(message, (err, data) => {
                        if (err)
                            reject(err);
                        else {
                            data = JSON.parse(data);
                            fullfill([data.time, data.content]);
                            console.log('step 1 reached.');
                        }
                    });
                });
            }

            var clientSet = (dataFromGet) => {
                return new Promise((fullfill, reject) => {
                    let time, content = dataFromGet;
                    fullfill(function () {
                        myMap.set(time, content);
                        return myMap;
                    });
                });
            }

            clientGet(message)
                .then(clientSet)
                .then(() => {
                    console.log("Operations done.")
                })
                .catch((ex) => {
                    console.err(ex.message);
                });

*/

            /*
            var replay = '';
            var timeCount = '';
            let Pm = new Promise(function (myResolve, myReject) {
                redis.get(element, function (err, reply) {
                    var tempReply = JSON.parse(reply);
                    replay = tempReply.content;
                    timeCount = tempReply.time;
                    myResolve([replay, timeCount]);
                });
            });

            Pm.then(
                function (value) {
                    myMap.set(JSON.stringify(value[1]), JSON.stringify(value[0]));
                    console.log('set: ', newData);
                }
            
            );
            */

            // redis.get(element, function (err, reply) {
                // reply = JSON.parse(reply);
                // var newData = reply.content;
                
                // myMap.set(JSON.stringify(reply.time), JSON.stringify(newData));
                // console.log('set: ', newData);
                // sleep(reply.time);
                // console.log('element: ', element, '; broadcastRange ', newData);
                // report();

                // avoid storing the message again, keep the async feature of the database

                // broadcastReplay(reply.content);


            // })
