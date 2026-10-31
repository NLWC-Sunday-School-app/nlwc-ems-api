ALTER TABLE session ADD COLUMN mfa_verified_at timestamptz;

CREATE TABLE email_verification (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL REFERENCES account(id),
  token_hash text NOT NULL UNIQUE,
  expires_at timestamptz NOT NULL,
  used_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now()
);
