import { z } from "zod";

export const HealthResponse = z.object({ status: z.literal("ok"), time: z.string() });
export type HealthResponse = z.infer<typeof HealthResponse>;

/** Money is always integer kobo. Never a float, never sent by the client as a price. */
export const Kobo = z.number().int().nonnegative();
