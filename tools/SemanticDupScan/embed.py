"""Embedding-based semantic duplication pass over a SemanticDupScan corpus.

Reads corpus.jsonl (produced by the C# extractor), embeds each member body with a
local FastEmbed model (ONNX, no PyTorch, no API key), and ranks member pairs by
cosine similarity. Complements the extractor's exact-hash + Jaccard pass by
catching restructured (not just renamed) duplicates.

Setup:
    python3 -m venv .venv && .venv/bin/pip install fastembed numpy
Run (from repo root, after the C# extractor has written the corpus):
    tools/SemanticDupScan/.venv/bin/python tools/SemanticDupScan/embed.py \
        --corpus tools/SemanticDupScan/out/corpus.jsonl
"""
import argparse
import json

import numpy as np
from fastembed import TextEmbedding


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--corpus", default="tools/SemanticDupScan/out/corpus.jsonl")
    parser.add_argument("--out", default="tools/SemanticDupScan/out/embed_pairs.json")
    parser.add_argument("--model", default="BAAI/bge-small-en-v1.5")
    parser.add_argument("--field", default="rawText", choices=["rawText", "normalizedText"])
    parser.add_argument("--min-tokens", type=int, default=40)
    parser.add_argument("--top", type=int, default=45)
    args = parser.parse_args()

    rows = []
    for line in open(args.corpus):
        row = json.loads(line)
        if row.get("tokenCount", 0) >= args.min_tokens:
            rows.append(row)
    print(f"embedding {len(rows)} members (>= {args.min_tokens} tokens) with {args.model}", flush=True)

    model = TextEmbedding(model_name=args.model)
    embeddings = np.array(list(model.embed(row[args.field] for row in rows)), dtype=np.float32)
    embeddings /= np.clip(np.linalg.norm(embeddings, axis=1, keepdims=True), 1e-9, None)

    similarity = embeddings @ embeddings.T
    upper = np.triu_indices(len(rows), k=1)
    scores = similarity[upper]
    order = np.argsort(-scores)

    def loc(row):
        return f'{row["file"].split("Documents/Pdf/")[-1]}:{row["startLine"]}'

    for threshold in [0.97, 0.95, 0.92, 0.9, 0.88, 0.85]:
        print(f"pairs >= {threshold:.2f}: {int((scores >= threshold).sum())}", flush=True)

    print(f"=== top {args.top} pairs by cosine ===", flush=True)
    for rank in order[:args.top]:
        i, j = int(upper[0][rank]), int(upper[1][rank])
        a, b = rows[i], rows[j]
        print(f'{scores[rank]:.3f}  {loc(a)}  {a["member"]}   <->   {loc(b)}  {b["member"]}', flush=True)

    full = []
    for rank in order[:400]:
        i, j = int(upper[0][rank]), int(upper[1][rank])
        a, b = rows[i], rows[j]
        full.append({"cosine": float(scores[rank]),
                     "a": {"loc": loc(a), "member": a["member"], "signature": a["signature"]},
                     "b": {"loc": loc(b), "member": b["member"], "signature": b["signature"]}})
    json.dump(full, open(args.out, "w"), indent=1)
    print(f"wrote {args.out} ({len(full)} pairs)", flush=True)


if __name__ == "__main__":
    main()
