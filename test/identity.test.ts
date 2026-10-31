import { afterAll, beforeAll, beforeEach, expect, test } from "vitest";
import { TOTP, Secret, URI } from "otpauth";
import type { NestFastifyApplication } from "@nestjs/platform-fastify";
import { createApp } from "../src/app.js";
import { loadConfig } from "../src/config.js";
import { startTestDb } from "./db.js";
import { MAILER } from "../src/identity/mailer.js";

const ORIGIN = "http://localhost:3000";
let t: Awaited<ReturnType<typeof startTestDb>>;
let app: NestFastifyApplication;
let mails: { to: string; template: string; data: Record<string, string> }[] = [];

beforeAll(async () => {
  t = await startTestDb();
  const config = loadConfig({ DATABASE_URL: t.url, MASTER_KEY: "ab".repeat(32), COOKIE_SECURE: "false" } as never);
  app = await createApp(config, t.db);
  (app.get(MAILER) as { send: unknown }).send = async (to: string, template: string, data: Record<string, string>) => { mails.push({ to, template, data }); };
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
}, 120_000);
afterAll(async () => { await app.close(); await t.stop(); });
beforeEach(() => { mails = []; });

const post = (url: string, payload: unknown, headers: Record<string, string> = {}) =>
  app.inject({ method: "POST", url, payload: payload as never, headers: { origin: ORIGIN, ...headers } });

const user = { email: "Ada@Example.com", password: "correct horse battery", givenName: "Ada", familyName: "Obi" };

async function signUpAndLogin(u = user) {
  await post("/v1/auth/signup", u);
  const login = await post("/v1/auth/login", { email: u.email, password: u.password });
  const cookies = Object.fromEntries(login.cookies.map((c) => [c.name, c.value]));
  const headers = { cookie: `nlwc_session=${cookies.nlwc_session}; nlwc_csrf=${cookies.nlwc_csrf}`, "x-csrf-token": cookies.nlwc_csrf! };
  return { login, headers };
}

test("sign-up answers the same whether or not the email exists", async () => {
  const a = await post("/v1/auth/signup", user);
  const b = await post("/v1/auth/signup", user);
  expect(a.statusCode).toBe(202);
  expect(b.statusCode).toBe(202);
  expect(b.body).toBe(a.body);
  expect(mails.map((m) => m.template)).toEqual(["verify_email", "account_exists"]);
});

test("weak passwords are rejected", async () => {
  const r = await post("/v1/auth/signup", { ...user, email: "x@example.com", password: "short" });
  expect(r.statusCode).toBe(400);
});

test("login sets httpOnly session cookie, and /me works", async () => {
  const { login, headers } = await signUpAndLogin();
  const session = login.cookies.find((c) => c.name === "nlwc_session")!;
  expect(session.httpOnly).toBe(true);
  expect(session.sameSite).toBe("Lax");
  const me = await app.inject({ method: "GET", url: "/v1/auth/me", headers });
  expect(me.statusCode).toBe(200);
  expect(me.json().emailVerified).toBe(false);
});

test("email verification token works once", async () => {
  await post("/v1/auth/signup", { ...user, email: "v@example.com" });
  const token = mails.find((m) => m.to === "v@example.com")!.data.token!;
  expect((await post("/v1/auth/verify-email", { token })).statusCode).toBe(204);
  expect((await post("/v1/auth/verify-email", { token })).statusCode).toBe(400);
});

test("wrong password and unknown email give the same error", async () => {
  const a = await post("/v1/auth/login", { email: user.email, password: "wrong wrong wrong" });
  const b = await post("/v1/auth/login", { email: "nobody@example.com", password: "wrong wrong wrong" });
  expect(a.statusCode).toBe(401);
  expect(b.json()).toEqual(a.json());
});

test("account locks after repeated failures", async () => {
  await post("/v1/auth/signup", { ...user, email: "l@example.com" });
  for (let i = 0; i < 5; i++) await post("/v1/auth/login", { email: "l@example.com", password: "bad bad bad bad" });
  const r = await post("/v1/auth/login", { email: "l@example.com", password: user.password });
  expect(r.json().code).toBe("locked");
});

test("mutations need Origin and CSRF token", async () => {
  const { headers } = await signUpAndLogin();
  const noCsrf = await post("/v1/auth/logout", {}, { cookie: headers.cookie });
  expect(noCsrf.statusCode).toBe(403);
  const badOrigin = await app.inject({ method: "POST", url: "/v1/auth/logout", headers: { ...headers, origin: "https://evil.example" } });
  expect(badOrigin.statusCode).toBe(403);
  expect((await post("/v1/auth/logout", {}, headers)).statusCode).toBe(204);
  expect((await app.inject({ method: "GET", url: "/v1/auth/me", headers })).statusCode).toBe(401);
});

test("login from a non-allowed origin is refused", async () => {
  const r = await app.inject({ method: "POST", url: "/v1/auth/login", payload: { email: user.email, password: user.password }, headers: { origin: "https://evil.example" } });
  expect(r.statusCode).toBe(400);
});

test("TOTP enrol, verify, and reject a bad code", async () => {
  const { headers } = await signUpAndLogin({ ...user, email: "m@example.com" });
  const enrol = await post("/v1/auth/mfa/enroll", {}, headers);
  const totp = URI.parse(enrol.json().otpauthUri) as TOTP;
  expect((await post("/v1/auth/mfa/verify", { code: "000000" }, headers)).statusCode).toBe(400);
  const code = totp.generate();
  expect((await post("/v1/auth/mfa/verify", { code }, headers)).statusCode).toBe(204);
  expect((await app.inject({ method: "GET", url: "/v1/auth/me", headers })).json().mfaVerified).toBe(true);
});
