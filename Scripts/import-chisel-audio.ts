import { randomBytes } from "node:crypto";
import { readFile, rename, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { importAsset, replaceAssetSource } from "../../chisel/src/main/asset-store";
import { assetsJsonSchema } from "../../chisel/src/shared/schemas";
import { sourceStateJsonSchema } from "../../chisel/src/shared/source-state";
import { AssetCategoryEnum } from "../../chisel/src/shared/types";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const staging = path.resolve(process.argv[2] ?? "");
if (!process.argv[2]) throw new Error("Usage: bun Scripts/import-chisel-audio.ts <staging-directory>");

const manifest = JSON.parse(await readFile(path.join(root, "Content/Audio/ovani-import.json"), "utf8")) as {
  assets: Array<{ entry: string; note: string; slug: string }>;
};
const assetsPath = path.join(root, ".chisel/assets.json");
let existing = assetsJsonSchema.parse(JSON.parse(await readFile(assetsPath, "utf8")));
for (const selection of manifest.assets) {
  const extension = path.extname(selection.entry).toLowerCase();
  const sourcePath = path.join(staging, `${selection.slug}${extension}`);
  const current = existing.assets.find((asset) => asset.id === selection.slug);
  const asset = current
    ? await replaceAssetSource({
        assetId: current.id,
        projectPath: root,
        sourcePath
      })
    : await importAsset({
        category: AssetCategoryEnum.audio,
        name: selection.slug,
        note: selection.note,
        projectPath: root,
        sourcePath
      });
  console.log(`${current ? "Updated" : "Imported"} ${asset.id} -> ${asset.relativePath}`);
  existing = assetsJsonSchema.parse(JSON.parse(await readFile(assetsPath, "utf8")));
}

const statePath = path.join(root, ".chisel/commits.json");
const state = sourceStateJsonSchema.parse(JSON.parse(await readFile(statePath, "utf8")));
const latest = state.commits[0];
if (!latest) throw new Error("Commit source data in Chisel before importing audio.");
const changed = JSON.stringify(latest.assets) !== JSON.stringify(existing);
if (changed) {
  const commit = {
    ...latest,
    id: randomBytes(16).toString("base64url").slice(0, 21),
    committedAt: new Date().toISOString(),
    assets: existing
  };
  const next = sourceStateJsonSchema.parse({
    ...state,
    commits: [commit, ...state.commits].slice(0, 20)
  });
  const temporaryPath = `${statePath}.tmp`;
  await writeFile(temporaryPath, `${JSON.stringify(next, null, 2)}\n`, "utf8");
  await rename(temporaryPath, statePath);
  console.log(`Committed Chisel source snapshot ${commit.id}.`);
} else {
  console.log("Latest Chisel source snapshot already contains the selected audio assets.");
}
