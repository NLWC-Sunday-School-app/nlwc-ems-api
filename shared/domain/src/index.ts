/** Display-only helpers. Never authoritative: the API decides eligibility and price. */
export const formatNaira = (kobo: number): string =>
  kobo === 0 ? "Free" : `₦${(kobo / 100).toLocaleString("en-NG")}`;
