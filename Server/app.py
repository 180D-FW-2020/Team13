# Modified based on https://flask-socketio.readthedocs.io/en/latest/
# Also from https://medium.com/@rohanjdev/how-to-use-socket-io-with-flask-heroku-af9909e2e9a4

from flask import Flask, render_template, make_response, redirect, jsonify
from flask_socketio import SocketIO, send, emit, join_room, leave_room
import os
import random
import json
import redis
import eventlet
eventlet.monkey_patch()

from client import Client

REDIS_URL = os.environ.get("REDIS_URL")
r = redis.from_url(REDIS_URL)
app = Flask(__name__)
socketio = SocketIO(app, async_mode='eventlet', logging=True)
socketio.init_app(app, cors_allowed_origins="*")
port = int(os.environ.get("PORT", 5000))

connected_clients = {}
enemies = {}

#test
spawn_points = [
    (-20.0, 0.0), (-16.0, 0.0), (-12.0, 0.0), (-8.0, 0.0), (-4.0, 0.0), (0.0, 0.0), (4.0, 0.0), (8.0, 0.0), (12.0, 0.0), (16.0, 0.0), (20.0, 0.0),
    (-20.0, 4.0), (-16.0, 4.0), (-12.0, 4.0), (-8.0, 4.0), (-4.0, 4.0), (0.0, 4.0), (4.0, 4.0), (8.0, 4.0), (12.0, 4.0), (16.0, 4.0), (20.0, 4.0),
    (-20.0, 8.0), (-16.0, 8.0), (-12.0, 8.0), (-8.0, 8.0), (-4.0, 8.0), (0.0, 8.0), (4.0, 8.0), (8.0, 8.0), (12.0, 8.0), (16.0, 8.0), (20.0, 8.0),
    (-20.0, 12.0), (-16.0, 12.0), (-12.0, 12.0), (-8.0, 12.0), (-4.0, 12.0), (0.0, 12.0), (4.0, 12.0), (8.0, 12.0), (12.0, 12.0), (16.0, 12.0), (20.0, 12.0),
    (-20.0, 20.0), (-16.0, 20.0), (-12.0, 20.0), (-8.0, 20.0), (-4.0, 20.0), (0.0, 20.0), (4.0, 20.0), (8.0, 20.0), (12.0, 20.0), (16.0, 20.0), (20.0, 20.0)
]
num_enemies = 3

@app.route('/')
def index():
    return render_template('index.html')


# - handle all socketio errors
@socketio.on_error_default
def error_handler(e):
    print('ERROR: ' + str(e))

# - reset cache
@socketio.on("reset")
def on_reset():
    connected_clients = {}
    enemies = {}
    print("Reset")

# - a player started the game
@socketio.on("start")
def on_start():
    print("Start game")
    emit("start", broadcast=True)

# - received player state, send updates to all clients
@socketio.on("state")
def on_state(s):
    emit("remote_state", s, broadcast=True)

# - received player state, send updates to all clients
@socketio.on("shoot")
def on_shoot(s):
    emit("remote_shoot", s, broadcast=True)

# - client connected
# - update connected player list
@socketio.on("register")
def on_register(s):
    data = json.loads(s)
    if len(connected_clients) == 0:
        init_enemies()

    name = data["id"]
    client = Client(name)
    connected_clients[name] = client
    print("Registered " + name)
    print("Player list: " + ", ".join(connected_clients))

    response = {}
    response["playerList"] = list(connected_clients.keys())
    response["enemyPositions"] = enemies
    emit("initialize", json.dumps(response), broadcast=True)

# - client disconnected
# - updates the connected player list
@socketio.on("leave")
def on_leave(s):
    data = json.loads(s)
    name = data["id"]
    del connected_clients[name]
    print("Removed " + name)

    response = {}
    response["id"] = name
    response["playerList"] = list(connected_clients.keys())
    emit("leave", json.dumps(response), broadcast=True)

# - client updates that it shot a zombie
# - server figures out if it is already shot or that it should
# - notify all clients that it is killed (to play animation/update score/etc)
@socketio.on("shoot_enemy")
def on_shoot_enemy(s):
    data = json.loads(s)
    enemy_id = data["enemyId"]
    name = data["id"]

    print("Enemy " + enemy_id + " shot by " + name)
    if str(enemy_id) in enemies.keys():
        connected_clients[name].register_kill()
        response = connected_clients[name].get_json()
        emit("enemy_killed", json.dumps(s), broadcast=True)
        emit("update_values", json.dumps(response), broadcast=True)
        print("Enemy " + enemy_id + " killed by " + name)
        del enemies[enemy_id]

@socketio.on("enemy_attack")
def on_attach_player(s):
    data = json.loads(s)
    name = data["id"]

    print("Enemy attacking " + name)
    connected_clients[name].decrease_health()
    response = connected_clients[name].get_json()
    emit("update_values", json.dumps(response), broadcast=True)


# - each enemy has an id, so server can figure out which one is shot
# - randomly sampled positions
def init_enemies():
    spawns = random.sample(spawn_points, num_enemies)
    for i in range(num_enemies): 
        enemies[str(i)] = str(spawns[i][0]) + "," + str(spawns[i][1])
    print("Enemies initialized to: " + json.dumps(enemies))

if __name__ == "__main__":
    socketio.run(app, debug=False, host='0.0.0.0', port=port)
