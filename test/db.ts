import { PostgreSqlContainer, type StartedPostgreSqlContainer } from "@testcontainers/postgresql";
import { createDb } from "../src/db/db.js";
import { migrate } from "../src/db/migrate.js";

export async function startTestDb() {
  const container: StartedPostgreSqlContainer = await new PostgreSqlContainer("postgres:16").start();
  const db = createDb(container.getConnectionUri());
  await migrate(db);
  return { db, url: container.getConnectionUri(), stop: async () => { await db.destroy(); await container.stop(); } };
}
