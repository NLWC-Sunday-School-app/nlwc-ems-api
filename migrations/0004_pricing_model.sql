-- Pricing model. Bootcamp 2026 is free, expressed as explicit published FREE rules (PRICE-03),
-- never as a missing price. Charging itself is Phase 2.
CREATE TABLE rate_plan (
  id uuid PRIMARY KEY,
  edition_id uuid NOT NULL UNIQUE REFERENCES event_edition(id),
  name text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE rate_plan_version (
  id uuid PRIMARY KEY,
  plan_id uuid NOT NULL REFERENCES rate_plan(id),
  version int NOT NULL,
  state text NOT NULL DEFAULT 'draft' CHECK (state IN ('draft','approved','published','retired')),
  drafted_by uuid NOT NULL REFERENCES account(id),
  approved_by uuid REFERENCES account(id),
  approved_at timestamptz,
  published_at timestamptz,
  note text,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (plan_id, version),
  -- drafter cannot approve their own plan
  CHECK (approved_by IS NULL OR approved_by <> drafted_by),
  CHECK (state = 'draft' OR approved_by IS NOT NULL)
);
-- At most one published version per plan.
CREATE UNIQUE INDEX rate_plan_one_published ON rate_plan_version(plan_id) WHERE state = 'published';

CREATE TABLE rate_rule (
  id uuid PRIMARY KEY,
  version_id uuid NOT NULL REFERENCES rate_plan_version(id),
  age_min int NOT NULL CHECK (age_min >= 0),
  age_max int CHECK (age_max IS NULL OR age_max >= age_min),   -- null = no upper bound
  participation_mode text CHECK (participation_mode IN ('day','camp','online')),  -- null = any
  accommodation_category text,
  kind text NOT NULL CHECK (kind IN ('AMOUNT','FREE')),
  amount_kobo bigint CHECK (amount_kobo IS NULL OR amount_kobo > 0),
  CHECK ((kind = 'AMOUNT' AND amount_kobo IS NOT NULL) OR (kind = 'FREE' AND amount_kobo IS NULL))
);
CREATE INDEX rate_rule_version ON rate_rule(version_id);

-- Published and retired versions are immutable; only published -> retired is allowed.
CREATE FUNCTION guard_rate_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'DELETE' THEN
    IF OLD.state <> 'draft' THEN RAISE EXCEPTION 'only draft rate versions can be deleted'; END IF;
    RETURN OLD;
  END IF;
  IF OLD.state IN ('published','retired') AND NOT (OLD.state = 'published' AND NEW.state = 'retired') THEN
    RAISE EXCEPTION 'rate version is immutable once published';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER rate_plan_version_guard BEFORE UPDATE OR DELETE ON rate_plan_version
  FOR EACH ROW EXECUTE FUNCTION guard_rate_version();

CREATE FUNCTION guard_rate_rule() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v uuid; s text;
BEGIN
  v := CASE WHEN TG_OP = 'DELETE' THEN OLD.version_id ELSE NEW.version_id END;
  SELECT state INTO s FROM rate_plan_version WHERE id = v;
  IF s <> 'draft' THEN RAISE EXCEPTION 'rules can only change while the version is a draft'; END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;
CREATE TRIGGER rate_rule_guard BEFORE INSERT OR UPDATE OR DELETE ON rate_rule
  FOR EACH ROW EXECUTE FUNCTION guard_rate_rule();
