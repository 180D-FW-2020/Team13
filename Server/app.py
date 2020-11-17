# Modified based on https://flask-socketio.readthedocs.io/en/latest/
# Also from https://medium.com/@rohanjdev/how-to-use-socket-io-with-flask-heroku-af9909e2e9a4

from flask import Flask, render_template, make_response, redirect
from flask_socketio import SocketIO, send, emit, join_room, leave_room
import os
import random
import json
from datetime import datetime

app = Flask(__name__)
socketio = SocketIO(app, async_mode='eventlet', logging=True)
socketio.init_app(app, cors_allowed_origins="*")
port = int(os.environ.get("PORT", 5000))

enemy_x_min = -5
enemy_x_max = 5
num_enemies = 10

connected_clients = []
enemies = {}

@app.route('/')
def index():
    return render_template('index.html')

@socketio.on("state")
def on_state(s):
    emit("remote_state", s, broadcast=True)

@socketio.on("register")
def on_register(s):
    data = json.loads(s)
    if len(connected_clients) == 0:
        init_enemies()
    name = data["id"]
    connected_clients.append(name)
    print("Registered " + name)
    print("Player list: " + ", ".join(connected_clients))
    response = {}
    response["playerList"] = connected_clients
    response["enemyPositions"] = enemies
    emit("initialize", json.dumps(response), broadcast=True)

@socketio.on("leave")
def on_leave(s):
    data = json.loads(s)
    name = data["id"]
    connected_clients.remove(name)
    print("Removed " + name)

@socketio.on("shoot_enemy")
def on_shoot_enemy(s):
    data = json.loads(s)
    enemy_id = data["enemyId"]
    player = data["id"]
    print("Enemy " + enemy_id + " shot by " + player)
    if str(enemy_id) in enemies.keys():
        emit("enemy_killed", json.dumps(data), broadcast=True)
        print("Enemy " + enemy_id + " killed by " + player)
        del enemies[enemy_id]

def init_enemies():
    for i in range(num_enemies):
        enemies[str(i)] = random.uniform(enemy_x_min, enemy_x_max)
    print("Enemies initialized to: " + json.dumps(enemies))

def get_timestamp():
    return (datetime.now() - datetime(1, 1, 1)).total_seconds() * 10000000 # C# style ticks

if __name__ == "__main__":
    socketio.run(app, debug=True, host='0.0.0.0', port=port)
