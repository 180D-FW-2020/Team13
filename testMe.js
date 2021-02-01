const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:3000');

function getRandomInt(max) {
    return Math.floor(Math.random() * Math.floor(max));
}

function between(min, max) {
    return Math.floor(
        Math.random() * (max - min) + min
    )
}

function sleep(milliseconds) {
    const date = Date.now();
    let currentDate = null;
    do {
        currentDate = Date.now();
    } while (currentDate - date < milliseconds);
}

ws.on('open', function open() {
    for (var i = 0; i < 10; i++) {
        var temp = between(0, 1000);
        ws.send(JSON.stringify({ 'id': 1, 'type': 'test', 'content': i }));

        sleep(between(0, 200)); // sleep for a random interval of time
    }
    ws.send(JSON.stringify({ 'id': 1, 'type': 'replay', 'content': 'Start to replay' }));
    console.log('All message sent, start to retrieve.');
});


var label = 0;
var prevTime = 0;
var interval = 0;

ws.on('message', function incoming(data) {
    // console.log(`received: ${data}`);

    console.log('Received data');
    var timeNow = Date.now();
    if (label == 0) {
        interval = 0;
        prevTime = timeNow;
    }
    if (label != 0) {
        interval = timeNow - prevTime;
        prevTime = timeNow;
    }
    data = JSON.parse(data);
    console.log(label, interval, data);
    label += 1;
});
