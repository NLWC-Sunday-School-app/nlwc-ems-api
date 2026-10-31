-- Roles hold (resource, level) permissions. Grants attach a role to an account for an org or one edition.
CREATE TABLE role (
  id uuid PRIMARY KEY,
  key text NOT NULL UNIQUE,
  name text NOT NULL
);

CREATE TABLE role_permission (
  role_id uuid NOT NULL REFERENCES role(id),
  resource text NOT NULL,
  level text NOT NULL,
  PRIMARY KEY (role_id, resource)
);

CREATE TABLE role_grant (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL REFERENCES account(id),
  role_id uuid NOT NULL REFERENCES role(id),
  edition_id uuid REFERENCES event_edition(id),   -- null = church-wide
  granted_by uuid NOT NULL REFERENCES account(id),
  reason text NOT NULL,
  granted_at timestamptz NOT NULL DEFAULT now(),
  expires_at timestamptz,
  revoked_at timestamptz
);
CREATE INDEX role_grant_account ON role_grant(account_id) WHERE revoked_at IS NULL;

-- Append-only, hash-chained. seq gives the chain order.
CREATE TABLE audit_event (
  seq bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  id uuid NOT NULL UNIQUE,
  occurred_at timestamptz NOT NULL DEFAULT now(),
  actor_account_id uuid,
  action text NOT NULL,
  resource text,
  edition_id uuid,
  reason text,
  detail text NOT NULL DEFAULT '{}',   -- exact JSON text that was hashed
  prev_hash text NOT NULL,
  hash text NOT NULL
);
CREATE TRIGGER audit_event_append_only BEFORE UPDATE OR DELETE ON audit_event
  FOR EACH ROW EXECUTE FUNCTION forbid_mutation();

-- Reads of sensitive data (health, collectors, minors' contact details).
CREATE TABLE access_log (
  id uuid PRIMARY KEY,
  occurred_at timestamptz NOT NULL DEFAULT now(),
  actor_account_id uuid NOT NULL,
  resource text NOT NULL,
  subject_person_id uuid,
  edition_id uuid,
  reason text
);
CREATE TRIGGER access_log_append_only BEFORE UPDATE OR DELETE ON access_log
  FOR EACH ROW EXECUTE FUNCTION forbid_mutation();

CREATE TABLE device (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL REFERENCES account(id),
  label text NOT NULL,
  registered_at timestamptz NOT NULL DEFAULT now(),
  revoked_at timestamptz
);

-- Draft role matrix, to be reviewed with NLWC. Medical and finance are never in a generic admin role.
INSERT INTO role(id, key, name) VALUES
 (gen_random_uuid(),'super_admin','Super Admin'),
 (gen_random_uuid(),'event_admin','Event Admin'),
 (gen_random_uuid(),'finance_officer','Finance Officer'),
 (gen_random_uuid(),'checkin_officer','Child Check-in Officer'),
 (gen_random_uuid(),'child_safety_officer','Child Safety Officer'),
 (gen_random_uuid(),'accommodation_manager','Accommodation Manager'),
 (gen_random_uuid(),'medical_welfare','Medical / Welfare'),
 (gen_random_uuid(),'volunteer_usher','Volunteer / Usher'),
 (gen_random_uuid(),'registration_officer','Registration Officer'),
 (gen_random_uuid(),'camp_coordinator','Camp Coordinator'),
 (gen_random_uuid(),'communications_officer','Communications Officer'),
 (gen_random_uuid(),'auditor','Auditor');

INSERT INTO role_permission(role_id, resource, level)
SELECT r.id, p.resource, p.level FROM role r JOIN (VALUES
 ('super_admin','config','edit'),('super_admin','staff','edit'),('super_admin','audit','view'),
 ('super_admin','registrations','edit'),('super_admin','people','edit'),('super_admin','custody','view'),('super_admin','health','flags'),
 ('event_admin','config','edit'),('event_admin','registrations','edit'),('event_admin','people','edit'),
 ('event_admin','pricing','draft'),('event_admin','custody','view'),('event_admin','health','flags'),('event_admin','communications','edit'),
 ('finance_officer','money','edit'),('finance_officer','pricing','approve'),('finance_officer','audit','view'),('finance_officer','registrations','view'),
 ('checkin_officer','custody','act'),('checkin_officer','health','flags'),
 ('child_safety_officer','custody','view'),('child_safety_officer','health','flags'),('child_safety_officer','registrations','view'),
 ('accommodation_manager','registrations','view'),('accommodation_manager','config','view'),
 ('medical_welfare','health','full'),('medical_welfare','registrations','view'),
 ('volunteer_usher','custody','scan'),
 ('registration_officer','registrations','edit'),('registration_officer','people','edit'),('registration_officer','health','flags'),
 ('camp_coordinator','custody','act'),('camp_coordinator','health','flags'),('camp_coordinator','registrations','view'),
 ('communications_officer','communications','edit'),('communications_officer','registrations','view'),
 ('auditor','audit','view'),('auditor','registrations','view'),('auditor','money','view')
) AS p(role_key, resource, level) ON p.role_key = r.key;
