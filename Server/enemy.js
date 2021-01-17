
var maxHealth = 100;

class Enemy {
    constructor(id, pos, numPlayers) {
        this.type = "enemyState";
        this.enemyId = id;
        this.initialPosition = pos;
        this.health = maxHealth;
        this.target = Math.floor(Math.random() * numPlayers);
        this.running = Math.floor(Math.random() * 2);
    }

    decrementHealth(damage) {
        this.health -= damage;
    }
}

module.exports = Enemy;