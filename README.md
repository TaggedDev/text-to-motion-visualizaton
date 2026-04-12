# Human Motion Data visualization service
A part of my Bachelor's diploma (2026) - Generative Human Motion Generation model.

---

## Features:
- A web server (asp.net core) that runs GET and POST endpoints for .npy files
- Select and play/pause the animation at localhost:5000/homepage
- Try the prompt embeddings mechanism using CLIP model at localhost:5000/embeddings 

## Data used
1. Source of motions - [HumanML3D](https://github.com/EricGuo5513/HumanML3D)
2. Text embedding - [CLIP](https://github.com/openai/CLIP)

## Stack:
- Used `NumSharp` for reading the .npy data
- Used `Tokenizers.DotNet` to load and use the [tokenizer.json](https://huggingface.co/Xenova/clip-vit-base-patch16/blob/main/tokenizer.json) config
- CLIP model is loaded and running using `Microsoft.ML.OnnxRuntime`
- Embeddings scatter plot visualization are computed locally, the vector dimension is reduced by the PCA algorithm using the `MathNet.Numerics` and then passed to plotly.js to be rendered
- Rigidbody motion is displayed using the `three.js` 

## Requirements:
- .NET 8
- Download the HumanML3D dataset and adjust the `appsettings.json`
- Put the [`clip-text-vit-32-float32-int32.onnx`](https://huggingface.co/rocca/openai-clip-js/blob/main/clip-text-vit-32-float32-int32.onnx) and [`tokenizer.json`](https://huggingface.co/Xenova/clip-vit-base-patch16/blob/main/tokenizer.json) downloaded from hugging face to the Weights/ folder in the project root.
