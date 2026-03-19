const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";

/**
 * Base API helper. Adds X-User-Email header when userEmail is provided.
 * @param {string} path
 * @param {RequestInit & { userEmail?: string }} options
 */
export async function apiRequest(path, options = {}) {
  const { userEmail, headers: extraHeaders, ...rest } = options;

  const headers = {
    "Content-Type": "application/json",
    ...(userEmail ? { "X-User-Email": userEmail } : {}),
    ...(extraHeaders || {}),
  };

  const res = await fetch(`${API_BASE}${path}`, { ...rest, headers });

  if (!res.ok) {
    let errorMsg = res.statusText;
    try {
      const body = await res.json();
      errorMsg = body.error || errorMsg;
    } catch {
      // ignore parse errors
    }
    throw new Error(errorMsg);
  }

  if (res.status === 204) return null;
  return res.json();
}
