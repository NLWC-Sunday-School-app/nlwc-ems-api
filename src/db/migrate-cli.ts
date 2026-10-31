import { loadConfig } from "../config.js";
import { createDb } from "./db.js";
import { migrate } from "./migrate.js";

const db = createDb(loadConfig().databaseUrl);
console.log("applied:", await migrate(db));
await db.destroy();
