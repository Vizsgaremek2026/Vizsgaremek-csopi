import { apiRequest } from "./client";

export function saveSearchHistory(userEmail, query) {
  return apiRequest("/api/search-history", {
    method: "POST",
    userEmail,
    body: JSON.stringify({ query }),
  });
}
