import { apiRequest } from "./client";

export function getFavorites(userEmail) {
  return apiRequest("/api/favorites", { userEmail });
}

export function addFavorite(userEmail, songId) {
  return apiRequest(`/api/favorites/${songId}`, {
    method: "POST",
    userEmail,
  });
}

export function removeFavorite(userEmail, songId) {
  return apiRequest(`/api/favorites/${songId}`, {
    method: "DELETE",
    userEmail,
  });
}
