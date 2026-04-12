// State: list of { text, embedding }
let entries = [];

// Get DOM elements
const textInput = document.getElementById('text-input');
const textList = document.getElementById('text-list');
const plotDiv = document.getElementById('plot');

// Initialize page
textInput.focus();

// Event: Enter key to add text
textInput.addEventListener('keydown', async (e) => {
    if (e.key !== 'Enter') return;

    const text = textInput.value.trim();
    if (!text) return;

    // Check for duplicates
    if (entries.some(entry => entry.text === text)) {
        alert('Text already in list');
        return;
    }

    textInput.disabled = true;

    try {
        const embedding = await fetchEmbedding(text);
        entries.push({ text, embedding });
        textInput.value = '';
        recomputeAndRender();
    } catch (err) {
        alert(`Error: ${err.message}`);
    } finally {
        textInput.disabled = false;
        textInput.focus();
    }
});

// Event delegation: remove button click
textList.addEventListener('click', (e) => {
    if (e.target.classList.contains('remove-btn')) {
        const index = parseInt(e.target.getAttribute('data-index'), 10);
        entries.splice(index, 1);
        recomputeAndRender();
    }
});

/**
 * Fetch embedding for a single text
 */
async function fetchEmbedding(text) {
    const res = await fetch('/api/embedding-visualization/embed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text })
    });

    if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || `HTTP ${res.status}`);
    }

    const data = await res.json();
    return data.embedding;
}

/**
 * Project all embeddings to 2D and re-render
 */
async function recomputeAndRender() {
    // Render text list
    renderList();

    // Clear plot if no entries
    if (entries.length === 0) {
        Plotly.purge(plotDiv);
        return;
    }

    // Get 2D projections from backend
    try {
        const projectedPoints = await projectEmbeddings();
        renderPlot(projectedPoints);
    } catch (err) {
        console.error('Error projecting embeddings:', err);
        alert(`Error projecting: ${err.message}`);
    }
}

/**
 * Call /project endpoint to get 2D coordinates
 */
async function projectEmbeddings() {
    const request = {
        entries: entries.map(e => ({
            text: e.text,
            embedding: e.embedding
        }))
    };

    const res = await fetch('/api/embedding-visualization/project', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
    });

    if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || `HTTP ${res.status}`);
    }

    return res.json();
}

/**
 * Render the text list with remove buttons
 */
function renderList() {
    textList.innerHTML = '';
    entries.forEach((entry, index) => {
        const li = document.createElement('li');
        li.innerHTML = `
            <span title="${escape(entry.text)}">${escape(entry.text)}</span>
            <button class="remove-btn" data-index="${index}" title="Remove">✕</button>
        `;
        textList.appendChild(li);
    });
}

/**
 * Render Plotly scatter plot
 */
function renderPlot(points) {
    if (!points || points.length === 0) {
        Plotly.purge(plotDiv);
        return;
    }

    const trace = {
        x: points.map(p => p.x),
        y: points.map(p => p.y),
        text: points.map(p => p.text),
        mode: 'markers',
        type: 'scatter',
        hovertemplate: '<b>%{text}</b><extra></extra>',
        marker: {
            color: '#e94560',
            size: 12,
            opacity: 0.8,
            line: {
                color: '#ff6b7a',
                width: 1
            }
        }
    };

    const layout = {
        paper_bgcolor: '#1a1a2e',
        plot_bgcolor: '#16213e',
        font: {
            color: '#e0e0e0',
            family: 'Courier New, monospace'
        },
        margin: { t: 40, b: 50, l: 50, r: 20 },
        xaxis: {
            gridcolor: '#0f3460',
            zeroline: true,
            zerolinewidth: 1,
            zerolinecolor: '#0f3460',
            title: 'PC1'
        },
        yaxis: {
            gridcolor: '#0f3460',
            zeroline: true,
            zerolinewidth: 1,
            zerolinecolor: '#0f3460',
            title: 'PC2'
        },
        showlegend: false,
        hovermode: 'closest'
    };

    const config = {
        responsive: true,
        displayModeBar: true,
        displaylogo: false
    };

    Plotly.react(plotDiv, [trace], layout, config);
}

/**
 * Simple HTML escape for tooltips
 */
function escape(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
