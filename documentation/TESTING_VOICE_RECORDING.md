# 🎙️ Voice Recording End-to-End Testing Guide

## Task #18: Test Voice Recording End-to-End Flow

This document provides comprehensive testing procedures for the voice recording system in MedRemind.

---

## 🎯 Testing Objectives

1. Verify browser microphone access and permissions
2. Test recording functionality (start, stop, duration tracking)
3. Validate audio quality and playback
4. Test file upload and storage
5. Verify CRUD operations (create, read, update, delete)
6. Test error handling and edge cases

---

## 🔧 Prerequisites

### Environment Setup
- Backend API running on `http://localhost:5000`
- Web application running on `http://localhost:5001`
- SQLite database initialized
- User account created and logged in

### Browser Requirements
- **Chrome/Edge** (Recommended): Full support
- **Firefox**: Full support
- **Safari**: Limited support (requires HTTPS in production)

### Hardware Requirements
- Working microphone/audio input device
- Speakers/headphones for playback testing

---

## ✅ Test Cases

### 1. Microphone Permission Request

**Test ID**: VR-001
**Priority**: Critical
**Objective**: Verify microphone permission flow

**Steps**:
1. Navigate to `/voice-recordings`
2. Click "Record New Voice" button
3. Observe browser permission prompt

**Expected Results**:
- ✅ Browser shows microphone permission dialog
- ✅ Dialog shows "MedRemind wants to access your microphone"
- ✅ Options: "Allow" and "Block"

**Acceptance Criteria**:
- Permission prompt appears on first recording attempt
- If allowed: microphone icon shows "Microphone ready"
- If blocked: error message "Microphone access denied"

---

### 2. Start Recording

**Test ID**: VR-002
**Priority**: Critical
**Objective**: Verify recording can be started successfully

**Steps**:
1. Ensure microphone permission is granted
2. Enter recording name: "Mom's Voice"
3. Click "Start Recording" button
4. Speak into microphone: "Sweetheart, it's time for your medication"

**Expected Results**:
- ✅ "Start Recording" button changes to "Stop Recording"
- ✅ Recording pulse animation appears (red dot pulsing)
- ✅ Timer starts counting: "Recording... 1s", "Recording... 2s", etc.
- ✅ Recording indicator visible

**Acceptance Criteria**:
- Recording starts within 1 second of button click
- Timer increments every second
- Visual feedback is clear and responsive

---

### 3. Stop Recording

**Test ID**: VR-003
**Priority**: Critical
**Objective**: Verify recording can be stopped and audio captured

**Steps**:
1. Start recording (see VR-002)
2. Record for 5-10 seconds
3. Click "Stop Recording" button

**Expected Results**:
- ✅ Recording stops immediately
- ✅ Timer stops
- ✅ Audio preview section appears
- ✅ "Preview Recording" section shows recorded audio player
- ✅ File size and duration displayed
- ✅ "Save Recording" button enabled

**Acceptance Criteria**:
- Recording stops within 500ms
- Audio data is captured and converted to base64
- Preview player is functional
- Duration matches actual recording time (±1 second)

---

### 4. Audio Preview Playback

**Test ID**: VR-004
**Priority**: High
**Objective**: Verify recorded audio can be played back

**Steps**:
1. Complete recording (see VR-003)
2. Click play button on audio preview
3. Listen to playback

**Expected Results**:
- ✅ Audio plays back clearly
- ✅ Voice is recognizable
- ✅ No distortion or artifacts
- ✅ Progress bar moves during playback
- ✅ Pause/resume functionality works

**Acceptance Criteria**:
- Audio quality is acceptable for speech recognition
- Playback controls are responsive
- Volume is adequate

---

### 5. Save Voice Recording

**Test ID**: VR-005
**Priority**: Critical
**Objective**: Verify recording can be saved to backend

**Steps**:
1. Complete recording and preview (see VR-004)
2. Ensure name is entered: "Mom's Voice"
3. Click "Save Recording" button
4. Wait for upload to complete

**Expected Results**:
- ✅ Loading spinner appears during upload
- ✅ Success message: "✓ Voice recording saved successfully!"
- ✅ Recording appears in "My Voice Recordings" list
- ✅ Card shows: name, duration, creation date
- ✅ Play, Edit, Delete buttons visible

**API Verification**:
```bash
# Check backend logs
POST /api/voice-recordings/upload
Status: 200 OK
Response: { "id": 1, "name": "Mom's Voice", "durationSeconds": 8 }
```

**Database Verification**:
```sql
SELECT * FROM VoiceRecordings WHERE Name = 'Mom''s Voice';
-- Should show 1 record with correct UserId, FilePath, DurationSeconds
```

**Acceptance Criteria**:
- File saved to `backend/MedRemind.API/files/VoiceRecordings/{userId}/`
- Database record created with correct metadata
- File size under 10MB
- Duration matches recorded time

---

### 6. List Voice Recordings

**Test ID**: VR-006
**Priority**: High
**Objective**: Verify all user recordings are displayed

**Steps**:
1. Navigate to `/voice-recordings`
2. View "My Voice Recordings" section
3. Record multiple voices if needed (Mom, Dad, Sister)

**Expected Results**:
- ✅ All recordings displayed in cards
- ✅ Sorted by creation date (newest first)
- ✅ Each card shows:
  - Recording name
  - Duration (e.g., "8 seconds")
  - Creation date
  - Play button
  - Edit button (pencil icon)
  - Delete button (trash icon)

**Acceptance Criteria**:
- Empty state shows if no recordings: "No voice recordings yet"
- Multiple recordings display correctly
- Pagination works if >20 recordings

---

### 7. Play Existing Recording

**Test ID**: VR-007
**Priority**: High
**Objective**: Verify saved recordings can be played

**Steps**:
1. View voice recordings list
2. Click play button on "Mom's Voice" card
3. Listen to playback

**Expected Results**:
- ✅ Audio loads and plays
- ✅ Same quality as preview
- ✅ Play button changes to pause during playback
- ✅ Audio controls functional

**API Verification**:
```bash
GET /api/voice-recordings/{id}/play
Status: 200 OK
Content-Type: audio/webm
Content-Length: {fileSize}
```

**Acceptance Criteria**:
- Audio streams correctly
- No buffering issues
- Playback matches original recording

---

### 8. Edit Recording Name

**Test ID**: VR-008
**Priority**: Medium
**Objective**: Verify recording name can be updated

**Steps**:
1. Click edit button (pencil icon) on "Mom's Voice"
2. Dialog appears with current name
3. Change to "Mother's Loving Voice"
4. Click "Update" button

**Expected Results**:
- ✅ Edit dialog appears with pre-filled name
- ✅ Name field is editable
- ✅ Update button enabled when name changed
- ✅ Success message: "✓ Voice recording updated"
- ✅ Card shows new name immediately

**API Verification**:
```bash
PUT /api/voice-recordings/{id}
Body: { "name": "Mother's Loving Voice" }
Status: 200 OK
```

**Acceptance Criteria**:
- Name updates without page refresh
- Database record updated
- Original audio file unchanged

---

### 9. Delete Recording

**Test ID**: VR-009
**Priority**: High
**Objective**: Verify recording can be deleted

**Steps**:
1. Click delete button (trash icon) on a recording
2. Confirmation dialog appears
3. Click "Delete" button

**Expected Results**:
- ✅ Confirmation dialog: "Are you sure you want to delete this recording?"
- ✅ Options: "Delete" (red) and "Cancel"
- ✅ After delete: Success message "✓ Voice recording deleted"
- ✅ Recording removed from list
- ✅ UI updates immediately

**API Verification**:
```bash
DELETE /api/voice-recordings/{id}
Status: 200 OK
```

**Database Verification**:
```sql
SELECT * FROM VoiceRecordings WHERE Id = {id};
-- Should return 0 rows
```

**File System Verification**:
```bash
# Audio file should be deleted
ls backend/MedRemind.API/files/VoiceRecordings/{userId}/
# File should not exist
```

**Acceptance Criteria**:
- Database record deleted
- Audio file removed from file system
- UI updates without page refresh
- No broken references

---

### 10. Maximum Duration Limit (60s)

**Test ID**: VR-010
**Priority**: High
**Objective**: Verify recording auto-stops at 60 seconds

**Steps**:
1. Start recording
2. Wait for 60 seconds without stopping manually
3. Observe behavior

**Expected Results**:
- ✅ Recording stops automatically at 60 seconds
- ✅ Message: "Maximum recording duration reached (60 seconds)"
- ✅ Audio preview appears
- ✅ Duration shows 60 seconds

**Acceptance Criteria**:
- Recording doesn't exceed 60 seconds
- Auto-stop is smooth (no audio cutoff)
- User is notified of auto-stop

---

### 11. File Size Validation

**Test ID**: VR-011
**Priority**: Medium
**Objective**: Verify 10MB file size limit enforcement

**Steps**:
1. Record a very long audio (approaching 60s)
2. Stop and attempt to save
3. Check file size

**Expected Results**:
- ✅ If under 10MB: Upload succeeds
- ✅ If over 10MB: Error message "File size exceeds 10MB limit"
- ✅ Recording not saved

**Acceptance Criteria**:
- Backend validates file size before saving
- Clear error message displayed
- User can record again

---

### 12. Empty Name Validation

**Test ID**: VR-012
**Priority**: Medium
**Objective**: Verify name is required

**Steps**:
1. Start and complete recording
2. Clear the name field (leave blank)
3. Click "Save Recording"

**Expected Results**:
- ✅ Validation error: "Please enter a name for this recording"
- ✅ Save button disabled or validation error shown
- ✅ Recording not uploaded

**Acceptance Criteria**:
- Name field is required
- Validation prevents empty names
- Error message is clear

---

### 13. Concurrent Recording Prevention

**Test ID**: VR-013
**Priority**: Low
**Objective**: Verify only one recording at a time

**Steps**:
1. Start recording
2. Attempt to start another recording in different component/page

**Expected Results**:
- ✅ Only one active recording session
- ✅ Previous recording stopped if new one started
- ✅ Or: New recording prevented with message

**Acceptance Criteria**:
- No simultaneous recordings
- Consistent behavior across components

---

### 14. Browser Compatibility

**Test ID**: VR-014
**Priority**: High
**Objective**: Verify cross-browser support

**Browsers to Test**:
1. **Chrome 120+**
2. **Microsoft Edge 120+**
3. **Firefox 121+**
4. **Safari 17+** (if available)

**Test Matrix**:

| Feature | Chrome | Edge | Firefox | Safari |
|---------|--------|------|---------|--------|
| Microphone access | ✅ | ✅ | ✅ | ⚠️ |
| Recording (WebM) | ✅ | ✅ | ✅ | ❌ |
| Recording (MP4) | ✅ | ✅ | ✅ | ✅ |
| Playback | ✅ | ✅ | ✅ | ⚠️ |
| Upload | ✅ | ✅ | ✅ | ✅ |

**Notes**:
- Safari requires HTTPS for microphone access (except localhost)
- Safari uses different codec (may need format conversion)
- WebM not supported in Safari (need MP4 fallback)

---

### 15. Error Handling - No Microphone

**Test ID**: VR-015
**Priority**: Medium
**Objective**: Handle missing microphone gracefully

**Steps**:
1. Disable/disconnect microphone in OS settings
2. Navigate to `/voice-recordings`
3. Click "Record New Voice"

**Expected Results**:
- ✅ Error message: "No microphone detected. Please connect a microphone."
- ✅ Recording button disabled
- ✅ Clear instructions provided

**Acceptance Criteria**:
- Graceful error handling
- User is informed of issue
- No crashes or freezes

---

### 16. Error Handling - Network Failure

**Test ID**: VR-016
**Priority**: Medium
**Objective**: Handle upload failure gracefully

**Steps**:
1. Complete recording
2. Stop backend API (simulate network failure)
3. Click "Save Recording"

**Expected Results**:
- ✅ Error message: "Failed to save recording. Please check your connection."
- ✅ Recording data preserved locally
- ✅ User can retry upload
- ✅ No data loss

**Acceptance Criteria**:
- Network errors caught and displayed
- Retry functionality available
- Audio data not lost

---

### 17. Authentication & Authorization

**Test ID**: VR-017
**Priority**: Critical
**Objective**: Verify security and user isolation

**Test A: Unauthenticated Access**
1. Logout of application
2. Navigate to `/voice-recordings`
3. Expected: Redirect to `/login`

**Test B: User Isolation**
1. Login as User A
2. Create recording "User A Voice"
3. Logout and login as User B
4. Navigate to `/voice-recordings`
5. Expected: Only User B's recordings visible (not User A's)

**Test C: JWT Token Validation**
```bash
# Attempt to access API without token
curl http://localhost:5000/api/voice-recordings
# Expected: 401 Unauthorized

# Attempt with invalid token
curl -H "Authorization: Bearer invalid_token" http://localhost:5000/api/voice-recordings
# Expected: 401 Unauthorized
```

**Acceptance Criteria**:
- Unauthenticated users cannot access voice recordings
- Users can only see/modify their own recordings
- JWT tokens are validated on all endpoints

---

## 🔄 End-to-End Complete Flow

### Scenario: New User Records First Voice

**Test ID**: VR-E2E-001
**Duration**: ~5 minutes

**Steps**:
1. ✅ Register new user account
2. ✅ Login successfully
3. ✅ Navigate to `/voice-recordings`
4. ✅ See empty state: "No voice recordings yet"
5. ✅ Click "Record New Voice"
6. ✅ Allow microphone permission (browser prompt)
7. ✅ See "Microphone ready" message
8. ✅ Enter name: "Mom's Reminder Voice"
9. ✅ Click "Start Recording"
10. ✅ Speak for 8 seconds: "Hello sweetheart, it's time for your medication. Don't forget to take it with water."
11. ✅ Click "Stop Recording"
12. ✅ See preview with audio player
13. ✅ Click play to verify audio quality
14. ✅ Audio plays correctly
15. ✅ Click "Save Recording"
16. ✅ See success message
17. ✅ Recording appears in list
18. ✅ Duration shows "8 seconds"
19. ✅ Click play on saved recording
20. ✅ Audio plays correctly from server
21. ✅ Edit name to "Mother's Voice"
22. ✅ See updated name
23. ✅ Recording ready for use in reminder setup

**Success Criteria**:
- All steps complete without errors
- Audio quality is acceptable
- Recording is saved and retrievable
- Ready to link to medication reminder

---

## 📊 Test Results Template

```markdown
## Test Execution Report

**Date**: YYYY-MM-DD
**Tester**: [Name]
**Environment**: [Local/Dev/Staging]
**Browser**: [Chrome 120 / Firefox 121 / etc.]

### Test Results Summary
- Total Test Cases: 17
- Passed: ✅ __
- Failed: ❌ __
- Blocked: ⏸️ __
- Not Tested: ⏭️ __

### Failed Test Cases
| Test ID | Description | Status | Notes |
|---------|-------------|--------|-------|
| VR-XXX | Test name | ❌ Failed | Reason for failure |

### Blocker Issues
1. [Issue description and impact]

### Recommendations
1. [Suggested fixes or improvements]
```

---

## 🐛 Common Issues and Solutions

### Issue 1: Microphone Permission Denied
**Symptom**: Error "Microphone access denied"
**Solution**:
1. Check browser settings → Permissions → Microphone
2. Ensure site is allowed to access microphone
3. Refresh page after changing permissions

### Issue 2: Audio Not Playing
**Symptom**: Play button clicks but no sound
**Solution**:
1. Check browser console for errors
2. Verify audio codec support (WebM vs MP4)
3. Check device volume and mute settings
4. Test with different browser

### Issue 3: Upload Fails with 413 Error
**Symptom**: "File too large" error
**Solution**:
1. Check file size (should be under 10MB)
2. Verify backend `MaxRequestBodySize` setting
3. Shorten recording duration

### Issue 4: Recording Stops Immediately
**Symptom**: Recording starts then stops within 1 second
**Solution**:
1. Check microphone input level
2. Verify MediaRecorder support in browser
3. Check browser console for JavaScript errors

---

## 📈 Performance Benchmarks

### Expected Performance:
- **Recording Start**: < 1 second
- **Recording Stop**: < 500ms
- **Upload (5s audio)**: < 2 seconds
- **Playback Load**: < 1 second
- **List Load**: < 500ms

### File Sizes:
- 10s recording: ~100-200 KB
- 30s recording: ~300-600 KB
- 60s recording: ~600-1200 KB

---

## ✅ Sign-off Checklist

Before marking Task #18 as complete:
- [ ] All 17 test cases executed
- [ ] End-to-end flow tested successfully
- [ ] Cross-browser testing completed (minimum: Chrome + Firefox)
- [ ] Error handling verified
- [ ] Security testing passed
- [ ] Performance benchmarks met
- [ ] No critical or high-priority bugs
- [ ] Documentation reviewed and accurate

---

## 📝 Notes

- Test with different microphone types (built-in, USB, wireless)
- Test in quiet and noisy environments
- Verify audio quality is sufficient for voice recognition
- Check mobile browser behavior if applicable
- Document any browser-specific issues or workarounds

---

*Document Version: 1.0*
*Last Updated: 2026-02-09*
*Task #18: Test Voice Recording End-to-End Flow*
