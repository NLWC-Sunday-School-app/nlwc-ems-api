import { z } from "zod";

const Env = z.object({
  DATABASE_URL: z.string().url(),
  PORT: z.coerce.number().default(4000),
  /** 32 bytes, hex. Encrypts MFA secrets and other field-level data. */
  MASTER_KEY: z.string().regex(/^[0-9a-f]{64}$/i),
  ALLOWED_ORIGINS: z.string().default("http://localhost:3000,http://localhost:3001"),
  COOKIE_SECURE: z.enum(["true", "false"]).default("true"),
});
export type Config = ReturnType<typeof loadConfig>;

export function loadConfig(env: NodeJS.ProcessEnv = process.env) {
  const e = Env.parse(env);
  return {
    databaseUrl: e.DATABASE_URL,
    port: e.PORT,
    masterKey: Buffer.from(e.MASTER_KEY, "hex"),
    allowedOrigins: e.ALLOWED_ORIGINS.split(",").map((s) => s.trim()),
    cookieSecure: e.COOKIE_SECURE === "true",
  };
}
