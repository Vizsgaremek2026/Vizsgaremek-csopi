import { apiRequest } from "./client";

export function getResumeTimes(userEmail) {
  return apiRequest("/api/resume", { userEmail });
}

export function saveResumeTime(userEmail, songId, positionSeconds) {
  return apiRequest(`/api/resume/${songId}`, {
    method: "PUT",
    userEmail,
    body: JSON.stringify({ positionSeconds }),
  });
}
