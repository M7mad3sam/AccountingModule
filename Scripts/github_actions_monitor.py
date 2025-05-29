#!/usr/bin/env python3
"""
GitHub Actions Workflow Monitor and Auto-Fix Script

This script monitors GitHub Actions workflow runs for a repository,
detects build failures, extracts error logs, and attempts to automatically
fix common build errors.

Usage:
    python github_actions_monitor.py --token <github_token> --repo <owner/repo>
"""

import argparse
import json
import os
import re
import requests
import subprocess
import sys
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Tuple, Any

# Configuration
GITHUB_API_URL = "https://api.github.com"
POLL_INTERVAL = 60  # seconds
MAX_RETRIES = 3
REPO_PATH = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Error patterns and fixes
ERROR_PATTERNS = {
    # Missing property in view model
    r"'(\w+)' does not contain a definition for '(\w+)'": {
        "type": "missing_property",
        "description": "Missing property in view model"
    },
    # Missing using directive
    r"The type or namespace name '(\w+)' could not be found": {
        "type": "missing_namespace",
        "description": "Missing using directive or reference"
    },
    # Razor syntax error
    r"The attribute '([^']+)' is not a valid attribute": {
        "type": "razor_syntax",
        "description": "Invalid Razor syntax in view"
    }
}

class GitHubActionsMonitor:
    def __init__(self, token: str, repo: str):
        """
        Initialize the GitHub Actions monitor.
        
        Args:
            token: GitHub personal access token
            repo: Repository in format 'owner/repo'
        """
        self.token = token
        self.repo = repo
        self.headers = {
            "Authorization": f"token {token}",
            "Accept": "application/vnd.github.v3+json"
        }
        self.last_checked_run_id = None
        
    def get_workflow_runs(self, status: Optional[str] = None) -> List[Dict]:
        """
        Get recent workflow runs from GitHub Actions.
        
        Args:
            status: Filter by status (e.g., 'completed', 'failure')
            
        Returns:
            List of workflow run objects
        """
        url = f"{GITHUB_API_URL}/repos/{self.repo}/actions/runs"
        params = {}
        if status:
            params["status"] = status
            
        response = requests.get(url, headers=self.headers, params=params)
        if response.status_code != 200:
            print(f"Error fetching workflow runs: {response.status_code}")
            print(response.text)
            return []
            
        data = response.json()
        return data.get("workflow_runs", [])
    
    def get_workflow_logs(self, run_id: int) -> str:
        """
        Get logs for a specific workflow run.
        
        Args:
            run_id: Workflow run ID
            
        Returns:
            Log content as string
        """
        # First, get the available logs
        url = f"{GITHUB_API_URL}/repos/{self.repo}/actions/runs/{run_id}/logs"
        response = requests.get(url, headers=self.headers, allow_redirects=False)
        
        if response.status_code == 302:
            # Follow redirect to download logs
            download_url = response.headers.get("Location")
            log_response = requests.get(download_url)
            if log_response.status_code == 200:
                return log_response.text
        
        print(f"Error fetching logs for run {run_id}: {response.status_code}")
        return ""
    
    def extract_build_errors(self, log_content: str) -> List[Dict]:
        """
        Extract build errors from log content.
        
        Args:
            log_content: Log content as string
            
        Returns:
            List of error objects with type, message, and file info
        """
        errors = []
        
        # Look for .NET build errors
        error_lines = re.findall(r'(.*?): error (\w+): (.*?)(?:\r?\n|$)', log_content)
        
        for match in error_lines:
            file_info, error_code, error_message = match
            
            # Extract file path if available
            file_path = re.search(r'([\w\\/.]+\.cs)', file_info)
            file_path = file_path.group(1) if file_path else None
            
            # Extract line number if available
            line_number = re.search(r'\((\d+),(\d+)\)', file_info)
            line = int(line_number.group(1)) if line_number else None
            column = int(line_number.group(2)) if line_number else None
            
            error = {
                "code": error_code,
                "message": error_message.strip(),
                "file": file_path,
                "line": line,
                "column": column
            }
            
            # Identify error type based on patterns
            for pattern, info in ERROR_PATTERNS.items():
                if re.search(pattern, error_message):
                    error["type"] = info["type"]
                    error["description"] = info["description"]
                    break
            else:
                error["type"] = "unknown"
                error["description"] = "Unknown error type"
                
            errors.append(error)
            
        return errors
    
    def fix_missing_property(self, error: Dict) -> bool:
        """
        Fix missing property in view model.
        
        Args:
            error: Error object with details
            
        Returns:
            True if fix was applied, False otherwise
        """
        if not error.get("file") or not os.path.exists(error["file"]):
            return False
            
        match = re.search(r"'(\w+)' does not contain a definition for '(\w+)'", error["message"])
        if not match:
            return False
            
        class_name = match.group(1)
        property_name = match.group(2)
        
        # Read the file
        with open(error["file"], "r") as f:
            content = f.read()
        
        # Find the class definition
        class_pattern = rf"public\s+class\s+{class_name}"
        class_match = re.search(class_pattern, content)
        if not class_match:
            return False
            
        # Find the class closing brace
        class_start = class_match.start()
        brace_count = 0
        class_end = -1
        
        for i in range(class_start, len(content)):
            if content[i] == '{':
                brace_count += 1
            elif content[i] == '}':
                brace_count -= 1
                if brace_count == 0:
                    class_end = i
                    break
        
        if class_end == -1:
            return False
            
        # Add the missing property
        property_code = f"""
        [Display(Name = "{' '.join(re.findall('[A-Z][^A-Z]*', property_name))}")]
        public string {property_name} {{ get; set; }}
        """
        
        new_content = content[:class_end] + property_code + content[class_end:]
        
        # Write the updated content
        with open(error["file"], "w") as f:
            f.write(new_content)
            
        print(f"Added missing property '{property_name}' to class '{class_name}' in {error['file']}")
        return True
    
    def fix_errors(self, errors: List[Dict]) -> int:
        """
        Apply fixes for detected errors.
        
        Args:
            errors: List of error objects
            
        Returns:
            Number of successfully applied fixes
        """
        fixes_applied = 0
        
        for error in errors:
            if error["type"] == "missing_property":
                if self.fix_missing_property(error):
                    fixes_applied += 1
            # Add more fix implementations here
                    
        return fixes_applied
    
    def commit_and_push_fixes(self, fixes_count: int) -> bool:
        """
        Commit and push fixes to the repository.
        
        Args:
            fixes_count: Number of fixes applied
            
        Returns:
            True if successful, False otherwise
        """
        if fixes_count == 0:
            return False
            
        try:
            # Stage changes
            subprocess.run(["git", "add", "."], cwd=REPO_PATH, check=True)
            
            # Commit changes
            commit_message = f"Auto-fix: Applied {fixes_count} fixes for build errors"
            subprocess.run(["git", "commit", "-m", commit_message], cwd=REPO_PATH, check=True)
            
            # Push changes
            subprocess.run(["git", "push"], cwd=REPO_PATH, check=True)
            
            print(f"Successfully committed and pushed {fixes_count} fixes")
            return True
        except subprocess.CalledProcessError as e:
            print(f"Error committing and pushing fixes: {e}")
            return False
    
    def monitor_and_fix(self) -> None:
        """
        Main monitoring loop to check for failed builds and apply fixes.
        """
        print(f"Starting GitHub Actions monitor for {self.repo}...")
        
        while True:
            try:
                # Get recent workflow runs
                runs = self.get_workflow_runs()
                
                if not runs:
                    print("No workflow runs found")
                    time.sleep(POLL_INTERVAL)
                    continue
                
                # Check for new failed runs
                for run in runs:
                    run_id = run["id"]
                    
                    # Skip if we've already checked this run
                    if self.last_checked_run_id and run_id <= self.last_checked_run_id:
                        continue
                    
                    # Update the last checked run ID
                    if not self.last_checked_run_id or run_id > self.last_checked_run_id:
                        self.last_checked_run_id = run_id
                    
                    # Only process completed runs with failure status
                    if run["status"] == "completed" and run["conclusion"] == "failure":
                        print(f"Found failed workflow run: {run_id} ({run['name']})")
                        
                        # Get logs for the failed run
                        logs = self.get_workflow_logs(run_id)
                        
                        if logs:
                            # Extract build errors
                            errors = self.extract_build_errors(logs)
                            
                            if errors:
                                print(f"Found {len(errors)} build errors")
                                
                                # Apply fixes
                                fixes_count = self.fix_errors(errors)
                                
                                if fixes_count > 0:
                                    # Commit and push fixes
                                    self.commit_and_push_fixes(fixes_count)
                            else:
                                print("No actionable build errors found in logs")
                
                # Wait before checking again
                time.sleep(POLL_INTERVAL)
                
            except Exception as e:
                print(f"Error in monitoring loop: {e}")
                time.sleep(POLL_INTERVAL)

def main():
    # Define argument parser
    parser = argparse.ArgumentParser(description="Monitor GitHub Actions workflow runs and auto-fix build errors")
    parser.add_argument("--token", required=True, help="GitHub personal access token")
    parser.add_argument("--repo", required=True, help="Repository in format 'owner/repo'")
    parser.add_argument("--interval", type=int, help="Polling interval in seconds")
    
    args = parser.parse_args()
    
    # Use global variable
    if args.interval:
        global POLL_INTERVAL
        POLL_INTERVAL = args.interval
    
    monitor = GitHubActionsMonitor(args.token, args.repo)
    monitor.monitor_and_fix()

if __name__ == "__main__":
    main()
