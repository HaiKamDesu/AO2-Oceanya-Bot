#!/usr/bin/env python3

def process_special_chars(text):
    """Python implementation of the special character processing algorithm"""
    if text is None or text == "":
        return text
    
    # Define special characters
    special_chars = {'-': True, '"': True, '~': True, '{': True, '}': True}
    
    result = []
    escaped = False
    i = 0
    
    while i < len(text):
        c = text[i]
        print(f"Processing char: '{c}' at position {i}, escaped={escaped}")
        
        # Handle backslash for escaping
        if c == '\\' and not escaped:
            print(f"  Found backslash at {i}")
            # End of string - append the backslash
            if i == len(text) - 1:
                result.append(c)
                i += 1
                print(f"  End of string, appending backslash")
                continue
            
            next_char = text[i + 1]
            print(f"  Next char is '{next_char}'")
            
            # Special \s case
            if next_char == 's':
                print(f"  Special \\s sequence, skipping both")
                # Skip both the backslash and 's'
                i += 2
                continue
            
            # Escaped backslash
            if next_char == '\\':
                print(f"  Escaped backslash, appending one backslash")
                result.append(c)
                i += 2
                escaped = True
                continue
            
            # Next character is a special character
            if next_char in special_chars:
                print(f"  Next char '{next_char}' is special, setting escaped=True")
                escaped = True
                i += 1
                continue
            
            # Regular backslash
            print(f"  Regular backslash, appending")
            result.append(c)
            i += 1
            continue
        
        # If the character is special and not escaped, skip it
        if c in special_chars and not escaped:
            print(f"  '{c}' is special and not escaped, skipping")
            i += 1
            continue
        
        # Add the character to the result
        print(f"  Adding '{c}' to result")
        result.append(c)
        escaped = False
        i += 1
    
    return ''.join(result)

# Test the failing case
test_input = "Normal {hidden} and \\{visible\\} special chars"
expected = "Normal  and {visible} special chars"
result = process_special_chars(test_input)

print("\nFinal results:")
print(f"Input:    '{test_input}'")
print(f"Expected: '{expected}'")
print(f"Result:   '{result}'")
print(f"Status:   {'PASSED' if result == expected else 'FAILED'}")

# What is different:
print("\nWhere the differences are:")
for i in range(min(len(expected), len(result))):
    if i < len(expected) and i < len(result) and expected[i] != result[i]:
        print(f"Position {i}: Expected '{expected[i]}', Got '{result[i]}'")

# Also check string representation to see any hidden characters
print("\nString representation:")
print(f"Expected: {repr(expected)}")
print(f"Result:   {repr(result)}")