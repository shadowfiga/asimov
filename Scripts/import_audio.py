"""Import only the selected licensed assets; works with ZIP64 on Windows."""

import argparse
from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import shutil
from zipfile import ZipFile


def digest(stream):
    checksum = hashlib.sha256()
    for block in iter(lambda: stream.read(1024 * 1024), b""):
        checksum.update(block)
    return checksum.digest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("pack_directory", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent / "Content" / "Audio"
    output = (root / "Ovani").resolve()
    manifest = json.loads((root / "ovani-import.json").read_text(encoding="utf-8"))
    with ExitStack() as stack:
        archives = {}
        pending = []
        destinations = set()
        # Validate every path and existing asset before writing anything.
        for asset in manifest["assets"]:
            archive_path = args.pack_directory / asset["archive"]
            if archive_path not in archives:
                archives[archive_path] = stack.enter_context(ZipFile(archive_path))
            archive = archives[archive_path]
            entry = archive.getinfo(asset["entry"])
            target = (output / asset["output"]).resolve()
            if not target.is_relative_to(output) or target == output:
                raise ValueError(f"Asset path escapes the import directory: {target}")
            if target in destinations:
                raise ValueError(f"Duplicate asset destination: {target}")
            destinations.add(target)
            if target.exists():
                with archive.open(entry) as source, target.open("rb") as existing:
                    if digest(source) != digest(existing):
                        raise ValueError(f"Existing asset differs; preserve or rename it first: {target}")
            else:
                pending.append((archive, entry, target))
        for archive, entry, target in pending:
            target.parent.mkdir(parents=True, exist_ok=True)
            # Exclusive creation: never overwrite local files. ZIP reads verify CRC.
            with archive.open(entry) as source, target.open("xb") as destination:
                try:
                    shutil.copyfileobj(source, destination)
                except BaseException:
                    destination.close()
                    target.unlink()  # Only the incomplete file this invocation just created.
                    raise
            print(f"Imported {target.relative_to(output)}")
    print("Audio assets ready. Rebuild the game to copy them into its output directory.")


if __name__ == "__main__":
    main()
