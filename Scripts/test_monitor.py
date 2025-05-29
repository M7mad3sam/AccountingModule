#!/usr/bin/env python3
"""
Test script for GitHub Actions Monitor auto-fix capabilities

This script tests the error detection and auto-fix capabilities of the
GitHub Actions Monitor without requiring actual GitHub API calls.
"""

import os
import sys
import tempfile
import unittest
from unittest.mock import patch, MagicMock

# Add the Scripts directory to the path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from Scripts.github_actions_monitor import GitHubActionsMonitor, ERROR_PATTERNS

class TestGitHubActionsMonitor(unittest.TestCase):
    def setUp(self):
        self.monitor = GitHubActionsMonitor("fake_token", "owner/repo")
    
    def test_extract_build_errors(self):
        """Test extraction of build errors from log content"""
        log_content = """
2025-05-29T00:00:00.0000000Z ##[group]Starting: Build
2025-05-29T00:00:01.0000000Z ##[command]dotnet build --no-restore --configuration Release
2025-05-29T00:00:02.0000000Z Microsoft (R) Build Engine version 17.0.0+c9eb9dd64 for .NET
2025-05-29T00:00:03.0000000Z Build started 5/29/2025 00:00:03.
2025-05-29T00:00:04.0000000Z /home/runner/work/AccountingModule/Areas/Accounting/ViewModels/ClientVendorViewModels.cs(120,21): error CS1061: 'ClientListViewModel' does not contain a definition for 'Clients' and no accessible extension method 'Clients' accepting a first argument of type 'ClientListViewModel' could be found
2025-05-29T00:00:05.0000000Z /home/runner/work/AccountingModule/Areas/Accounting/ViewModels/JournalEntryViewModels.cs(45,16): error CS0246: The type or namespace name 'DisplayAttribute' could not be found (are you missing a using directive or an assembly reference?)
2025-05-29T00:00:06.0000000Z Build FAILED.
        """
        
        errors = self.monitor.extract_build_errors(log_content)
        
        self.assertEqual(len(errors), 2)
        self.assertEqual(errors[0]["type"], "missing_property")
        self.assertEqual(errors[0]["message"], "'ClientListViewModel' does not contain a definition for 'Clients' and no accessible extension method 'Clients' accepting a first argument of type 'ClientListViewModel' could be found")
        self.assertEqual(errors[1]["type"], "missing_namespace")
    
    def test_fix_missing_property(self):
        """Test fixing missing property in view model"""
        # Create a temporary file with a class missing a property
        with tempfile.NamedTemporaryFile(mode='w+', suffix='.cs', delete=False) as temp:
            temp.write("""
using System;
namespace Test {
    public class TestViewModel
    {
        public string Name { get; set; }
    }
}
            """)
            temp_path = temp.name
        
        try:
            # Create an error object
            error = {
                "type": "missing_property",
                "message": "'TestViewModel' does not contain a definition for 'Description'",
                "file": temp_path,
                "line": 5,
                "column": 10
            }
            
            # Apply the fix
            result = self.monitor.fix_missing_property(error)
            
            # Verify the fix was applied
            self.assertTrue(result)
            
            # Read the file and check if the property was added
            with open(temp_path, 'r') as f:
                content = f.read()
                self.assertIn("public string Description { get; set; }", content)
        
        finally:
            # Clean up
            os.unlink(temp_path)

if __name__ == '__main__':
    unittest.main()
