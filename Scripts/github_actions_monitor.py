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
POLL_INTERVAL = 50  # seconds - updated to 50 seconds as requested
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
    },
    # Cannot implicitly convert type
    r"Cannot implicitly convert type '([^']+)' to '([^']+)'": {
        "type": "type_conversion",
        "description": "Type conversion error"
    },
    # Missing definition (CS1061)
    r"error CS1061: '(\w+)' does not contain a definition for '(\w+)'": {
        "type": "missing_definition",
        "description": "Missing property or method definition"
    },
    # Type conversion error (CS0266)
    r"error CS0266: Cannot implicitly convert type '([^']+)' to '([^']+)'": {
        "type": "implicit_conversion",
        "description": "Cannot implicitly convert between types"
    },
    # Missing member (CS0117)
    r"error CS0117: '(\w+)' does not contain a definition for '(\w+)'": {
        "type": "missing_member",
        "description": "Missing member in class"
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
        
        try:
            # Download the logs (they're usually in a zip file)
            response = requests.get(url, headers=self.headers, allow_redirects=True, stream=True)
            
            if response.status_code == 302:
                # Follow redirect to download logs
                download_url = response.headers.get("Location")
                log_response = requests.get(download_url, stream=True)
                
                if log_response.status_code == 200:
                    # Save the logs to a temporary file
                    temp_zip = f"/tmp/workflow_logs_{run_id}.zip"
                    temp_dir = f"/tmp/workflow_logs_{run_id}"
                    
                    with open(temp_zip, 'wb') as f:
                        for chunk in log_response.iter_content(chunk_size=8192):
                            f.write(chunk)
                    
                    # Extract the logs
                    os.makedirs(temp_dir, exist_ok=True)
                    subprocess.run(['unzip', '-o', temp_zip, '-d', temp_dir], check=False)
                    
                    # Read all log files and concatenate them
                    all_logs = ""
                    for root, _, files in os.walk(temp_dir):
                        for file in files:
                            try:
                                with open(os.path.join(root, file), 'r') as f:
                                    all_logs += f.read() + "\n\n"
                            except Exception as e:
                                print(f"Error reading log file {file}: {e}")
                    
                    # Clean up
                    subprocess.run(['rm', '-rf', temp_dir, temp_zip], check=False)
                    
                    return all_logs
            
            print(f"Error fetching logs for run {run_id}: {response.status_code}")
            return ""
            
        except Exception as e:
            print(f"Exception while fetching logs for run {run_id}: {e}")
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
        
        # Look for .NET build errors with file paths
        error_lines = re.findall(r'([/\\][\w/\\.-]+\.cs)(?:\((\d+),(\d+)\))?: error (\w+): (.*?)(?:\r?\n|$)', log_content)
        
        for match in error_lines:
            if len(match) >= 5:
                file_path, line_str, col_str, error_code, error_message = match
                
                # Convert line and column to integers if possible
                line = int(line_str) if line_str and line_str.isdigit() else None
                column = int(col_str) if col_str and col_str.isdigit() else None
                
                error = {
                    "code": error_code,
                    "message": error_message.strip(),
                    "file": file_path,
                    "line": line,
                    "column": column
                }
                
                # Identify error type based on patterns
                for pattern, info in ERROR_PATTERNS.items():
                    if re.search(pattern, error_message) or re.search(pattern, f"error {error_code}: {error_message}"):
                        error["type"] = info["type"]
                        error["description"] = info["description"]
                        break
                else:
                    error["type"] = "unknown"
                    error["description"] = "Unknown error type"
                    
                errors.append(error)
        
        # If no errors found with the first pattern, try a more general pattern
        if not errors:
            general_errors = re.findall(r'error (\w+): (.*?)(?:\r?\n|$)', log_content)
            for error_code, error_message in general_errors:
                error = {
                    "code": error_code,
                    "message": error_message.strip(),
                    "file": None,
                    "line": None,
                    "column": None
                }
                
                # Identify error type based on patterns
                for pattern, info in ERROR_PATTERNS.items():
                    if re.search(pattern, error_message) or re.search(pattern, f"error {error_code}: {error_message}"):
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
        if not error.get("file"):
            # Try to extract file path from the error message
            file_match = re.search(r'([/\\][\w/\\.-]+\.cs)', error["message"])
            if file_match:
                error["file"] = file_match.group(1)
                # Convert to local path
                error["file"] = error["file"].replace("/home/runner/work/AccountingModule/AccountingModule/", REPO_PATH + "/")
        
        if not error.get("file") or not os.path.exists(error["file"]):
            print(f"File not found: {error.get('file')}")
            return False
            
        # Extract class name and property name
        match = None
        if error["type"] == "missing_definition" or error["type"] == "missing_property" or error["type"] == "missing_member":
            match = re.search(r"'(\w+)' does not contain a definition for '(\w+)'", error["message"])
            if not match:
                match = re.search(r"'(\w+)' does not contain a definition for '(\w+)'", f"error {error['code']}: {error['message']}")
        
        if not match:
            print(f"Could not extract class and property names from: {error['message']}")
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
            print(f"Could not find class {class_name} in {error['file']}")
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
            print(f"Could not find class end for {class_name} in {error['file']}")
            return False
        
        # Determine property type based on name
        property_type = "string"
        if property_name.startswith("Is") or property_name.startswith("Has") or property_name.endswith("Enabled") or property_name.endswith("Active"):
            property_type = "bool"
        elif property_name.endswith("Id") or property_name.endswith("Count") or property_name.endswith("Number"):
            property_type = "int"
        elif property_name.endswith("Date") or property_name.endswith("Time"):
            property_type = "DateTime"
        elif property_name.endswith("Amount") or property_name.endswith("Price") or property_name.endswith("Value"):
            property_type = "decimal"
        elif property_name.startswith("Available") and property_name.endswith("s"):
            # Collection property
            item_type = property_name[9:-1]  # Remove "Available" and "s"
            property_type = f"IEnumerable<{item_type}>"
            
            # Add using System.Collections.Generic if not present
            if "using System.Collections.Generic;" not in content:
                using_match = re.search(r"using [^;]+;", content)
                if using_match:
                    content = content[:using_match.end()] + "\nusing System.Collections.Generic;" + content[using_match.end():]
        
        # Add the missing property
        property_code = f"""
        [Display(Name = "{' '.join(re.findall('[A-Z][^A-Z]*', property_name))}")]
        public {property_type} {property_name} {{ get; set; }}
        """
        
        # Add using System.ComponentModel.DataAnnotations if not present and we're using Display attribute
        if "using System.ComponentModel.DataAnnotations;" not in content and "[Display" in property_code:
            using_match = re.search(r"using [^;]+;", content)
            if using_match:
                content = content[:using_match.end()] + "\nusing System.ComponentModel.DataAnnotations;" + content[using_match.end():]
        
        new_content = content[:class_end] + property_code + content[class_end:]
        
        # Write the updated content
        with open(error["file"], "w") as f:
            f.write(new_content)
            
        print(f"Added missing property '{property_name}' to class '{class_name}' in {error['file']}")
        return True
    
    def fix_type_conversion(self, error: Dict) -> bool:
        """
        Fix type conversion errors by adding explicit casts.
        
        Args:
            error: Error object with details
            
        Returns:
            True if fix was applied, False otherwise
        """
        if not error.get("file"):
            # Try to extract file path from the error message
            file_match = re.search(r'([/\\][\w/\\.-]+\.cs)', error["message"])
            if file_match:
                error["file"] = file_match.group(1)
                # Convert to local path
                error["file"] = error["file"].replace("/home/runner/work/AccountingModule/AccountingModule/", REPO_PATH + "/")
        
        if not error.get("file") or not os.path.exists(error["file"]):
            print(f"File not found: {error.get('file')}")
            return False
        
        # Extract source and target types
        match = None
        if error["type"] == "type_conversion" or error["type"] == "implicit_conversion":
            match = re.search(r"Cannot implicitly convert type '([^']+)' to '([^']+)'", error["message"])
            if not match:
                match = re.search(r"Cannot implicitly convert type '([^']+)' to '([^']+)'", f"error {error['code']}: {error['message']}")
        
        if not match:
            print(f"Could not extract type information from: {error['message']}")
            return False
        
        source_type = match.group(1)
        target_type = match.group(2)
        
        # Read the file
        with open(error["file"], "r") as f:
            content = f.read()
        
        # If we have line information, try to fix that specific line
        if error.get("line") is not None:
            lines = content.split('\n')
            if 0 <= error["line"] - 1 < len(lines):
                line = lines[error["line"] - 1]
                
                # Look for assignment patterns
                assignment_match = re.search(r'(\w+)\s*=\s*([^;]+);', line)
                if assignment_match:
                    var_name = assignment_match.group(1)
                    expression = assignment_match.group(2)
                    
                    # Add explicit cast
                    new_line = line.replace(f"{var_name} = {expression}", f"{var_name} = ({target_type}){expression}")
                    lines[error["line"] - 1] = new_line
                    
                    # Write the updated content
                    with open(error["file"], "w") as f:
                        f.write('\n'.join(lines))
                    
                    print(f"Added explicit cast to {target_type} in {error['file']} at line {error['line']}")
                    return True
        
        print(f"Could not fix type conversion error in {error['file']}")
        return False
    
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
            success = False
            
            if error["type"] == "missing_property" or error["type"] == "missing_definition" or error["type"] == "missing_member":
                success = self.fix_missing_property(error)
            elif error["type"] == "type_conversion" or error["type"] == "implicit_conversion":
                success = self.fix_type_conversion(error)
            # Add more fix implementations here
            
            if success:
                fixes_applied += 1
                    
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
                                for i, error in enumerate(errors):
                                    print(f"Error {i+1}: {error['type']} - {error['message']}")
                                
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
