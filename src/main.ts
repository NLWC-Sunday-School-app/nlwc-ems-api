import { loadConfig } from "./config.js";
import { createDb } from "./db/db.js";
import { createApp } from "./app.js";

const config = loadConfig();
const app = await createApp(config, createDb(config.databaseUrl));
await app.listen(config.port, "0.0.0.0");
