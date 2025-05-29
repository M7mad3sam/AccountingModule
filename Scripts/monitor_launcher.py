#!/usr/bin/env python3
"""
GitHub Actions Monitor Launcher Script

This script sets up and runs the GitHub Actions monitor as a background process.
It handles authentication, configuration, and ensures the monitor stays running.

Usage:
    python monitor_launcher.py --token <github_token> --repo <owner/repo>
"""

import argparse
import os
import subprocess
import sys
import time
from pathlib import Path

# Get the absolute path to the script directory
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_PATH = os.path.dirname(SCRIPT_DIR)
MONITOR_SCRIPT = os.path.join(SCRIPT_DIR, "github_actions_monitor.py")
LOG_FILE = os.path.join(SCRIPT_DIR, "monitor.log")

def setup_monitor(token, repo, interval=60):
    """
    Set up and launch the GitHub Actions monitor.
    
    Args:
        token: GitHub personal access token
        repo: Repository in format 'owner/repo'
        interval: Polling interval in seconds
    """
    print(f"Setting up GitHub Actions monitor for {repo}")
    print(f"Logs will be written to {LOG_FILE}")
    
    # Ensure the monitor script is executable
    os.chmod(MONITOR_SCRIPT, 0o755)
    
    # Launch the monitor script as a background process
    with open(LOG_FILE, "a") as log:
        process = subprocess.Popen(
            [
                "python3", 
                MONITOR_SCRIPT, 
                "--token", token, 
                "--repo", repo, 
                "--interval", str(interval)
            ],
            stdout=log,
            stderr=log,
            cwd=REPO_PATH
        )
    
    print(f"Monitor started with PID {process.pid}")
    print("The monitor will now run in the background and automatically fix build errors.")
    print("You can check the logs at any time by running: cat " + LOG_FILE)

def main():
    parser = argparse.ArgumentParser(description="Launch GitHub Actions monitor")
    parser.add_argument("--token", required=True, help="GitHub personal access token")
    parser.add_argument("--repo", required=True, help="Repository in format 'owner/repo'")
    parser.add_argument("--interval", type=int, default=60, help="Polling interval in seconds")
    
    args = parser.parse_args()
    
    setup_monitor(args.token, args.repo, args.interval)

if __name__ == "__main__":
    main()
