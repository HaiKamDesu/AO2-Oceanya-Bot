#!/bin/bash

# Simple script to verify special character handling 
# For use when dotnet is not available

# Function that simulates the special character processing logic
process_special_chars() {
  local input="$1"
  local result=""
  local escaped=false
  local i=0
  
  while [ $i -lt ${#input} ]; do
    c="${input:$i:1}"
    
    # Handle backslash for escaping
    if [ "$c" = "\\" ] && [ "$escaped" = false ]; then
      # End of string - append the backslash
      if [ $i -eq $((${#input} - 1)) ]; then
        result="${result}${c}"
        i=$((i + 1))
        continue
      fi
      
      next_char="${input:$((i+1)):1}"
      
      # Special \s case
      if [ "$next_char" = "s" ]; then
        i=$((i + 2))
        continue
      fi
      
      # Escaped backslash
      if [ "$next_char" = "\\" ]; then
        result="${result}${c}"
        i=$((i + 2))
        escaped=true
        continue
      fi
      
      # Next character is a special character
      if [ "$next_char" = "-" ] || [ "$next_char" = "\"" ] || [ "$next_char" = "~" ] || 
         [ "$next_char" = "{" ] || [ "$next_char" = "}" ]; then
        escaped=true
        i=$((i + 1))
        continue
      fi
      
      # Regular backslash
      result="${result}${c}"
      i=$((i + 1))
      continue
    fi
    
    # If character is special and not escaped, skip it
    if [ "$escaped" = false ] && 
       ([ "$c" = "-" ] || [ "$c" = "\"" ] || [ "$c" = "~" ] || 
        [ "$c" = "{" ] || [ "$c" = "}" ]); then
      i=$((i + 1))
      continue
    fi
    
    # Add the character to the result
    result="${result}${c}"
    escaped=false
    i=$((i + 1))
  done
  
  echo "$result"
}

# Run tests
echo "Special Character Processing Tests"
echo "=================================="
echo

# Basic test
input="Hello world!"
expected="Hello world!"
result=$(process_special_chars "$input")
echo "Test: Basic text"
echo "Input: $input"
echo "Expected: $expected"
echo "Result: $result"
if [[ "$result" == "$expected" ]]; then
  echo "PASSED"
else
  echo "FAILED"
fi
echo

# Special characters test
input="Text with - dash and ~ tilde and { brace }"
expected="Text with  dash and  tilde and  brace "
result=$(process_special_chars "$input")
echo "Test: Special characters"
echo "Input: $input"
echo "Expected: $expected"
echo "Result: $result"
if [[ "$result" == "$expected" ]]; then
  echo "PASSED"
else
  echo "FAILED"
fi
echo

# Escaped characters test
input="Text with \\- escaped dash and \\~ escaped tilde"
expected="Text with - escaped dash and ~ escaped tilde"
result=$(process_special_chars "$input")
echo "Test: Escaped characters"
echo "Input: $input"
echo "Expected: $expected"
echo "Result: $result"
if [[ "$result" == "$expected" ]]; then
  echo "PASSED"
else
  echo "FAILED"
fi
echo

# Complex test
input="Text with {hidden} and \\{visible\\} special chars"
expected="Text with  and {visible} special chars"
result=$(process_special_chars "$input")
echo "Test: Complex case"
echo "Input: $input"
echo "Expected: $expected"
echo "Result: $result"
if [[ "$result" == "$expected" ]]; then
  echo "PASSED"
else
  echo "FAILED"
fi
echo

# Backslash test
input="Double backslash: \\\\"
expected="Double backslash: \\"
result=$(process_special_chars "$input")
echo "Test: Backslash handling"
echo "Input: $input"
echo "Expected: $expected"
echo "Result: $result"
if [[ "$result" == "$expected" ]]; then
  echo "PASSED"
else
  echo "FAILED"
fi
echo

echo "Tests completed!"