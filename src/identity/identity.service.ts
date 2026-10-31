import { Inject, Injectable } from "@nestjs/common";
import { Kysely, sql } from "kysely";
import { TOTP, Secret } from "otpauth";
import type { DB } from "../db/types.js";
import { DB_TOKEN } from "../db/db.js";
import { CONFIG } from "../config.token.js";
import type { Config } from "../config.js";
import { MAILER, type Mailer } from "./mailer.js";
import { DUMMY_HASH, decrypt, encrypt, hashPassword, hashToken, newId, newToken, verifyPassword } from "./crypto.js";

const MAX_FAILED = 5;
const LOCK_MINUTES = 15;
const IDLE_MS = { web: 8 * 3600e3, admin: 30 * 60e3, checkin: 15 * 60e3 } as const;
const ABSOLUTE_MS = { web: 30 * 24 * 3600e3, admin: 12 * 3600e3, checkin: 12 * 3600e3 } as const;

export type Surface = keyof typeof IDLE_MS;
export class AuthError extends Error {
  constructor(public code: "invalid_credentials" | "locked" | "mfa_required" | "invalid_token" | "invalid_code") { super(code); }
}
export interface SessionInfo {
  sessionId: string; accountId: string; personId: string; surface: Surface; mfaVerified: boolean; emailVerified: boolean;
}

@Injectable()
export class IdentityService {
  constructor(
    @Inject(DB_TOKEN) private db: Kysely<DB>,
    @Inject(CONFIG) private config: Config,
    @Inject(MAILER) private mailer: Mailer,
  ) {}

  /** Always succeeds from the caller's view, so sign-up cannot be used to find out who has an account. */
  async signUp(i: { email: string; password: string; givenName: string; familyName: string; gender?: "male" | "female" }): Promise<void> {
    const email = i.email.trim().toLowerCase();
    const passwordHash = await hashPassword(i.password);
    const token = newToken();
    const created = await this.db.transaction().execute(async (trx) => {
      const existing = await trx.selectFrom("account").select("id").where("email", "=", email).executeTakeFirst();
      if (existing) return false;
      const personId = newId(), accountId = newId();
      await trx.insertInto("person").values({ id: personId, given_name: i.givenName, family_name: i.familyName, middle_name: null, date_of_birth: null, gender: i.gender ?? null }).execute();
      await trx.insertInto("account").values({ id: accountId, person_id: personId, email, password_hash: passwordHash, google_sub: null, locked_until: null }).execute();
      await trx.insertInto("verified_contact").values({ id: newId(), person_id: personId, kind: "email", value: email, verified_at: null }).execute();
      await trx.insertInto("email_verification").values({ id: newId(), account_id: accountId, token_hash: hashToken(token), expires_at: new Date(Date.now() + 24 * 3600e3), used_at: null }).execute();
      return true;
    });
    await this.mailer.send(email, created ? "verify_email" : "account_exists", created ? { token } : {});
  }

  async verifyEmail(token: string): Promise<void> {
    await this.db.transaction().execute(async (trx) => {
      const row = await trx.selectFrom("email_verification").selectAll().where("token_hash", "=", hashToken(token)).forUpdate().executeTakeFirst();
      if (!row || row.used_at || row.expires_at < new Date()) throw new AuthError("invalid_token");
      const acct = await trx.selectFrom("account").select(["person_id", "email"]).where("id", "=", row.account_id).executeTakeFirstOrThrow();
      await trx.updateTable("email_verification").set({ used_at: new Date() }).where("id", "=", row.id).execute();
      await trx.updateTable("verified_contact").set({ verified_at: new Date() }).where("person_id", "=", acct.person_id).where("kind", "=", "email").where("value", "=", acct.email).execute();
    });
  }

  async logIn(emailRaw: string, password: string, surface: Surface): Promise<{ token: string; session: SessionInfo }> {
    const email = emailRaw.trim().toLowerCase();
    const acct = await this.db.selectFrom("account").selectAll().where("email", "=", email).executeTakeFirst();
    const ok = await verifyPassword(acct?.password_hash ?? DUMMY_HASH, password);
    if (!acct || !acct.password_hash) throw new AuthError("invalid_credentials");
    if (acct.locked_until && acct.locked_until > new Date()) throw new AuthError("locked");
    if (!ok || acct.status !== "active") {
      const failed = acct.failed_logins + 1;
      await this.db.updateTable("account").set({ failed_logins: failed, locked_until: failed >= MAX_FAILED ? new Date(Date.now() + LOCK_MINUTES * 60e3) : null }).where("id", "=", acct.id).execute();
      throw new AuthError("invalid_credentials");
    }
    await this.db.updateTable("account").set({ failed_logins: 0, locked_until: null }).where("id", "=", acct.id).execute();
    return this.createSession(acct.id, surface);
  }

  private async createSession(accountId: string, surface: Surface) {
    const token = newToken();
    const id = newId();
    const now = Date.now();
    await this.db.insertInto("session").values({
      id, account_id: accountId, token_hash: hashToken(token), surface, device_id: null,
      idle_expires_at: new Date(now + IDLE_MS[surface]), absolute_expires_at: new Date(now + ABSOLUTE_MS[surface]),
      revoked_at: null, mfa_verified_at: null,
    }).execute();
    return { token, session: (await this.resolve(token))! };
  }

  async resolve(token: string): Promise<SessionInfo | null> {
    const now = new Date();
    const row = await this.db.selectFrom("session as s")
      .innerJoin("account as a", "a.id", "s.account_id")
      .select(["s.id", "s.account_id", "s.surface", "s.mfa_verified_at", "s.idle_expires_at", "a.person_id", "a.email"])
      .where("s.token_hash", "=", hashToken(token)).where("s.revoked_at", "is", null)
      .where("s.idle_expires_at", ">", now).where("s.absolute_expires_at", ">", now)
      .executeTakeFirst();
    if (!row) return null;
    await this.db.updateTable("session").set({ last_seen_at: now, idle_expires_at: new Date(now.getTime() + IDLE_MS[row.surface]) }).where("id", "=", row.id).execute();
    const vc = await this.db.selectFrom("verified_contact").select("verified_at").where("person_id", "=", row.person_id).where("kind", "=", "email").where("value", "=", row.email).executeTakeFirst();
    return { sessionId: row.id, accountId: row.account_id, personId: row.person_id, surface: row.surface, mfaVerified: !!row.mfa_verified_at, emailVerified: !!vc?.verified_at };
  }

  async logOut(sessionId: string) {
    await this.db.updateTable("session").set({ revoked_at: new Date() }).where("id", "=", sessionId).execute();
  }

  // ---- TOTP MFA ----
  private totp(secret: Buffer, label: string) {
    return new TOTP({ issuer: "NLWC EMS", label, secret: new Secret({ buffer: secret.buffer.slice(secret.byteOffset, secret.byteOffset + secret.byteLength) as ArrayBuffer }) });
  }

  async mfaEnroll(s: SessionInfo): Promise<{ otpauthUri: string }> {
    const acct = await this.db.selectFrom("account").select("email").where("id", "=", s.accountId).executeTakeFirstOrThrow();
    const secret = Buffer.from(new Secret({ size: 20 }).bytes);
    await this.db.deleteFrom("mfa_factor").where("account_id", "=", s.accountId).where("confirmed_at", "is", null).execute();
    await this.db.insertInto("mfa_factor").values({ id: newId(), account_id: s.accountId, kind: "totp", secret_encrypted: encrypt(this.config.masterKey, secret), confirmed_at: null }).execute();
    return { otpauthUri: this.totp(secret, acct.email).toString() };
  }

  /** Confirms a pending factor, or step-up verifies an existing one. Marks the session MFA-verified. */
  async mfaVerify(s: SessionInfo, code: string): Promise<void> {
    const f = await this.db.selectFrom("mfa_factor").selectAll().where("account_id", "=", s.accountId).orderBy("confirmed_at", "desc").executeTakeFirst();
    if (!f) throw new AuthError("mfa_required");
    const secret = decrypt(this.config.masterKey, f.secret_encrypted);
    if (this.totp(secret, "x").validate({ token: code, window: 1 }) === null) throw new AuthError("invalid_code");
    if (!f.confirmed_at) await this.db.updateTable("mfa_factor").set({ confirmed_at: new Date() }).where("id", "=", f.id).execute();
    await this.db.updateTable("session").set({ mfa_verified_at: new Date() }).where("id", "=", s.sessionId).execute();
  }
}
