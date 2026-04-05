## Original Game Code 

Task: Understand the original game code and identify the key components that need to be modified or replaced to be implemented in Unity. 
The Game should be a 2D game where the player controls a rocket and tries to avoid obstacles like planets that pull the rocket with gravity. The object of the game is to shoot the rocket to the docking gate (a target) and land safely within 1-3 range of the target. The middle is worth 3 stars, the outer ring is worth 2 stars, and the outermost ring is worth 1 star. [1][2][3][2][1] for example.
The game should have the Core mechanics first (we just need a 1 level now) then we can add more level. DO NOT make anything more than what is asked unless we continue. 
The game should have a win/lose condition (out of bounce or hit a planet = lose, hit the docking gate = win) and just 1 level on repeat.
Since this is a HTML version some things in Unity should be done with the unity engine like gravity, pull, speed etc.

Output: A unity playable demo of the original game code. 


Rules:
- Do not make anything more than what is asked unless we continue. 
- Do not deviate from the original game code. 
- If in editor implementation is needed, use the original game code as a reference and write a plan on how to implement it in Unity step by step in the implementation_plan.md file that you will create. Be as thorugh as possible. We are on Unity version 6000.3.10f1

Original Game code:
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
    <title>Gravity Rocket: Monochrome</title>
    <style>
        :root {
            --bg-color: #000000;
            --text-color: #ffffff;
            --accent: #ffffff;
            --accent-dark: #333333;
            --danger: #ffffff;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            background-color: #000;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            width: 100vw;
            overflow: hidden;
            font-family: 'Courier New', Courier, monospace;
            color: var(--text-color);
        }

        #game-container {
            position: relative;
            width: 100%;
            height: 100%;
            max-width: 500px;
            aspect-ratio: 9 / 16;
            background-color: var(--bg-color);
            border-left: 1px solid #222;
            border-right: 1px solid #222;
        }

        canvas {
            display: block;
            width: 100%;
            height: 100%;
        }

        #ui-layer {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
        }

        .panel {
            background: rgba(0, 0, 0, 0.85);
            border: 1px solid var(--accent);
            padding: 2rem;
            text-align: center;
            pointer-events: auto;
            transition: opacity 0.3s ease;
            backdrop-filter: blur(8px);
            min-width: 280px;
        }

        .hidden {
            opacity: 0;
            pointer-events: none;
            display: none !important;
        }

        h1 {
            color: var(--accent);
            margin-bottom: 1rem;
            font-size: 1.8rem;
            text-transform: uppercase;
            letter-spacing: 4px;
        }

        h2 {
            color: var(--accent);
            margin-bottom: 0.5rem;
            font-size: 1.2rem;
            letter-spacing: 2px;
            text-transform: uppercase;
        }

        p {
            margin-bottom: 2rem;
            line-height: 1.6;
            color: #ccc;
            font-size: 0.9rem;
        }

        .stars-display {
            font-size: 2.5rem;
            color: var(--accent);
            margin-bottom: 0.5rem;
            letter-spacing: 5px;
        }

        .total-stars {
            font-size: 0.9rem;
            color: #aaa;
            margin-bottom: 2rem;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        button {
            background-color: transparent;
            color: var(--accent);
            border: 1px solid var(--accent);
            padding: 12px 24px;
            font-size: 1rem;
            font-family: 'Courier New', Courier, monospace;
            cursor: pointer;
            text-transform: uppercase;
            letter-spacing: 2px;
            transition: all 0.2s;
            margin: 5px;
        }

        button:hover, button:active {
            background-color: var(--accent);
            color: var(--bg-color);
        }

        #hud {
            position: absolute;
            top: 20px;
            left: 20px;
            font-size: 1rem;
            color: var(--accent);
            letter-spacing: 2px;
            text-transform: uppercase;
            z-index: 10;
        }

        .shield-bar {
            font-size: 0.8rem;
            margin-top: 5px;
            letter-spacing: 1px;
            color: #ccc;
        }

        #instruction {
            position: absolute;
            bottom: 80px;
            left: 0;
            width: 100%;
            text-align: center;
            font-size: 0.8rem;
            color: rgba(255, 255, 255, 0.5);
            pointer-events: none;
            transition: opacity 0.5s;
            text-transform: uppercase;
            letter-spacing: 1px;
        }
    </style>
</head>
<body>

<div id="game-container">
    <canvas id="gameCanvas" width="400" height="800"></canvas>
    
    <div id="hud">
        U<span id="hud-universe">1</span> - SEC <span id="hud-sector">1</span> - LVL <span id="hud-level">1</span><br>
        <span style="font-size: 0.8rem; color:#888;">★ <span id="hud-stars">0</span></span><br>
        <div class="shield-bar">SHIELD: <span id="hud-shields">█████</span></div>
    </div>
    <div id="instruction">Drag to plot trajectory</div>

    <div id="ui-layer">
        <div id="menu-panel" class="panel">
            <h1>Gravity<br>Rocket</h1>
            <p>Monochrome Edition<br>Hit dead center for 3 stars.</p>
            <button id="start-btn">Initiate</button>
        </div>

        <div id="gameover-panel" class="panel hidden">
            <h2>Signal Lost</h2>
            <p>Trajectory compromised.</p>
            <button id="retry-btn">Recalculate</button>
        </div>

        <div id="shield-depleted-panel" class="panel hidden">
            <h2>Shields Critical</h2>
            <p>Hull integrity compromised.</p>
            <button id="refill-btn">Refill Shields (Ad)</button>
            <button id="retreat-btn">Restart Sector</button>
        </div>

        <div id="levelcomplete-panel" class="panel hidden">
            <h2>Docking Successful</h2>
            <div id="current-stars" class="stars-display">☆☆☆</div>
            <div id="total-stars" class="total-stars">Total Stars: 0</div>
            <button id="replay-btn">Replay Level</button>
            <button id="next-btn">Next Level</button>
        </div>
        
        <div id="universe-gate-panel" class="panel hidden">
            <h2 id="ug-title">Universe 1 Cleared</h2>
            <p id="ug-desc">Gather 40 stars in this universe to activate the warp gate to Universe 2.</p>
            <div id="ug-stars" class="stars-display" style="font-size: 1.5rem; margin-bottom: 2rem;">Stars: 0/75</div>
            <button id="ug-grind-btn">Replay Universe 1</button>
            <button id="ug-warp-btn" class="hidden">Warp to Universe 2</button>
        </div>

        <div id="victory-panel" class="panel hidden">
            <h1>Mission Complete</h1>
            <p>All universes cleared successfully.</p>
            <div id="final-stars" class="stars-display" style="font-size: 1.5rem; margin-bottom: 2rem;">Total Stars: 0/150</div>
            <button id="restart-btn">Restart Fleet</button>
        </div>
    </div>
</div>

<script>
    const canvas = document.getElementById('gameCanvas');
    const ctx = canvas.getContext('2d');
    
    // UI Elements
    const menuPanel = document.getElementById('menu-panel');
    const gameoverPanel = document.getElementById('gameover-panel');
    const shieldDepletedPanel = document.getElementById('shield-depleted-panel');
    const levelcompletePanel = document.getElementById('levelcomplete-panel');
    const universeGatePanel = document.getElementById('universe-gate-panel');
    const victoryPanel = document.getElementById('victory-panel');
    const instruction = document.getElementById('instruction');
    
    const hudUniverse = document.getElementById('hud-universe');
    const hudSector = document.getElementById('hud-sector');
    const hudLevel = document.getElementById('hud-level');
    
    const currentStarsDisplay = document.getElementById('current-stars');
    const totalStarsDisplay = document.getElementById('total-stars');
    const hudStarsDisplay = document.getElementById('hud-stars');
    const hudShieldsDisplay = document.getElementById('hud-shields');

    // Game Constants
    const GAME_WIDTH = 400;
    const GAME_HEIGHT = 800;
    const G = 15; 
    const LAUNCH_SPEED = 9;
    const ROCKET_RADIUS = 8;
    const GOAL_WIDTH = GAME_WIDTH / 2.5;
    const GOAL_HEIGHT = 30;
    const ABERRATION_MULT = 12;
    
    const LEVELS_PER_SECTOR = 5;
    const TOTAL_LEVELS = 50; // 2 Universes * 25 levels

    // Game State
    let state = 'MENU'; 
    let currentLevel = 0;
    let animationId;
    let starsEarnedOnLevel = 0;
    let levelStars = new Array(TOTAL_LEVELS).fill(0); // Tracks best stars per level
    let maxShields = 5;
    let currentShields = 5;

    // Entities
    let rocket = { x: 0, y: 0, vx: 0, vy: 0, ax: 0, ay: 0, angle: -Math.PI / 2, warpX: 0 };
    let planets = [];
    let trail = [];
    let particles = [];
    let stars = [];
    let pulses = [];
    let currentLvlData = {};

    // Input
    let isDragging = false;
    let dragCurrent = { x: 0, y: 0 };
    let aimAngle = -Math.PI / 2;

    // Level Generator (Creates 50 deterministic levels across 2 universes)
    function generateLevels() {
        const generated = [];
        for (let u = 0; u < 2; u++) { // 2 Universes
            for (let i = 0; i < 25; i++) {
                let diff = (i / 25) + (u * 0.3); // Universe 2 scales to be generally harder
                let type = i % 5;  
                let pList = [];
                
                let startX = GAME_WIDTH / 2 + Math.sin((i + u*10) * 1.3) * 100;
                let goalX = (GAME_WIDTH / 2 - GOAL_WIDTH / 2) + Math.cos((i + u*10) * 1.7) * 80;
                
                // First 3 levels of Universe 1 are centered tutorials
                if (u === 0 && i < 3) {
                    startX = GAME_WIDTH / 2;
                    goalX = GAME_WIDTH / 2 - GOAL_WIDTH / 2;
                }

                // Generate Planets based on layout type (Modified slightly for Universe 2)
                if (type === 0) { // Corridors
                    pList.push({ x: 80, y: 400, r: 30 + diff*15, m: 100 + diff*100 });
                    pList.push({ x: 320, y: 400, r: 30 + diff*15, m: 100 + diff*100 });
                    if (diff > 0.5) pList.push({ x: 200 + (u*20), y: 200, r: 20, m: 80 + u*20 });
                } 
                else if (type === 1) { // Blockade
                    let blockX = GAME_WIDTH / 2 + Math.sin(i)*50;
                    pList.push({ x: blockX, y: 450, r: 40 + diff*20, m: 150 + diff*150 });
                    if (diff > 0.3) pList.push({ x: GAME_WIDTH - blockX, y: 250, r: 25 + u*5, m: 100 + u*50 });
                } 
                else if (type === 2) { // Zigzag
                    pList.push({ x: 120, y: 550, r: 30, m: 120 + diff*80 });
                    pList.push({ x: 280, y: 350, r: 30, m: 120 + diff*80 });
                    if (diff > 0.4) pList.push({ x: 120, y: 150, r: 30, m: 100 + u*40 });
                } 
                else if (type === 3) { // Slingshot (One Massive Body)
                    pList.push({ x: GAME_WIDTH / 2 + (i%2==0?60:-60), y: 400, r: 50 + diff*20, m: 300 + diff*200 + (u*100) });
                } 
                else if (type === 4) { // Asteroid Field
                    let count = Math.floor(3 + diff * 4) + u;
                    for (let j = 0; j < count; j++) {
                        let px = 80 + ((i * j * 31) % 240);
                        let py = 150 + ((i * j * 47) % 450);
                        pList.push({ x: px, y: py, r: 15 + (j%2)*5, m: 50 + diff*40 + (u*10) });
                    }
                }

                generated.push({ 
                    planets: pList, 
                    startX: startX, 
                    startY: GAME_HEIGHT - 60, 
                    goalX: goalX, 
                    goalY: 40 
                });
            }
        }
        return generated;
    }

    const levels = generateLevels();

    // Initialization
    function init() {
        generateStars();
        bindEvents();
        updateShieldDisplay();
        draw(); 
    }

    function updateShieldDisplay() {
        let shieldStr = "";
        for(let i=0; i<maxShields; i++) {
            shieldStr += (i < currentShields) ? "█" : "▒";
        }
        hudShieldsDisplay.innerText = shieldStr;
        
        if (currentShields > 2) {
            hudShieldsDisplay.style.color = "#fff";
        } else if (currentShields > 0) {
            hudShieldsDisplay.style.color = "#aaa";
        } else {
            hudShieldsDisplay.style.color = "#555";
        }
    }

    function generateStars() {
        stars = [];
        for (let i = 0; i < 150; i++) {
            stars.push({
                x: Math.random() * GAME_WIDTH,
                y: Math.random() * GAME_HEIGHT,
                size: Math.random() * 1.5,
                alpha: Math.random() * 0.8 + 0.2
            });
        }
    }

    function loadLevel(index) {
        currentLevel = index;
        currentLvlData = levels[currentLevel];
        
        // Calculate UI mapping based on structure
        let universeIdx = Math.floor(currentLevel / 25) + 1;
        let levelInUniverse = currentLevel % 25;
        let sectorIdx = Math.floor(levelInUniverse / LEVELS_PER_SECTOR) + 1;
        
        hudUniverse.innerText = universeIdx;
        hudSector.innerText = sectorIdx;
        hudLevel.innerText = currentLevel + 1;
        
        updateTotalStarsDisplay();
        
        rocket = {
            x: currentLvlData.startX,
            y: currentLvlData.startY,
            vx: 0, vy: 0, ax: 0, ay: 0,
            angle: -Math.PI / 2,
            warpX: 0
        };
        
        planets = currentLvlData.planets;
        trail = [];
        particles = [];
        pulses = []; 
        starsEarnedOnLevel = 0;
        
        state = 'AIMING';
        instruction.classList.remove('hidden');
        hideAllPanels();
        
        if (animationId) cancelAnimationFrame(animationId);
        gameLoop();
    }

    function hideAllPanels() {
        menuPanel.classList.add('hidden');
        gameoverPanel.classList.add('hidden');
        shieldDepletedPanel.classList.add('hidden');
        levelcompletePanel.classList.add('hidden');
        universeGatePanel.classList.add('hidden');
        victoryPanel.classList.add('hidden');
    }

    function updateTotalStarsDisplay() {
        let total = levelStars.reduce((a, b) => a + b, 0);
        hudStarsDisplay.innerText = total;
        totalStarsDisplay.innerText = `Total Stars: ${total} / 150`;
        document.getElementById('final-stars').innerText = `Total Stars: ${total} / 150`;
    }

    function gameOver(crashed) {
        state = 'GAMEOVER';
        instruction.classList.add('hidden');
        if (crashed) createExplosion(rocket.x, rocket.y);
        
        currentShields--;
        updateShieldDisplay();

        setTimeout(() => { 
            if (currentShields > 0) {
                gameoverPanel.classList.remove('hidden'); 
            } else {
                shieldDepletedPanel.classList.remove('hidden');
            }
        }, crashed ? 800 : 200);
    }

    function levelComplete(hitRatio) {
        state = 'LEVEL_COMPLETE';
        rocket.warpTimer = 0;
        rocket.warpX = rocket.x; 
        instruction.classList.add('hidden');
        
        if (hitRatio > 0.4 && hitRatio < 0.6) starsEarnedOnLevel = 3; 
        else if (hitRatio > 0.2 && hitRatio < 0.8) starsEarnedOnLevel = 2; 
        else starsEarnedOnLevel = 1; 
        
        levelStars[currentLevel] = Math.max(levelStars[currentLevel], starsEarnedOnLevel);
        
        let starText = "";
        for(let i=0; i<3; i++) {
            starText += (i < starsEarnedOnLevel) ? "★" : "☆";
        }
        currentStarsDisplay.innerText = starText;
        updateTotalStarsDisplay();

        createSuccessPulse(rocket.warpX, currentLvlData.goalY + GOAL_HEIGHT / 2);
        
        setTimeout(() => {
            // Universe 1 Check
            if (currentLevel === 24) {
                let u1Stars = levelStars.slice(0, 25).reduce((a, b) => a + b, 0);
                document.getElementById('ug-stars').innerText = `U1 Stars: ${u1Stars}/75`;
                
                if (u1Stars >= 40) {
                    document.getElementById('ug-desc').innerText = "Warp gate aligned. Ready for Universe 2.";
                    document.getElementById('ug-warp-btn').classList.remove('hidden');
                } else {
                    document.getElementById('ug-desc').innerText = "Insufficient stellar energy. Gather 40 stars in Universe 1 to unlock the gate.";
                    document.getElementById('ug-warp-btn').classList.add('hidden');
                }
                universeGatePanel.classList.remove('hidden');
            } 
            // End of Game Check
            else if (currentLevel === TOTAL_LEVELS - 1) {
                victoryPanel.classList.remove('hidden');
            } 
            // Normal Next Level
            else {
                levelcompletePanel.classList.remove('hidden');
            }
        }, 1500); 
    }

    function createSuccessPulse(x, y) {
        pulses.push({ x: x, y: y, r: 5, life: 1.0 });
        setTimeout(() => { pulses.push({ x: x, y: y, r: 5, life: 1.0 }); }, 150);
    }

    // Input Handling
    function bindEvents() {
        document.getElementById('start-btn').addEventListener('click', () => {
            currentShields = maxShields;
            updateShieldDisplay();
            loadLevel(0);
        });
        
        document.getElementById('retry-btn').addEventListener('click', () => loadLevel(currentLevel));
        
        document.getElementById('refill-btn').addEventListener('click', () => {
            currentShields = maxShields;
            updateShieldDisplay();
            loadLevel(currentLevel);
        });
        
        document.getElementById('retreat-btn').addEventListener('click', () => {
            currentShields = maxShields;
            updateShieldDisplay();
            // Jump to the first level of the current sector
            let sectorStart = Math.floor(currentLevel / LEVELS_PER_SECTOR) * LEVELS_PER_SECTOR;
            loadLevel(sectorStart);
        });
        
        document.getElementById('replay-btn').addEventListener('click', () => loadLevel(currentLevel));
        
        document.getElementById('next-btn').addEventListener('click', () => {
            currentShields = Math.min(currentShields + 1, maxShields);
            updateShieldDisplay();
            loadLevel(currentLevel + 1);
        });

        document.getElementById('ug-grind-btn').addEventListener('click', () => {
            currentShields = maxShields;
            updateShieldDisplay();
            loadLevel(0); // Restart Universe 1 to get more stars
        });
        
        document.getElementById('ug-warp-btn').addEventListener('click', () => {
            currentShields = maxShields;
            updateShieldDisplay();
            loadLevel(25); // Jump to Level 26 (Start of Universe 2)
        });
        
        document.getElementById('restart-btn').addEventListener('click', () => {
            levelStars = new Array(TOTAL_LEVELS).fill(0); 
            currentShields = maxShields;
            updateShieldDisplay();
            loadLevel(0);
        });

        const container = document.getElementById('game-container');
        
        const pointerDown = (e) => {
            if (state !== 'AIMING') return;
            e.preventDefault();
            isDragging = true;
            updateAim(e);
            instruction.classList.add('hidden');
        };

        const pointerMove = (e) => {
            if (!isDragging || state !== 'AIMING') return;
            e.preventDefault();
            updateAim(e);
        };

        const pointerUp = (e) => {
            if (!isDragging || state !== 'AIMING') return;
            e.preventDefault();
            isDragging = false;
            launchRocket();
        };

        container.addEventListener('mousedown', pointerDown);
        container.addEventListener('mousemove', pointerMove);
        window.addEventListener('mouseup', pointerUp);

        container.addEventListener('touchstart', pointerDown, {passive: false});
        container.addEventListener('touchmove', pointerMove, {passive: false});
        window.addEventListener('touchend', pointerUp);
    }

    function getPointerPos(e) {
        const rect = canvas.getBoundingClientRect();
        const clientX = e.touches ? (e.touches[0] ? e.touches[0].clientX : e.changedTouches[0].clientX) : e.clientX;
        const clientY = e.touches ? (e.touches[0] ? e.touches[0].clientY : e.changedTouches[0].clientY) : e.clientY;
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;
        
        return {
            x: (clientX - rect.left) * scaleX,
            y: (clientY - rect.top) * scaleY
        };
    }

    function updateAim(e) {
        const pos = getPointerPos(e);
        dragCurrent = pos;
        aimAngle = Math.atan2(pos.y - rocket.y, pos.x - rocket.x);
        rocket.angle = aimAngle;
    }

    function launchRocket() {
        state = 'FLYING';
        rocket.vx = Math.cos(aimAngle) * LAUNCH_SPEED;
        rocket.vy = Math.sin(aimAngle) * LAUNCH_SPEED;
    }

    // Physics and Updates
    function update() {
        if (state === 'FLYING') {
            let ax = 0;
            let ay = 0;

            for (let p of planets) {
                let dx = p.x - rocket.x;
                let dy = p.y - rocket.y;
                let distSq = dx * dx + dy * dy;
                let dist = Math.sqrt(distSq);

                if (dist < p.r + ROCKET_RADIUS) {
                    gameOver(true);
                    return;
                }

                let force = (G * p.m) / distSq;
                ax += force * (dx / dist);
                ay += force * (dy / dist);
            }

            rocket.ax = ax;
            rocket.ay = ay;

            rocket.vx += ax;
            rocket.vy += ay;
            rocket.x += rocket.vx;
            rocket.y += rocket.vy;
            rocket.angle = Math.atan2(rocket.vy, rocket.vx);

            if (trail.length === 0 || Math.hypot(trail[trail.length-1].x - rocket.x, trail[trail.length-1].y - rocket.y) > 4) {
                trail.push({ x: rocket.x, y: rocket.y, ax: rocket.ax, ay: rocket.ay });
                if (trail.length > 100) trail.shift();
            }

            if (Math.random() > 0.4) {
                particles.push({
                    x: rocket.x - Math.cos(rocket.angle) * 10,
                    y: rocket.y - Math.sin(rocket.angle) * 10,
                    vx: -rocket.vx * 0.15 + (Math.random() - 0.5),
                    vy: -rocket.vy * 0.15 + (Math.random() - 0.5),
                    ax: rocket.ax,
                    ay: rocket.ay,
                    life: 1.0
                });
            }

            if (rocket.x < -20 || rocket.x > GAME_WIDTH + 20 || rocket.y < -20 || rocket.y > GAME_HEIGHT + 20) {
                gameOver(false);
                return;
            }

            // AABB Collision check against the dynamic rectangular goal zone
            let closestX = Math.max(currentLvlData.goalX, Math.min(rocket.x, currentLvlData.goalX + GOAL_WIDTH));
            let closestY = Math.max(currentLvlData.goalY, Math.min(rocket.y, currentLvlData.goalY + GOAL_HEIGHT));
            let dxGoal = rocket.x - closestX;
            let dyGoal = rocket.y - closestY;
            
            if (dxGoal * dxGoal + dyGoal * dyGoal < ROCKET_RADIUS * ROCKET_RADIUS) {
                // Calculate where it hit on the goal line for star calculation
                let hitRatio = (closestX - currentLvlData.goalX) / GOAL_WIDTH;
                levelComplete(hitRatio);
                return;
            }
        }

        if (state === 'LEVEL_COMPLETE') {
            rocket.warpTimer++;
            
            if (rocket.warpTimer < 30) {
                let targetX = rocket.warpX;
                let targetY = currentLvlData.goalY + GOAL_HEIGHT / 2;
                rocket.x += (targetX - rocket.x) * 0.15;
                rocket.y += (targetY - rocket.y) * 0.15;
                
                let da = (-Math.PI/2) - rocket.angle;
                while (da > Math.PI) da -= Math.PI * 2;
                while (da < -Math.PI) da += Math.PI * 2;
                rocket.angle += da * 0.15;
                
                rocket.ax *= 0.6;
                rocket.ay *= 0.6;
            } 
            else {
                rocket.y -= (rocket.warpTimer - 20) * 1.5;
                
                if (Math.random() > 0.2) {
                    particles.push({
                        x: rocket.x + (Math.random() - 0.5) * 8, 
                        y: rocket.y + 10,
                        vx: 0, vy: Math.random() * 3 + 2,
                        ax: 0, ay: 0,
                        life: 0.8
                    });
                }
            }
        }

        for (let i = pulses.length - 1; i >= 0; i--) {
            let p = pulses[i];
            p.r += 6;
            p.life -= 0.03;
            if (p.life <= 0) pulses.splice(i, 1);
        }

        for (let i = particles.length - 1; i >= 0; i--) {
            let p = particles[i];
            p.x += p.vx;
            p.y += p.vy;
            p.life -= 0.025;
            if (p.life <= 0) particles.splice(i, 1);
        }
    }

    function createExplosion(x, y) {
        for (let i = 0; i < 40; i++) {
            let angle = Math.random() * Math.PI * 2;
            let speed = Math.random() * 5 + 1;
            particles.push({
                x: x, y: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                ax: Math.cos(angle), 
                ay: Math.sin(angle),
                life: 1.0
            });
        }
    }

    function drawRocketShape(x, y, angle) {
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(angle);
        ctx.beginPath();
        ctx.moveTo(12, 0); 
        ctx.lineTo(-8, 6);  
        ctx.lineTo(-4, 0);  
        ctx.lineTo(-8, -6); 
        ctx.closePath();
        ctx.fill();
        
        if (state === 'FLYING' || (state === 'LEVEL_COMPLETE' && rocket.warpTimer > 25)) {
            ctx.beginPath();
            ctx.moveTo(-4, 0);
            let flameLen = (state === 'LEVEL_COMPLETE') ? 25 + Math.random() * 15 : 15 + Math.random() * 5;
            ctx.lineTo(-flameLen, 0);
            ctx.lineWidth = 2;
            ctx.stroke();
        }
        ctx.restore();
    }

    function draw() {
        ctx.fillStyle = '#000000';
        ctx.fillRect(0, 0, GAME_WIDTH, GAME_HEIGHT);

        ctx.fillStyle = '#ffffff';
        stars.forEach(s => {
            ctx.globalAlpha = s.alpha;
            ctx.fillRect(s.x, s.y, s.size, s.size);
        });
        ctx.globalAlpha = 1.0;

        if (currentLvlData && currentLvlData.goalX !== undefined) {
            let gX = currentLvlData.goalX;
            let gY = currentLvlData.goalY;

            ctx.save();
            
            ctx.strokeStyle = '#ffffff';
            ctx.lineWidth = 1;

            ctx.setLineDash([4, 4]);
            ctx.lineDashOffset = -Date.now() * 0.01;
            ctx.beginPath();
            ctx.rect(gX, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); 
            ctx.rect(gX + GOAL_WIDTH * 0.8, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); 
            ctx.stroke();

            ctx.setLineDash([2, 2]);
            ctx.beginPath();
            ctx.rect(gX + GOAL_WIDTH * 0.2, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT);
            ctx.rect(gX + GOAL_WIDTH * 0.6, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT);
            ctx.stroke();

            ctx.setLineDash([]);
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.rect(gX + GOAL_WIDTH * 0.4, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT);
            ctx.stroke();

            let pulse = (Math.sin(Date.now() * 0.005) + 1) * 0.15;
            ctx.fillStyle = `rgba(255, 255, 255, ${pulse})`;
            ctx.fillRect(gX + GOAL_WIDTH * 0.4, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT);
            
            let bSize = 8;
            ctx.beginPath(); ctx.moveTo(gX, gY + bSize); ctx.lineTo(gX, gY); ctx.lineTo(gX + bSize, gY); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(gX + GOAL_WIDTH - bSize, gY); ctx.lineTo(gX + GOAL_WIDTH, gY); ctx.lineTo(gX + GOAL_WIDTH, gY + bSize); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(gX, gY + GOAL_HEIGHT - bSize); ctx.lineTo(gX, gY + GOAL_HEIGHT); ctx.lineTo(gX + bSize, gY + GOAL_HEIGHT); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(gX + GOAL_WIDTH - bSize, gY + GOAL_HEIGHT); ctx.lineTo(gX + GOAL_WIDTH, gY + GOAL_HEIGHT); ctx.lineTo(gX + GOAL_WIDTH, gY + GOAL_HEIGHT - bSize); ctx.stroke();

            ctx.restore();
        }

        planets.forEach(p => {
            ctx.save();
            ctx.translate(p.x, p.y);
            
            let waveScale = (Date.now() * 0.001) % 1;
            ctx.strokeStyle = `rgba(255, 255, 255, ${1 - waveScale})`;
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.arc(0, 0, p.r + (p.m * 0.2 * waveScale), 0, Math.PI * 2);
            ctx.stroke();

            ctx.fillStyle = '#000000';
            ctx.strokeStyle = '#ffffff';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.arc(0, 0, p.r, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();

            ctx.lineWidth = 0.5;
            ctx.beginPath();
            ctx.moveTo(-p.r * 0.5, 0); ctx.lineTo(p.r * 0.5, 0);
            ctx.moveTo(0, -p.r * 0.5); ctx.lineTo(0, p.r * 0.5);
            ctx.stroke();

            ctx.restore();
        });

        if (state === 'AIMING' && currentLvlData) {
            ctx.save();
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.3)';
            ctx.setLineDash([2, 4]);
            ctx.beginPath();
            ctx.arc(currentLvlData.startX, currentLvlData.startY, 20, 0, Math.PI*2);
            ctx.stroke();
            ctx.restore();
        }

        ctx.save();
        pulses.forEach(p => {
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
            ctx.strokeStyle = `rgba(255, 255, 255, ${p.life})`;
            ctx.lineWidth = 2;
            ctx.stroke();
        });
        ctx.restore();

        ctx.globalCompositeOperation = 'lighter';
        
        const channels = [
            { color: '#ff0000', mult: ABERRATION_MULT },
            { color: '#00ff00', mult: 0 },
            { color: '#0000ff', mult: -ABERRATION_MULT }
        ];

        if (trail.length > 1) {
            ctx.lineWidth = 1.5;
            ctx.setLineDash([]);
            
            channels.forEach(ch => {
                ctx.beginPath();
                let first = trail[0];
                ctx.moveTo(first.x + first.ax * ch.mult, first.y + first.ay * ch.mult);
                
                for (let i = 1; i < trail.length; i++) {
                    let pt = trail[i];
                    ctx.lineTo(pt.x + pt.ax * ch.mult, pt.y + pt.ay * ch.mult);
                }
                
                let grad = ctx.createLinearGradient(trail[0].x, trail[0].y, trail[trail.length-1].x, trail[trail.length-1].y);
                grad.addColorStop(0, 'rgba(0,0,0,0)');
                grad.addColorStop(1, ch.color);
                
                ctx.strokeStyle = grad;
                ctx.stroke();
            });
        }

        channels.forEach(ch => {
            ctx.fillStyle = ch.color;
            particles.forEach(p => {
                ctx.globalAlpha = Math.max(0, p.life);
                ctx.beginPath();
                ctx.rect((p.x - 1) + (p.ax * ch.mult * p.life), (p.y - 1) + (p.ay * ch.mult * p.life), 2, 2);
                ctx.fill();
            });
        });
        ctx.globalAlpha = 1.0;

        if (state !== 'GAMEOVER') {
            channels.forEach(ch => {
                ctx.fillStyle = ch.color;
                ctx.strokeStyle = ch.color;
                drawRocketShape(rocket.x + rocket.ax * ch.mult, rocket.y + rocket.ay * ch.mult, rocket.angle);
            });
        }

        ctx.globalCompositeOperation = 'source-over';

        if (state === 'AIMING' && isDragging) {
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(rocket.x, rocket.y);
            ctx.lineTo(dragCurrent.x, dragCurrent.y);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 4]);
            ctx.stroke();

            ctx.beginPath();
            ctx.arc(dragCurrent.x, dragCurrent.y, 6, 0, Math.PI*2);
            ctx.stroke();
            ctx.restore();
        }
    }

    function gameLoop() {
        update();
        draw();
        if (state !== 'MENU') {
            animationId = requestAnimationFrame(gameLoop);
        }
    }

    init();
</script>
</body>
</html>