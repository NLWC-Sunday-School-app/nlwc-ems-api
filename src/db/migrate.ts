import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { sql, type Kysely } from "kysely";

const dir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../migrations");
const LOCK_ID = 727001;

/** Forward-only. Each file runs in one transaction; an advisory lock serialises concurrent deploys. */
export async function migrate(db: Kysely<any>): Promise<string[]> {
  const files = (await readdir(dir)).filter((f) => f.endsWith(".sql")).sort();
  const applied: string[] = [];
  await db.transaction().execute(async (trx) => {
    await sql`SELECT pg_advisory_xact_lock(${LOCK_ID})`.execute(trx);
    await sql`CREATE TABLE IF NOT EXISTS schema_migration (name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())`.execute(trx);
    const done = new Set((await sql<{ name: string }>`SELECT name FROM schema_migration`.execute(trx)).rows.map((r) => r.name));
    for (const f of files) {
      if (done.has(f)) continue;
      await sql.raw(await readFile(path.join(dir, f), "utf8")).execute(trx);
      await sql`INSERT INTO schema_migration (name) VALUES (${f})`.execute(trx);
      applied.push(f);
    }
  });
  return applied;
}
