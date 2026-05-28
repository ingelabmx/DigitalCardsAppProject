-- Migration 75: Store plaintext enrollment token so the same link can be returned on repeat clicks.
-- The hash is kept for secure lookup in /Enroll/{token}; Token stores the Base64URL plaintext.
-- Rows created before this migration have Token = '' and will be treated as invalid (a new token
-- will be created on first click after deploy).

ALTER TABLE BusinessEnrollmentLinkToken
  ADD COLUMN Token varchar(64) NOT NULL DEFAULT '' AFTER TokenSuffix;
