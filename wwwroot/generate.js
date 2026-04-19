import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { buildSkeleton, updateSkeletonFrame } from './skeleton-renderer.js';

const canvas        = document.getElementById('canvas');
const modelSelect   = document.getElementById('model-select');
const textInput     = document.getElementById('text-input');
const generateBtn   = document.getElementById('generate-btn');
const statusEl      = document.getElementById('status');
const captionBar    = document.getElementById('caption-bar');
const playBtn       = document.getElementById('play-btn');
const timeline      = document.getElementById('timeline');
const frameInfo     = document.getElementById('frame-info');
const noSelection   = document.getElementById('no-selection');

const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setClearColor(0x1a1a2e);

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
camera.position.set(0, 1.5, 4);

const controls = new OrbitControls(camera, canvas);
controls.target.set(0, 1, 0);
controls.enableDamping = true;

scene.add(new THREE.AmbientLight(0xffffff, 0.6));
const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
dirLight.position.set(5, 10, 5);
scene.add(dirLight);

scene.add(new THREE.GridHelper(10, 20, 0x0f3460, 0x0a1530));

let animData = null;
let currentFrame = 0;
let playing = false;
let lastFrameTime = 0;
const FPS = 20;
let jointMeshes = [];
let boneLine = null;

async function loadModels() {
  try {
    const res = await fetch('/api/text2motion/models');
    const models = await res.json();
    modelSelect.innerHTML = '';
    for (const m of models) {
      const opt = document.createElement('option');
      opt.value = m;
      opt.textContent = m;
      modelSelect.appendChild(opt);
    }
    if (models.length === 0) {
      statusEl.textContent = 'No models found.';
    }
  } catch (err) {
    statusEl.textContent = `Error loading models: ${err.message}`;
  }
}

async function generate() {
  const text = textInput.value.trim();
  if (!text) return;

  generateBtn.disabled = true;
  statusEl.textContent = 'Generating…';

  try {
    const res = await fetch('/api/text2motion/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, modelName: modelSelect.value })
    });

    if (!res.ok) {
      const err = await res.json();
      statusEl.textContent = `Error: ${err.error}`;
      return;
    }

    animData = await res.json();
    statusEl.textContent = `Generated ${animData.frameCount} frames.`;
    captionBar.textContent = animData.caption;
    noSelection.style.display = 'none';

    currentFrame = 0;
    playing = false;
    playBtn.textContent = 'Play';
    playBtn.disabled = false;
    timeline.disabled = false;
    timeline.max = animData.frameCount - 1;
    timeline.value = 0;
    updateFrameInfo();

    // Remove old skeleton
    for (const m of jointMeshes) scene.remove(m);
    if (boneLine) scene.remove(boneLine);

    // Build new skeleton
    const built = buildSkeleton(scene, animData);
    jointMeshes = built.jointMeshes;
    boneLine = built.boneLine;
    updateSkeletonFrame(animData, jointMeshes, boneLine, 0);
  } catch (err) {
    statusEl.textContent = `Error: ${err.message}`;
  } finally {
    generateBtn.disabled = false;
  }
}

generateBtn.addEventListener('click', generate);
textInput.addEventListener('keydown', e => {
  if (e.key === 'Enter') generate();
});

playBtn.addEventListener('click', () => {
  playing = !playing;
  playBtn.textContent = playing ? 'Pause' : 'Play';
  if (playing) lastFrameTime = performance.now();
});

timeline.addEventListener('input', () => {
  currentFrame = parseInt(timeline.value);
  if (animData) {
    updateSkeletonFrame(animData, jointMeshes, boneLine, currentFrame);
    updateFrameInfo();
  }
});

function updateFrameInfo() {
  if (!animData) {
    frameInfo.textContent = '0 / 0';
    return;
  }
  frameInfo.textContent = `${currentFrame + 1} / ${animData.frameCount}`;
}

function resize() {
  const vp = document.getElementById('viewport');
  const w = vp.clientWidth;
  const h = vp.clientHeight;
  renderer.setSize(w, h);
  camera.aspect = w / h;
  camera.updateProjectionMatrix();
}
window.addEventListener('resize', resize);

function animate(time) {
  requestAnimationFrame(animate);

  if (playing && animData) {
    const elapsed = time - lastFrameTime;
    if (elapsed >= 1000 / FPS) {
      lastFrameTime = time - (elapsed % (1000 / FPS));
      currentFrame = (currentFrame + 1) % animData.frameCount;
      timeline.value = currentFrame;
      updateSkeletonFrame(animData, jointMeshes, boneLine, currentFrame);
      updateFrameInfo();
    }
  }

  controls.update();
  renderer.render(scene, camera);
}

resize();
animate(0);
loadModels();
