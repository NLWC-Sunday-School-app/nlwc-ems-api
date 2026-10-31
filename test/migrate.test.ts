import { afterAll, beforeAll, expect, test } from "vitest";
import { sql } from "kysely";
import { startTestDb } from "./db.js";
import { migrate } from "../src/db/migrate.js";

let t: Awaited<ReturnType<typeof startTestDb>>;
beforeAll(async () => { t = await startTestDb(); }, 120_000);
afterAll(async () => { await t.stop(); });

test("migrations are idempotent", async () => {
  expect(await migrate(t.db)).toEqual([]);
});

test("one active registration per person per edition", async () => {
  const id = (n: number) => `00000000-0000-0000-0000-${String(n).padStart(12, "0")}`;
  await sql`INSERT INTO person(id,given_name,family_name) VALUES (${id(1)},'A','B')`.execute(t.db);
  await sql`INSERT INTO organization VALUES (${id(2)},'NLWC')`.execute(t.db);
  await sql`INSERT INTO event_template VALUES (${id(3)},${id(2)},'BB','Bible Bootcamp','{}')`.execute(t.db);
  await sql`INSERT INTO event_edition(id,template_id,slug,code,name,starts_on,ends_on,age_reference_date) VALUES (${id(4)},${id(3)},'bb','BB26','BB','2026-12-21','2026-12-23','2026-12-21')`.execute(t.db);
  const ins = (n: number) => sql`INSERT INTO registration(id,reference,edition_id,person_id,participation_mode) VALUES (${id(10 + n)},${"R" + n},${id(4)},${id(1)},'day')`.execute(t.db);
  await ins(1);
  await expect(ins(2)).rejects.toThrow(/registration_one_active/);
});
