#!/bin/bash

echo "Simple Special Character Test"
echo "==========================="

# Test our C# algorithm directly
function test_special_char() {
    local input="$1"
    local expected="$2"
    local desc="$3"
    
    echo "Test: $desc"
    echo "Input: $input"
    echo "Expected: $expected"
    echo ""
}

# Run a few key tests
test_special_char "Hello world!" "Hello world!" "Basic text - no special chars"
test_special_char "Text with - dash" "Text with  dash" "Dash should be hidden"
test_special_char "Text with ~ tilde" "Text with  tilde" "Tilde should be hidden"
test_special_char "Text with \\- escaped dash" "Text with - escaped dash" "Escaped dash should be visible"
test_special_char "Text with \\~ escaped tilde" "Text with ~ escaped tilde" "Escaped tilde should be visible"
test_special_char "Text with \\s sequence" "Text with  sequence" "\\s sequence should be hidden"
test_special_char "Double backslash: \\\\" "Double backslash: \\" "Double backslash should be shown as a single backslash"

echo "Tests completed - Please compare expected vs actual results in the C# code"