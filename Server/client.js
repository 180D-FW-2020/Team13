
var healthIncrement = 5;
var maxHealth = 100;
var killScore = 100;


class Client {
    constructor(name) {
        this.state = {
            id: name,
            score: 0,
            kills: 0,
            health: maxHealth,
            rotation: [0, 0, 0],
            shooting: 0
        };
    }

    registerKill() {
        this.state.score += killScore;
        this.state.kills += 1;
    }

    decrementHealth() {
        this.state.health -= healthIncrement;
    }

    updatePlayerState(data) {
        this.state.rotation = data.rotation;
        this.state.shooting = data.shooting;
    }
}

module.exports = Client;