import * as THREE from 'three';

export const groupColors = {
  spine:     0xffffff,
  left_leg:  0x4488ff,
  right_leg: 0xff4444,
  left_arm:  0x44ff88,
  right_arm: 0xffaa22,
};

export function buildSkeleton(scene, animData) {
  const jointGeo = new THREE.SphereGeometry(0.025, 8, 8);
  const jointMeshes = [];

  for (let j = 0; j < animData.joints; j++) {
    const color = groupColors[animData.jointGroup[j]] || 0xcccccc;
    const mat = new THREE.MeshStandardMaterial({ color });
    const mesh = new THREE.Mesh(jointGeo, mat);
    scene.add(mesh);
    jointMeshes.push(mesh);
  }

  // Bone lines
  const linePositions = new Float32Array(animData.edges.length * 2 * 3);
  const lineColors = new Float32Array(animData.edges.length * 2 * 3);
  const lineGeo = new THREE.BufferGeometry();
  lineGeo.setAttribute('position', new THREE.BufferAttribute(linePositions, 3));
  lineGeo.setAttribute('color', new THREE.BufferAttribute(lineColors, 3));

  const lineMat = new THREE.LineBasicMaterial({ vertexColors: true, linewidth: 2 });
  const boneLine = new THREE.LineSegments(lineGeo, lineMat);
  scene.add(boneLine);

  return { jointMeshes, boneLine };
}

export function updateSkeletonFrame(animData, jointMeshes, boneLine, frame) {
  if (!animData) return;

  const pos = animData.positions[frame]; // 66 floats: 22 joints x 3

  for (let j = 0; j < animData.joints; j++) {
    const x = pos[j * 3];
    const y = pos[j * 3 + 1];
    const z = pos[j * 3 + 2];
    jointMeshes[j].position.set(x, y, z);
  }

  // Update bone lines
  const linePos = boneLine.geometry.attributes.position.array;
  const lineCol = boneLine.geometry.attributes.color.array;

  for (let i = 0; i < animData.edges.length; i++) {
    const [src, dst] = animData.edges[i];
    const si = i * 6;

    linePos[si]     = pos[src * 3];
    linePos[si + 1] = pos[src * 3 + 1];
    linePos[si + 2] = pos[src * 3 + 2];
    linePos[si + 3] = pos[dst * 3];
    linePos[si + 4] = pos[dst * 3 + 1];
    linePos[si + 5] = pos[dst * 3 + 2];

    const color = new THREE.Color(groupColors[animData.jointGroup[dst]] || 0xcccccc);
    lineCol[si]     = color.r; lineCol[si + 1] = color.g; lineCol[si + 2] = color.b;
    lineCol[si + 3] = color.r; lineCol[si + 4] = color.g; lineCol[si + 5] = color.b;
  }

  boneLine.geometry.attributes.position.needsUpdate = true;
  boneLine.geometry.attributes.color.needsUpdate = true;
}
