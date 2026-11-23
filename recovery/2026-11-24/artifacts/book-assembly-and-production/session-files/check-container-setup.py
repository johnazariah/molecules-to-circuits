import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path("/Users/johnaz/PhD/copilot-worktrees/encodings-book/johnazariah-didactic-telegram")


class ContainerSetupTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="book container ")
        self.root = Path(self.temporary.name) / "checkout with spaces"
        (self.root / ".devcontainer").mkdir(parents=True)
        for name in ("setup.sh", "devcontainer.json"):
            shutil.copy2(ROOT / ".devcontainer" / name, self.root / ".devcontainer" / name)
        self.log = self.root / "calls.jsonl"
        self.bin = self.root / "mock-bin"
        self.bin.mkdir()
        mock = (
            f"#!{sys.executable}\n"
            "import json, os, pathlib, sys\n"
            "with open(os.environ['CALL_LOG'], 'a') as stream:\n"
            "    stream.write(json.dumps({'command':pathlib.Path(sys.argv[0]).name,"
            "'args':sys.argv[1:], 'env':{key:os.environ.get(key) for key in "
            "['PATH','PYTHON','MMDC','PLAYWRIGHT_BROWSERS_PATH','MMDC_NO_SANDBOX',"
            "'PUPPETEER_SKIP_DOWNLOAD']}})+'\\n')\n"
            "if os.environ.get('FAIL_COMMAND') == pathlib.Path(sys.argv[0]).name:\n"
            "    sys.exit(17)\n"
        )
        for path in [
            *(self.bin / name for name in ("sudo", "npm", "dotnet", "python3")),
            self.root / ".venv/bin/python",
            self.root / "_build/tools/node_modules/.bin/mmdc",
        ]:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(mock)
            path.chmod(0o755)
        self.env = dict(os.environ, CODESPACES="true", CALL_LOG=str(self.log),
                        PATH=str(self.bin) + os.pathsep + os.environ["PATH"])

    def tearDown(self):
        self.temporary.cleanup()

    def run_setup(self, *args):
        return subprocess.run(
            ["bash", str(self.root / ".devcontainer/setup.sh"), *args],
            cwd=self.temporary.name, env=self.env, capture_output=True, text=True,
        )

    def calls(self):
        return [json.loads(line) for line in self.log.read_text().splitlines()]

    def test_configuration(self):
        config = json.loads((self.root / ".devcontainer/devcontainer.json").read_text())
        self.assertEqual(config["image"], "mcr.microsoft.com/devcontainers/dotnet:1-10.0")
        self.assertEqual(config["features"]["ghcr.io/devcontainers/features/python:1"]["version"], "3.12")
        self.assertEqual(config["features"]["ghcr.io/devcontainers/features/node:1"]["version"], "22")
        self.assertEqual(config["postCreateCommand"], "bash .devcontainer/setup.sh")
        result = self.run_setup()
        self.assertEqual(result.returncode, 0, result.stderr)
        calls = self.calls()
        for key, value in config["remoteEnv"].items():
            expected = value.replace("${containerWorkspaceFolder}", str(self.root))
            expected = expected.replace("${containerEnv:PATH}", self.env["PATH"])
            self.assertTrue(all(call["env"][key] == expected for call in calls), key)
        self.assertEqual(
            config["customizations"]["vscode"]["settings"]["python.defaultInterpreterPath"],
            config["remoteEnv"]["PYTHON"],
        )

    def test_all_install_commands_are_local_and_ordered(self):
        result = self.run_setup()
        self.assertEqual(result.returncode, 0, result.stderr)
        calls = self.calls()
        self.assertEqual([c["command"] for c in calls],
                         ["sudo", "sudo", "python3", "python", "npm", "python",
                          "dotnet", "python", "mmdc"])
        self.assertEqual(calls[2]["args"], ["-m", "venv", str(self.root / ".venv")])
        self.assertEqual(calls[3]["args"],
                         ["-m", "pip", "install", "--quiet", "-r", str(self.root / "requirements-data.txt"),
                          "-r", str(self.root / "requirements.txt"), "playwright"])
        self.assertEqual(calls[4]["args"],
                         ["install", "--prefix", str(self.root / "_build/tools"),
                          "--no-audit", "--no-fund", "@mermaid-js/mermaid-cli@11.12.0"])
        self.assertEqual(calls[4]["env"]["PUPPETEER_SKIP_DOWNLOAD"], "true")
        self.assertEqual(calls[5]["args"], ["-m", "playwright", "install", "--with-deps", "chromium"])
        self.assertEqual(calls[7]["args"], ["-m", "jupyter_book", "--version"])
        for call in calls:
            self.assertNotIn("--break-system-packages", call["args"])
            self.assertNotIn("-g", call["args"])
            self.assertNotIn("chown", call["args"])
            self.assertNotIn("restore", call["args"])

    def test_dry_run_executes_no_commands(self):
        result = self.run_setup("--dry-run")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse(self.log.exists())
        self.assertIn("Dry run only; no dependencies installed.", result.stdout)
        self.assertNotIn("Environment ready.", result.stdout)
        self.assertIn("@mermaid-js/mermaid-cli@11.12.0", result.stdout)

    def test_bad_arguments_fail_before_commands(self):
        for args in (("--wrong",), ("--dry-run", "--wrong")):
            result = self.run_setup(*args)
            self.assertEqual(result.returncode, 2)
            self.assertIn("Usage:", result.stderr)
            self.assertFalse(self.log.exists())

    def test_install_failure_stops_before_browser_or_success_message(self):
        self.env["FAIL_COMMAND"] = "npm"
        result = self.run_setup()
        self.assertEqual(result.returncode, 17)
        self.assertEqual(self.calls()[-1]["command"], "npm")
        self.assertNotIn("Environment ready.", result.stdout)


if __name__ == "__main__":
    unittest.main()
