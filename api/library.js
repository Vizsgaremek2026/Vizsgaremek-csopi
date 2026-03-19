import { apiRequest } from "./client";

export function getLibrary(userEmail) {
  return apiRequest("/api/library", { userEmail });
}

/**
 * @param {string} userEmail
 * @param {{ youtubeVideoId: string, title: string, artist: string, artworkUrl?: string }} song
 */
export function addToLibrary(userEmail, song) {
  return apiRequest("/api/library", {
    method: "POST",
    userEmail,
    body: JSON.stringify(song),
  });
}

export function removeFromLibrary(userEmail, songId) {
  return apiRequest(`/api/library/${songId}`, {
    method: "DELETE",
    userEmail,
  });
}
