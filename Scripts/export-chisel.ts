// Use the real sibling Chisel exporter; never maintain a second runtime contract here.
import { mkdir, readFile, writeFile, copyFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createMonoGameExportBundle, monoGameAssetExportPath } from "../../chisel/src/shared/monogame-export";
import { sourceStateJsonSchema } from "../../chisel/src/shared/source-state";
import { validatedDataTableSchema } from "../../chisel/src/shared/schemas";
import { validateProjectContent, ProjectValidationSeverity } from "../../chisel/src/shared/project-validation";
import { validateLocalizationDocument, LocalizationProblemSeverity } from "../../chisel/src/shared/localization";
import { EDITOR_ONLY_TERRAIN_TABLE_IDS } from "../../chisel/src/shared/terrain-tables";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const state = sourceStateJsonSchema.parse(JSON.parse(await readFile(path.join(root, ".chisel/commits.json"), "utf8")));
const commit = state.commits[0];
if (!commit) throw new Error("Commit source data in Chisel before exporting.");
const tables = commit.tables.filter(table => !EDITOR_ONLY_TERRAIN_TABLE_IDS.has(table.id)).map(table => validatedDataTableSchema.parse(table));
const assets = commit.assets.assets;
const errors = [
  ...validateProjectContent(tables, assets, commit.localization).filter(issue => issue.severity === ProjectValidationSeverity.error),
  ...validateLocalizationDocument(commit.localization, assets).filter(issue => issue.severity === LocalizationProblemSeverity.error)
];
if (errors.length) throw new Error(errors.map(issue => issue.message).join("\n"));
const bundle = createMonoGameExportBundle({ ...commit.project, path: root }, tables, commit.committedAt, assets, commit.localization);
const check = process.argv.includes("--check");
for (const file of bundle.files) {
  if (!file.path.startsWith("GameData/Generated/") || file.path.includes("..")) throw new Error(`Invalid generated path: ${file.path}`);
  const target = path.join(root, file.path);
  if (check) {
    if ((await readFile(target, "utf8")).replaceAll("\r\n", "\n") !== file.content.replaceAll("\r\n", "\n")) {
      throw new Error(`Stale Chisel export: ${file.path}`);
    }
  } else {
    await mkdir(path.dirname(target), { recursive: true });
    await writeFile(target, file.content);
  }
}
for (const asset of assets) {
  const source = path.resolve(root, asset.relativePath);
  const relative = path.relative(path.join(root, ".chisel/assets"), source);
  if (relative.startsWith("..") || path.isAbsolute(relative)) throw new Error(`Asset is outside managed source: ${asset.relativePath}`);
  const target = path.join(root, monoGameAssetExportPath(asset));
  if (check) {
    if (!(await readFile(source)).equals(await readFile(target))) throw new Error(`Stale asset: ${asset.name}`);
  } else {
    await mkdir(path.dirname(target), { recursive: true });
    await copyFile(source, target);
  }
}
console.log(`${check ? "Verified" : "Exported"} ${bundle.files.length} MonoGame files from Chisel snapshot ${commit.id}.`);
