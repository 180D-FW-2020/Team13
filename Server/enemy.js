
var maxHealth = 100;

class Enemy {
    constructor(id, pos, numPlayers) {
        this.type = "enemyState";
        this.enemyId = id;
        this.position = pos;
        this.health = maxHealth;
        this.target = Math.floor(Math.random() * numPlayers);
        this.running = Math.floor(Math.random() * 2);
        this.attacking = 0;
    }

    decrementHealth(damage) {
        this.health -= damage;
    }

    startAttacking(ts) {
        if (this.attacking == 0) {
            this.attacking = ts;
        }
    }
}

module.exports = Enemy;