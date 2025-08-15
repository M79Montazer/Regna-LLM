# Regna-LLM

## Overview
This repository contains the implementation and artifacts produced for the LLM course project that investigated natural-language NPCs for open-world RPGs. The project was developed in two phases:

### Phase 1 (Proof-of-concept):
local benchmarking and small-scale LoRA fine-tuning of an open-weight LLM (Colab notebooks), and a lightweight REST API + simple HTML client for text playtesting.

### Phase 2 (Playable demo):
integration of a stronger API-hosted LLM into a minimal JS game and a C# backend with semantic search FAISS-backed retrieval, access-level memory and function-calling.

## Repository structure
Top-level folders and important files (presented at a glance):
- `LICENSE.md`  Formal license statement and restrictions.
- `README.md`  This document.
- `phase_1/`
  - `different-llms.ipynb` we tested different open-weight and closed models to find which is best for us
  - `Gemma-fine-tune.ipynb` fine-tuned the model
  - `ngrok-test.ipynb` used ngrok to expose an API gate for using the finetuned model
  - `play.html the html` file used to test the phase 1
  - `key_sentence.txt` sentences describing the scenarios. used to make the scaniros using AI and refined by humans 
  - `train.jsonl` scenarios used to fine-tune the model
  - `valid.jsonl` scenarios used to validate the model
- `temp\` weirdly named, but contains the C# .net core webAPI backend of the phase 2
- `Regna_RPG` contains the JS frontend of the game. uses NW.js and pixi.js as the game engine
- `launcher` simple exe file, connecting the front and backend of the game on the client machine
