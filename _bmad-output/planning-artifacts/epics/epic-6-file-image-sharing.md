# Epic 6: File & Image Sharing

Users can upload, preview, and download files and images in any conversation, with configurable size limits and server-side thumbnail generation.

## Story 6.1: File Upload Backend & Storage

As a user,
I want to upload files to a conversation,
So that I can share documents and resources with my team.

**Acceptance Criteria:**

**Given** an authenticated user in a conversation
**When** they upload a file via POST `/api/files/upload`
**Then** the file is validated: MIME type checked against a whitelist, magic bytes verified, executable files rejected (NFR10)
**And** the file size is checked against the configurable maximum (environment variable, default e.g., 25MB)
**And** if valid, the file is stored on local disk via IFileStorageService (Docker volume)
**And** a FileAttachment entity is created: Id, FileName, ContentType, Size, StoragePath, ThumbnailPath (nullable), UploadedAt, UploadedById
**And** the FileAttachment Id is returned

**Given** the uploaded file is an image (JPEG, PNG, GIF, WebP)
**When** storage completes
**Then** a thumbnail is generated server-side using SkiaSharp and saved to ThumbnailPath
**And** the thumbnail is appropriately sized for inline chat display

**Given** a file exceeds the configurable maximum size
**When** the upload is attempted
**Then** the request is rejected with a 400 Problem Details response before the full file is received
**And** the error message includes the size limit (e.g., "File exceeds 25MB limit")

**Given** a file with a disallowed MIME type or executable signature
**When** the upload is attempted
**Then** the request is rejected with a 400 Problem Details response
**And** the rejection is logged at Warning level

## Story 6.2: File Messages & Download

As a user,
I want to send files as part of a conversation and download files others have shared,
So that shared resources are accessible in their conversation context.

**Acceptance Criteria:**

**Given** a user has uploaded a file (Story 6.1)
**When** they send a message with the file attached
**Then** a Message is created with MessageType=File and FileId referencing the FileAttachment
**And** the message is broadcast to all conversation participants via SignalR (same persist-first flow as text messages)

**Given** a file message in the chat
**When** the FilePreview component renders (UX-DR7)
**Then** image files display as an inline thumbnail (click to expand to full size)
**And** non-image files display as a file card: icon + filename + file size + download link
**And** the component is accessible with appropriate alt text and aria-labels

**Given** a user clicks download on a shared file
**When** GET `/api/files/{fileId}/download` is called
**Then** the file is streamed from disk with correct Content-Type and Content-Disposition headers
**And** the endpoint verifies the user is a participant in the conversation containing the file

## Story 6.3: File Upload UX & Attach Flow

As a user,
I want a smooth file upload experience with progress feedback,
So that I know my file is being shared and can recover from errors.

**Acceptance Criteria:**

**Given** the MessageInput component
**When** the user taps the labeled "Attach" button
**Then** a native file picker opens

**Given** a file is selected for upload
**When** the upload begins
**Then** a progress bar is displayed within the FilePreview component in the chat area
**And** an "Uploading..." label is visible
**And** the file size hint is visible near the attach button ("Files up to {max}MB")

**Given** the upload fails (network error, server error)
**When** the failure is detected
**Then** the file preview shows "Upload failed — tap to retry" (UX-DR7)
**And** tapping retry re-attempts the upload

**Given** the upload succeeds
**When** the file message is sent
**Then** the file preview transitions to its final display state (thumbnail or file card)
**And** the message follows the same optimistic send pattern (checkmark states)
