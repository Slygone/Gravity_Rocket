# Original Game code:

import React, { useState, useEffect, useMemo, useRef, useCallback } from 'react';
import { Star, Lock, Unlock, Rocket, Shield, Gift, Map as MapIcon, ChevronRight, CheckCircle2, AlertTriangle, Crown, Wind, Play, RotateCcw, Box, ShoppingCart, Zap, Coins, CreditCard, Loader2 } from 'lucide-react';

// --- DATA STRUCTURES ---

const SHIPS = {
  shipA: { id: 'shipA', name: 'Starter Cruiser', perk: 'Standard issue.', color: 'text-white', hex: '#ffffff' },
  shipB: { id: 'shipB', name: 'Anti-Grav Skiff', perk: 'Reduced gravity effect.', color: 'text-purple-400', hex: '#c084fc' },
  shipC: { id: 'shipC', name: 'Nebula Piercer', perk: 'Immune to toxic nebulae.', color: 'text-green-400', hex: '#4ade80' }
};

const TRAILS = {
  trailDefault: { id: 'trailDefault', name: 'Default Exhaust', perk: 'Standard engine output.', color: 'text-slate-500', hex: ['#ff0000', '#00ff00', '#0000ff'], isChroma: true },
  trailBasic: { id: 'trailBasic', name: 'Basic Trail', perk: 'A simple ion exhaust.', color: 'text-gray-400', hex: ['#aaaaaa', '#ffffff', '#888888'], isChroma: false },
  trailNeon: { id: 'trailNeon', name: 'Neon Trail', perk: 'Bright and flashy.', color: 'text-pink-400', hex: ['#ff00ff', '#ff88ff', '#aa00aa'], isChroma: false },
  trailPlasma: { id: 'trailPlasma', name: 'Plasma Trail', perk: 'Superheated plasma.', color: 'text-blue-400', hex: ['#00ffff', '#88ffff', '#008888'], isChroma: false },
  trailStardust: { id: 'trailStardust', name: 'Stardust Trail', perk: 'Leaves a trail of stars.', color: 'text-yellow-400', hex: ['#ffff00', '#ffaa00', '#aaaa00'], isChroma: false },
  trailComet: { id: 'trailComet', name: 'Comet Trail', perk: 'Icy comet tail.', color: 'text-cyan-400', hex: ['#e0ffff', '#00ffff', '#00aaaa'], isChroma: false },
  trailSupernova: { id: 'trailSupernova', name: 'Supernova', perk: 'Explosive energy.', color: 'text-orange-500', hex: ['#ff4500', '#ff8c00', '#ff0000'], isChroma: false }
};

const BATTLE_PASS_TIERS = [
  { level: 1, starsReq: 10, free: { type: 'ship', name: 'Ship B (Anti-Grav)', id: 'shipB', credits: 10 }, premium: { type: 'skin', name: 'Elite Skin' } },
  { level: 2, starsReq: 20, free: { type: 'ship', name: 'Ship C (Nebula)', id: 'shipC' }, premium: { type: 'powerup', name: 'Mega Power Bundle', credits: 20 } },
  { level: 3, starsReq: 30, free: { type: 'powerup', name: 'Small Powerup', credits: 10 }, premium: { type: 'buff', name: 'Shield Capacity 6/6', id: 'buffShield' } },
  { level: 4, starsReq: 40, free: { type: 'skin', name: 'Rookie Skin' }, premium: { type: 'trail', name: 'Neon Trail', id: 'trailNeon', credits: 20 } },
  { level: 5, starsReq: 50, free: { type: 'skin', name: 'Veteran Skin', credits: 10 }, premium: { type: 'perk', name: 'Free Replay Token / Sector' } },
  { level: 6, starsReq: 60, free: { type: 'trail', name: 'Plasma Trail', id: 'trailPlasma' }, premium: { type: 'skin', name: 'Galactic Skin', credits: 20 } },
  { level: 7, starsReq: 70, free: { type: 'powerup', name: 'Medium Powerup', credits: 10 }, premium: { type: 'powerup', name: 'Ultra Power Bundle' } },
  { level: 8, starsReq: 80, free: { type: 'skin', name: 'Ace Skin' }, premium: { type: 'trail', name: 'Stardust Trail', id: 'trailStardust', credits: 20 } },
  { level: 9, starsReq: 90, free: { type: 'powerup', name: 'Large Powerup', credits: 10 }, premium: { type: 'skin', name: 'Cosmic Skin' } },
  { level: 10, starsReq: 100, free: { type: 'trail', name: 'Basic Trail', id: 'trailBasic' }, premium: { type: 'skin', name: 'Legendary Skin', credits: 20 } },
  { level: 11, starsReq: 110, free: { type: 'trail', name: 'Comet Trail', id: 'trailComet', credits: 10 }, premium: { type: 'powerup', name: 'Infinite Power Bundle' } },
  { level: 12, starsReq: 120, free: { type: 'skin', name: 'Master Skin' }, premium: { type: 'trail', name: 'Supernova Trail', id: 'trailSupernova', credits: 20 } },
];

const SECTOR_MECHANICS = {
  normal: { name: 'Normal Space', reqShip: null, warning: 'Standard physics apply.', color: 'border-white text-white' },
  highGrav: { name: 'High Gravity', reqShip: 'shipB', warning: 'High gravity detected. Ship B (Anti-Grav) recommended.', color: 'border-purple-400 text-purple-400' },
  nebula: { name: 'Toxic Nebula', reqShip: 'shipC', warning: 'Lethal gases. Ship C (Nebula Piercer) REQUIRED.', color: 'border-green-400 text-green-400' }
};

const getSectorMechanicsList = (sectorIndex) => {
  if (sectorIndex === 2) return ['highGrav'];
  if (sectorIndex === 3) return ['nebula'];
  if (sectorIndex === 4) return ['highGrav', 'nebula']; 
  if (sectorIndex >= 8) return ['nebula'];
  if (sectorIndex >= 5) return ['highGrav'];
  return ['normal'];
};

// --- SECTOR SUMMARY TERMINAL OVERLAY ---
const TerminalRow = ({ left, right, active, onComplete, isTotal = false }) => {
  const [charIndex, setCharIndex] = useState(0);
  const totalChars = left.length + right.length;

  useEffect(() => {
    if (!active) return;
    if (charIndex < totalChars) {
      const timer = setTimeout(() => setCharIndex(c => c + 1), 40);
      return () => clearTimeout(timer);
    } else if (onComplete) {
      const timer = setTimeout(onComplete, 200);
      return () => clearTimeout(timer);
    }
  }, [active, charIndex, totalChars, onComplete]);

  const displayLeft = left.slice(0, charIndex);
  const displayRight = charIndex > left.length ? right.slice(0, charIndex - left.length) : '';

  const isTypingLeft = active && charIndex < left.length;
  const isTypingRight = active && charIndex >= left.length && charIndex < totalChars;
  const isPerfectScore = right === '3 Stars' && charIndex >= totalChars;

  return (
    <div className={`flex justify-between items-center ${isTotal ? 'font-bold' : 'text-sm'}`}>
      <span className={`uppercase ${isTotal ? 'text-white' : 'text-white/70'}`}>
        {displayLeft}
        {isTypingLeft && <span className="w-2 h-3 bg-white/70 inline-block ml-1 align-baseline animate-pulse" />}
      </span>
      <span className={`text-white uppercase inline-block ${isPerfectScore ? 'rgb-notify' : ''}`}>
        {displayRight}
        {isTypingRight && <span className="w-2 h-3 bg-white inline-block ml-1 align-baseline animate-pulse" />}
      </span>
    </div>
  );
};

const SectorSummaryOverlay = ({ u, s, scores, onClose }) => {
  const [step, setStep] = useState(0); 
  let totalStars = 0;
  const lines = [1, 2, 3, 4, 5].map(l => {
    const st = scores[`${u}-${s}-${l}`] || 0;
    totalStars += st;
    return { id: l, left: `Level ${l}`, right: `${st} Star${st !== 1 ? 's' : ''}` };
  });

  return (
    <div className="absolute inset-0 flex items-center justify-center bg-black/90 backdrop-blur-sm z-[100] font-mono tracking-widest text-left text-white">
      <div className="text-left p-8 border border-white bg-black min-w-[340px] shadow-[0_0_20px_rgba(255,255,255,0.2)]">
         <h2 className="text-xl text-white uppercase mb-4 border-b border-white pb-2 flex items-center text-left">
           <ChevronRight className="w-5 h-5 mr-2 animate-pulse shrink-0" /> Sector {s} Report
         </h2>
         <div className="space-y-3 mb-8 min-h-[180px] text-left">
           {lines.map((line, idx) => (
             <TerminalRow key={line.id} left={line.left} right={line.right} active={step >= idx} onComplete={() => { if (step === idx) setStep(step + 1); }} />
           ))}
           <div className="mt-6 pt-4 border-t border-white/50 text-left">
             <TerminalRow left="Total Stars" right={`${totalStars}/15`} active={step >= 5} onComplete={() => { if (step === 5) setStep(6); }} isTotal={true} />
           </div>
         </div>
         {step >= 6 && (
           <button onClick={onClose} className="rgb-notify px-6 py-3 bg-white text-black border border-white hover:bg-transparent hover:text-white transition-colors font-bold uppercase tracking-widest w-full text-xs text-center">
             Sector Completed!
           </button>
         )}
      </div>
    </div>
  );
};

// --- GAME ENGINE COMPONENT ---
const GravityGame = ({ 
  levelIndex, universe, sector, levelInSector, 
  shipIds, trailId, maxShields,
  currentShields, setCurrentShields, onComplete, onExit 
}) => {
  const canvasRef = useRef(null);
  const resetLevelRef = useRef(() => {});
  const [gameState, setGameState] = useState('AIMING');
  const [activeShipId, setActiveShipId] = useState(shipIds[0]);
  const [isFleetOpen, setIsFleetOpen] = useState(false);
  const [starsEarned, setStarsEarned] = useState(0);
  const [hudMessage, setHudMessage] = useState('Drag to plot trajectory');

  useEffect(() => {
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    
    const GAME_WIDTH = 400; const GAME_HEIGHT = 800;
    const LAUNCH_SPEED = 9; const ROCKET_RADIUS = 8;
    const GOAL_WIDTH = GAME_WIDTH / 2.5; const GOAL_HEIGHT = 30;
    const ABERRATION_MULT = 12;

    const sectorMechanics = getSectorMechanicsList(sector);
    
    let G = activeShipId === 'shipB' ? 9 : 15;
    const isNebula = sectorMechanics.includes('nebula'); 
    let hullIntegrity = 100;

    let animationId;
    let localState = 'AIMING'; 
    let rocket = { x: 0, y: 0, vx: 0, vy: 0, ax: 0, ay: 0, angle: -Math.PI / 2, warpX: 0, warpTimer: 0 };
    let planets = []; let trail = []; let particles = []; let stars = []; let pulses = [];
    let isDragging = false; let dragCurrent = { x: 0, y: 0 }; let aimAngle = -Math.PI / 2;

    const generateLevels = () => {
        const generated = [];
        for (let u = 0; u < 2; u++) { 
            for (let i = 0; i < 25; i++) {
                let diff = (i / 25) + (u * 0.3); let type = i % 5;  
                let pList = []; let startX = GAME_WIDTH / 2 + Math.sin((i + u*10) * 1.3) * 100;
                let goalX = (GAME_WIDTH / 2 - GOAL_WIDTH / 2) + Math.cos((i + u*10) * 1.7) * 80;
                if (u === 0 && i < 3) { startX = GAME_WIDTH / 2; goalX = GAME_WIDTH / 2 - GOAL_WIDTH / 2; }
                if (type === 0) { pList.push({ x: 80, y: 400, r: 30 + diff*15, m: 100 + diff*100 }, { x: 320, y: 400, r: 30 + diff*15, m: 100 + diff*100 }); if (diff > 0.5) pList.push({ x: 200 + (u*20), y: 200, r: 20, m: 80 + u*20 }); } 
                else if (type === 1) { let blockX = GAME_WIDTH / 2 + Math.sin(i)*50; pList.push({ x: blockX, y: 450, r: 40 + diff*20, m: 150 + diff*150 }); if (diff > 0.3) pList.push({ x: GAME_WIDTH - blockX, y: 250, r: 25 + u*5, m: 100 + u*50 }); } 
                else if (type === 2) { pList.push({ x: 120, y: 550, r: 30, m: 120 + diff*80 }, { x: 280, y: 350, r: 30, m: 120 + diff*80 }); if (diff > 0.4) pList.push({ x: 120, y: 150, r: 30, m: 100 + u*40 }); } 
                else if (type === 3) { pList.push({ x: GAME_WIDTH / 2 + (i%2==0?60:-60), y: 400, r: 50 + diff*20, m: 300 + diff*200 + (u*100) }); } 
                else if (type === 4) { let count = Math.floor(3 + diff * 4) + u; for (let j = 0; j < count; j++) pList.push({ x: 80 + ((i * j * 31) % 240), y: 150 + ((i * j * 47) % 450), r: 15 + (j%2)*5, m: 50 + diff*40 + (u*10) }); }
                generated.push({ planets: pList, startX, startY: GAME_HEIGHT - 60, goalX, goalY: 40 });
            }
        }
        return generated;
    };

    const levelsData = generateLevels();
    const currentLvlData = levelsData[levelIndex] || levelsData[0];

    for (let i = 0; i < 150; i++) stars.push({ x: Math.random() * GAME_WIDTH, y: Math.random() * GAME_HEIGHT, size: Math.random() * 1.5, alpha: Math.random() * 0.8 + 0.2 });
    rocket = { x: currentLvlData.startX, y: currentLvlData.startY, vx: 0, vy: 0, ax: 0, ay: 0, angle: -Math.PI / 2, warpX: 0, warpTimer: 0 };
    planets = currentLvlData.planets;

    resetLevelRef.current = () => {
        rocket = { x: currentLvlData.startX, y: currentLvlData.startY, vx: 0, vy: 0, ax: 0, ay: 0, angle: -Math.PI / 2, warpX: 0, warpTimer: 0 };
        trail = []; particles = []; pulses = []; hullIntegrity = 100; localState = 'AIMING'; setGameState('AIMING'); setHudMessage('Drag to plot trajectory');
    };

    const getPointerPos = (e) => {
        const rect = canvas.getBoundingClientRect();
        const cX = e.touches ? (e.touches[0] ? e.touches[0].clientX : e.changedTouches[0].clientX) : e.clientX;
        const cY = e.touches ? (e.touches[0] ? e.touches[0].clientY : e.changedTouches[0].clientY) : e.clientY;
        return { x: (cX - rect.left) * (GAME_WIDTH / rect.width), y: (cY - rect.top) * (GAME_HEIGHT / rect.height) };
    };

    const handleDown = (e) => { if (localState !== 'AIMING' || isFleetOpen) return; isDragging = true; setHudMessage(''); updateAim(e); };
    const handleMove = (e) => { if (!isDragging || localState !== 'AIMING') return; updateAim(e); };
    const handleUp = () => { if (!isDragging || localState !== 'AIMING') return; isDragging = false; localState = 'FLYING'; setGameState('FLYING'); rocket.vx = Math.cos(aimAngle) * LAUNCH_SPEED; rocket.vy = Math.sin(aimAngle) * LAUNCH_SPEED; };
    const updateAim = (e) => { dragCurrent = getPointerPos(e); aimAngle = Math.atan2(dragCurrent.y - rocket.y, dragCurrent.x - rocket.x); rocket.angle = aimAngle; };

    canvas.addEventListener('mousedown', handleDown); canvas.addEventListener('mousemove', handleMove); window.addEventListener('mouseup', handleUp);
    canvas.addEventListener('touchstart', handleDown, {passive: false}); canvas.addEventListener('touchmove', handleMove, {passive: false}); window.addEventListener('touchend', handleUp);

    const triggerGameOver = (crashed) => {
        localState = 'GAMEOVER'; setGameState('GAMEOVER'); setCurrentShields(prev => Math.max(0, prev - 1));
        if (crashed) for (let i = 0; i < 40; i++) { let a = Math.random() * Math.PI * 2; let s = Math.random() * 5 + 1; particles.push({ x: rocket.x, y: rocket.y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, ax: Math.cos(a), ay: Math.sin(a), life: 1.0 }); }
    };

    const triggerSuccess = (hitRatio) => {
        localState = 'SUCCESS'; rocket.warpTimer = 0; rocket.warpX = rocket.x; 
        let earned = 1; if (hitRatio > 0.4 && hitRatio < 0.6) earned = 3; else if (hitRatio > 0.2 && hitRatio < 0.8) earned = 2;
        setStarsEarned(earned);
        pulses.push({ x: rocket.warpX, y: currentLvlData.goalY + GOAL_HEIGHT / 2, r: 5, life: 1.0 }); setTimeout(() => { pulses.push({ x: rocket.warpX, y: currentLvlData.goalY + GOAL_HEIGHT / 2, r: 5, life: 1.0 }); }, 150);
        setTimeout(() => setGameState('SUCCESS'), 1000);
    };

    const update = () => {
        if (localState === 'FLYING') {
            let ax = 0, ay = 0;
            for (let p of planets) {
                let dx = p.x - rocket.x; let dy = p.y - rocket.y; let distSq = dx * dx + dy * dy; let dist = Math.sqrt(distSq);
                if (dist < p.r + ROCKET_RADIUS) return triggerGameOver(true);
                let force = (G * p.m) / distSq; ax += force * (dx / dist); ay += force * (dy / dist);
            }
            if (isNebula && activeShipId !== 'shipC') { hullIntegrity -= 0.5; if (hullIntegrity <= 0) return triggerGameOver(true); }

            rocket.ax = ax; rocket.ay = ay; rocket.vx += ax; rocket.vy += ay; rocket.x += rocket.vx; rocket.y += rocket.vy; rocket.angle = Math.atan2(rocket.vy, rocket.vx);
            if (trail.length === 0 || Math.hypot(trail[trail.length-1].x - rocket.x, trail[trail.length-1].y - rocket.y) > 4) { trail.push({ x: rocket.x, y: rocket.y, ax: rocket.ax, ay: rocket.ay }); if (trail.length > 100) trail.shift(); }
            if (Math.random() > 0.4) particles.push({ x: rocket.x - Math.cos(rocket.angle) * 10, y: rocket.y - Math.sin(rocket.angle) * 10, vx: -rocket.vx * 0.15 + (Math.random() - 0.5), vy: -rocket.vy * 0.15 + (Math.random() - 0.5), ax: rocket.ax, ay: rocket.ay, life: 1.0 });
            if (rocket.x < -20 || rocket.x > GAME_WIDTH + 20 || rocket.y < -20 || rocket.y > GAME_HEIGHT + 20) return triggerGameOver(false);

            let closestX = Math.max(currentLvlData.goalX, Math.min(rocket.x, currentLvlData.goalX + GOAL_WIDTH)); let closestY = Math.max(currentLvlData.goalY, Math.min(rocket.y, currentLvlData.goalY + GOAL_HEIGHT));
            if ((rocket.x - closestX) ** 2 + (rocket.y - closestY) ** 2 < ROCKET_RADIUS * ROCKET_RADIUS) return triggerSuccess((closestX - currentLvlData.goalX) / GOAL_WIDTH);
        }

        if (localState === 'SUCCESS') {
            rocket.warpTimer++;
            if (rocket.warpTimer < 30) {
                let targetX = rocket.warpX; let targetY = currentLvlData.goalY + GOAL_HEIGHT / 2;
                rocket.x += (targetX - rocket.x) * 0.15; rocket.y += (targetY - rocket.y) * 0.15;
                let da = (-Math.PI/2) - rocket.angle; while (da > Math.PI) da -= Math.PI * 2; while (da < -Math.PI) da += Math.PI * 2;
                rocket.angle += da * 0.15; rocket.ax *= 0.6; rocket.ay *= 0.6;
            } else { rocket.y -= (rocket.warpTimer - 20) * 1.5; if (Math.random() > 0.2) particles.push({ x: rocket.x + (Math.random() - 0.5) * 8, y: rocket.y + 10, vx: 0, vy: Math.random() * 3 + 2, ax: 0, ay: 0, life: 0.8 }); }
        }

        for (let i = pulses.length - 1; i >= 0; i--) { pulses[i].r += 6; pulses[i].life -= 0.03; if (pulses[i].life <= 0) pulses.splice(i, 1); }
        for (let i = particles.length - 1; i >= 0; i--) { particles[i].x += particles[i].vx; particles[i].y += particles[i].vy; particles[i].life -= 0.025; if (particles[i].life <= 0) particles.splice(i, 1); }
    };

    const draw = () => {
        ctx.fillStyle = '#000000'; ctx.fillRect(0, 0, GAME_WIDTH, GAME_HEIGHT);
        if (isNebula) { ctx.fillStyle = `rgba(100, 0, 150, ${0.1 + Math.sin(Date.now() * 0.001)*0.05})`; ctx.fillRect(0, 0, GAME_WIDTH, GAME_HEIGHT); if (activeShipId !== 'shipC') { ctx.fillStyle = `rgba(255, 0, 0, ${(100 - hullIntegrity) / 100 * 0.5})`; ctx.fillRect(0, 0, GAME_WIDTH, GAME_HEIGHT); } }

        ctx.fillStyle = '#ffffff'; stars.forEach(s => { ctx.globalAlpha = s.alpha; ctx.fillRect(s.x, s.y, s.size, s.size); }); ctx.globalAlpha = 1.0;
        let gX = currentLvlData.goalX; let gY = currentLvlData.goalY; ctx.save(); ctx.strokeStyle = '#ffffff'; ctx.lineWidth = 1; ctx.setLineDash([4, 4]); ctx.lineDashOffset = -Date.now() * 0.01; ctx.beginPath(); ctx.rect(gX, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.rect(gX + GOAL_WIDTH * 0.8, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.stroke(); ctx.setLineDash([2, 2]); ctx.beginPath(); ctx.rect(gX + GOAL_WIDTH * 0.2, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.rect(gX + GOAL_WIDTH * 0.6, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.stroke(); ctx.setLineDash([]); ctx.lineWidth = 2; ctx.beginPath(); ctx.rect(gX + GOAL_WIDTH * 0.4, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.stroke(); let pulse = (Math.sin(Date.now() * 0.005) + 1) * 0.15; ctx.fillStyle = `rgba(255, 255, 255, ${pulse})`; ctx.fillRect(gX + GOAL_WIDTH * 0.4, gY, GOAL_WIDTH * 0.2, GOAL_HEIGHT); ctx.restore();

        planets.forEach(p => {
            ctx.save(); ctx.translate(p.x, p.y); let waveScale = (Date.now() * 0.001) % 1; ctx.strokeStyle = `rgba(255, 255, 255, ${1 - waveScale})`; ctx.lineWidth = 1; ctx.beginPath(); ctx.arc(0, 0, p.r + (p.m * 0.2 * waveScale), 0, Math.PI * 2); ctx.stroke(); ctx.fillStyle = '#000000'; ctx.strokeStyle = '#ffffff'; ctx.lineWidth = 2; ctx.beginPath(); ctx.arc(0, 0, p.r, 0, Math.PI * 2); ctx.fill(); ctx.stroke(); ctx.lineWidth = 0.5; ctx.beginPath(); ctx.moveTo(-p.r * 0.5, 0); ctx.lineTo(p.r * 0.5, 0); ctx.moveTo(0, -p.r * 0.5); ctx.lineTo(0, p.r * 0.5); ctx.stroke(); ctx.restore();
        });

        if (localState === 'AIMING') { ctx.save(); ctx.strokeStyle = 'rgba(255, 255, 255, 0.3)'; ctx.setLineDash([2, 4]); ctx.beginPath(); ctx.arc(currentLvlData.startX, currentLvlData.startY, 20, 0, Math.PI*2); ctx.stroke(); ctx.restore(); }
        ctx.save(); pulses.forEach(p => { ctx.beginPath(); ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2); ctx.strokeStyle = `rgba(255, 255, 255, ${p.life})`; ctx.lineWidth = 2; ctx.stroke(); }); ctx.restore();
        ctx.globalCompositeOperation = 'lighter';
        
        const activeTrailObj = TRAILS[trailId];
        const activeShipHex = SHIPS[activeShipId].hex;

        const channels = activeTrailObj.isChroma ? [ 
          { color: activeTrailObj.hex[0], mult: ABERRATION_MULT }, 
          { color: activeTrailObj.hex[1], mult: 0 }, 
          { color: activeTrailObj.hex[2], mult: -ABERRATION_MULT } 
        ] : [ 
          { color: activeShipHex, mult: 3 }, 
          { color: activeShipHex, mult: 0 }, 
          { color: activeShipHex, mult: -3 } 
        ];

        if (trail.length > 1) {
            ctx.lineWidth = 1.5; ctx.setLineDash([]);
            channels.forEach(ch => {
                ctx.beginPath(); let first = trail[0]; ctx.moveTo(first.x + first.ax * ch.mult, first.y + first.ay * ch.mult);
                for (let i = 1; i < trail.length; i++) { let pt = trail[i]; ctx.lineTo(pt.x + pt.ax * ch.mult, pt.y + pt.ay * ch.mult); }
                let grad = ctx.createLinearGradient(trail[0].x, trail[0].y, trail[trail.length-1].x, trail[trail.length-1].y); grad.addColorStop(0, 'rgba(0,0,0,0)'); grad.addColorStop(1, ch.color); ctx.strokeStyle = grad; ctx.stroke();
            });
        }

        channels.forEach(ch => { ctx.fillStyle = ch.color; particles.forEach(p => { ctx.globalAlpha = Math.max(0, p.life); ctx.beginPath(); ctx.rect((p.x - 1) + (p.ax * ch.mult * p.life), (p.y - 1) + (p.ay * ch.mult * p.life), 2, 2); ctx.fill(); }); }); ctx.globalAlpha = 1.0;

        if (localState !== 'GAMEOVER') {
            channels.forEach(ch => {
                ctx.fillStyle = ch.color; ctx.strokeStyle = ch.color; ctx.save(); ctx.translate(rocket.x + rocket.ax * ch.mult, rocket.y + rocket.ay * ch.mult); ctx.rotate(rocket.angle);
                ctx.beginPath(); ctx.moveTo(12, 0); ctx.lineTo(-8, 6); ctx.lineTo(-4, 0); ctx.lineTo(-8, -6); ctx.closePath(); ctx.fill();
                if (localState === 'FLYING' || (localState === 'SUCCESS' && rocket.warpTimer > 25)) { ctx.beginPath(); ctx.moveTo(-4, 0); let flameLen = (localState === 'SUCCESS') ? 25 + Math.random() * 15 : 15 + Math.random() * 5; ctx.lineTo(-flameLen, 0); ctx.lineWidth = 2; ctx.stroke(); }
                ctx.restore();
            });
        }

        ctx.globalCompositeOperation = 'source-over';
        if (localState === 'AIMING' && isDragging) { ctx.save(); ctx.beginPath(); ctx.moveTo(rocket.x, rocket.y); ctx.lineTo(dragCurrent.x, dragCurrent.y); ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)'; ctx.lineWidth = 1; ctx.setLineDash([4, 4]); ctx.stroke(); ctx.beginPath(); ctx.arc(dragCurrent.x, dragCurrent.y, 6, 0, Math.PI*2); ctx.stroke(); ctx.restore(); }
    };

    const loop = () => { update(); draw(); animationId = requestAnimationFrame(loop); };
    loop();
    return () => { cancelAnimationFrame(animationId); canvas.removeEventListener('mousedown', handleDown); canvas.removeEventListener('mousemove', handleMove); window.removeEventListener('mouseup', handleUp); };
  }, [levelIndex, shipIds, activeShipId, trailId, universe, sector, isFleetOpen]);

  return (
    <div className="absolute inset-0 bg-black flex justify-center items-center z-50">
      <div className="relative w-full h-full max-w-[500px] aspect-[9/16] bg-black border-l border-r border-slate-800 text-left">
        <canvas ref={canvasRef} width={400} height={800} className="block w-full h-full" />
        
        <div className="absolute top-4 left-4 font-mono text-white tracking-widest text-sm pointer-events-none z-10 text-left">
          U{universe} - SEC {sector} - LVL {levelInSector}
          <div className="text-slate-400 text-xs mt-1 uppercase text-left">Shield: <span className={currentShields > 0 ? "text-white" : "text-red-500"}>{'█'.repeat(currentShields)}{'▒'.repeat(maxShields - currentShields)}</span></div>
          <div className={`text-[10px] mt-1 font-bold uppercase transition-colors text-left ${SHIPS[activeShipId].color}`}>Fleet: {SHIPS[activeShipId].name}</div>
        </div>

        {gameState === 'AIMING' && (
          <div className="absolute inset-0 pointer-events-none transition-opacity text-center text-white">
            <div className="absolute bottom-24 w-full text-center text-white/50 font-mono text-xs uppercase tracking-widest">
              {hudMessage}
            </div>
            
            <button 
              onClick={() => setIsFleetOpen(true)}
              className="absolute bottom-6 left-6 px-4 py-3 border border-white bg-black hover:bg-white hover:text-black text-white font-mono text-[10px] uppercase tracking-widest pointer-events-auto transition-colors flex items-center text-center"
            >
              <Rocket className="w-4 h-4 mr-2" /> Fleet
            </button>

            {isFleetOpen && (
              <div className="absolute inset-0 bg-black/80 flex items-center justify-center z-[60] pointer-events-auto backdrop-blur-sm text-left text-white">
                <div className="border border-white bg-black p-8 w-[80%] max-w-[320px] shadow-[0_0_30px_rgba(255,255,255,0.2)]">
                  <h3 className="text-white font-mono text-sm uppercase tracking-widest mb-6 border-b border-white pb-2 text-left">Select Vessel</h3>
                  <div className="flex flex-col space-y-3 text-left">
                    {shipIds.map(id => (
                      <button 
                        key={id}
                        onClick={() => { setActiveShipId(id); setIsFleetOpen(false); }}
                        className={`p-4 border transition-all flex items-center justify-between text-left ${activeShipId === id ? 'bg-white border-white text-black' : 'bg-black border-white/30 text-white hover:border-white'}`}
                      >
                        <div className="flex items-center text-left">
                          <Rocket className={`w-5 h-5 mr-3 shrink-0 ${activeShipId === id ? 'text-black' : SHIPS[id].color}`} />
                          <span className={`text-[10px] font-bold uppercase text-left ${activeShipId === id ? 'text-black' : 'text-white'}`}>
                            {SHIPS[id].name.split(' ')[0]}
                          </span>
                        </div>
                        {activeShipId === id && <CheckCircle2 className="w-4 h-4 text-black shrink-0" />}
                      </button>
                    ))}
                  </div>
                  <button 
                    onClick={() => setIsFleetOpen(false)}
                    className="w-full mt-8 py-3 border border-white/30 text-white/50 hover:bg-white hover:text-black hover:border-white transition-colors text-[10px] uppercase font-bold tracking-widest text-center"
                  >
                    Close
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {gameState === 'GAMEOVER' && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/80 backdrop-blur-sm z-20 text-center text-white">
            <div className="text-center p-8 border border-white bg-black min-w-[280px]">
              <h2 className="text-xl text-white tracking-widest uppercase mb-2">Signal Lost</h2>
              <p className="text-slate-400 text-sm mb-6 font-mono tracking-widest uppercase text-center">Trajectory compromised.</p>
              {currentShields > 0 ? (
                <button onClick={() => resetLevelRef.current()} className="px-6 py-3 border border-white text-white hover:bg-white hover:text-black transition-colors font-mono uppercase tracking-widest w-full text-center">
                  Recalculate
                </button>
              ) : (
                <div className="space-y-3 text-center">
                  <p className="text-red-400 text-xs font-bold mb-4 uppercase text-center">Shields Depleted</p>
                  <button onClick={() => { setCurrentShields(maxShields); resetLevelRef.current(); }} className="px-6 py-3 border border-white text-white hover:bg-white hover:text-black transition-colors font-mono uppercase tracking-widest w-full flex items-center justify-center text-center">
                    <Play className="w-4 h-4 mr-2" /> Refill Shields (Ad)
                  </button>
                  <button onClick={onExit} className="px-6 py-3 border border-white/30 text-white/70 hover:bg-white hover:text-black hover:border-white transition-colors font-mono uppercase tracking-widest w-full text-center">
                    Retreat to Sector
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {gameState === 'SUCCESS' && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/80 backdrop-blur-sm z-20 text-center text-white">
            <div className="text-center p-8 border border-white bg-black min-w-[280px]">
              <h2 className="text-xl text-white tracking-widest uppercase mb-4 tracking-tighter text-center">Docking Successful</h2>
              <div className="flex justify-center space-x-2 mb-6 text-center">
                 {[1, 2, 3].map(s => <Star key={s} className={`w-10 h-10 ${s <= starsEarned ? 'text-white fill-current' : 'text-slate-700'} shrink-0`} />)}
              </div>
              <button onClick={() => onComplete(starsEarned)} className="px-6 py-3 bg-white text-black hover:bg-slate-200 transition-colors font-mono font-bold uppercase tracking-widest w-full flex items-center justify-center text-xs text-center">
                Continue <ChevronRight className="w-5 h-5 ml-2 shrink-0" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};


// --- WEBGL BACKGROUND COMPONENT ---
const WebGLBackground = () => {
  const mountRef = useRef(null);

  useEffect(() => {
    let scene, camera, renderer, animationId, stars, planet;

    const init = () => {
      if (!window.THREE) return;
      scene = new window.THREE.Scene();
      camera = new window.THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
      renderer = new window.THREE.WebGLRenderer({ alpha: true, antialias: true });
      renderer.setSize(window.innerWidth, window.innerHeight);
      if (mountRef.current) { mountRef.current.innerHTML = ''; mountRef.current.appendChild(renderer.domElement); }

      const starGeo = new window.THREE.BufferGeometry();
      const starMat = new window.THREE.PointsMaterial({ color: 0xffffff, size: 0.1 });
      const starVerts = [];
      for(let i = 0; i < 2000; i++) starVerts.push((Math.random() - 0.5) * 100, (Math.random() - 0.5) * 100, (Math.random() - 0.5) * 100);
      starGeo.setAttribute('position', new window.THREE.Float32BufferAttribute(starVerts, 3));
      stars = new window.THREE.Points(starGeo, starMat);
      scene.add(stars);

      planet = new window.THREE.Mesh(new window.THREE.SphereGeometry(15, 32, 32), new window.THREE.MeshBasicMaterial({ color: 0xffffff, wireframe: true, transparent: true, opacity: 0.1 }));
      planet.position.set(20, -10, -30);
      scene.add(planet);

      camera.position.z = 30;

      const animate = () => {
        animationId = requestAnimationFrame(animate);
        stars.rotation.y += 0.0005; stars.rotation.x += 0.0002; planet.rotation.y += 0.002;
        renderer.render(scene, camera);
      };
      animate();
    };

    if (!window.THREE) {
      const script = document.createElement('script'); script.src = 'https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js';
      script.onload = init; document.body.appendChild(script);
    } else init();

    const handleResize = () => {
      if (camera && renderer) { camera.aspect = window.innerWidth / window.innerHeight; camera.updateProjectionMatrix(); renderer.setSize(window.innerWidth, window.innerHeight); }
    };
    window.addEventListener('resize', handleResize);
    return () => { window.removeEventListener('resize', handleResize); if (animationId) cancelAnimationFrame(animationId); if (renderer) renderer.dispose(); };
  }, []);

  return <div ref={mountRef} className="fixed inset-0 z-0 pointer-events-none bg-black" />;
};


// --- MAIN APP COMPONENT ---
export default function App() {
  const [view, setView] = useState('map'); 
  const [levelScores, setLevelScores] = useState({}); 
  const [isPremium, setIsPremium] = useState(false);
  const [activeShips, setActiveShips] = useState(['shipA']); 
  const [activeTrail, setActiveTrail] = useState('trailDefault');
  const [currentUniverse, setCurrentUniverse] = useState(1);
  const [activeSector, setActiveSector] = useState(null); 
  const [claimedRewards, setClaimedRewards] = useState([]);
  const [credits, setCredits] = useState(0); 
  
  // Game Session State
  const [playSession, setPlaySession] = useState(null); 
  const [pendingLoadout, setPendingLoadout] = useState(null); 
  const [adOverlay, setAdOverlay] = useState(false);
  const [purchaseOverlay, setPurchaseOverlay] = useState(null); // { amount, price }
  const [globalShields, setGlobalShields] = useState(5); 
  const [sectorSummary, setSectorSummary] = useState(null); 

  // Derived State
  const totalStars = useMemo(() => Object.values(levelScores).reduce((sum, val) => sum + val, 0), [levelScores]);
  const accountLevel = Math.floor(totalStars / 10);
  
  const unlockedShips = useMemo(() => {
    const ships = ['shipA'];
    BATTLE_PASS_TIERS.forEach(tier => { if (accountLevel >= tier.level && tier.free.type === 'ship') ships.push(tier.free.id); });
    return ships;
  }, [accountLevel]);

  const unlockedTrails = useMemo(() => {
    const trails = ['trailDefault'];
    BATTLE_PASS_TIERS.forEach(tier => {
      if (accountLevel >= tier.level && tier.free.type === 'trail') trails.push(tier.free.id);
      if (accountLevel >= tier.level && isPremium && tier.premium.type === 'trail') trails.push(tier.premium.id);
    });
    return trails;
  }, [accountLevel, isPremium]);

  const hasShieldBuff = useMemo(() => {
     return claimedRewards.includes(BATTLE_PASS_TIERS.find(t => t.premium.id === 'buffShield')?.level + '-premium');
  }, [claimedRewards]);

  const maxShields = hasShieldBuff ? 6 : 5;

  const hasUnclaimedRewards = useMemo(() => {
    return BATTLE_PASS_TIERS.some(tier => {
      if (accountLevel >= tier.level) {
        const freeUnclaimed = !claimedRewards.includes(`${tier.level}-free`);
        const premiumUnclaimed = isPremium && !claimedRewards.includes(`${tier.level}-premium`);
        return freeUnclaimed || premiumUnclaimed;
      }
      return false;
    });
  }, [accountLevel, claimedRewards, isPremium]);

  const universe1Cleared = useMemo(() => {
    for (let s = 1; s <= 10; s++) { for (let l = 1; l <= 5; l++) { if (!levelScores[`1-${s}-${l}`]) return false; } }
    return true;
  }, [levelScores]);

  const canEnterUniverse2 = universe1Cleared && totalStars >= 100;

  // --- ACTIONS ---
  const launchLevel = (u, s, l, isMenuLaunch = false) => {
    if (isMenuLaunch) setGlobalShields(maxShields); 
    const globalIndex = (u - 1) * 25 + (s - 1) * 5 + (l - 1);
    setPlaySession({ u, s, l, index: globalIndex, sessionKey: Date.now() }); 
  };

  const simulatePurchase = (pack) => {
    setPurchaseOverlay(pack);
    setTimeout(() => {
        setCredits(prev => prev + pack.amount);
        setPurchaseOverlay(null);
    }, 2000); // 2s processing time
  };

  const executeLaunch = () => {
     if (pendingLoadout.requiresAd) {
         setAdOverlay(true);
         setTimeout(() => {
             setAdOverlay(false);
             launchLevel(pendingLoadout.u, pendingLoadout.s, pendingLoadout.l, pendingLoadout.isMenuLaunch);
             setPendingLoadout(null);
         }, 1500); 
     } else {
         launchLevel(pendingLoadout.u, pendingLoadout.s, pendingLoadout.l, pendingLoadout.isMenuLaunch);
         setPendingLoadout(null);
     }
  }

  const handleLevelComplete = (starsEarned) => {
    const { u, s, l } = playSession;
    const key = `${u}-${s}-${l}`;
    const currentStars = levelScores[key] || 0;
    
    let newScores = { ...levelScores };
    if (starsEarned > currentStars) {
        newScores[key] = starsEarned;
        setLevelScores(newScores);
    }
    
    setGlobalShields(prev => Math.min(maxShields, prev + 1));

    if (l < 5) {
      launchLevel(u, s, l + 1, false);
    } else {
      setPlaySession(null); 
      setSectorSummary({ u, s, scores: newScores }); 
    }
  };

  const claimReward = (tierLevel, isPremiumReward) => {
    const rewardId = `${tierLevel}-${isPremiumReward ? 'premium' : 'free'}`;
    if (!claimedRewards.includes(rewardId)) {
        const tier = BATTLE_PASS_TIERS.find(t => t.level === tierLevel);
        const rewardData = isPremiumReward ? tier.premium : tier.free;
        
        if (rewardData.credits) {
            setCredits(prev => prev + rewardData.credits);
        }

        setClaimedRewards(prev => [...prev, rewardId]);
    }
  };

  const toggleShipEquip = (id) => {
    setActiveShips(prev => {
      if (prev.includes(id)) return prev.length > 1 ? prev.filter(s => s !== id) : prev;
      return [...prev, id];
    });
  };

  // --- RENDERERS ---
  const renderTopNav = () => (
    <div className="relative z-10 flex flex-wrap items-center justify-between p-4 bg-black border-b border-white font-mono tracking-widest text-left text-white">
      <div className="flex space-x-4 items-center text-left">
        <div className="flex items-center text-white font-bold text-lg uppercase text-left">
          <Star className="w-5 h-5 mr-1.5 fill-current shrink-0 text-white" /> {totalStars}
        </div>
        <div className="flex items-center text-white font-bold text-lg uppercase text-left">
          <Shield className="w-5 h-5 mr-1.5 shrink-0 text-white" /> LVL {accountLevel}
        </div>
        <button 
          onClick={() => setView('shop')}
          className="flex items-center text-white font-bold text-lg uppercase hover:bg-white/10 px-2 -ml-2 py-1 transition-colors group shrink-0 text-left"
        >
          <Coins className="w-5 h-5 mr-1.5 text-yellow-400 group-hover:scale-110 transition-transform shrink-0" /> 
          {credits}
          <span className="ml-1.5 text-green-400 font-bold group-hover:animate-pulse">+</span>
        </button>
      </div>

      <div className="flex space-x-2 overflow-x-auto text-center">
        <button onClick={() => setView('map')} className={`px-3 py-2 border text-[10px] font-bold uppercase tracking-widest flex items-center transition-colors ${view === 'map' ? 'bg-white text-black border-white' : 'bg-black text-white border-white hover:bg-white hover:text-black'}`}><MapIcon className="w-4 h-4 mr-2 shrink-0" /> Map</button>
        <button onClick={() => setView('battlepass')} className={`px-3 py-2 border text-[10px] font-bold uppercase tracking-widest flex items-center transition-colors ${view === 'battlepass' ? 'bg-white text-black border-white' : 'bg-black text-white border-white hover:bg-white hover:text-black'} ${hasUnclaimedRewards && view !== 'battlepass' ? 'rgb-notify' : ''}`}>
          <Gift className="w-4 h-4 mr-2 shrink-0" /> Pass
          {hasUnclaimedRewards && view !== 'battlepass' && <span className="ml-1 w-1.5 h-1.5 bg-white rounded-full animate-pulse shrink-0"></span>}
        </button>
        <button onClick={() => setView('hangar')} className={`px-3 py-2 border text-[10px] font-bold uppercase tracking-widest flex items-center transition-colors ${view === 'hangar' ? 'bg-white text-black border-white' : 'bg-black text-white border-white hover:bg-white hover:text-black'}`}><Rocket className="w-4 h-4 mr-2 shrink-0" /> Hangar</button>
        <button onClick={() => setView('shop')} className={`px-3 py-2 border text-[10px] font-bold uppercase tracking-widest flex items-center transition-colors ${view === 'shop' ? 'bg-white text-black border-white' : 'bg-black text-white border-white hover:bg-white hover:text-black'}`}><ShoppingCart className="w-4 h-4 mr-2 shrink-0" /> Shop</button>
      </div>
    </div>
  );

  const renderMapView = () => {
    return (
      <div className="relative z-10 p-8 max-w-5xl mx-auto h-[calc(100vh-80px)] overflow-y-auto font-mono tracking-widest text-left text-white">
        <div className="flex justify-between items-center mb-8 border-b border-white pb-4 text-left">
          <h1 className="text-3xl font-bold text-white uppercase text-left">UNIVERSE {currentUniverse}</h1>
          <div className="flex space-x-2 text-center">
            <button onClick={() => setCurrentUniverse(1)} className={`px-4 py-2 border text-xs font-bold uppercase transition-colors ${currentUniverse === 1 ? 'bg-white text-black border-white' : 'bg-black text-white border-white hover:bg-white hover:text-black'}`}>U1</button>
            <button onClick={() => canEnterUniverse2 && setCurrentUniverse(2)} className={`px-4 py-2 border text-xs font-bold uppercase flex items-center transition-colors ${currentUniverse === 2 ? 'bg-white text-black border-white' : canEnterUniverse2 ? 'bg-black text-white border-white hover:bg-white hover:text-black' : 'bg-black text-white/30 border-white/30 cursor-not-allowed border-dashed'}`}>
              U2 {!canEnterUniverse2 && <Lock className="w-3 h-3 ml-2 shrink-0" />}
            </button>
          </div>
        </div>

        {!canEnterUniverse2 && currentUniverse === 1 && (
          <div className="mb-6 p-4 border border-white bg-black text-white text-xs uppercase flex items-center text-left">
            <Unlock className="w-5 h-5 mr-3 shrink-0" />
            <span>Clear all 10 Sectors and earn 100 Stars to unlock Universe 2. (Current: {totalStars}/100 Stars)</span>
          </div>
        )}

        <div className="grid grid-cols-2 md:grid-cols-5 gap-6 text-left">
          {[...Array(10)].map((_, i) => {
            const sectorNum = i + 1;
            const mechanics = getSectorMechanicsList(sectorNum).map(id => SECTOR_MECHANICS[id]);
            let sectorStars = 0; let levelsCleared = 0;
            for(let l=1; l<=5; l++) { const s = levelScores[`${currentUniverse}-${sectorNum}-${l}`]; if (s) { sectorStars += s; levelsCleared++; } }
            let isUnlocked = false;
            if (currentUniverse === 2 && !canEnterUniverse2) isUnlocked = false;
            else if (sectorNum === 1) isUnlocked = true;
            else { let prevCleared = 0; for(let l=1; l<=5; l++) { if (levelScores[`${currentUniverse}-${sectorNum-1}-${l}`] > 0) prevCleared++; } isUnlocked = prevCleared === 5; }
            return (
              <div key={sectorNum} onClick={() => isUnlocked && setActiveSector({ u: currentUniverse, s: sectorNum, mechs: mechanics })} className={`relative group p-6 border transition-all flex flex-col ${isUnlocked ? 'cursor-pointer hover:bg-white hover:text-black text-white bg-black border-white' : 'opacity-50 cursor-not-allowed text-white/30 bg-black border-white/30 border-dashed'}`}>
                <div className="flex justify-between items-start mb-1 text-left">
                  <h3 className="text-lg font-bold uppercase text-left">Sector {sectorNum}</h3>
                  {!isUnlocked ? <Lock className="w-5 h-5 shrink-0" /> : <ChevronRight className={`w-5 h-5 opacity-0 group-hover:opacity-100 transition-opacity shrink-0 ${isUnlocked ? 'group-hover:text-black text-white' : ''}`} />}
                </div>
                <div className="flex flex-wrap gap-1 mb-6 mt-2 text-left">
                  {mechanics.map((mech, midx) => (
                    <div key={midx} className={`text-[9px] font-bold uppercase px-2 py-1 border transition-colors ${isUnlocked ? mech.color : 'border-white/30 text-white/30'} ${isUnlocked ? 'group-hover:!border-black group-hover:!text-black' : ''}`}>{mech.name}</div>
                  ))}
                </div>
                <div className="mt-auto flex flex-col space-y-1 text-left">
                  <div className="text-xs uppercase font-bold text-left">LVL: {levelsCleared}/5</div>
                  <div className="flex items-center text-sm font-bold text-left">{sectorStars}/15 <Star className="w-3 h-3 ml-1 fill-current shrink-0" /></div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  const renderSectorView = () => {
    if (!activeSector) return null;
    const { u, s, mechs } = activeSector;
    const isSectorBeaten = [1, 2, 3, 4, 5].every(l => (levelScores[`${u}-${s}-${l}`] || 0) > 0);
    const hasNextSector = s < 10;
    return (
      <div className="relative z-10 p-8 max-w-4xl mx-auto h-[calc(100vh-80px)] overflow-y-auto pb-20 font-mono tracking-widest text-left text-white">
        <button onClick={() => setActiveSector(null)} className="mb-6 text-white hover:bg-white hover:text-black border border-transparent hover:border-white px-2 py-1 transition-colors flex items-center text-xs uppercase font-bold w-fit text-left">
          <ChevronRight className="w-4 h-4 mr-1 transform rotate-180 shrink-0" /> Back
        </button>
        <div className="bg-black border border-white p-8 text-left text-white">
          <div className="flex justify-between items-center mb-2 border-b border-white pb-4 text-left">
            <h2 className="text-3xl font-bold text-white uppercase text-left">Universe {u} - Sector {s}</h2>
            {isSectorBeaten && hasNextSector && (
              <button 
                onClick={() => setActiveSector({ u, s: s + 1, mechs: getSectorMechanicsList(s + 1).map(id => SECTOR_MECHANICS[id]) })}
                className="px-4 py-2 border border-white text-white hover:bg-white hover:text-black transition-colors font-bold uppercase text-xs tracking-widest flex items-center text-center text-white"
              >
                Next Sector <ChevronRight className="w-4 h-4 ml-1 shrink-0" />
              </button>
            )}
          </div>
          <div className="grid grid-cols-1 gap-2 mt-6 mb-8 text-left text-white">
            {mechs.map((mech, i) => {
               const isMet = !mech.reqShip || activeShips.includes(mech.reqShip);
               return (
                  <div key={i} className={`p-4 border flex items-start ${isMet ? `bg-black ${mech.color}` : 'bg-white text-black border-black'}`}>
                    <AlertTriangle className="w-6 h-6 mr-3 shrink-0" />
                    <div className="text-left">
                      <h4 className="font-bold uppercase text-sm text-left">Hazard: {mech.name}</h4>
                      <p className="text-xs mt-1 text-left">{mech.warning}</p>
                      {!isMet && <p className="text-[10px] font-bold mt-2 uppercase text-red-600 text-left">WARNING: Correct ship not equipped in Fleet!</p>}
                    </div>
                  </div>
               )
            })}
          </div>
          <div className="space-y-4 text-left">
            {[...Array(5)].map((_, i) => {
              const l = i + 1;
              const key = `${u}-${s}-${l}`;
              const stars = levelScores[key] || 0;
              const isLvl1 = l === 1;
              const isCleared = stars > 0;
              const canPlayFromMenu = isLvl1 || isCleared;
              const requiresAd = !isLvl1 && isCleared;
              return (
                <div key={l} className={`flex items-center justify-between p-4 border ${canPlayFromMenu ? 'bg-black border-white text-white' : 'bg-black border-white/30 border-dashed text-white/30'}`}>
                  <div className="flex items-center text-left">
                    <div className={`w-10 h-10 flex items-center justify-center font-bold border mr-4 shrink-0 ${canPlayFromMenu ? 'border-white' : 'border-white/30'}`}>{l}</div>
                    <div className="text-left">
                      <h4 className="font-bold flex items-center uppercase text-sm text-left">Level {l} {isCleared && <span className="ml-2 text-[10px] border px-1 uppercase text-left">Cleared</span>}</h4>
                      <div className="flex mt-1 text-left">
                        {[1, 2, 3].map(starIdx => ( <Star key={starIdx} className={`w-4 h-4 mr-1 shrink-0 ${starIdx <= stars ? 'fill-current' : 'opacity-30'}`} /> ))}
                      </div>
                    </div>
                  </div>
                  {canPlayFromMenu ? (
                    <button 
                      onClick={() => setPendingLoadout({ u, s, l, isMenuLaunch: true, requiresAd })}
                      className={`px-6 py-3 border font-bold transition-colors flex items-center text-xs uppercase tracking-widest text-center ${requiresAd ? 'bg-black text-white border-white hover:bg-white hover:text-black' : 'bg-white text-black hover:bg-transparent hover:text-white border-white'}`}
                    >
                      <Play className="w-4 h-4 mr-2 fill-current shrink-0" /> PRE-FLIGHT
                    </button>
                  ) : (
                    <div className="flex items-center text-[10px] uppercase tracking-widest text-white/50 mr-2 text-right">
                        <Lock className="w-4 h-4 mr-2 shrink-0" /> Reach via Lvl {l-1}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  };

  const renderShop = () => {
    return (
      <div className="relative z-10 p-8 max-w-5xl mx-auto h-[calc(100vh-80px)] overflow-y-auto pb-20 font-mono tracking-widest text-left text-white">
        <h1 className="text-3xl font-bold text-white uppercase border-b border-white pb-4 mb-8 text-left text-white">Currency Terminal</h1>
        
        {/* Credit Packs */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12 text-left">
          {[
            { amount: 100, price: '4.99$' },
            { amount: 300, price: '9.99$' },
            { amount: 900, price: '14.99$' }
          ].map((pack, i) => (
            <div key={i} className="border border-white p-6 bg-black flex flex-col items-center justify-center group hover:bg-white hover:text-black transition-all text-center">
              <Coins className="w-10 h-10 mb-4 text-yellow-400 group-hover:text-black shrink-0" />
              <div className="text-xl font-bold uppercase mb-2 text-center">
                {pack.amount} Credits <span className="text-green-400 font-bold ml-1">+</span>
              </div>
              <button 
                onClick={() => simulatePurchase(pack)}
                className="rgb-notify w-full py-3 mt-4 border border-current text-xs font-bold uppercase tracking-widest transition-colors hover:bg-black hover:text-white group-hover:border-black text-center"
              >
                Acquire {pack.price}
              </button>
            </div>
          ))}
        </div>

        <h2 className="text-xl font-bold text-white/70 uppercase mb-6 flex items-center text-left text-white">
          <Zap className="w-5 h-5 mr-2 shrink-0 text-white/70" /> Power Ups
        </h2>
        <div className="border-t border-white/30 mb-8 text-white"></div>

        {/* Power Up Items */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 text-left text-white">
          {[
            { name: 'Magnet', qty: 10, cost: 200, desc: 'Pulls rocket toward center.' },
            { name: 'Gate Extender', qty: 10, cost: 200, desc: 'Entire gate counts as 3-stars.' },
            { name: 'Placeholder', qty: 1, cost: 50, desc: 'Standard system utility.' },
            { name: 'Placeholder', qty: 5, cost: 80, original: 100, desc: 'System utility bundle.' },
            { name: 'Placeholder', qty: 10, cost: 110, original: 150, desc: 'Bulk system utility.' },
          ].map((item, i) => (
            <div key={i} className="border border-white/50 p-5 bg-black flex flex-col transition-all hover:border-white text-left text-white">
              <div className="flex justify-between items-start mb-2 text-left">
                <h3 className="font-bold uppercase text-sm text-left">{item.name} x{item.qty}</h3>
                <Box className="w-5 h-5 text-white/50 shrink-0" />
              </div>
              <p className="text-[10px] text-white/50 uppercase mb-6 leading-relaxed text-left">{item.desc}</p>
              
              <div className="mt-auto pt-4 border-t border-white/10 flex justify-between items-center text-left">
                 <div className="flex flex-col text-left">
                   {item.original && <span className="text-[9px] line-through text-red-500 uppercase text-left">{item.original} Credits</span>}
                   <div className="text-xs font-bold uppercase flex items-center text-left">
                     <Coins className="w-3 h-3 mr-1 text-yellow-400 shrink-0" /> {item.cost} Credits
                   </div>
                 </div>
                 <button className="rgb-notify px-4 py-2 border border-white bg-white text-black text-[10px] font-bold uppercase hover:bg-transparent hover:text-white transition-colors text-center shrink-0">
                    Buy
                 </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  const renderLoadoutPanel = () => {
    if (!pendingLoadout) return null;
    const { u, s, l, requiresAd } = pendingLoadout;
    const mechs = getSectorMechanicsList(s).map(id => SECTOR_MECHANICS[id]);
    return (
      <div className="absolute inset-0 flex items-center justify-center bg-black/90 backdrop-blur-md z-[150] font-mono tracking-widest overflow-y-auto py-10 text-left text-white">
        <div className="text-left p-8 border border-white bg-black w-full max-w-4xl shadow-[0_0_30px_rgba(255,255,255,0.1)]">
           <div className="flex justify-between items-end border-b border-white pb-4 mb-8 text-left">
              <div className="text-left">
                <div className="text-xs text-white/50 mb-1 uppercase text-left">Transmission Secure</div>
                <h2 className="text-2xl text-white uppercase font-bold text-left">Pre-Flight Loadout</h2>
              </div>
              <div className="text-right text-white">
                <div className="text-xs text-white/50 mb-1 uppercase text-right">Target Coords</div>
                <h3 className="text-xl text-white uppercase font-bold text-right">U{u} - S{s} - L{l}</h3>
              </div>
           </div>
           <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mb-8 text-left text-white">
             <div className="text-left">
               <h4 className="text-sm uppercase text-white/70 mb-4 font-bold border-b border-white/30 pb-2 text-left">Squadron Details (Fleet)</h4>
               <div className="space-y-3 text-left">
                 {Object.values(SHIPS).filter(ship => unlockedShips.includes(ship.id)).map(ship => {
                   const isEquipped = activeShips.includes(ship.id);
                   return (
                     <div key={ship.id} onClick={() => toggleShipEquip(ship.id)} className={`cursor-pointer border p-3 flex items-center justify-between transition-colors ${isEquipped ? 'bg-white text-black border-white' : 'bg-black text-white/50 border-white/30 hover:border-white hover:text-white'}`}>
                       <div className="flex items-center text-left">
                         <Rocket className={`w-5 h-5 mr-3 shrink-0 ${isEquipped ? 'text-black' : ship.color}`} />
                         <span className="text-xs uppercase font-bold text-left">{ship.name}</span>
                       </div>
                       {isEquipped && <CheckCircle2 className="w-4 h-4 shrink-0" />}
                     </div>
                   );
                 })}
               </div>
             </div>
             <div className="text-left">
                <h4 className="text-sm uppercase text-white/70 mb-4 font-bold border-b border-white/30 pb-2 text-left">Hazard Analysis</h4>
                <div className="space-y-3 text-left text-white">
                  {mechs.map((mech, i) => {
                    const isMet = !mech.reqShip || activeShips.includes(mech.reqShip);
                    return (
                      <div key={i} className={`p-3 border flex flex-col text-left ${isMet ? `bg-black border-white ${mech.color}` : 'bg-black border-red-500 text-red-500'}`}>
                        <div className="flex items-center mb-1 text-xs uppercase font-bold text-left">
                           <AlertTriangle className="w-4 h-4 mr-2 shrink-0" /> {mech.name}
                        </div>
                        {!isMet && <span className="text-[10px] uppercase text-left text-red-500">Missing: {SHIPS[mech.reqShip].name}</span>}
                        {isMet && <span className="text-[10px] uppercase opacity-70 text-left">Addressed by Fleet</span>}
                      </div>
                    )
                  })}
                </div>
             </div>
           </div>
           <div className="flex space-x-4 text-center">
              <button onClick={executeLaunch} className={`flex-1 px-6 py-4 transition-colors font-bold uppercase tracking-widest flex items-center justify-center text-sm text-center ${requiresAd ? 'rgb-notify bg-white text-black border border-white' : 'bg-white text-black hover:bg-transparent hover:text-white border border-white'}`}>
                <Play className="w-5 h-5 mr-2 fill-current shrink-0" /> {requiresAd ? 'WATCH AD & LAUNCH' : 'LAUNCH MISSION'}
              </button>
              <button onClick={() => setPendingLoadout(null)} className="px-6 py-4 border border-white/30 text-white/70 hover:bg-white hover:text-black hover:border-white transition-colors font-bold uppercase tracking-widest text-xs text-center">Abort</button>
           </div>
        </div>
      </div>
    );
  };

  const renderBattlePass = () => (
      <div className="relative z-10 p-8 h-[calc(100vh-80px)] overflow-y-auto flex flex-col font-mono tracking-widest text-left text-white">
        <div className="flex justify-between items-center mb-8 max-w-6xl mx-auto w-full shrink-0 border-b border-white pb-4 text-left text-white">
          <div className="text-left">
            <h1 className="text-3xl font-bold uppercase text-left text-white">Progression</h1>
            <p className="text-white/70 text-xs uppercase text-left">Earn stars to unlock. Current Level: <span className="text-white font-bold text-sm">[{accountLevel}]</span></p>
          </div>
          <div className="bg-black p-4 border border-white flex flex-col items-center text-center text-white">
            <span className="text-[10px] font-bold text-white/70 mb-2 uppercase text-center">Pass Status</span>
            {isPremium ? ( <span className="px-4 py-1.5 bg-white text-black font-bold text-xs flex items-center uppercase text-center"><Crown className="w-4 h-4 mr-2 shrink-0" /> Premium Active</span> ) : ( <button onClick={() => setIsPremium(true)} className="rgb-notify px-4 py-1.5 border border-white bg-transparent text-white hover:bg-white hover:text-black font-bold text-xs flex items-center transition-colors uppercase text-center"><Crown className="w-4 h-4 mr-2 shrink-0" /> Upgrade Pass</button> )}
          </div>
        </div>
        <div className="flex-1 overflow-x-auto pb-8 hide-scrollbar text-left text-white">
          <div className="flex space-x-4 min-w-max px-8 text-left text-white">
            {BATTLE_PASS_TIERS.map((tier) => {
              const isUnlocked = accountLevel >= tier.level;
              const freeClaimed = claimedRewards.includes(`${tier.level}-free`);
              const premiumClaimed = claimedRewards.includes(`${tier.level}-premium`);
              return (
                <div key={tier.level} className={`w-64 flex flex-col shrink-0 border transition-all text-left ${isUnlocked ? 'border-white bg-black' : 'border-white/30 border-dashed bg-black opacity-70'}`}>
                  <div className={`p-4 border-b flex justify-between items-center text-left ${isUnlocked ? 'border-white bg-white text-black' : 'border-white/30 text-white/70'}`}>
                    <span className="font-bold text-sm uppercase text-left">Tier {tier.level}</span>
                    <span className="text-[10px] font-bold flex items-center uppercase border border-current px-2 py-1 shrink-0">{tier.starsReq} <Star className="w-3 h-3 ml-1 fill-current shrink-0" /></span>
                  </div>
                  <div className="p-4 flex-1 flex flex-col justify-between border-b border-white/50 relative text-white text-left min-h-[140px]">
                    <div className="text-left text-white">
                        <div className="text-[10px] font-bold text-white/50 mb-2 uppercase text-left">FREE</div>
                        <div className={`font-bold text-sm uppercase text-left ${tier.free.type === 'ship' ? 'underline text-blue-300' : ''}`}>
                            {tier.free.type === 'ship' && <Rocket className="w-4 h-4 inline mr-1 mb-1 shrink-0" />} {tier.free.name}
                        </div>
                        {tier.free.credits && (
                            <div className="text-[10px] font-bold text-yellow-400 mt-1 uppercase flex items-center text-left">
                                <Coins className="w-3 h-3 mr-1 shrink-0" /> +{tier.free.credits} Credits
                            </div>
                        )}
                    </div>
                    <div className="mt-4 text-left">{isUnlocked ? ( freeClaimed ? <div className="text-xs font-bold flex items-center uppercase border border-white px-2 py-1 w-fit text-left text-white"><CheckCircle2 className="w-3 h-3 mr-1 shrink-0 text-white" /> Claimed</div> : <button onClick={() => claimReward(tier.level, false)} className="rgb-notify w-full py-2 bg-transparent hover:bg-white border border-white text-white hover:text-black text-xs font-bold transition-colors uppercase tracking-widest text-center">Claim Free</button> ) : <Lock className="w-5 h-5 text-white/30 mx-auto shrink-0" />}</div>
                  </div>
                  <div className={`p-4 flex-1 flex flex-col justify-between relative text-left min-h-[140px] ${isPremium ? 'bg-white text-black' : 'bg-black text-white'}`}>
                    {!isPremium && <div className="absolute inset-0 bg-black/80 flex items-center justify-center border-t border-white/30 text-center text-white"><Lock className="w-6 h-6 text-white/50 shrink-0" /></div>}
                    <div className="text-left">
                        <div className="text-[10px] font-bold mb-2 flex items-center uppercase text-left"><Crown className="w-3 h-3 mr-1 shrink-0" /> PREMIUM</div>
                        <div className="font-bold text-sm uppercase text-left">{tier.premium.name}</div>
                        {tier.premium.credits && (
                            <div className={`text-[10px] font-bold mt-1 uppercase flex items-center text-left ${isPremium ? 'text-black/70' : 'text-yellow-400'}`}>
                                <Coins className="w-3 h-3 mr-1 shrink-0" /> +{tier.premium.credits} Credits
                            </div>
                        )}
                    </div>
                    <div className="mt-4 z-20 text-left">{isUnlocked && isPremium ? ( premiumClaimed ? <div className="text-xs font-bold flex items-center uppercase border border-black px-2 py-1 w-fit text-left"><CheckCircle2 className="w-3 h-3 mr-1 shrink-0" /> Claimed</div> : <button onClick={() => claimReward(tier.level, true)} className="rgb-notify w-full py-2 bg-black hover:bg-transparent text-white hover:text-black border border-black text-xs font-bold transition-colors uppercase tracking-widest text-center">Claim Prem</button> ) : null}</div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
  );

  const renderHangar = () => (
      <div className="relative z-10 p-8 max-w-5xl mx-auto h-[calc(100vh-80px)] overflow-y-auto pb-20 font-mono tracking-widest text-left text-white">
         <h1 className="text-3xl font-bold text-white uppercase border-b border-white pb-4 mb-8 text-left">Hangar Terminal</h1>
         <h2 className="text-xl font-bold text-white/70 mb-6 uppercase text-left">Fleet / Ships (Multi-Select)</h2>
         <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12 text-left text-white">
            {Object.values(SHIPS).map(ship => {
              const isUnlocked = unlockedShips.includes(ship.id);
              const isEquipped = activeShips.includes(ship.id);
              return (
                <div key={ship.id} className={`border p-6 transition-all flex flex-col text-left ${isEquipped ? 'border-white bg-white text-black shadow-[0_0_15px_rgba(255,255,255,0.3)]' : isUnlocked ? 'border-white/50 bg-black text-white hover:border-white' : 'border-white/20 bg-black text-white/30 border-dashed'}`}>
                  <div className="flex justify-between items-start mb-4 text-left text-current">
                    <Rocket className={`w-10 h-10 shrink-0 ${isEquipped ? 'text-black' : ship.color}`} strokeWidth={isEquipped ? 2 : 1.5} />
                    {isEquipped && <span className="bg-black text-white text-[10px] font-bold px-2 py-1 uppercase border border-black text-center">Active</span>}
                    {!isUnlocked && <Lock className="w-5 h-5 text-white/30 shrink-0" />}
                  </div>
                  <h3 className="text-lg font-bold mb-2 uppercase text-left text-current">{ship.name}</h3>
                  <p className={`text-xs mb-6 h-10 text-left text-current ${isEquipped ? 'opacity-70' : 'opacity-50'}`}>{ship.perk}</p>
                  <div className="mt-auto text-center"><button onClick={() => toggleShipEquip(ship.id)} className={`w-full py-3 font-bold uppercase text-xs tracking-widest transition-colors text-center ${isUnlocked ? (isEquipped ? 'border border-black bg-black text-white hover:bg-transparent hover:text-black' : 'border border-white bg-transparent text-white hover:bg-white hover:text-black') : 'border border-white/20 bg-transparent text-white/30 cursor-not-allowed'}`}>{isUnlocked ? (isEquipped ? 'Detach' : 'Assign to Fleet') : 'Locked'}</button></div>
                </div>
              );
            })}
         </div>
         <h2 className="text-xl font-bold text-white/70 mb-6 uppercase text-left text-white">Exhaust / Trails</h2>
         <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-left text-white">
            {Object.values(TRAILS).map(trail => {
              const isUnlocked = unlockedTrails.includes(trail.id);
              const isEquipped = activeTrail === trail.id;
              return (
                <div key={trail.id} className={`border p-6 transition-all flex flex-col text-left ${isEquipped ? 'border-white bg-white text-black shadow-[0_0_15px_rgba(255,255,255,0.3)]' : isUnlocked ? 'border-white/50 bg-black text-white hover:border-white' : 'border-white/20 bg-black text-white/30 border-dashed'}`}>
                  <div className="flex justify-between items-start mb-4 text-left text-current">
                    <Wind className={`w-10 h-10 shrink-0 ${isEquipped ? 'text-black' : isUnlocked ? 'text-white' : 'text-white/30'}`} strokeWidth={isEquipped ? 2 : 1.5} />
                    {isEquipped && <span className="bg-black text-white text-[10px] font-bold px-2 py-1 uppercase border border-black text-center">Active</span>}
                    {!isUnlocked && <Lock className="w-5 h-5 text-white/30 shrink-0" />}
                  </div>
                  <h3 className="text-lg font-bold mb-2 uppercase text-left text-current">{trail.name}</h3>
                  <p className={`text-xs mb-6 h-10 text-left text-current ${isEquipped ? 'opacity-70' : 'opacity-50'}`}>{trail.perk}</p>
                  <div className="mt-auto text-center"><button disabled={!isUnlocked || isEquipped} onClick={() => setActiveTrail(trail.id)} className={`w-full py-3 font-bold uppercase text-xs tracking-widest transition-colors text-center ${isUnlocked ? (isEquipped ? 'border border-black bg-black text-white cursor-default' : 'border border-white bg-transparent text-white hover:bg-white hover:text-black') : 'border border-white/20 bg-transparent text-white/30 cursor-not-allowed'}`}>{isUnlocked ? (isEquipped ? 'Equipped' : 'Initialize') : 'Locked'}</button></div>
                </div>
              );
            })}
         </div>
      </div>
  );

  return (
    <div className="min-h-screen bg-black font-mono text-white overflow-hidden selection:bg-white selection:text-black">
      <WebGLBackground />
      
      {playSession ? (
        <GravityGame 
          key={playSession.sessionKey}
          levelIndex={playSession.index}
          universe={playSession.u}
          sector={playSession.s}
          levelInSector={playSession.l}
          shipIds={activeShips}
          trailId={activeTrail}
          maxShields={maxShields}
          currentShields={globalShields}
          setCurrentShields={setGlobalShields}
          onComplete={handleLevelComplete}
          onExit={() => setPlaySession(null)}
        />
      ) : (
        <>
          {renderTopNav()}
          <main className="relative z-10 h-[calc(100vh-80px)] overflow-hidden text-left text-white">
            {view === 'map' && !activeSector && renderMapView()}
            {view === 'map' && activeSector && renderSectorView()}
            {view === 'battlepass' && renderBattlePass()}
            {view === 'hangar' && renderHangar()}
            {view === 'shop' && renderShop()}
          </main>

          {renderLoadoutPanel()}

          {/* Secure Purchase Simulated Modal */}
          {purchaseOverlay && (
            <div className="absolute inset-0 flex items-center justify-center bg-black/95 backdrop-blur-md z-[250] font-mono tracking-widest">
              <div className="text-center p-12 border-2 border-white bg-black max-w-md w-full shadow-[0_0_50px_rgba(255,255,255,0.15)]">
                <div className="flex justify-center mb-8">
                   <div className="relative">
                     <CreditCard className="w-16 h-16 text-white" />
                     <Loader2 className="w-8 h-8 text-white absolute -top-2 -right-2 animate-spin" />
                   </div>
                </div>
                <h2 className="text-2xl text-white uppercase font-bold mb-4 tracking-tighter">Secure Link Established</h2>
                <div className="text-white/50 text-[10px] mb-8 border-y border-white/20 py-4 uppercase">
                    Processing Transaction...<br/>
                    Acquiring: {purchaseOverlay.amount} Galactic Credits<br/>
                    Authorization: 0x{Math.random().toString(16).slice(2, 10).toUpperCase()}
                </div>
                <p className="text-white text-xs animate-pulse uppercase tracking-[0.2em]">Contacting Bank Server...</p>
              </div>
            </div>
          )}

          {adOverlay && (
            <div className="absolute inset-0 bg-black z-[200] flex justify-center items-center font-mono text-white tracking-widest uppercase text-center">
               <span className="animate-pulse">Establishing Warp Link...</span>
            </div>
          )}

          {sectorSummary && (
             <SectorSummaryOverlay u={sectorSummary.u} s={sectorSummary.s} scores={sectorSummary.scores} onClose={() => setSectorSummary(null)} />
          )}
        </>
      )}

      <style dangerouslySetInnerHTML={{__html: `
        .hide-scrollbar::-webkit-scrollbar { display: none; }
        .hide-scrollbar { -ms-overflow-style: none; scrollbar-width: none; }

        @keyframes rgb-glitch {
          0%, 85%, 100% { transform: translate(0, 0); box-shadow: none; text-shadow: none; }
          88% { transform: translate(-1.5px, 1.5px); box-shadow: -3px 0 rgba(255,0,0,0.8), 3px 0 rgba(0,255,255,0.8); text-shadow: -3px 0 rgba(255,0,0,0.8), 3px 0 rgba(0,255,255,0.8); }
          91% { transform: translate(1.5px, -1.5px); box-shadow: 3px 0 rgba(255,0,0,0.8), -3px 0 rgba(0,255,255,0.8); text-shadow: 3px 0 rgba(255,0,0,0.8), -3px 0 rgba(0,255,255,0.8); }
          94% { transform: translate(-1.5px, -1.5px); box-shadow: -3px 0 rgba(0,255,0,0.8), 3px 0 rgba(255,0,255,0.8); text-shadow: -3px 0 rgba(0,255,0,0.8), 3px 0 rgba(255,0,255,0.8); }
          97% { transform: translate(1.5px, 1.5px); box-shadow: 3px 0 rgba(0,0,255,0.8), -3px 0 rgba(255,255,0,0.8); text-shadow: 3px 0 rgba(0,0,255,0.8), -3px 0 rgba(255,255,0,0.8); }
        }
        .rgb-notify {
          animation: rgb-glitch 2s infinite;
        }
      `}} />
    </div>
  );
}