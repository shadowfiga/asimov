"""Import selected licensed audio through Chisel; works with ZIP64 on Windows."""

import argparse
from contextlib import ExitStack
import json
from pathlib import Path
import shutil
import subprocess
from tempfile import TemporaryDirectory
from zipfile import ZipFile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("pack_directory", type=Path)
    args = parser.parse_args()
    project = Path(__file__).resolve().parent.parent
    if shutil.which("bun") is None:
        raise RuntimeError("Bun is required to import and export Chisel assets.")
    if not (project.parent / "chisel" / "src" / "main" / "asset-store.ts").is_file():
        raise RuntimeError("The sibling Chisel checkout is required at ../chisel.")
    audio_root = project / "Content" / "Audio"
    manifest = json.loads((audio_root / "ovani-import.json").read_text(encoding="utf-8"))
    with TemporaryDirectory(prefix="asimov-chisel-audio-") as temporary:
        staging = Path(temporary)
        with ExitStack() as stack:
            archives = {}
            for asset in manifest["assets"]:
                archive_path = args.pack_directory / asset["archive"]
                if archive_path not in archives:
                    archives[archive_path] = stack.enter_context(ZipFile(archive_path))
                archive = archives[archive_path]
                entry = archive.getinfo(asset["entry"])
                suffix = Path(asset["entry"]).suffix.lower()
                target = staging / f"{asset['slug']}{suffix}"
                with archive.open(entry) as source, target.open("xb") as destination:
                    shutil.copyfileobj(source, destination)
                print(f"Extracted {asset['slug']} from {asset['archive']}", flush=True)
        subprocess.run(["bun", "Scripts/import-chisel-audio.ts", str(staging)], cwd=project, check=True)
        subprocess.run(["bun", "Scripts/export-chisel.ts"], cwd=project, check=True)
    print("Chisel audio assets committed and exported. Rebuild the game to copy them beside the executable.")


if __name__ == "__main__":
    main()
