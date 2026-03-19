import { useState } from "react";
import { login } from "./api/auth";

export default function Login({ onSwitch, onLogin }) {
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [focusedInput, setFocusedInput] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    const em = email.trim();
    const pw = password.trim();

    if (!em || !pw) {
      setError("Invalid email or password");
      setEmail("");
      setPassword("");
      return;
    }

    setLoading(true);
    setError("");
    try {
      const result = await login(em, pw);
      onLogin(result.email);
    } catch (err) {
      setError(err.message || "Invalid email or password");
      setEmail("");
      setPassword("");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        height: "100vh",
        width: "100vw",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "radial-gradient(circle at center, #1a1a1a 0%, #000000 80%)",
        fontFamily: "sans-serif"
      }}
    >
      <div
        style={{
          background: "rgba(255,255,255,0.05)",
          padding: "40px",
          borderRadius: "12px",
          boxShadow: "0 0 20px rgba(0,0,0,0.6)",
          width: "320px",
          display: "flex",
          flexDirection: "column",
          gap: "16px",
          position: "relative"
        }}
      >
        <h2 style={{ color: "#fff", textAlign: "center" }}>Log in</h2>

        {error && (
          <div style={{ color: "red", textAlign: "center", marginBottom: "-8px" }}>
            {error}
          </div>
        )}

        <input
          type="email"
          placeholder="Email"
          value={email}
          autoComplete="username"
          onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); handleLogin(); } }}
          onFocus={() => setFocusedInput("email")}
          onBlur={() => setFocusedInput(null)}
          onChange={(e) => setEmail(e.target.value)}
          style={{
            padding: "10px",
            borderRadius: "6px",
            border: focusedInput === "email" ? "2px solid #fff" : "none",
            background: "#222",
            color: "#fff",
            width: "100%",
            outline: "none"
          }}
        />

        <div
          style={{
            position: "relative",
            width: "100%",
            background: "#222",
            borderRadius: "6px",
            display: "flex",
            alignItems: "center"
          }}
        >
          <input
            type={showPassword ? "text" : "password"}
            placeholder="Password"
            value={password}
            autoComplete="current-password"
            onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); handleLogin(); } }}
            onFocus={() => setFocusedInput("password")}
            onBlur={() => setFocusedInput(null)}
            onChange={(e) => setPassword(e.target.value)}
            style={{
              padding: "10px 40px 10px 10px",
              border: focusedInput === "password" ? "2px solid #fff" : "none",
              background: "transparent",
              color: "#fff",
              width: "100%",
              outline: "none"
            }}
          />
          <span
            onClick={() => setShowPassword(!showPassword)}
            style={{
              position: "absolute",
              right: "10px",
              cursor: "pointer",
              fontSize: "20px",
              color: "#ffcc00"
            }}
          >
            {showPassword ? "🔓" : "🔒"}
          </span>
        </div>

        <button
          onClick={handleLogin}
          disabled={loading}
          style={{
            padding: "12px",
            borderRadius: "6px",
            border: "none",
            background: "#1db954",
            color: "#fff",
            fontWeight: "600",
            cursor: loading ? "not-allowed" : "pointer",
            transition: "transform 0.15s ease, box-shadow 0.15s ease",
            boxShadow: "0 4px 10px rgba(0,0,0,0.3)",
            opacity: loading ? 0.7 : 1
          }}
        >
          {loading ? "Logging in…" : "Log in"}
        </button>

        <button
          onClick={onSwitch}
          style={{
            background: "transparent",
            border: "none",
            color: "#ccc",
            fontSize: "14px",
            cursor: "pointer",
            textDecoration: "underline",
            transition: "transform 0.15s ease"
          }}
        >
          You don't have an Account? Register!
        </button>
      </div>
    </div>
  );
}