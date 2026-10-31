import type { ColumnType, Generated } from "kysely";

type Ts = Date;

export interface DB {
  person: {
    id: string; given_name: string; family_name: string; middle_name: string | null;
    date_of_birth: string | null; gender: "male" | "female" | null;
    created_at: Generated<Ts>; updated_at: Generated<Ts>;
  };
  account: {
    id: string; person_id: string; email: string; password_hash: string | null; google_sub: string | null;
    status: Generated<"active" | "locked" | "disabled">;
    failed_logins: Generated<number>; locked_until: Date | null;
  };
  verified_contact: { id: string; person_id: string; kind: "email" | "phone"; value: string; verified_at: Date | null };
  email_verification: { id: string; account_id: string; token_hash: string; expires_at: Date; used_at: Date | null };
  session: {
    id: string; account_id: string; token_hash: string; surface: "web" | "admin" | "checkin";
    device_id: string | null; last_seen_at: Generated<Ts>; idle_expires_at: Date; absolute_expires_at: Date;
    revoked_at: Date | null; mfa_verified_at: Date | null;
  };
  mfa_factor: { id: string; account_id: string; kind: "totp"; secret_encrypted: Buffer; confirmed_at: Date | null };
}
