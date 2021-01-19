
var healthIncrement = 5;
var maxHealth = 100;
var killScore = 100;
var hitScore = 50;

class Client {
    constructor(name) {
        this.type = "remoteState";
        this.id = name;
        this.score = 0;
        this.kills = 0;
        this.health = maxHealth;
        this.rotation = [0, 0, 0];
        this.shooting = 0;
    }

    registerKill() {
        this.score += killScore;
        this.kills += 1;
    }

    registerShot(enemy, damage) {
        this.score += hitScore;
        enemy.decrementHealth(damage);
        if (enemy.health <= 0) {
            this.registerKill();
            return true;
        }
        else {
            return false;
        }
    }

    decrementHealth() {
        this.health -= healthIncrement;
    }

    updatePlayerState(data) {
        this.rotation = data.rotation;
        this.shooting = data.shooting;
    }
}

module.exports = Client;