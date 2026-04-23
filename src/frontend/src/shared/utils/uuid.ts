// ─────────────────────────────────────────────────────────────────────────────
// UUID utility — crypto.randomUUID() chỉ available trên HTTPS hoặc localhost.
// Khi truy cập qua http://LAN-IP (non-secure context) cần fallback.
// ─────────────────────────────────────────────────────────────────────────────

export function generateUUID(): string {
  if (
    typeof crypto !== 'undefined' &&
    typeof crypto.randomUUID === 'function'
  ) {
    return crypto.randomUUID();
  }
  // RFC 4122 v4 UUID fallback
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
