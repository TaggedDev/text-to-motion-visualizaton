import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { buildSkeleton, updateSkeletonFrame } from './skeleton-renderer.js';

// --- DOM refs ---
const canvas = document.getElementById('canvas');
const animList = document.getElementById('anim-list');
const searchInput = document.getElementById('search');
const captionBar = document.getElementById('caption-bar');
const playBtn = document.getElementById('play-btn');
const timeline = document.getElementById('timeline');
const frameInfo = document.getElementById('frame-info');
const noSelection = document.getElementById('no-selection');

// --- Three.js setup ---
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(window.devicePixelRatio);
renderer.setClearColor(0x1a1a2e);

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
camera.position.set(0, 1.5, 4);

const controls = new OrbitControls(camera, canvas);
controls.target.set(0, 1, 0);
controls.enableDamping = true;

// Lighting
scene.add(new THREE.AmbientLight(0xffffff, 0.6));
const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
dirLight.position.set(5, 10, 5);
scene.add(dirLight);

// Ground grid
const grid = new THREE.GridHelper(10, 20, 0x0f3460, 0x0a1530);
scene.add(grid);

// --- State ---
let animData = null;       // current loaded animation
let currentFrame = 0;
let playing = false;
let lastFrameTime = 0;
const FPS = 20;            // HumanML3D is 20fps

// Scene objects for the skeleton
let jointMeshes = [];
let boneLine = null;
let jointGroupData = [];
let edgeData = [];

// --- Animation list ---
let allAnimations = [];

async function loadAnimationList() {
  const res = await fetch('/api/animations');
  allAnimations = await res.json();
  renderList(filterAnimations(searchInput.value));
}

function filterAnimations(q) {
  q = (q || '').toLowerCase();
  if (!q) return allAnimations;
  return allAnimations.filter(a =>
    a.id.toLowerCase().includes(q) || (a.caption || '').toLowerCase().includes(q)
  );
}

function renderList(anims) {
  animList.innerHTML = '';
  for (const a of anims) {
    const li = document.createElement('li');
    li.dataset.split = a.split;
    li.dataset.id = a.id;
    li.innerHTML = `<span class="id">${a.id}</span> <span class="split">[${a.split}]</span> <span class="frames">${a.frameCount}f</span>
      <span class="caption" title="${esc(a.caption)}">${esc(a.caption)}</span>`;
    li.addEventListener('click', () => selectAnimation(a.split, a.id, li));
    animList.appendChild(li);
  }
}

function esc(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

searchInput.addEventListener('input', () => {
  renderList(filterAnimations(searchInput.value));
});

// --- Load animation ---
async function selectAnimation(split, id, li) {
  // Highlight
  document.querySelectorAll('#anim-list li').forEach(el => el.classList.remove('active'));
  if (li) li.classList.add('active');

  const res = await fetch(`/api/animation/${split}/${encodeURIComponent(id)}`);
  animData = await res.json();
  edgeData = animData.edges;
  jointGroupData = animData.jointGroup;

  captionBar.textContent = animData.caption || '(no caption)';
  noSelection.style.display = 'none';

  // Reset playback
  currentFrame = 0;
  playing = false;
  playBtn.textContent = 'Play';
  playBtn.disabled = false;
  timeline.disabled = false;
  timeline.max = animData.frameCount - 1;
  timeline.value = 0;
  updateFrameInfo();

  const { jointMeshes: jm, boneLine: bl } = buildSkeleton(scene, animData);
  jointMeshes = jm;
  boneLine = bl;
  updateSkeletonFrame(animData, jointMeshes, boneLine, 0);
}


// --- Playback controls ---
playBtn.addEventListener('click', () => {
  playing = !playing;
  playBtn.textContent = playing ? 'Pause' : 'Play';
  if (playing) lastFrameTime = performance.now();
});

timeline.addEventListener('input', () => {
  currentFrame = parseInt(timeline.value);
  updateSkeletonFrame(animData, jointMeshes, boneLine, currentFrame);
  updateFrameInfo();
});

function updateFrameInfo() {
  if (!animData) {
    frameInfo.textContent = '0 / 0';
    return;
  }
  frameInfo.textContent = `${currentFrame + 1} / ${animData.frameCount}`;
}

// --- Resize ---
function resize() {
  const vp = document.getElementById('viewport');
  const w = vp.clientWidth;
  const h = vp.clientHeight;
  renderer.setSize(w, h);
  camera.aspect = w / h;
  camera.updateProjectionMatrix();
}
window.addEventListener('resize', resize);

// --- Render loop ---
function animate(time) {
  requestAnimationFrame(animate);

  if (playing && animData) {
    const elapsed = time - lastFrameTime;
    if (elapsed >= 1000 / FPS) {
      lastFrameTime = time - (elapsed % (1000 / FPS));
      currentFrame++;
      if (currentFrame >= animData.frameCount) {
        currentFrame = 0;
      }
      timeline.value = currentFrame;
      updateSkeletonFrame(animData, jointMeshes, boneLine, currentFrame);
      updateFrameInfo();
    }
  }

  controls.update();
  renderer.render(scene, camera);
}

// --- Init ---
resize();
animate(0);
loadAnimationList();
