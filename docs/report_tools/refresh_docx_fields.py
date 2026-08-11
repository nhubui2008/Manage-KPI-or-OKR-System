#!/usr/bin/env python3
"""Refresh Word fields and indexes in a DOCX through headless LibreOffice."""

from __future__ import annotations

import argparse
import socket
import subprocess
import tempfile
import time
from pathlib import Path

import uno
from com.sun.star.beans import PropertyValue


def property_value(name: str, value: object) -> PropertyValue:
    prop = PropertyValue()
    prop.Name = name
    prop.Value = value
    return prop


def reserve_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
        listener.bind(("127.0.0.1", 0))
        return int(listener.getsockname()[1])


def refresh_fields(document_path: Path) -> None:
    document_path = document_path.resolve()
    if not document_path.is_file():
        raise FileNotFoundError(document_path)

    port = reserve_port()
    with tempfile.TemporaryDirectory(prefix="kpi-report-libreoffice-") as profile:
        process = subprocess.Popen(
            [
                "soffice",
                f"-env:UserInstallation={Path(profile).as_uri()}",
                "--headless",
                "--nologo",
                "--nodefault",
                "--nofirststartwizard",
                "--norestore",
                f"--accept=socket,host=127.0.0.1,port={port};urp;StarOffice.ServiceManager",
            ],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )

        document = None
        desktop = None
        try:
            local_context = uno.getComponentContext()
            resolver = local_context.ServiceManager.createInstanceWithContext(
                "com.sun.star.bridge.UnoUrlResolver",
                local_context,
            )
            remote_context = None
            for _ in range(100):
                try:
                    remote_context = resolver.resolve(
                        f"uno:socket,host=127.0.0.1,port={port};urp;StarOffice.ComponentContext"
                    )
                    break
                except Exception:
                    if process.poll() is not None:
                        raise RuntimeError("LibreOffice stopped before accepting the UNO connection.")
                    time.sleep(0.1)
            if remote_context is None:
                raise TimeoutError("Timed out while connecting to LibreOffice.")

            desktop = remote_context.ServiceManager.createInstanceWithContext(
                "com.sun.star.frame.Desktop",
                remote_context,
            )
            document = desktop.loadComponentFromURL(
                uno.systemPathToFileUrl(str(document_path)),
                "_blank",
                0,
                (
                    property_value("Hidden", True),
                    property_value("UpdateDocMode", 3),
                ),
            )
            if document is None:
                raise RuntimeError(f"LibreOffice could not open {document_path}.")

            indexes = document.getDocumentIndexes()
            for index in range(indexes.getCount()):
                indexes.getByIndex(index).update()
            document.getTextFields().refresh()
            if hasattr(document, "calculateAll"):
                document.calculateAll()
            document.store()
            document.close(True)
            document = None
        finally:
            if document is not None:
                document.close(True)
            if desktop is not None:
                desktop.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.terminate()
                process.wait(timeout=5)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("document", type=Path)
    args = parser.parse_args()
    refresh_fields(args.document)
    print(args.document.resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
