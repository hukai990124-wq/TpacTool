# Changelog

## 0.4.1 - 2026-10-18

- Model export now falls back to a mesh's second material or a default material when its primary material reference cannot be resolved, instead of crashing. Material collection also reads the actual second-material reference. This covers the broken references found in `mebody`, `mebody2`, and `meleg`.
- Contributor: OpenAI Codex.
