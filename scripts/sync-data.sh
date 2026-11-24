#!/usr/bin/env bash
# Copies the item lists (skins, gloves, agents, stickers, keychains, music, pins) from the original
# WeaponPaints (Nereziel/cs2-WeaponPaints, website/data) into data/ (the plugin) and website/data/.
# Refuses lists that do not parse or lost more than a tenth of their items.
# Last line of output: "changed" or "unchanged".
set -euo pipefail
cd "$(dirname "$0")/.."

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
git clone -q --depth 1 --filter=blob:none --sparse https://github.com/Nereziel/cs2-WeaponPaints "$tmp/upstream"
git -C "$tmp/upstream" sparse-checkout set website/data
src="$tmp/upstream/website/data"

node - "$src" data <<'EOF'
const fs = require('fs');
const path = require('path');
const [src, dst] = process.argv.slice(2);
const files = fs.readdirSync(src).filter((f) => f.endsWith('.json'));
if (!files.includes('skins_en.json')) throw new Error('no skins_en.json in the original');
for (const f of files) {
  const items = JSON.parse(fs.readFileSync(path.join(src, f), 'utf8'));
  if (!Array.isArray(items)) throw new Error(`${f}: not a list`);
  const old = path.join(dst, f);
  if (!fs.existsSync(old)) continue;
  const before = JSON.parse(fs.readFileSync(old, 'utf8')).length;
  if (items.length < before * 0.9) throw new Error(`${f}: ${before} -> ${items.length} items`);
}
console.log(`${files.length} lists checked`);
EOF

cp "$src"/*.json data/
cp "$src"/*.json website/data/
if [ -z "$(git status --porcelain -- data website/data)" ]; then echo unchanged; else echo changed; fi
