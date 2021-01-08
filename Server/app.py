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

#test
spawn_points = [
    (-20.0, 0.0), (-16.0, 0.0), (-12.0, 0.0), (-8.0, 0.0), (-4.0, 0.0), (0.0, 0.0), (4.0, 0.0), (8.0, 0.0), (12.0, 0.0), (16.0, 0.0), (20.0, 0.0),
    (-20.0, 4.0), (-16.0, 4.0), (-12.0, 4.0), (-8.0, 4.0), (-4.0, 4.0), (0.0, 4.0), (4.0, 4.0), (8.0, 4.0), (12.0, 4.0), (16.0, 4.0), (20.0, 4.0),
    (-20.0, 8.0), (-16.0, 8.0), (-12.0, 8.0), (-8.0, 8.0), (-4.0, 8.0), (0.0, 8.0), (4.0, 8.0), (8.0, 8.0), (12.0, 8.0), (16.0, 8.0), (20.0, 8.0),
    (-20.0, 12.0), (-16.0, 12.0), (-12.0, 12.0), (-8.0, 12.0), (-4.0, 12.0), (0.0, 12.0), (4.0, 12.0), (8.0, 12.0), (12.0, 12.0), (16.0, 12.0), (20.0, 12.0),
    (-20.0, 20.0), (-16.0, 20.0), (-12.0, 20.0), (-8.0, 20.0), (-4.0, 20.0), (0.0, 20.0), (4.0, 20.0), (8.0, 20.0), (12.0, 20.0), (16.0, 20.0), (20.0, 20.0)
]
num_enemies = 10

connected_clients = []
enemies = {}

@app.route('/')
def index():
    return render_template('index.html')

# - a player started the game
@socketio.on("start")
def on_start():
    emit("start", broadcast=True)

# - received player state, send updates to all clients
@socketio.on("state")
def on_state(s):
    emit("remote_state", s, broadcast=True)

# - client connected
# - update connected player list
@socketio.on("register")
def on_register(s):
    data = json.loads(s)
    global connected_clients
    global enemies
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

# - client disconnected
# - updates the connected player list
@socketio.on("leave")
def on_leave(s):
    data = json.loads(s)
    name = data["id"]
    global connected_clients
    connected_clients.remove(name)
    print("Removed " + name)

# - client updates that it shot a zombie
# - server figures out if it is already shot or that it should
# notify all clients that it is killed (to play animation/update score/etc)
@socketio.on("shoot_enemy")
def on_shoot_enemy(s):
    data = json.loads(s)
    enemy_id = data["enemyId"]
    player = data["id"]
    global enemies
    print("Enemy " + enemy_id + " shot by " + player)
    if str(enemy_id) in enemies.keys():
        emit("enemy_killed", json.dumps(data), broadcast=True)
        print("Enemy " + enemy_id + " killed by " + player)
        del enemies[enemy_id]

# - for testing, doesn't really work
# - line up all enemies in a row
# - each enemy has an id, so server can figure out which one is shot
# - they all overlap
def init_enemies():
    global num_enemies
    global enemies
    spawns = random.sample(spawn_points, num_enemies)
    for i in range(num_enemies): 
        enemies[str(i)] = str(spawns[i][0]) + "," + str(spawns[i][1])
        # enemies[str(i)] = random.uniform(enemy_x_min, enemy_x_max)
    print("Enemies initialized to: " + json.dumps(enemies))

if __name__ == "__main__":
    socketio.run(app, debug=False, host='0.0.0.0', port=port)
