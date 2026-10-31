import { createCipheriv, createDecipheriv, createHash, randomBytes } from "node:crypto";
import { hash, verify } from "@node-rs/argon2";
import { v7 as uuidv7 } from "uuid";

export const newId = (): string => uuidv7();

/** Opaque random token for sessions, claims, verification. Only the hash is stored. */
export const newToken = (): string => randomBytes(32).toString("base64url");
export const hashToken = (t: string): string => createHash("sha256").update(t).digest("hex");

// argon2id (the library default is argon2id)
export const hashPassword = (pw: string): Promise<string> => hash(pw);
export const verifyPassword = (h: string, pw: string): Promise<boolean> => verify(h, pw).catch(() => false);

/** Verified against when the account does not exist, so response time does not reveal it. */
export const DUMMY_HASH = await hashPassword("dummy-password-for-timing");

export function encrypt(key: Buffer, plain: Buffer): Buffer {
  const iv = randomBytes(12);
  const c = createCipheriv("aes-256-gcm", key, iv);
  const body = Buffer.concat([c.update(plain), c.final()]);
  return Buffer.concat([iv, c.getAuthTag(), body]);
}

export function decrypt(key: Buffer, blob: Buffer): Buffer {
  const d = createDecipheriv("aes-256-gcm", key, blob.subarray(0, 12));
  d.setAuthTag(blob.subarray(12, 28));
  return Buffer.concat([d.update(blob.subarray(28)), d.final()]);
}
