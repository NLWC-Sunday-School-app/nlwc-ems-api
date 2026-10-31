export const MAILER = Symbol("MAILER");
export interface Mailer {
  send(to: string, template: "verify_email" | "account_exists", data: Record<string, string>): Promise<void>;
}
/** Placeholder until the notifications outbox lands (plan 5.9). Never logs tokens outside dev. */
export class ConsoleMailer implements Mailer {
  async send(to: string, template: string, data: Record<string, string>) {
    if (process.env.NODE_ENV !== "production") console.log(`[mail] ${template} -> ${to}`, data);
  }
}
