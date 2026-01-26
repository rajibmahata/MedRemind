# ? Dose Count Notation Support - Additional Documentation

## Important Clarification on Handwritten Prescription Formats

There are TWO different notations used in handwritten prescriptions, and the OpenAI parser needs to distinguish between them:

### 1. Binary Notation (Take/Skip Pattern)
**Format:** `1` = take, `0` = skip
- `1 0 0` = Take once daily in morning (skip afternoon, evening)
- `1 1 0` = Take twice daily (morning and afternoon)
- `1 1 1` = Take three times daily

### 2. Dose Count Notation (Frequency Indicator)
**Format:** Numbers represent frequency, NOT binary flags
- `0 0 0` = **Three times daily** (Morning, Afternoon, Evening) - dose not specified
- `0 0` = **Twice daily** (Morning, Evening) - dose not specified
- `0` = **Once daily** (Evening) - dose not specified

**CRITICAL:** When you see ONLY zeros (`0 0 0`, `0 0`, `0`), it means "take at these times" NOT "skip medication"!

### How to Distinguish:
1. **Only zeros** (`0 0 0`, `0 0`, `0`) ? Dose count notation = frequency indicator
2. **Mix of 1s and 0s** (`1-0-1`, `1-1-0`) ? Binary notation = take/skip pattern
3. **Numbers > 1** (`2-1-1`, `1-2-1`) ? Dose count notation with variable dosing

### Real Examples:

**Example 1: Dose Count - Three Times Daily**
```
Input: "Tab Metformin 500mg 0 0 0 x 30 days"
Interpretation:
- Format: 0 0 0 = THREE TIMES DAILY (not skip!)
- Frequency: Three times daily
- Timing: Morning, Afternoon, Evening
- Dose per time: To be specified by pharmacist
```

**Example 2: Dose Count - Twice Daily**
```
Input: "Cap Omeprazole 20mg 0 0 x 14 days"
Interpretation:
- Format: 0 0 = TWICE DAILY
- Frequency: Twice daily
- Timing: Morning and Evening
- Dose per time: To be specified by pharmacist
```

**Example 3: Dose Count - Once Daily**
```
Input: "Syp Paracetamol 250mg/5ml 0 x 3 days"
Interpretation:
- Format: 0 = ONCE DAILY (evening)
- Frequency: Once daily
- Timing: Evening
- Dose per time: To be specified by pharmacist
```

**Example 4: Binary Notation - Once Daily Morning**
```
Input: "Tab Aspirin 75mg 1-0-0 AC x 30 days"
Interpretation:
- Format: 1-0-0 = Take morning, Skip afternoon/evening
- Frequency: Once daily (morning)
- Timing: Morning before meals
```

**Example 5: Variable Dosing**
```
Input: "Tab Prednisolone 10mg 2-1-1 PC x 5 days"
Interpretation:
- Format: 2-1-1 = 2 tablets morning, 1 tablet afternoon, 1 tablet evening
- Frequency: Three times daily (variable dosing)
- Timing: After meals
```

---

## Implementation Notes

The parser prompt in `OpenAIPrescriptionParserAgent.cs` needs to be updated to clarify this distinction. Key points:

1. **Don't assume `0 0 0` means "skip medication"** - it's a frequency indicator
2. **Context matters** - look at the pattern as a whole
3. **Regional variations** - some doctors use this notation differently
4. **When uncertain** - treat as dose count notation and mark confidence lower

This clarification is particularly important for prescriptions from India and other countries where this notation is commonly used.

---

## TODO: Update Parser Prompt

The `CreateParserPrompt` method in `OpenAIPrescriptionParserAgent.cs` should be updated with this clarification. The current prompt needs modification to handle both notations correctly.

**Status:** Documentation updated, code update pending proper string escaping fix.
