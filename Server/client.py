from constants import *

class Client:
    def __init__(self, name):
        self.score = 0
        self.kill_count = 0
        self.health = max_health
        self.name = name
        self.json = {}

    def register_kill(self):
        self.score += kill_score
        self.kill_count += 1

    def decrease_health(self):
        self.health -= health_increment

    def get_json(self):
        self.json['id'] = self.name
        self.json['health'] = self.health
        self.json['score'] = self.score
        self.json['kills'] = self.kill_count
        return self.json